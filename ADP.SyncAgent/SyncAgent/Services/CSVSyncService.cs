using ADP.SyncAgent.Services;
using ADP.SyncAgent.Services.Interfaces;
using AutoMapper;
using LibGit2Sharp;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;

namespace ADP.SyncAgent.Services;

public class CSVSyncServiceFactory
{
    private readonly CosmosClient cosmosClient;
    private readonly IMapper mapper;
    private readonly IStorageService storageService;
    private readonly SyncAgentOptions options;

    public CSVSyncServiceFactory(
        CosmosClient cosmosClient,
        IMapper mapper,
        IStorageService storageService, 
        SyncAgentOptions options,
        IServiceProvider services)
    {
        this.cosmosClient = cosmosClient;
        this.mapper = mapper;
        this.storageService = storageService;
        this.options = options;
        Services = services;
    }

    public IServiceProvider Services { get; }

    public CSVSyncService<TCSV, TCosmos> Create<TCSV, TCosmos>()
        where TCSV : CacheableCSV
        where TCosmos : class
    {
        return new CSVSyncService<TCSV, TCosmos>(storageService, options, cosmosClient, mapper);
    }
}

public class CSVSyncService<TCSV, TCosmos> : IDisposable
    where TCSV : CacheableCSV
    where TCosmos : class
{
    private DirectoryInfo WorkingDirectory;

    private readonly CacheableCSVEngine<TCSV> CacheableCSVEngine = new();

    private readonly CSVSyncResult<TCSV> CSVSyncResult = new();

    // Create an instance of builder that exposes various extensions for adding resilience strategies
    ResiliencePipeline ResiliencePipeline;

    private readonly IStorageService storageService;
    private readonly SyncAgentOptions options;
    private readonly CosmosClient cosmosClient;
    private readonly IMapper mapper;

    public CSVSyncService(IStorageService storageService, SyncAgentOptions options, CosmosClient cosmosClient, IMapper mapper)
    {
        this.storageService = storageService;
        this.options = options;
        this.cosmosClient = cosmosClient;
        this.mapper = mapper;
        var tempFolder = Guid.NewGuid().ToString();

        WorkingDirectory = new DirectoryInfo(Path.Combine(options.CSVCompareWorkingDirectory , tempFolder));

        WorkingDirectory.Create();

        ResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions()) // Add retry using the default options
        .Build(); // Builds the resilience pipeline

        Repository.Init(WorkingDirectory.FullName);
    }

    private void StageAndCommit()
    {
        using (var repo = new Repository(WorkingDirectory.FullName))
        {
            Commands.Stage(repo, "*");

            Signature author = new Signature("Agent", "@Agent", DateTime.Now);
            Signature committer = author;

            Commit commit = repo.Commit("First Commit", author, committer);
        }
    }

    private (IEnumerable<string> Added, IEnumerable<string> Deleted) CompareVersionsAndGetDiff()
    {
        IEnumerable<string> added = new List<string>();
        IEnumerable<string> deleted = new List<string>();

        using (var repo = new Repository(WorkingDirectory.FullName))
        {
            foreach (var item in repo.RetrieveStatus())
            {
                if (item.State == FileStatus.ModifiedInWorkdir)
                {
                    var patch = repo.Diff.Compare<Patch>(new List<string>() { item.FilePath }).First();

                    added = patch.AddedLines.Select(x => x.Content);
                    deleted = patch.DeletedLines.Select(x => x.Content);
                }
            }
        }

        return (added, deleted);
    }

    private void CleanUp()
    {
        SetAttributesNormal(WorkingDirectory);

        WorkingDirectory.Delete(true);
    }

    private void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
            SetAttributesNormal(subDir);

        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }

    private async Task MutateFileRandomlyAsync()
    {
        var filePath = Path.Combine(WorkingDirectory.FullName, "file");
        var file2Path = Path.Combine(WorkingDirectory.FullName, "file2");

        var lineCount = File.ReadLines(filePath).Count();

        var random = new Random();

        var maxLineChange = lineCount > 1000 ? 700 : lineCount / 2;

        var numberOfLinesToChange = random.Next(1, maxLineChange);

        var linesToChange = new HashSet<int>();

        for (var i = 0; i < numberOfLinesToChange; i++)
        {
            var lineToChange = random.Next(1, lineCount);

            if (!linesToChange.Contains(lineToChange))
                linesToChange.Add(lineToChange);
        }

        using (FileStream sourceFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
        using (FileStream destFileStream = new FileStream(file2Path, FileMode.Create, FileAccess.Write, FileShare.None))
        using (StreamReader streamReader = new StreamReader(sourceFileStream))
        using (StreamWriter streamWriter = new StreamWriter(destFileStream))
        {
            var line = streamReader.ReadLine();
            var lineIndex = 0;

            while (line != null)
            {
                if (linesToChange.Contains(lineIndex))
                {
                    char[] charArray = line.ToCharArray();
                    Array.Reverse(charArray);
                    line = new string(charArray);
                }

                await streamWriter.WriteLineAsync(line);

                line = await streamReader.ReadLineAsync();

                lineIndex++;
            }
        }

        File.Delete(filePath);
        File.Move(file2Path, filePath);
    }

    public async Task ProcessFilesAsync(string csvFileName, string? sourceContainerOrShareName, string? sourceDirectory, 
        string? destinationContainerOrShareName, string? destinationDirectory)
    {
        CSVSyncResult.WorkingDirectory = WorkingDirectory;

        var stopWatch = new Stopwatch();

        stopWatch.Start();

        await storageService.LoadOriginalFileAsync(Path.Combine(destinationDirectory ?? "", csvFileName), Path.Combine(WorkingDirectory.FullName, "file.csv"),
            this.CacheableCSVEngine.Options.IgnoreFirstLines, destinationContainerOrShareName);
        StageAndCommit();

        stopWatch.Stop();

        CSVSyncResult.TimeToDownloadOriginalFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        await this.storageService.LoadNewVersionAsync(Path.Combine(sourceDirectory ?? "", csvFileName), Path.Combine(WorkingDirectory.FullName, "file.csv"),
            this.CacheableCSVEngine.Options.IgnoreFirstLines, sourceContainerOrShareName);

        stopWatch.Stop();

        CSVSyncResult.TimeToDownloadNewFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        var comparision = CompareVersionsAndGetDiff();

        // Find duplicates in Added and Deleted
        var duplicates = comparision.Added.Intersect(comparision.Deleted).ToList();

        // Remove duplicates from both lists
        comparision.Added = comparision.Added.Except(duplicates).ToList();
        comparision.Deleted = comparision.Deleted.Except(duplicates).ToList();

        if (comparision.Added.Count() == 0 && comparision.Deleted.Count() == 0)
            return;

        stopWatch.Stop();

        CSVSyncResult.TimeToCompareWithGit = stopWatch.Elapsed.TotalSeconds;

        string toInsertFilePath = Path.Combine(WorkingDirectory.FullName, "to-insert.csv");
        string toDeleteFilePath = Path.Combine(WorkingDirectory.FullName, "to-delete.csv");

        stopWatch.Restart();

        var engine = new CacheableCSVEngine<TCSV>();

        using (TextWriter textWriter = new StreamWriter(toInsertFilePath, true, System.Text.Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(engine.GetFileHeader());

            foreach (var line in comparision.Added)
                await textWriter.WriteAsync(line);
        }

        stopWatch.Stop();

        CSVSyncResult.TimeToWriteToInsertFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, System.Text.Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(engine.GetFileHeader());

            foreach (var line in comparision.Deleted)
                await textWriter.WriteAsync(line);
        }

        stopWatch.Stop();

        CSVSyncResult.TimeToWriteToDeleteFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        var toInsert = engine.ReadFileAsList(toInsertFilePath);

        stopWatch.Stop();

        CSVSyncResult.TimeToParseToInsertFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        var toDelete = engine.ReadFileAsList(toDeleteFilePath);

        stopWatch.Stop();

        CSVSyncResult.TimeToParseToDeleteFile = stopWatch.Elapsed.TotalSeconds;

        CSVSyncResult.ToInsert = toInsert;
        CSVSyncResult.ToDelete = toDelete;

        engine.Dispose();
    }

    public async Task ProcessCosmosAsync(
        string destinationRelativePath, 
        string? destinationContainerOrShareName,
        string databaseId, string containerId,
        Func<List<TCSV>, CosmosActionType, ValueTask<List<TCosmos>>>? mapping,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null)
    {
        var stopWatch = new Stopwatch();

        stopWatch.Start();

        var container = cosmosClient.GetContainer(databaseId, containerId);

        stopWatch.Stop();

        CSVSyncResult.TimeToInitializeCosmosClient = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        var addTasks = new List<Task>();
        var deleteTasks = new List<Task>();

        List<TCosmos> toInsert;
        List<TCosmos> toDelete;

        if (mapping is null)
        {
            toInsert = mapper.Map<List<TCosmos>>(CSVSyncResult.ToInsert);
            toDelete = mapper.Map<List<TCosmos>>(CSVSyncResult.ToDelete);
        }
        else
        {
            toInsert = await mapping(CSVSyncResult.ToInsert, CosmosActionType.Upsert);
            toDelete = await mapping(CSVSyncResult.ToDelete, CosmosActionType.Delete);
        }

        var toInsertCount = toInsert.Count();
        var toDeleteCount = toDelete.Count();

        CSVSyncResult.ToInsertCount = toInsertCount;
        CSVSyncResult.ToDeleteCount = toDeleteCount;

        if (toInsertCount == 0 && toDeleteCount == 0)
        {
            CSVSyncResult.Status = CSVSyncStatus.Skipped;

            return;
        }

        List<SyncCosmosAction<TCosmos>?> cosmosActions = new();
        
        if(cosmosAction is null)
        {
            cosmosActions.AddRange(toInsert.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Upsert)));
            cosmosActions.AddRange(toDelete.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Delete)));
        }
        else
        {
            foreach (var item in toInsert)
                cosmosActions.Add(await cosmosAction(new(item, CosmosActionType.Upsert)));

            foreach (var item in toDelete)
                cosmosActions.Add(await cosmosAction(new(item, CosmosActionType.Delete)));
        }

        var deletedCount = await DeleteFromCosmosAsync(
                databaseId,
                containerId,
                cosmosActions.Where(x => x.ActionType == CosmosActionType.Delete),
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                null
            );

        var insertedCount = await UpsertToCosmosAsync(
                databaseId,
                containerId,
                cosmosActions.Where(x => x.ActionType == CosmosActionType.Upsert),
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                null
            );

        stopWatch.Stop();

        CSVSyncResult.TimeToSyncToCosmos = stopWatch.Elapsed.TotalSeconds;

        CSVSyncResult.CosmosDBInsertedCount = insertedCount;
        CSVSyncResult.CosmosDBDeletedCount = deletedCount;

        if (insertedCount + deletedCount == toInsertCount + toDeleteCount)
        {
            try
            {
                await ResiliencePipeline.ExecuteAsync(async token =>
                {
                    await this.storageService.StoreNewVersionAsync(Path.Combine(WorkingDirectory.FullName, "file.csv"),
                        destinationRelativePath, destinationContainerOrShareName);

                    CSVSyncResult.Status = CSVSyncStatus.SuccessSync;
                });
            }
            catch
            {
                CSVSyncResult.Status = CSVSyncStatus.FailedBlobUpload;
            }
        }
        else
        {
            CSVSyncResult.Status = CSVSyncStatus.FailedCosmosDBSync;
        }

        CleanUp();
    }

    private async Task<int> UpsertToCosmosAsync(
            string databaseId, 
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
            long? batchSize
        )
    {
        var successCount = 0;
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            tasks = tasks.Concat([Task.Run(async () =>
            {
                try
                {
                    if (item?.Mapping is not null)
                        item.Item = await item.Mapping(item.Item);

                    if (item?.Item is not null)
                    {
                        var partitionKey = Utility.GetPartitionKey(item.Item, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);
                        await ResiliencePipeline.ExecuteAsync(async token =>
                        {
                            await container.UpsertItemAsync(item.Item, partitionKey, new ItemRequestOptions { EnableContentResponseOnWrite = false });
                            Interlocked.Increment(ref successCount);
                        });
                    }
                    else
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch
                {
                }
            })]);
        }

        await Task.WhenAll(tasks);

        return successCount;
    }

    private async Task<int> DeleteFromCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
            long? batchSize
        )
    {
        var successCount = 0;
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            tasks = tasks.Concat([Task.Run(async () =>
            {
                try
                {
                    if (item?.Mapping is not null)
                        item.Item = await item.Mapping(item.Item);

                    if (item?.Item is not null)
                    {
                        var partitionKey = Utility.GetPartitionKey(item.Item, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);
                        await ResiliencePipeline.ExecuteAsync(async token =>
                        {
                            try
                            {
                                var type = item.Item.GetType();
                                var id = (string?)type.GetProperty("id")?.GetValue(item.Item);
                                await container.DeleteItemAsync<TCosmos>(id, partitionKey);
                                Interlocked.Increment(ref successCount);
                            }
                            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                            {
                                Interlocked.Increment(ref successCount);
                            }
                        });
                    }
                    else
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch
                {
                }
            })]);
        }

        await Task.WhenAll(tasks);

        return successCount;
    }

    public CSVSyncResult<TCSV> GetSyncResult()
    {
        return CSVSyncResult;
    }

    public void Dispose()
    {
        CleanUp();
    }
}

public class CSVSyncService<TCSV> : CSVSyncService<TCSV, TCSV>, IDisposable
    where TCSV : CacheableCSV
{
    public CSVSyncService(IStorageService storageService, SyncAgentOptions options, CosmosClient cosmosClient, IMapper mapper) : 
        base(storageService, options, cosmosClient, mapper)
    {
    }
}
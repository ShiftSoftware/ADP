using ADP.SyncAgent.Services.Interfaces;
using AutoMapper;
using LibGit2Sharp;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.Models;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

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

    public CSVSyncService<TCSV, TCosmos> Create<TCSV, TCosmos>(ILogger logger)
        where TCSV : CacheableCSV
        where TCosmos : class
    {
        return new CSVSyncService<TCSV, TCosmos>(storageService, options, cosmosClient,logger, mapper);
    }
}

public class CSVSyncService<TCSV, TCosmos> : IDisposable
    where TCSV : CacheableCSV
    where TCosmos : class
{
    private CancellationTokenSource cancellationTokenSource = new();
    private int operationTimeoutInSeconds = 300;
    private DateTime operationStart = new();

    private DirectoryInfo WorkingDirectory;

    private readonly CacheableCSVEngine<TCSV> CacheableCSVEngine = new();

    private readonly CSVSyncResult<TCSV> CSVSyncResult = new();

    // Create an instance of builder that exposes various extensions for adding resilience strategies
    ResiliencePipeline ResiliencePipeline;

    private readonly IStorageService storageService;
    private readonly SyncAgentOptions options;
    private readonly CosmosClient cosmosClient;
    private readonly ILogger logger;
    private readonly IMapper mapper;

    public CSVSyncService(
        IStorageService storageService,
        SyncAgentOptions options, 
        CosmosClient cosmosClient, 
        ILogger logger,
        IMapper mapper)
    {
        this.storageService = storageService;
        this.options = options;
        this.cosmosClient = cosmosClient;
        this.logger = logger;
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
        IEnumerable<string> added = [];
        IEnumerable<string> deleted = [];

        using (var repo = new Repository(WorkingDirectory.FullName))
        {
            foreach (var item in repo.RetrieveStatus())
            {
                if (item.State == FileStatus.ModifiedInWorkdir)
                {
                    var patch = repo.Diff.Compare<Patch>([item.FilePath]).First();

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
            file.Attributes = FileAttributes.Normal;
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

    public async Task<string> StartSyncAsync(
        string csvFileName,
        string? sourceContainerOrShareName,
        string? sourceDirectory,
        string? destinationContainerOrShareName,
        string? destinationDirectory,
        string databaseId,
        string containerId,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null,
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSecond = 300)
    {
        operationStart = DateTime.UtcNow;
        this.operationTimeoutInSeconds = operationTimeoutInSecond;
        this.cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(operationTimeoutInSeconds));
        CSVSyncResult.WorkingDirectory = WorkingDirectory;

        using (logger.BeginScope("Syncing {csvFileName}", csvFileName))
        {
            logger.LogInformation("Processing Files");

            await ProcessFilesAsync(csvFileName, sourceContainerOrShareName, sourceDirectory, destinationContainerOrShareName, destinationDirectory);

            logger.LogInformation("Processing Cosmos DB");

            await ProcessCosmosAsync(
                destinationRelativePath: Path.Combine(destinationDirectory ?? "", csvFileName),
                destinationContainerOrShareName: destinationContainerOrShareName,
                databaseId: databaseId,
                containerId: containerId,
                mapping: mapping,
                partitionKeyLevel1Expression: partitionKeyLevel1Expression,
                partitionKeyLevel2Expression: partitionKeyLevel2Expression,
                partitionKeyLevel3Expression: partitionKeyLevel3Expression,
                cosmosAction: cosmosAction,
                batchSize: batchSize,
                retryCount: retryCount
            );

            var syncResult = GetSyncResult();

            logger.LogInformation(
                """
                
                -----------------------------------------------------------------------------------------
                Selected File:                          {file}
                Number of Added Lines:                  {ToInsert}
                Number of Deleted Lines:                {ToDelete}
                Original (Synced) File Download Time:   {TimeToDownloadOriginalFile}
                New File Download Time:                 {TimeToDownloadNewFile}
                Git Diff Process Time:                  {TimeToCompareWithGit}
                Write To Insert File Time:              {TimeToWriteToInsertFile}
                Write To Delete File Time:              {TimeToWriteToDeleteFile}
                Parse (To Insert File) Time:            {TimeToParseToInsertFile}
                Parse (To Delete File) Time:            {TimeToParseToDeleteFile}
                Initialize Cosmos Client Time:          {TimeToInitializeCosmosClient}
                Sync To Cosmos Time:                    {TimeToSyncToCosmos}
                Cosmos DB Inserted Count:               {CosmosDBInsertedCount}
                Cosmos DB Deleted Count:                {CosmosDBDeletedCount}
                Status:                                 {Status}
                Success:                                {Success}
                -----------------------------------------------------------------------------------------

                """
                ,
                csvFileName,
                syncResult.ToInsertCount,
                syncResult.ToDeleteCount,
                syncResult.TimeToDownloadOriginalFile,
                syncResult.TimeToDownloadNewFile,
                syncResult.TimeToCompareWithGit,
                syncResult.TimeToWriteToInsertFile,
                syncResult.TimeToWriteToDeleteFile,
                syncResult.TimeToParseToInsertFile,
                syncResult.TimeToParseToDeleteFile,
                syncResult.TimeToInitializeCosmosClient,
                syncResult.TimeToSyncToCosmos,
                syncResult.CosmosDBInsertedCount,
                syncResult.CosmosDBDeletedCount,
                syncResult.Status,
                syncResult.Success
            );

            return $"""
                
                -----------------------------------------------------------------------------------------
                Selected File:                          {csvFileName}
                Number of Added Lines:                  {syncResult.ToInsertCount}
                Number of Deleted Lines:                {syncResult.ToDeleteCount}
                Original (Synced) File Download Time:   {syncResult.TimeToDownloadOriginalFile}
                New File Download Time:                 {syncResult.TimeToDownloadNewFile}
                Git Diff Process Time:                  {syncResult.TimeToCompareWithGit}
                Write To Insert File Time:              {syncResult.TimeToWriteToInsertFile}
                Write To Delete File Time:              {syncResult.TimeToWriteToDeleteFile}
                Parse (To Insert File) Time:            {syncResult.TimeToParseToInsertFile}
                Parse (To Delete File) Time:            {syncResult.TimeToParseToDeleteFile}
                Initialize Cosmos Client Time:          {syncResult.TimeToInitializeCosmosClient}
                Sync To Cosmos Time:                    {syncResult.TimeToSyncToCosmos}
                Cosmos DB Inserted Count:               {syncResult.CosmosDBInsertedCount}
                Cosmos DB Deleted Count:                {syncResult.CosmosDBDeletedCount}
                Status:                                 {syncResult.Status}
                Success:                                {syncResult.Success}
                -----------------------------------------------------------------------------------------

                """;
        }
    }

    public async Task ProcessFilesAsync(string csvFileName, string? sourceContainerOrShareName, string? sourceDirectory, 
        string? destinationContainerOrShareName, string? destinationDirectory)
    {
        var stopWatch = new Stopwatch();

        stopWatch.Start();

        await storageService.LoadOriginalFileAsync(Path.Combine(destinationDirectory ?? "", csvFileName), Path.Combine(WorkingDirectory.FullName, "file.csv"),
            this.CacheableCSVEngine.Options.IgnoreFirstLines, destinationContainerOrShareName, GetCancellationToken());
        StageAndCommit();

        stopWatch.Stop();

        CSVSyncResult.TimeToDownloadOriginalFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        await this.storageService.LoadNewVersionAsync(Path.Combine(sourceDirectory ?? "", csvFileName), Path.Combine(WorkingDirectory.FullName, "file.csv"),
            this.CacheableCSVEngine.Options.IgnoreFirstLines, sourceContainerOrShareName, GetCancellationToken());

        stopWatch.Stop();

        CSVSyncResult.TimeToDownloadNewFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        var comparision = CompareVersionsAndGetDiff();

        // Find duplicates in Added and Deleted
        var duplicates = comparision.Added.Intersect(comparision.Deleted);

        // Remove duplicates from both lists
        comparision.Added = comparision.Added.Except(duplicates);
        comparision.Deleted = comparision.Deleted.Except(duplicates);

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
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), GetCancellationToken());

            foreach (var line in comparision.Added)
                await textWriter.WriteAsync(new StringBuilder(line), GetCancellationToken());
        }

        stopWatch.Stop();

        CSVSyncResult.TimeToWriteToInsertFile = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, System.Text.Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), GetCancellationToken());

            foreach (var line in comparision.Deleted)
                await textWriter.WriteAsync(new StringBuilder(line), GetCancellationToken());
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

    private CancellationToken GetCancellationToken()
    {
        var timeout = operationTimeoutInSeconds - (DateTime.UtcNow - operationStart).Seconds;
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeout)).Token;
    }

    private void CheckForCancellation()
    {
        if (cancellationTokenSource.Token.IsCancellationRequested)
        {
            CleanUp();
            throw new OperationCanceledException();
        }
    }

    public async Task ProcessCosmosAsync(
        string destinationRelativePath,
        string? destinationContainerOrShareName,
        string databaseId, string containerId,
        Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping,
        Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
        Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression = null,
        Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression = null,
        Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction = null,
        int? batchSize = null,
        int? retryCount = 0)
    {
        var stopWatch = new Stopwatch();

        stopWatch.Start();

        var container = cosmosClient.GetContainer(databaseId, containerId);

        stopWatch.Stop();

        CSVSyncResult.TimeToInitializeCosmosClient = stopWatch.Elapsed.TotalSeconds;

        stopWatch.Restart();

        CheckForCancellation();

        IEnumerable<TCosmos> toInsert;
        IEnumerable<TCosmos> toDelete;

        if (mapping is null)
        {
            toInsert = mapper.Map<IEnumerable<TCosmos>>(CSVSyncResult.ToInsert);
            toDelete = mapper.Map<IEnumerable<TCosmos>>(CSVSyncResult.ToDelete);
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

        IEnumerable<SyncCosmosAction<TCosmos>?> cosmosActions = [];
        
        if(cosmosAction is null)
        {
            cosmosActions = cosmosActions.Concat(toInsert.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Upsert, GetCancellationToken())));
            cosmosActions = cosmosActions.Concat(toDelete.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Delete, GetCancellationToken())));
        }
        else
        {
            foreach (var item in toInsert)
                cosmosActions = cosmosActions.Concat([await cosmosAction(new(item, CosmosActionType.Upsert, GetCancellationToken()))]);

            foreach (var item in toDelete)
                cosmosActions = cosmosActions.Concat([await cosmosAction(new(item, CosmosActionType.Delete, GetCancellationToken()))]);
        }

        var deletedCount = await DeleteBatchFromCosmosAsync(
                databaseId,
                containerId,
                cosmosActions.Where(x => x.ActionType == CosmosActionType.Delete),
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                batchSize,
                retryCount
            );

        var insertedCount = await UpsertBtachToCosmosAsync(
                databaseId,
                containerId,
                cosmosActions.Where(x => x.ActionType == CosmosActionType.Upsert),
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                batchSize,
                retryCount
            );

        stopWatch.Stop();

        CSVSyncResult.TimeToSyncToCosmos = stopWatch.Elapsed.TotalSeconds;

        CSVSyncResult.CosmosDBInsertedCount = insertedCount;
        CSVSyncResult.CosmosDBDeletedCount = deletedCount;

        if (insertedCount + deletedCount == toInsertCount + toDeleteCount)
        {
            try
            {
                await this.storageService.StoreNewVersionAsync(Path.Combine(WorkingDirectory.FullName, "file.csv"),
                    destinationRelativePath, destinationContainerOrShareName, this.CacheableCSVEngine.Options.IgnoreFirstLines,
                    GetCancellationToken());

                CSVSyncResult.Status = CSVSyncStatus.SuccessSync;
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

    private async Task<int> UpsertBtachToCosmosAsync(
            string databaseId, 
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
            int? batchSize,
            int? retryCount
        )
    {
        var successCount = 0;
        CheckForCancellation();

        var totalCount = items.Count();
        batchSize ??= totalCount;
        var totalSteps = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (double)batchSize);
        var currentStep = 0;
        int retry = 0;

        this.logger.LogInformation("Upserting started.");
        this.logger.LogInformation($"Total count {totalCount}");
        this.logger.LogInformation($"Total steps {totalSteps}");

        while (currentStep < totalSteps)
        {
            CheckForCancellation();

            this.logger.LogInformation($"Step {(currentStep + 1)} start proccessing.");

            var skip = currentStep * batchSize.GetValueOrDefault();
            var batchItems = items.Skip(skip).Take(batchSize.GetValueOrDefault()).ToList();

            try
            {
                var successCountForBatch = await UpsertToCosmosAsync(
                    databaseId,
                    containerId, 
                    batchItems, 
                    partitionKeyLevel1Expression,
                    partitionKeyLevel2Expression, 
                    partitionKeyLevel3Expression);

                successCount += successCountForBatch;

                this.logger.LogInformation($"Step {(currentStep + 1)} proccessed.");

                currentStep++;
                retry = 0;
            }
            catch (Exception)
            {
                retry++;

                if (retry > (retryCount ?? 0))
                    break;

                this.logger.LogWarning($"Step {(currentStep + 1)} proccess failed, we do retry {retry} time.");
            }
        }

        return successCount;
    }

    private async Task<int> UpsertToCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
        var successCount = 0;
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            CheckForCancellation();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, GetCancellationToken()));

                if (item?.Item is not null)
                {
                    var partitionKey = Utility.GetPartitionKey(item.Item, partitionKeyLevel1Expression, partitionKeyLevel2Expression, partitionKeyLevel3Expression);
                    await ResiliencePipeline.ExecuteAsync(async token =>
                    {
                        await container.UpsertItemAsync(item.Item, partitionKey,
                            new ItemRequestOptions { EnableContentResponseOnWrite = false }, GetCancellationToken());
                        Interlocked.Increment(ref successCount);
                    }, GetCancellationToken());
                }
                else
                {
                    Interlocked.Increment(ref successCount);
                }
            }, GetCancellationToken())]);
        }

        await Task.WhenAll(tasks);

        return successCount;
    }

    private async Task<int> DeleteBatchFromCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
            int? batchSize,
            int? retryCount
        )
    {
        var successCount = 0;
        CheckForCancellation();

        var totalCount = items.Count();
        batchSize ??= totalCount;
        var totalSteps = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (double)batchSize);
        var currentStep = 0;
        int retry = 0;

        this.logger.LogInformation("Deleting started.");
        this.logger.LogInformation($"Total count {totalCount}");
        this.logger.LogInformation($"Total steps {totalSteps}");

        while (currentStep < totalSteps)
        {
            CheckForCancellation();
            this.logger.LogInformation($"Step {(currentStep + 1)} start proccessing.");

            var skip = currentStep * batchSize.GetValueOrDefault();
            var batchItems = items.Skip(skip).Take(batchSize.GetValueOrDefault()).ToList();

            try
            {
                var successCountForBatch = await DeleteFromCosmosAsync(
                    databaseId,
                    containerId,
                    batchItems,
                    partitionKeyLevel1Expression,
                    partitionKeyLevel2Expression,
                    partitionKeyLevel3Expression);

                successCount += successCountForBatch;

                this.logger.LogInformation($"Step {(currentStep + 1)} proccessed.");

                currentStep++;
                retry = 0;
            }
            catch (Exception)
            {
                retry++;

                if (retry > (retryCount ?? 0))
                    break;

                this.logger.LogWarning($"Step {(currentStep + 1)} proccess failed, we do retry {retry} time.");
            }
        }

        return successCount;
    }

    private async Task<int> DeleteFromCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
        var successCount = 0;
        IEnumerable<Task> tasks = [];

        var container = cosmosClient.GetContainer(databaseId, containerId);

        foreach (var item in items)
        {
            CheckForCancellation();

            tasks = tasks.Concat([Task.Run(async () =>
            {
                if (item?.Mapping is not null)
                    item.Item = await item.Mapping(new(item.Item, GetCancellationToken()));

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
    public CSVSyncService(IStorageService storageService, SyncAgentOptions options, CosmosClient cosmosClient,ILogger logger, IMapper mapper) : 
        base(storageService, options, cosmosClient,logger, mapper)
    {
    }
}
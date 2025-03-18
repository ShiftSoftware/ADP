    using AutoMapper;
using LibGit2Sharp;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Utilities;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

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

    public CSVSyncService<TCSV, TCosmos> Create<TCSV, TCosmos>(ILogger logger, ISyncProgressIndicator? syncProgressIndicator)
        where TCSV : CacheableCSV
        where TCosmos : class
    {
        return new CSVSyncService<TCSV, TCosmos>(storageService, options, cosmosClient, logger, syncProgressIndicator, mapper);
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

    private readonly string toInsertFilePath = "";
    private readonly string toDeleteFilePath = "";

    // Create an instance of builder that exposes various extensions for adding resilience strategies
    ResiliencePipeline ResiliencePipeline;

    private readonly IStorageService storageService;
    private readonly SyncAgentOptions options;
    private readonly CosmosClient cosmosClient;
    private readonly ILogger logger;
    private readonly ISyncProgressIndicator? syncProgressIndicator;
    private readonly IMapper mapper;

    public CSVSyncService(
        IStorageService storageService,
        SyncAgentOptions options,
        CosmosClient cosmosClient,
        ILogger logger,
        ISyncProgressIndicator? syncProgressIndicator,
        IMapper mapper)
    {
        this.storageService = storageService;
        this.options = options;
        this.cosmosClient = cosmosClient;
        this.logger = logger;
        this.syncProgressIndicator = syncProgressIndicator;
        this.mapper = mapper;
        var tempFolder = Guid.NewGuid().ToString();

        WorkingDirectory = new DirectoryInfo(Path.Combine(options.CSVCompareWorkingDirectory, tempFolder));
        WorkingDirectory.Create();

        toInsertFilePath = Path.Combine(WorkingDirectory.FullName, "to-insert.csv");
        toDeleteFilePath = Path.Combine(WorkingDirectory.FullName, "to-delete.csv");

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

        GarbageCollection();
    }

    private void GarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
            SetAttributesNormal(subDir);

        foreach (var file in dir.GetFiles())
            file.Attributes = FileAttributes.Normal;
    }

    public async Task StartSyncAsync(
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
        int operationTimeoutInSecond = 300,
        string? syncId = null)
    {
        try
        {
            operationStart = DateTime.UtcNow;
            operationTimeoutInSeconds = operationTimeoutInSecond;
            cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(operationTimeoutInSeconds));

            using (logger.BeginScope("Syncing {csvFileName}", csvFileName))
            {
                logger.LogInformation("Processing Files");

                var compareTask = new SyncTask { SyncID = syncId, TaskDescription = "Comparing the new File with the Existing Data", TotalStep = 6, CurrentStep = -1 };

                this.UpdateProgress(compareTask);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(compareTask, "Processing Files");

                var fileProccessSuccess = await ProcessFilesAsync(
                    csvFileName,
                    sourceContainerOrShareName,
                    sourceDirectory,
                    destinationContainerOrShareName,
                    destinationDirectory,
                    compareTask);

                logger.LogInformation("Processing Cosmos DB");

                if (fileProccessSuccess)
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
                        retryCount: retryCount,
                        syncId: syncId
                    );

                CleanUp();
            }

            if (syncProgressIndicator is not null)
                await syncProgressIndicator.CompleteAllRunningTasks();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);

            if (syncProgressIndicator is not null)
            {
                await syncProgressIndicator.LogErrorAsync(
                    new SyncTask { TotalStep = 0, CurrentStep = 0, Failed = true, SyncID = syncId, TaskDescription = "Import Failed." },
                    $"{ex.Message}\r\n\r\n\r\n\r\n{ex.StackTrace}");


                await syncProgressIndicator.FailAllRunningTasks();
            }

            CleanUp();

            throw;
        }
    }

    private void UpdateProgress(SyncTask syncTask, bool incrementStep = true)
    {
        if (incrementStep)
            syncTask.CurrentStep++;

        syncTask.Progress = syncTask.TotalStep == 0 ? 0 : (double)(syncTask.CurrentStep) / syncTask.TotalStep;
        syncTask.Elapsed = DateTime.UtcNow - operationStart;
        syncTask.RemainingTimeToShutdown = operationStart.AddSeconds(operationTimeoutInSeconds) - DateTime.UtcNow;
    }

    public async Task<bool> ProcessFilesAsync(string csvFileName, string? sourceContainerOrShareName, string? sourceDirectory, string? destinationContainerOrShareName, string? destinationDirectory, SyncTask syncTask)
    {
        using CacheableCSVAsyncEngine<TCSV> engine = new();

        logger.LogInformation("Loding the existing file.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Loding the existing file.");

        await storageService
            .LoadOriginalFileAsync(
                Path.Combine(destinationDirectory ?? "", csvFileName),
                Path.Combine(WorkingDirectory.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines, destinationContainerOrShareName,
                GetCancellationToken());
        
        logger.LogInformation("Initializing a git repo.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Initializing a git repo.");

        StageAndCommit();

        logger.LogInformation("Loding the new file.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Loding the new file.");

        await storageService
            .LoadNewVersionAsync(
            Path.Combine(sourceDirectory ?? "", csvFileName), 
            Path.Combine(WorkingDirectory.FullName, "file.csv"),
            engine.Options.IgnoreFirstLines, 
            sourceContainerOrShareName, GetCancellationToken());

        logger.LogInformation("Execute 'git diff' on the files.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Execute 'git diff' on the files.");

        var comparision = CompareVersionsAndGetDiff();

        // Find duplicates in Added and Deleted
        var duplicates = comparision.Added.Intersect(comparision.Deleted);

        // Remove duplicates from both lists
        comparision.Added = comparision.Added.Except(duplicates);
        comparision.Deleted = comparision.Deleted.Except(duplicates);

        if (comparision.Added.Count() == 0 && comparision.Deleted.Count() == 0)
        {
            logger.LogInformation("Nothing to do. Skipping");

            syncTask.Completed = true;
            syncTask.CurrentStep = syncTask.TotalStep - 1;

            UpdateProgress(syncTask);

            if (syncProgressIndicator is not null)
                await syncProgressIndicator.LogInformationAsync(syncTask, "Nothing to do. Skipping.");

            return false;
        }

        logger.LogInformation("Generating a file for added records.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Generating a file for added records.");
        
        using (TextWriter textWriter = new StreamWriter(toInsertFilePath, true, Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), GetCancellationToken());

            foreach (var line in comparision.Added)
                await textWriter.WriteAsync(new StringBuilder(line), GetCancellationToken());
        }

        logger.LogInformation("Generating a file for deleted records.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Generating a file for deleted records.");

        using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), GetCancellationToken());

            foreach (var line in comparision.Deleted)
                await textWriter.WriteAsync(new StringBuilder(line), GetCancellationToken());
        }

        //Cleanup some memory usage
        comparision.Added = null;
        comparision.Deleted = null;
        GarbageCollection();

        syncTask.Completed = true;

        syncTask.Progress = 1;

        logger.LogInformation("File Processing Task has finished successfully.");
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "File Processing Task has finished successfully.");

        return true;
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
        int? retryCount = 0,
        string? syncId = null)
    {
        var deleteSucceeded = await SyncBtachToCosmosAsync(
                toDeleteFilePath,
                CosmosActionType.Delete,
                databaseId,
                containerId,
                mapping,
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                cosmosAction,
                batchSize,
                retryCount,
                syncId
            );
                
        var upsertSucceeded = await SyncBtachToCosmosAsync(
                toInsertFilePath,
                CosmosActionType.Upsert,
                databaseId,
                containerId,
                mapping,
                partitionKeyLevel1Expression,
                partitionKeyLevel2Expression,
                partitionKeyLevel3Expression,
                cosmosAction,
                batchSize,
                retryCount,
                syncId
            );

        if (upsertSucceeded && deleteSucceeded)
        {
            try
            {
                using CacheableCSVAsyncEngine<TCSV> engine = new();

                await storageService.StoreNewVersionAsync(Path.Combine(WorkingDirectory.FullName, "file.csv"),
                    destinationRelativePath, destinationContainerOrShareName, engine.Options.IgnoreFirstLines,
                    GetCancellationToken());
            }
            catch
            {
               logger.LogError("Failed to store the new version of the file.");
                throw;
            }
        }
        else
        {
            logger.LogError("Failed to sync to cosmos.");
        }
    }

    private async Task<bool> SyncBtachToCosmosAsync(
            string csvFilePath,
            CosmosActionType actionType,
            string databaseId,
            string containerId,
            Func<IEnumerable<TCSV>, CosmosActionType, ValueTask<IEnumerable<TCosmos>>>? mapping,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression,
            Func<SyncCosmosAction<TCosmos>, ValueTask<SyncCosmosAction<TCosmos>?>>? cosmosAction,
            int? batchSize,
            int? retryCount,
            string? syncId
        )
    {
        CheckForCancellation();

        var cosmosTask = new SyncTask
        {
            SyncID = syncId,
            TaskDescription = actionType == CosmosActionType.Upsert ? "Upserting to Cosmos DB" : "Deleting from Cosmos DB",
            TotalStep = 0,
            CurrentStep = 0
        };

        logger.LogInformation("Calculating Cosmos DB Task Steps: Operation Type is: {0}", actionType == CosmosActionType.Upsert ? "Upsert" : "Delete");
        this.UpdateProgress(cosmosTask, false);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(cosmosTask, string.Format("Calculating Cosmos DB Task Steps: Operation Type is: {0}\r\n", actionType == CosmosActionType.Upsert ? "Upsert" : "Delete"));

        using CacheableCSVAsyncEngine<TCSV> engine = new();
        engine.BeginReadFile(csvFilePath);

        var totalCount = await CalculateCSVRecordCountAsync(csvFilePath, engine.Options.IgnoreFirstLines);
        batchSize ??= totalCount;
        var totalSteps = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)batchSize);
        var currentStep = 0;
        int retry = 0;

        cosmosTask.TotalStep = totalSteps;

        logger.LogInformation("Total Item Count is: {0:#,0}", totalCount);
        logger.LogInformation("Batch Size is: {0:#,0}", batchSize);
        logger.LogInformation("Step Count is: {0:#,0}", totalSteps);

        this.UpdateProgress(cosmosTask, false);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(cosmosTask, string.Format("Total Item Count is: {0:#,0}, Batch Size is: {1:#,0}, Step Count is: {2:#,0}\r\n", totalCount, batchSize, totalSteps));

        IEnumerable<SyncCosmosAction<TCosmos>?>? items = null;

        while (true)
        {
            CheckForCancellation();

            if (currentStep < totalSteps)
            {
                logger.LogInformation(
                    "{0:P2}: Processing Step {1} of {2}. Elapsed: {3:c}. Remaining Allowed Time: {4:c}",
                    totalSteps == 0 ? 0 : (double)(currentStep + 1) / totalSteps,
                    currentStep + 1,
                    totalSteps,
                    DateTime.UtcNow - operationStart,
                    operationStart.AddSeconds(operationTimeoutInSeconds) - DateTime.UtcNow
                );

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask, 
                        string.Format("Processing Step {0} of {1}.\r\n\r\n", cosmosTask.CurrentStep + 1, cosmosTask.TotalStep));
            }

            if (items is null)
            {
                logger.LogInformation("Reading the current batch from the CSV file.");

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        "Reading the current batch from the CSV file.\r\n\r\n");

                var records = engine.ReadNexts(batchSize.GetValueOrDefault());

                logger.LogInformation("Successfully loaded the current batch to memory.");

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        "Successfully loaded the current batch to memory.\r\n\r\n");

                if (records?.Any() != true)
                {
                    logger.LogInformation("No items were loaded.");

                    this.UpdateProgress(cosmosTask, false);
                    if (syncProgressIndicator is not null)
                        await syncProgressIndicator.LogInformationAsync(
                            cosmosTask,
                            "No items were loaded.\r\n\r\n");

                    break;
                }

                IEnumerable<TCosmos> mappedRecords;

                logger.LogInformation("Start mapping CSV records to Cosmos Data Model.");

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        "Start mapping CSV records to Cosmos Data Model.\r\n\r\n");

                if (mapping is null)
                    mappedRecords = mapper.Map<IEnumerable<TCosmos>>(records);
                else
                    mappedRecords = await mapping(records, actionType);

                if (cosmosAction is null)
                    items = mappedRecords.Select(x => new SyncCosmosAction<TCosmos>(x, CosmosActionType.Upsert, GetCancellationToken()));
                else
                    foreach (var item in mappedRecords)
                        items = (items ?? []).Concat([await cosmosAction(new(item, CosmosActionType.Upsert, GetCancellationToken()))]);

                logger.LogInformation("Completed mapping CSV records to Cosmos Data Model.");

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        "Completed mapping CSV records to Cosmos Data Model.\r\n\r\n");
            }

            try
            {
                logger.LogInformation("Starting Cosmos Operation for the current Batch.");

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        "Starting Cosmos Operation for the current Batch.\r\n\r\n");

                //Some times the CSV comparer marks an item as deleted. But the SyncCosmosAction has the ability to change that to an upsert (This is done outside the sync agent by the programmer.) 

                await DeleteFromCosmosAsync(
                    databaseId,
                    containerId,
                    items?.Where(x => x.ActionType == CosmosActionType.Delete) ?? [],
                    partitionKeyLevel1Expression,
                    partitionKeyLevel2Expression,
                    partitionKeyLevel3Expression);

                await UpsertToCosmosAsync(
                    databaseId,
                    containerId,
                    items?.Where(x => x.ActionType == CosmosActionType.Upsert) ?? [],
                    partitionKeyLevel1Expression,
                    partitionKeyLevel2Expression,
                    partitionKeyLevel3Expression);

                logger.LogInformation("Completed Cosmos Operation for the current Batch: Step {0} of {1}.", currentStep + 1, totalSteps);

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        cosmosTask,
                        string.Format("Completed Cosmos Operation for the current Batch: Step {0} of {1}.\r\n\r\n", cosmosTask.CurrentStep + 1, cosmosTask.TotalStep));

                currentStep++;
                cosmosTask.CurrentStep++;
                retry = 0;
                items = null;
            }
            catch (Exception ex)
            {
                retry++;

                if (retry > (retryCount ?? 0))
                {
                    items = null;
                    logger.LogError(ex.Message);
                    throw;
                }

                logger.LogWarning("Failed Processing the Current Cosmos Operation (Step {0} of {1}). Starting Retry {2} of {3}.", currentStep + 1, totalSteps, retry, retryCount);

                this.UpdateProgress(cosmosTask, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogWarningAsync(
                        cosmosTask, 
                        string.Format("Failed Processing the Current Cosmos Operation (Step {0} of {1}). Starting Retry {2} of {3}.\r\n\r\n", cosmosTask.CurrentStep + 1, cosmosTask.TotalStep, retry, retryCount));
            }
        }

        cosmosTask.Completed = true;

        logger.LogInformation("Task Successfully Finished");
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(cosmosTask, "Task Successfully Finished.\r\n\r\n");

        return true;
    }

    private async Task<int> CalculateCSVRecordCountAsync(string filePath, int numberOfIgnoredLines = 0)
    {
        using var reader = new StreamReader(filePath);
        int count = 0;
        while (await reader.ReadLineAsync() != null)
            count++;

        return Math.Max(0, count - numberOfIgnoredLines);
    }

    private async Task UpsertToCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
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
                    }, GetCancellationToken());
                }
            }, GetCancellationToken())]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;
    }

    private async Task DeleteFromCosmosAsync(
            string databaseId,
            string containerId,
            IEnumerable<SyncCosmosAction<TCosmos>?> items,
            Expression<Func<TCosmos, object>> partitionKeyLevel1Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel2Expression,
            Expression<Func<TCosmos, object>>? partitionKeyLevel3Expression
        )
    {
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
                        }
                        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                        }
                    });
                }
            })]);
        }

        await Task.WhenAll(tasks);

        tasks = null;
        items = null;
    }

    public void Dispose()
    {
        CleanUp();
    }
}

public class CSVSyncService<TCSV> : CSVSyncService<TCSV, TCSV>, IDisposable
    where TCSV : CacheableCSV
{
    public CSVSyncService(IStorageService storageService, SyncAgentOptions options, CosmosClient cosmosClient, ILogger logger, ISyncProgressIndicator syncProgressIndicator, IMapper mapper) :
        base(storageService, options, cosmosClient, logger, syncProgressIndicator, mapper)
    {
    }
}
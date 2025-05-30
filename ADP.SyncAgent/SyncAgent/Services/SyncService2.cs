﻿using AutoMapper;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using Polly;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class SyncService2<TCSV, TData> : IDisposable
    where TCSV : CacheableCSV
    where TData : class
{
    private CSVConfigurations csvConfigurations;
    public SyncConfigurations<TCSV, TData> SyncConfigurations { get; private set; }
    private Func<DataProcessConfigurations<TData>, ValueTask<bool>> dataProcess;
    private Func<ValueTask> FailFunction;
    private Func<ValueTask> SuccessFunction;

    private readonly IServiceProvider services;
    private readonly IStorageService storageService;
    private readonly SyncAgentOptions options;
    private readonly IMapper mapper;
    private readonly ILogger logger;
    private readonly ISyncProgressIndicator? syncProgressIndicator;
    private DirectoryInfo workingDirectory;
    protected ResiliencePipeline ResiliencePipeline { get; private set; }

    private CancellationTokenSource cancellationTokenSource = new();
    private int operationTimeoutInSeconds = 300;
    private DateTime operationStart = new();

    private string toInsertFilePath = "";
    private string toDeleteFilePath = "";

    public SyncService2(
            IServiceProvider services,
            IStorageService storageService,
            SyncAgentOptions options,
            IMapper mapper,
            ILogger logger,
            ISyncProgressIndicator? syncProgressIndicator
        )
    {
        this.services = services;
        this.storageService = storageService;
        this.options = options;
        this.mapper = mapper;
        this.logger = logger;
        this.syncProgressIndicator = syncProgressIndicator;
        ResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Upsert retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public SyncService2<TCSV, TData> ConfigureCSVFile(
        string csvFileName,
        string? sourceContainerOrShareName,
        string? sourceDirectory,
        string? destinationContainerOrShareName,
        string? destinationDirectory)
    {
        csvConfigurations = new CSVConfigurations
        {
            CSVFileName = csvFileName,
            SourceContainerOrShareName = sourceContainerOrShareName,
            SourceDirectory = sourceDirectory,
            DestinationContainerOrShareName = destinationContainerOrShareName,
            DestinationDirectory = destinationDirectory
        };

        return this;
    }

    public SyncService2<TCSV, TData> ConfigureSync(
        int? batchSize = null,
        int? retryCount = 0,
        int operationTimeoutInSecond = 300,
        Func<IEnumerable<TCSV>, SyncActionType, ValueTask<IEnumerable<TData>>>? mapping = null,
        string? syncId = null)
    {
        SyncConfigurations = new SyncConfigurations<TCSV, TData>
        {
            BatchSize = batchSize,
            RetryCount = retryCount,
            OperationTimeoutInSeconds = operationTimeoutInSecond,
            Mapping = mapping,
            SyncId = syncId
        };

        return this;
    }

    public SyncService2<TCSV, TData> ConfigureDataProcess(
        Func<DataProcessConfigurations<TData>, ValueTask<bool>> dataProcess)
    {
        this.dataProcess = dataProcess;
        return this;
    }

    public SyncService2<TCSV, TData> ConfigureSuccess(Func<ValueTask> success)
    {
        SuccessFunction = success;
        return this;
    }

    public SyncService2<TCSV, TData> ConfigureFail(Func<ValueTask> fail)
    {
        FailFunction = fail;
        return this;
    }

    private void StageAndCommit()
    {
        using (var repo = new Repository(this.workingDirectory.FullName))
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

        using (var repo = new Repository(this.workingDirectory.FullName))
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

    public void SetupWorkingDirectory()
    {
        var tempFolder = Guid.NewGuid().ToString();
        workingDirectory = new DirectoryInfo(Path.Combine(options.CSVCompareWorkingDirectory, tempFolder));
        workingDirectory.Create();

        toInsertFilePath = Path.Combine(workingDirectory.FullName, "to-insert.csv");
        toDeleteFilePath = Path.Combine(workingDirectory.FullName, "to-delete.csv");

        Repository.Init(workingDirectory.FullName);
    }

    private void CleanUp()
    {
        SetAttributesNormal();

        this.workingDirectory.Delete(true);

        GarbageCollection();
    }

    private void SetAttributesNormal()
    {
        foreach (var subDir in this.workingDirectory.GetDirectories())
            SetAttributesNormal(subDir);

        foreach (var file in workingDirectory.GetFiles())
            file.Attributes = FileAttributes.Normal;
    }

    private void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
            SetAttributesNormal(subDir);

        foreach (var file in dir.GetFiles())
            file.Attributes = FileAttributes.Normal;
    }

    private void GarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private void CheckForCancellation()
    {
        if (cancellationTokenSource.Token.IsCancellationRequested)
        {
            CleanUp();
            throw new OperationCanceledException();
        }
    }

    private void UpdateProgress(SyncTaskStatus syncTask, bool incrementStep = true)
    {
        if (incrementStep)
            syncTask.CurrentStep++;

        syncTask.Progress = syncTask.TotalStep == 0 ? 0 : (double)(syncTask.CurrentStep) / syncTask.TotalStep;
        syncTask.Elapsed = DateTime.UtcNow - operationStart;
        syncTask.RemainingTimeToShutdown = operationStart.AddSeconds(operationTimeoutInSeconds) - DateTime.UtcNow;
    }

    private int CalculateCSVRecordCountAsync(string filePath)
    {
        using CacheableCSVAsyncEngine<TCSV> engine = new();
        engine.BeginReadFile(filePath);
        return engine.Count();
    }

    private async Task<bool> ProcessFilesAsync(SyncTaskStatus syncTask)
    {
        using CacheableCSVAsyncEngine<TCSV> engine = new();

        logger.LogInformation("Loding the existing file.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Loding the existing file.");

        await storageService
            .LoadOriginalFileAsync(
                Path.Combine(this.csvConfigurations.DestinationDirectory ?? "", this.csvConfigurations.CSVFileName!),
                Path.Combine(this.workingDirectory.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines, this.csvConfigurations.DestinationContainerOrShareName,
                this.cancellationTokenSource.Token);

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
            Path.Combine(this.csvConfigurations.SourceDirectory ?? "", this.csvConfigurations.CSVFileName!),
            Path.Combine(this.workingDirectory.FullName, "file.csv"),
            engine.Options.IgnoreFirstLines,
            this.csvConfigurations.SourceContainerOrShareName,
            this.cancellationTokenSource.Token);

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
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), this.cancellationTokenSource.Token);

            foreach (var line in comparision.Added)
                await textWriter.WriteAsync(new StringBuilder(line), this.cancellationTokenSource.Token);
        }

        logger.LogInformation("Generating a file for deleted records.");
        UpdateProgress(syncTask);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(syncTask, "Generating a file for deleted records.");

        using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, Encoding.UTF8))
        {
            for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), this.cancellationTokenSource.Token);

            foreach (var line in comparision.Deleted)
                await textWriter.WriteAsync(new StringBuilder(line), this.cancellationTokenSource.Token);
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

    private async Task ProcessDataAsync()
    {
        var deleteSucceeded = await ProcessBatchDataAsync(toDeleteFilePath, SyncActionType.Delete);

        var upsertSucceeded = await ProcessBatchDataAsync(toInsertFilePath, SyncActionType.Upsert);

        if (upsertSucceeded && deleteSucceeded)
        {
            try
            {
                using CacheableCSVAsyncEngine<TCSV> engine = new();

                await storageService.StoreNewVersionAsync(Path.Combine(workingDirectory.FullName, "file.csv"),
                    this.csvConfigurations.GetDestinationRelativePath(), this.csvConfigurations.DestinationContainerOrShareName, engine.Options.IgnoreFirstLines,
                    this.cancellationTokenSource.Token);

                if (SuccessFunction is not null)
                    await SuccessFunction();
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

    private async Task<bool> ProcessBatchDataAsync(string csvFilePath, SyncActionType actionType)
    {
        CheckForCancellation();

        var taskStatus = new SyncTaskStatus
        {
            SyncID = this.SyncConfigurations.SyncId,
            TaskDescription = actionType == SyncActionType.Upsert ? "Adding data" : "Deleting data",
            TotalStep = 0,
            CurrentStep = 0
        };

        logger.LogInformation("Calculating Cosmos DB Task Steps: Operation Type is: {0}", actionType == SyncActionType.Upsert ? "Add" : "Delete");
        this.UpdateProgress(taskStatus, false);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(taskStatus, string.Format("Calculating Cosmos DB Task Steps: Operation Type is: {0}\r\n", actionType == SyncActionType.Upsert ? "Add" : "Delete"));

        using CacheableCSVAsyncEngine<TCSV> engine = new();
        engine.BeginReadFile(csvFilePath);

        var totalCount = CalculateCSVRecordCountAsync(csvFilePath);
        var batchSize = this.SyncConfigurations.BatchSize ?? totalCount;
        var totalSteps = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)batchSize);
        var currentStep = 0;
        var retryCount = this.SyncConfigurations.RetryCount ?? 0;
        int retry = 0;

        taskStatus.TotalStep = totalSteps;

        logger.LogInformation("Total Item Count is: {0:#,0}", totalCount);
        logger.LogInformation("Batch Size is: {0:#,0}", batchSize);
        logger.LogInformation("Step Count is: {0:#,0}", totalSteps);

        this.UpdateProgress(taskStatus, false);
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(taskStatus, string.Format("Total Item Count is: {0:#,0}, Batch Size is: {1:#,0}, Step Count is: {2:#,0}\r\n", totalCount, batchSize, totalSteps));

        IEnumerable<TData?>? items = null;

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

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        string.Format("Processing Step {0} of {1}.\r\n\r\n", taskStatus.CurrentStep + 1, taskStatus.TotalStep));
            }

            if (items is null)
            {
                logger.LogInformation("Reading the current batch from the CSV file.");

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        "Reading the current batch from the CSV file.\r\n\r\n");

                var records = engine.ReadNexts(batchSize);

                logger.LogInformation("Successfully loaded the current batch to memory.");

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        "Successfully loaded the current batch to memory.\r\n\r\n");

                if (records?.Any() != true)
                {
                    logger.LogInformation("No items were loaded.");

                    this.UpdateProgress(taskStatus, false);
                    if (syncProgressIndicator is not null)
                        await syncProgressIndicator.LogInformationAsync(
                            taskStatus,
                            "No items were loaded.\r\n\r\n");

                    break;
                }

                logger.LogInformation("Start mapping CSV records to Data Model.");

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        "Start mapping CSV records to Cosmos Data Model.\r\n\r\n");

                if (this.SyncConfigurations.Mapping is null)
                    items = mapper.Map<IEnumerable<TData>>(records);
                else
                    items = await this.SyncConfigurations.Mapping(records, actionType);

                logger.LogInformation("Completed mapping CSV records to Data Model.");

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        "Completed mapping CSV records to Data Model.\r\n\r\n");
            }

            try
            {
                logger.LogInformation("Starting data proccessing for the current Batch.");

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        "Starting data proccessing for the current Batch.\r\n\r\n");

                var result = await this.dataProcess(new(currentStep, totalSteps, totalCount, actionType, this.cancellationTokenSource.Token, items!, taskStatus));

                if (!result)
                    throw new Exception("Failed to process the data.");

                logger.LogInformation("Completed data proccessing for the current Batch: Step {0} of {1}.", currentStep + 1, totalSteps);

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(
                        taskStatus,
                        string.Format("Completed data proccessing for the current Batch: Step {0} of {1}.\r\n\r\n", taskStatus.CurrentStep + 1, taskStatus.TotalStep));

                currentStep++;
                taskStatus.CurrentStep++;
                retry = 0;
                items = null;
            }
            catch (Exception ex)
            {
                retry++;

                if (retry > (retryCount))
                {
                    items = null;
                    logger.LogError(ex.Message);
                    throw;
                }

                logger.LogWarning("Failed Processing the Current data proccessing (Step {0} of {1}). Starting Retry {2} of {3}.", currentStep + 1, totalSteps, retry, retryCount);

                this.UpdateProgress(taskStatus, false);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogWarningAsync(
                        taskStatus,
                        string.Format("Failed Processing the Current data proccessing (Step {0} of {1}). Starting Retry {2} of {3}.\r\n\r\n", taskStatus.CurrentStep + 1, taskStatus.TotalStep, retry, retryCount));
            }
        }

        taskStatus.Completed = true;

        logger.LogInformation("Task Successfully Finished");
        if (syncProgressIndicator is not null)
            await syncProgressIndicator.LogInformationAsync(taskStatus, "Task Successfully Finished.\r\n\r\n");

        return true;
    }

    public async Task RunAsync()
    {
        try
        {
            SetupWorkingDirectory();

            operationStart = DateTime.UtcNow;
            cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(this.SyncConfigurations.OperationTimeoutInSeconds));

            using (logger.BeginScope("Syncing {csvFileName}", this.csvConfigurations.CSVFileName))
            {
                logger.LogInformation("Processing Files");

                var taskStatus = new SyncTaskStatus
                {
                    SyncID = this.SyncConfigurations.SyncId,
                    TaskDescription = "Comparing the new File with the Existing Data",
                    TotalStep = 6,
                    CurrentStep = -1,
                    OperationStart = operationStart,
                    OperationTimeoutInSeconds = this.SyncConfigurations.OperationTimeoutInSeconds
                };

                this.UpdateProgress(taskStatus);
                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.LogInformationAsync(taskStatus, "Processing Files");

                var fileProccessSuccess = await ProcessFilesAsync(taskStatus);

                logger.LogInformation("Start storing data");

                if (fileProccessSuccess)
                    await ProcessDataAsync();

                CleanUp();

                if (syncProgressIndicator is not null)
                    await syncProgressIndicator.CompleteAllRunningTasks();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);

            if (syncProgressIndicator is not null)
            {
                await syncProgressIndicator.LogErrorAsync(
                    new SyncTaskStatus { TotalStep = 0, CurrentStep = 0, Failed = true, SyncID = this.SyncConfigurations.SyncId, TaskDescription = "Import Failed." },
                    $"{ex.Message}\r\n\r\n\r\n\r\n{ex.StackTrace}");


                await syncProgressIndicator.FailAllRunningTasks();
            }

            CleanUp();

            if (FailFunction is not null)
                await FailFunction();

            throw;
        }
    }

    public void Dispose()
    {
        CleanUp();
    }
}

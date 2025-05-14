using LibGit2Sharp;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class CSVSyncDataSource<TSource, TDestination> : ISyncDataAdapter<TSource, TDestination, CSVSyncDataSource<TSource, TDestination>>
    where TSource : CacheableCSV, new()
    where TDestination : class, new()
{
    private readonly CSVSyncDataSourceOptions options;
    private readonly IStorageService storageService;

    private CSVConfigurations? csvConfigurations;
    private DirectoryInfo? workingDirectory;
    private ResiliencePipeline resiliencePipeline;
    private string? toInsertFilePath;
    private string? toDeleteFilePath;

    public ISyncService<TSource, TDestination> SyncService { get; private set; } = default!;

    public CSVSyncDataSource(CSVSyncDataSourceOptions options, IStorageService storageService)
    {
        this.options = options;
        this.storageService = storageService;
        this.resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Upsert retry using the default options
            .Build(); // Builds the resilience pipeline
    }

    public CSVSyncDataSource<TSource, TDestination> SetSyncService(ISyncService<TSource, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    public CSVSyncDataSource<TSource, TDestination> Configure(string csvFileName,
        string? sourceContainerOrShareName,
        string? sourceDirectory,
        string? destinationContainerOrShareName,
        string? destinationDirectory,
        bool skipReorderedLines = false)
    {
        this.csvConfigurations = new CSVConfigurations
        {
            CSVFileName = csvFileName,
            SourceContainerOrShareName = sourceContainerOrShareName,
            SourceDirectory = sourceDirectory,
            DestinationContainerOrShareName = destinationContainerOrShareName,
            DestinationDirectory = destinationDirectory,
            SkipReorderedLines = skipReorderedLines
        };

        this.SyncService
            .SetupPreparing(async (x) =>
            {
                var previousPreparing = this.SyncService.Preparing;

                if (previousPreparing is not null)
                    return await previousPreparing(x) && await this.Preparing(x);
                else
                    return await this.Preparing(x);
            })
            .SetupSourceTotalItemCount(SourceTotalItemCount)
            .SetupGetSourceBatchItems(GetSourceBatchItems)
            .SetupSucceeded(() =>
            {
                var previousSucceeded = this.SyncService.Succeeded;
                if (previousSucceeded is not null)
                    return previousSucceeded();

                return Succeeded();
            })
            .SetupFinished(() =>
            {
                var previousFinished = this.SyncService.Finished;
                if (previousFinished is not null)
                    return previousFinished();

                return Finished();
            });

        return this;
    }

    public async ValueTask<bool> Preparing(SyncFunctionInput input)
    {
        try
        {
            SetupWorkingDirectory();

            using CacheableCSVAsyncEngine<TSource> engine = new();

            // Load the last CSV file that was synced successfully
            await storageService.LoadOriginalFileAsync(
                Path.Combine(this.csvConfigurations!.DestinationDirectory ?? "", this.csvConfigurations.CSVFileName!),
                Path.Combine(this.workingDirectory!.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines, this.csvConfigurations.DestinationContainerOrShareName,
                this.SyncService.GetCancellationToken());

            StageAndCommit();

            // Load the new CSV file from the source
            await storageService.LoadNewVersionAsync(
                Path.Combine(this.csvConfigurations.SourceDirectory ?? "", this.csvConfigurations.CSVFileName!),
                Path.Combine(this.workingDirectory.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines,
                this.csvConfigurations.SourceContainerOrShareName,
                this.SyncService.GetCancellationToken());

            // Exute git diff to get the added and deleted lines
            var comparision = CompareVersionsAndGetDiff();

            // Find and skip reordered lines if the option is set
            if (this.csvConfigurations.SkipReorderedLines) { 
                // Find duplicates in Added and Deleted
                var duplicates = comparision.Added.Intersect(comparision.Deleted);

                // Remove duplicates from both lists
                comparision.Added = comparision.Added.Except(duplicates);
                comparision.Deleted = comparision.Deleted.Except(duplicates);
            }

            if (comparision.Added.Count() == 0 && comparision.Deleted.Count() == 0)
            {
                // No changes, so we can skip the sync process
                CleanUp();
                return false;
            }

            // Save the added lines to the toInsertFilePath
            using (TextWriter textWriter = new StreamWriter(toInsertFilePath, true, Encoding.UTF8))
            {
                for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                    await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), this.SyncService.GetCancellationToken());

                foreach (var line in comparision.Added)
                    await textWriter.WriteAsync(new StringBuilder(line), this.SyncService.GetCancellationToken());
            }

            // Save the deleted lines to the toDeleteFilePath
            using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, Encoding.UTF8))
            {
                for (int i = 0; i < engine.Options.IgnoreFirstLines; i++)
                    await textWriter.WriteLineAsync(new StringBuilder(engine.GetFileHeader()), this.SyncService.GetCancellationToken());

                foreach (var line in comparision.Deleted)
                    await textWriter.WriteAsync(new StringBuilder(line), this.SyncService.GetCancellationToken());
            }

            //Cleanup some memory usage
            comparision.Added = null;
            comparision.Deleted = null;
            GarbageCollection();
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        if(input.Input ==  SyncActionType.Upsert)
            return new(CalculateCSVRecordCountAsync(toInsertFilePath!));
        else if (input.Input == SyncActionType.Delete)
            return new(CalculateCSVRecordCountAsync(toDeleteFilePath!));

        throw new NotImplementedException("The action type is not supported.");
    }

    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        if (input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousItems is not null)
            return new(input.Input.PreviousItems);

        if (input.Input.Status.ActionType == SyncActionType.Upsert)
            return new(LoadItems(toInsertFilePath!, (int)input.Input.Status.BatchSize));
        else if (input.Input.Status.ActionType == SyncActionType.Delete)
            return new(LoadItems(toDeleteFilePath!, (int)input.Input.Status.BatchSize));

        throw new NotImplementedException("The action type is not supported.");
    }

    private IEnumerable<TSource?>? LoadItems(string filePath, int batchSize)
    {
        using CacheableCSVAsyncEngine<TSource> engine = new();
        engine.BeginReadFile(filePath);
        return engine.ReadNexts(batchSize);
    }

    public async ValueTask Succeeded()
    {
        using CacheableCSVAsyncEngine<TSource> engine = new();

        await storageService.StoreNewVersionAsync(Path.Combine(workingDirectory!.FullName, "file.csv"),
            this.csvConfigurations!.GetDestinationRelativePath(), this.csvConfigurations.DestinationContainerOrShareName, engine.Options.IgnoreFirstLines,
            this.SyncService.GetCancellationToken());
    }

    public ValueTask Finished()
    {
        CleanUp();
        return new();
    }

    public ValueTask Reset()
    {
        this.csvConfigurations = null;
        this.workingDirectory = null;
        this.toDeleteFilePath = null;
        this.toInsertFilePath = null;
        CleanUp();

        return new ValueTask();
    }

    public ValueTask DisposeAsync()
    {
        Reset();

        return new ValueTask();
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
        if (this.SyncService.GetCancellationToken().IsCancellationRequested)
        {
            CleanUp();
            throw new OperationCanceledException();
        }
    }

    private long CalculateCSVRecordCountAsync(string filePath)
    {
        using CacheableCSVAsyncEngine<TSource> engine = new();
        engine.BeginReadFile(filePath);
        return engine.LongCount();
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

    #region Not Implemented
    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Failed()
    {
        throw new NotImplementedException();
    }

    public ValueTask ActionCompleted(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        throw new NotImplementedException();
    }
    #endregion
}

using LibGit2Sharp;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// This provide Add and Delete operations for CSV files.
/// </summary>
/// <typeparam name="TCSV"></typeparam>
/// <typeparam name="TDestination"></typeparam>
public class CSVSyncDataSource<TCSV, TDestination> : ISyncDataAdapter<TCSV, TDestination, CSVSyncDataSourceConfigurations, CSVSyncDataSource<TCSV, TDestination>>
    where TCSV : CacheableCSV, new()
    where TDestination : class, new()
{
    private readonly CSVSyncDataSourceOptions options;
    private readonly IStorageService storageService;

    private DirectoryInfo? workingDirectory;
    private string? toInsertFilePath;
    private string? toDeleteFilePath;

    private CacheableCSVAsyncEngine<TCSV>? engine;

    public ISyncService<TCSV, TDestination> SyncService { get; private set; } = default!;

    public CSVSyncDataSourceConfigurations? Configurations { get; private set; } = default!;

    public CSVSyncDataSource(CSVSyncDataSourceOptions options, IStorageService storageService)
    {
        this.options = options;
        this.storageService = storageService;
    }

    public CSVSyncDataSource<TCSV, TDestination> SetSyncService(ISyncService<TCSV, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    /// <summary>
    /// To avoid unexpected behavior, call this before the destination adapter is configured
    /// </summary>
    /// <param name="configurations"></param>
    /// <param name="configureSyncService">
    /// If set false just configure DataAdapter and skip the configuration of the SyncService, 
    /// then you may be configure SyncService by your self
    /// </param>
    /// <returns></returns>
    public ISyncService<TCSV, TDestination> Configure(CSVSyncDataSourceConfigurations configurations, bool configureSyncService = true)
    {
        this.Configurations = configurations;

        if(configureSyncService)
        {
            var previousPreparing = this.SyncService.Preparing;
            var previousSucceeded = this.SyncService.Succeeded;
            var previousFinished = this.SyncService.Finished;
            var previousActionStarted = this.SyncService.ActionStarted;
            var previousActionCompleted = this.SyncService.ActionCompleted;

            this.SyncService
                .SetupPreparing(async (x) =>
                {
                    if (previousPreparing is not null)
                        return await previousPreparing(x) && await this.Preparing(x);
                    else
                        return await this.Preparing(x);
                })
                .SetupActionStarted(async (x) =>
                {
                    if (previousActionStarted is not null)
                        return await previousActionStarted(x) && await this.ActionStarted(x);
                    
                    return await this.ActionStarted(x);
                })
                .SetupSourceTotalItemCount(SourceTotalItemCount)
                .SetupGetSourceBatchItems(GetSourceBatchItems)
                .SetupActionCompleted(async (x) =>
                {
                    if (previousActionCompleted is not null)
                        return await previousActionCompleted(x) && await this.ActionCompleted(x);
                    
                    return await this.ActionCompleted(x);
                })
                .SetupSucceeded(async () =>
                {
                    if (previousSucceeded is not null)
                        await previousSucceeded();

                    await Succeeded();
                })
                .SetupFinished(async () =>
                {
                    if (previousFinished is not null)
                        await previousFinished();

                    await Finished();
                });
        }

        return this.SyncService;
    }

    public async ValueTask<bool> Preparing(SyncFunctionInput input)
    {
        try
        {
            SetupWorkingDirectory();

            using CacheableCSVAsyncEngine<TCSV> engine = new();

            // Load the last CSV file that was synced successfully
            await storageService.LoadOriginalFileAsync(
                Path.Combine(this.Configurations!.DestinationDirectory ?? "", this.Configurations.CSVFileName!),
                Path.Combine(this.workingDirectory!.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines, this.Configurations.DestinationContainerOrShareName,
                this.SyncService.GetCancellationToken());

            StageAndCommit();

            // Load the new CSV file from the source
            await storageService.LoadNewVersionAsync(
                Path.Combine(this.Configurations.SourceDirectory ?? "", this.Configurations.CSVFileName!),
                Path.Combine(this.workingDirectory.FullName, "file.csv"),
                engine.Options.IgnoreFirstLines,
                this.Configurations.SourceContainerOrShareName,
                this.SyncService.GetCancellationToken());

            // Exute git diff to get the added and deleted lines
            var comparision = CompareVersionsAndGetDiff();

            // Find and skip reordered lines if the option is set
            if (this.Configurations.SkipReorderedLines) { 
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

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        engine = new();

        try
        {
            if (input.Input == SyncActionType.Add)
                engine.BeginReadFile(toInsertFilePath!);
            else if (input.Input== SyncActionType.Delete)
                engine.BeginReadFile(toDeleteFilePath!);

            return new(true);
        }
        catch (Exception)
        {
            return new(false);
        }
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        CacheableCSVAsyncEngine<TCSV>? e = new();

        if (input.Input == SyncActionType.Add)
            e.BeginReadFile(toInsertFilePath!);
        else if (input.Input == SyncActionType.Delete)
            e.BeginReadFile(toDeleteFilePath!);

        if (input.Input == SyncActionType.Add || input.Input == SyncActionType.Delete)
            return new(e!.LongCount());

        return new(0);
    }

    public ValueTask<IEnumerable<TCSV?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TCSV>> input)
    {
        try
        {
            if (input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousItems is not null)
                return new(input.Input.PreviousItems);

            if (input.Input.Status.ActionType == SyncActionType.Add || input.Input.Status.ActionType == SyncActionType.Delete)
                return new(engine!.ReadNexts((int)input.Input.Status.BatchSize));

            return new([]);
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        this.engine?.Close();
        this.engine?.Dispose();
        this.engine = null;

        return new(input.Input.Succeeded);
    }

    public async ValueTask Succeeded()
    {
        using CacheableCSVAsyncEngine<TCSV> engine = new();

        await storageService.StoreNewVersionAsync(Path.Combine(workingDirectory!.FullName, "file.csv"),
            this.Configurations!.GetDestinationRelativePath(), this.Configurations.DestinationContainerOrShareName, engine.Options.IgnoreFirstLines,
            this.SyncService.GetCancellationToken());
    }

    public ValueTask Finished()
    {
        CleanUp();
        return ValueTask.CompletedTask;
    }

    public ValueTask Reset()
    {
        this.Configurations = null;
        this.workingDirectory = null;
        this.toDeleteFilePath = null;
        this.toInsertFilePath = null;
        CleanUp();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Reset();
        engine.Dispose();
        engine = null;

        return ValueTask.CompletedTask;
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
    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TCSV, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TCSV, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Failed()
    {
        throw new NotImplementedException();
    }

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        throw new NotImplementedException();
    }
    #endregion
}

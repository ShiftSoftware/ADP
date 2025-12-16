using LibGit2Sharp;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// This provide Add and Delete operations for CSV files.
/// </summary>
/// <typeparam name="TCSV"></typeparam>
/// <typeparam name="TDestination"></typeparam>
public abstract class CsvSyncDataSource<T> : IAsyncDisposable
    where T : class
{
    private readonly CSVSyncDataSourceOptions options;
    private readonly IStorageService storageService;

    private DirectoryInfo? workingDirectory;
    protected string? toInsertFilePath;
    protected string? toDeleteFilePath;

    protected SyncPreparingResponseAction? prepareResult;
    private int numberOfHeaderLines = 0;
    private IEnumerable<string> headers = [];

    public CSVSyncDataSourceConfigurations<T>? Configurations { get; private set; } = default!;

    public CsvSyncDataSource(CSVSyncDataSourceOptions options, IStorageService storageService)
    {
        this.storageService = storageService;
        this.options = options;
    }

    public void Configure(
        CSVSyncDataSourceConfigurations<T> configurations, 
        IEnumerable<string>? headers)
    {
        this.Configurations = configurations;
        this.numberOfHeaderLines = headers?.Count() ?? 0;
        this.headers = headers ?? [];
    }

    public async ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        try
        {
            SetupWorkingDirectory();

            // Load the last CSV file that was synced successfully
            await storageService.LoadOriginalFileAsync(
                Path.Combine(this.Configurations!.DestinationDirectory ?? "", this.Configurations.CSVFileName!),
                Path.Combine(this.workingDirectory!.FullName, "file.csv"),
                this.numberOfHeaderLines, this.Configurations.DestinationContainerOrShareName,
                input.CancellationToken);

            StageAndCommit();

            // Load the new CSV file from the source
            await storageService.LoadNewVersionAsync(
                Path.Combine(this.Configurations.SourceDirectory ?? "", this.Configurations.CSVFileName!),
                Path.Combine(this.workingDirectory.FullName, "file.csv"),
                this.numberOfHeaderLines,
                this.Configurations.SourceContainerOrShareName,
                input.CancellationToken);

            await ProccessSourceData(Path.Combine(this.workingDirectory.FullName, "file.csv"));

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
                return SyncPreparingResponseAction.Skiped;

            // Save the added lines to the toInsertFilePath
            using (TextWriter textWriter = new StreamWriter(toInsertFilePath, true, Encoding.UTF8))
            {
                foreach (var header in this.headers)
                    await textWriter.WriteLineAsync(new StringBuilder(header), input.CancellationToken);

                foreach (var line in comparision.Added)
                    await textWriter.WriteAsync(new StringBuilder(line), input.CancellationToken);
            }

            // Save the deleted lines to the toDeleteFilePath
            using (TextWriter textWriter = new StreamWriter(toDeleteFilePath, true, Encoding.UTF8))
            {
                foreach (var header in this.headers)
                    await textWriter.WriteLineAsync(new StringBuilder(header), input.CancellationToken);

                foreach (var line in comparision.Deleted)
                    await textWriter.WriteAsync(new StringBuilder(line), input.CancellationToken);
            }

            //Cleanup some memory usage
            comparision.Added = null;
            comparision.Deleted = null;
            GarbageCollection();

            // Process added and deleted items if any of the process functions is not null
            await ProccessAddedAndDeletedItems();
        }
        catch (Exception)
        {
            return SyncPreparingResponseAction.Failed;
        }

        return SyncPreparingResponseAction.Succeeded;
    }

    protected abstract ValueTask<IEnumerable<T>> ReadCsvFile(string path, bool hasHeader);
    protected abstract ValueTask WriteCsvFile(string path, IEnumerable<T> items, bool hasHeader);

    private async ValueTask ProccessSourceData(string path)
    {
        if(this.Configurations?.ProccessSourceData is null)
            return;

        var items = await ReadCsvFile(path, this.Configurations.HasHeaderRecord);
        var processedItems = await this.Configurations.ProccessSourceData(items);
        await WriteCsvFile(path, processedItems, false);
    }

    private async ValueTask ProccessAddedAndDeletedItems()
    {
        if (this.Configurations?.ProccessAddedItems is null && this.Configurations?.ProccessDeletedItems is null)
            return;

        // Parse CSV files once and reuse for both functions
        IEnumerable<T> addedItems = await ReadCsvFile(toInsertFilePath!, this.Configurations?.HasHeaderRecord ?? false);
        IEnumerable<T> deletedItems = await ReadCsvFile(toDeleteFilePath!, this.Configurations?.HasHeaderRecord ?? false);

        // Process added items if the function is provided
        if (this.Configurations?.ProccessAddedItems is not null)
        {
            var processedAddedItems = await this.Configurations.ProccessAddedItems(addedItems, deletedItems);
            await WriteCsvFile(toInsertFilePath!, processedAddedItems, this.Configurations?.HasHeaderRecord ?? false);
        }

        // Process deleted items if the function is provided
        if (this.Configurations?.ProccessDeletedItems is not null)
        {
            var processedDeletedItems = await this.Configurations.ProccessDeletedItems(addedItems, deletedItems);
            await WriteCsvFile(toDeleteFilePath!, processedDeletedItems, this.Configurations?.HasHeaderRecord ?? false);
        }
    }

    public async ValueTask Succeeded(SyncFunctionInput input)
    {
        if (this.prepareResult != SyncPreparingResponseAction.Skiped)
        {
            await storageService.StoreNewVersionAsync(Path.Combine(workingDirectory!.FullName, "file.csv"),
                this.Configurations!.GetDestinationRelativePath(), this.Configurations.DestinationContainerOrShareName, this.numberOfHeaderLines,
                input.CancellationToken);
        }
    }

    public ValueTask Finished(SyncFunctionInput input)
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
        return ValueTask.CompletedTask;
    }

    private void SetupWorkingDirectory()
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

        try
        {
            this.workingDirectory?.Delete(true);
        }
        catch {}

        GarbageCollection();
    }

    private void SetAttributesNormal()
    {
        foreach (var subDir in this.workingDirectory?.GetDirectories() ?? [])
        {
            try
            {
                SetAttributesNormal(subDir);
            }
            catch {}
        }

        foreach (var file in workingDirectory?.GetFiles() ?? [])
        {
            try
            {
                file.Attributes = FileAttributes.Normal;
            }
            catch {}
        }
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

    private void StageAndCommit()
    {
        using (var repo = new Repository(this.workingDirectory?.FullName))
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

    protected void ConfigureSyncService<TCSV, TDestination, TDataAdapter>(
        CSVSyncDataSourceConfigurations<TCSV> configurations,
        ISyncDataAdapter<TCSV, TDestination, CSVSyncDataSourceConfigurations<TCSV>, TDataAdapter> dataAdapter
    )
        where TCSV : class
        where TDestination : class
        where TDataAdapter : ISyncDataAdapter<TCSV, TDestination, CSVSyncDataSourceConfigurations<TCSV>, TDataAdapter>
    {
        var previousPreparing = dataAdapter.SyncService.Preparing;
        var previousSucceeded = dataAdapter.SyncService.Succeeded;
        var previousFinished = dataAdapter.SyncService.Finished;
        var previousActionStarted = dataAdapter.SyncService.ActionStarted;
        var previousActionCompleted = dataAdapter.SyncService.ActionCompleted;

        dataAdapter.SyncService
            .SetupPreparing(async (x) =>
            {
                SyncPreparingResponseAction previousResult = SyncPreparingResponseAction.Succeeded; ;

                if (previousPreparing is not null)
                    previousResult = await previousPreparing(x);

                var currentResult = await this.Preparing(x);

                if (previousResult == SyncPreparingResponseAction.Skiped || currentResult == SyncPreparingResponseAction.Skiped)
                    prepareResult = SyncPreparingResponseAction.Skiped;
                else if (previousResult == SyncPreparingResponseAction.Succeeded && currentResult == SyncPreparingResponseAction.Succeeded)
                    prepareResult = SyncPreparingResponseAction.Succeeded;
                else
                    prepareResult = SyncPreparingResponseAction.Failed;

                return prepareResult.Value;
            })
            .SetupActionStarted(async (x) =>
            {
                if (previousActionStarted is not null)
                    return await previousActionStarted(x) && await dataAdapter.ActionStarted(x);

                return await dataAdapter.ActionStarted(x);
            })
            .SetupSourceTotalItemCount(dataAdapter.SourceTotalItemCount)
            .SetupGetSourceBatchItems(dataAdapter.GetSourceBatchItems)
            .SetupActionCompleted(async (x) =>
            {
                if (previousActionCompleted is not null)
                    return await previousActionCompleted(x) && await dataAdapter.ActionCompleted(x);

                return await dataAdapter.ActionCompleted(x);
            })
            .SetupSucceeded(async (x) =>
            {
                if (previousSucceeded is not null)
                    await previousSucceeded(x);

                await dataAdapter.Succeeded(x);
            })
            .SetupFinished(async (x) =>
            {
                if (previousFinished is not null)
                    await previousFinished(x);

                await dataAdapter.Finished(x);
            });
    }
}

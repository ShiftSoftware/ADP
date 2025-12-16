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
public class FileHelperCsvSyncDataSource<TCSV, TDestination> : CsvSyncDataSource<TCSV>, ISyncDataAdapter<TCSV, TDestination, CSVSyncDataSourceConfigurations<TCSV>, FileHelperCsvSyncDataSource<TCSV, TDestination>>
    where TCSV : CacheableCSV
    where TDestination : class
{
    private CacheableCSVAsyncEngine<TCSV>? engine = new();
    
    public ISyncEngine<TCSV, TDestination> SyncService { get; private set; } = default!;

    public CSVSyncDataSourceConfigurations<TCSV>? Configurations { get; private set; } = default!;

    public FileHelperCsvSyncDataSource(CSVSyncDataSourceOptions options, IStorageService storageService) : base(options, storageService)
    {
    }

    public FileHelperCsvSyncDataSource<TCSV, TDestination> SetSyncService(ISyncEngine<TCSV, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    /// <summary>
    /// To avoid unexpected behavior, call this before the destination adapter is configured
    /// </summary>
    /// <param name="configurations"></param>
    /// <param name="configureSyncService">
    /// If set false just configure DataAdapter and skip the configuration of the SyncEngine, 
    /// then you may be configure SyncEngine by your self
    /// </param>
    /// <returns></returns>
    public ISyncEngine<TCSV, TDestination> Configure(CSVSyncDataSourceConfigurations<TCSV> configurations, bool configureSyncService = true)
    {
        base.Configure(
            configurations, 
            [engine!.GetFileHeader()]);

        this.Configurations = configurations;

        if (configureSyncService)
            base.ConfigureSyncService(configurations, this);

        return this.SyncService;
    }

    protected override ValueTask<IEnumerable<TCSV>> ReadCsvFile(string path, bool hasHeader)
    {
        CacheableCSVEngine<TCSV> csvEngine = new();
        return new ValueTask<IEnumerable<TCSV>>(csvEngine.ReadFile(path));
    }

    protected override ValueTask WriteCsvFile(string path, IEnumerable<TCSV> items, bool hasHeader)
    {
        CacheableCSVEngine<TCSV> csvEngine = new();
        csvEngine.Encoding = new UTF8Encoding(false);

        if(hasHeader)
            csvEngine.HeaderText = csvEngine.GetFileHeader();

        csvEngine.WriteFile(path, items);
        return ValueTask.CompletedTask;
    }

    public new ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        return base.Preparing(input);
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

    public new ValueTask Succeeded(SyncFunctionInput input)
    {
        return base.Succeeded(input);
    }

    public new ValueTask Finished(SyncFunctionInput input)
    {
        return base.Finished(input);
    }

    public new ValueTask Reset()
    {
        return base.Reset();
    }

    public async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        engine?.Dispose();
        engine = null;
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

    public ValueTask Failed(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TCSV, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TCSV?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }
    #endregion
}

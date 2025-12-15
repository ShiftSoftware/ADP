using CsvHelper;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Collections;
using System.Globalization;
using System.Text;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class CsvHelperCsvSyncDataSource<TCSV, TDestination> : CsvSyncDataSource<TCSV>, ISyncDataAdapter<TCSV, TDestination, CSVSyncDataSourceConfigurations<TCSV>, CsvHelperCsvSyncDataSource<TCSV, TDestination>>
    where TCSV : class, new()
    where TDestination : class
{
    StreamReader? streamReader = default!;
    CsvReader? csvReader = default!;

    public ISyncEngine<TCSV, TDestination> SyncService { get; private set; } = default!;

    public CSVSyncDataSourceConfigurations<TCSV>? Configurations { get; private set; } = default!;

    public CsvHelperCsvSyncDataSource(CSVSyncDataSourceOptions options, IStorageService storageService) : base(options, storageService)
    {
    }

    public CsvHelperCsvSyncDataSource<TCSV, TDestination> SetSyncService(ISyncEngine<TCSV, TDestination> syncService)
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
        base.Configure(configurations, configurations.HasHeaderRecord ? [""] : [], ProccessSourceData);

        this.Configurations = configurations;

        if (configureSyncService)
            base.ConfigureSyncService(configurations, this);

        return this.SyncService;
    }

    private async ValueTask ProccessSourceData(string path)
    {
        if (this.Configurations?.ProccessSourceData is null)
            return;

        IEnumerable<TCSV> records = [];
        using (var reader = new StreamReader(path))
        using (var csvReader = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = Configurations!.HasHeaderRecord
        }))
        {
            await foreach (var record in csvReader.GetRecordsAsync<TCSV>())
                records = records.Append(record);
        }

        var processedRecords = await Configurations.ProccessSourceData(records);

        using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
        using (var csvWriter = new CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false // Do not write headers
        }))
        {
            await csvWriter.WriteRecordsAsync(processedRecords);
        }
    }
    public new ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        return base.Preparing(input);
    }

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        try
        {
            if (input.Input == SyncActionType.Add)
                this.streamReader = new StreamReader(toInsertFilePath!);
            else if (input.Input == SyncActionType.Delete)
                this.streamReader = new StreamReader(toDeleteFilePath!);

            this.csvReader = new CsvReader(this.streamReader!, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = Configurations!.HasHeaderRecord
            });

            return new(true);
        }
        catch (Exception)
        {
            return new(false);
        }
    }

    public async ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        if (input.Input == SyncActionType.Add)
            return await GetItemCountAsync(toInsertFilePath!);
        else if (input.Input == SyncActionType.Delete)
            return await GetItemCountAsync(toDeleteFilePath!);

        return 0;
    }

    private async Task<long> GetItemCountAsync(string filePath)
    {
        long count = 0;
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = Configurations!.HasHeaderRecord
        });

        await foreach (var record in csv.EnumerateRecordsAsync(new TCSV()))
            count++;

        return count;
    }

    public async ValueTask<IEnumerable<TCSV?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TCSV>> input)
    {
        try
        {
            if (input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousItems is not null)
                return input.Input.PreviousItems;

            if (input.Input.Status.ActionType == SyncActionType.Add || input.Input.Status.ActionType == SyncActionType.Delete)
                return await ReadNextsAsync(input.Input.Status.BatchSize);

            return [];
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private async Task<IEnumerable<TCSV?>> ReadNextsAsync(long batchSize)
    {
        IEnumerable<TCSV?> records = [];
        long count = 0;

        await foreach (var record in this.csvReader!.GetRecordsAsync<TCSV>())
        {
            if (count >= batchSize) break;
            records = records.Append(record);
            count++;
        }
        
        return records;
    }

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        this.streamReader?.Close();
        this.streamReader?.Dispose();
        this.streamReader = null;

        this.csvReader?.Dispose();
        this.csvReader = null;

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
        this.streamReader?.Close();
        this.streamReader?.Dispose();
        this.streamReader = null;

        this.csvReader?.Dispose();
        this.csvReader = null;

        return base.Reset();
    }

    public async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        this.streamReader?.Close();
        this.streamReader?.Dispose();
        this.streamReader = null;

        this.csvReader?.Dispose();
        this.csvReader = null;
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

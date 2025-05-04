namespace ShiftSoftware.ADP.SyncAgent.ConfigurationModels;

internal class SyncConfigurations<TCSV, TData>
    where TCSV : CacheableCSV
    where TData : class
{
    public string? SyncId { get; set; } = null;
    public Func<IEnumerable<TCSV>, DataProcessActionType, ValueTask<IEnumerable<TData>>> Mapping { get; set; }

    public SyncConfigurations()
    {
        
    }

    public SyncConfigurations(string? syncId, Func<IEnumerable<TCSV>, DataProcessActionType, ValueTask<IEnumerable<TData>>> mapping)
    {
        SyncId = syncId;
        Mapping = mapping;
    }
}

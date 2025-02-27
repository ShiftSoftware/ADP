namespace ShiftSoftware.ADP.SyncAgent;

public class CSVSyncResult<T> where T : CacheableCSV
{
    public double TimeToDownloadOriginalFile { get; set; }
    public double TimeToDownloadNewFile { get; set; }
    public double TimeToCompareWithGit { get; set; }
    public double TimeToWriteToInsertFile { get; internal set; }
    public double TimeToWriteToDeleteFile { get; internal set; }
    public double TimeToParseToInsertFile { get; internal set; }
    public double TimeToParseToDeleteFile { get; internal set; }
    public double TimeToInitializeCosmosClient { get; internal set; }
    public double TimeToSyncToCosmos { get; internal set; }
    public DirectoryInfo WorkingDirectory { get; internal set; }
    public IEnumerable<T> ToInsert { get; internal set; } = [];
    public IEnumerable<T> ToDelete { get; internal set; } = [];
    public int ToInsertCount { get; internal set; }
    public int ToDeleteCount { get; internal set; }
    public int CosmosDBInsertedCount { get; internal set; }
    public int CosmosDBDeletedCount { get; internal set; }
    public CSVSyncStatus Status { get; set; } = CSVSyncStatus.None;

    public bool Success
    {
        get
        {
            return Status == CSVSyncStatus.SuccessSync || Status == CSVSyncStatus.Skipped;
        }
    }
}
namespace ShiftSoftware.ADP.SyncAgent;

public enum CSVSyncStatus
{
    None = 0,
    Skipped = 1,
    SuccessSync = 2,
    FailedCosmosDBSync = 3,
    FailedBlobUpload = 4,
}
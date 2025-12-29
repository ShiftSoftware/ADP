namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class FileSystemStorageOptions
{
    public string CompareWorkingDirectory { get; set; } = default!;
    public string SourceBasePath { get; set; } = default!;
    public string DestinationBasePath { get; set; } = default!;
}
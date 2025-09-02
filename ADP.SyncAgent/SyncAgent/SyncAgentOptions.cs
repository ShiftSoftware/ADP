namespace ShiftSoftware.ADP.SyncAgent;

public class SyncAgentOptions
{
    public string CSVCompareWorkingDirectory { get; set; } = default!;
    public string SourceBasePath { get; set; } = default!;
    public string DestinationBasePath { get; set; } = default!;
}
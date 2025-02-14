namespace ADP.SyncAgent;

public class FileLocationSetting
{
    public string? SourceContainerOrShareName { get; set; }
    public string? SourceDirectory { get; set; }
    public string? DestinationContainerOrShareName { get; set; }
    public string? DestinationDirectory { get; set; }
}

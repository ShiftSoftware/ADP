namespace ShiftSoftware.ADP.SyncAgent.Configurations;

public class CSVSyncDataSourceConfigurations
{
    public required string CSVFileName { get; set; }
    public string? SourceContainerOrShareName { get; set; }
    public string? SourceDirectory { get; set; }
    public string? DestinationContainerOrShareName { get; set; }
    public string? DestinationDirectory { get; set; }
    public bool SkipReorderedLines { get; set; }

    public CSVSyncDataSourceConfigurations()
    {
        
    }

    public CSVSyncDataSourceConfigurations(
        string? csvFileName,
        string? sourceContainerOrShareName,
        string? sourceDirectory, 
        string? destinationContainerOrShareName,
        string? destinationDirectory,
        bool skipReorderedLines)
    {
        CSVFileName = csvFileName;
        SourceContainerOrShareName = sourceContainerOrShareName;
        SourceDirectory = sourceDirectory;
        DestinationContainerOrShareName = destinationContainerOrShareName;
        DestinationDirectory = destinationDirectory;
        SkipReorderedLines = skipReorderedLines;
    }

    internal string GetDestinationRelativePath()
    {
        return Path.Combine(DestinationDirectory ?? "", CSVFileName!);
    }

    internal string GetSourceRelativePath()
    {
        return Path.Combine(SourceDirectory ?? "", CSVFileName!);
    }
}

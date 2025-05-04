namespace ShiftSoftware.ADP.SyncAgent.ConfigurationModels;

internal class CSVConfigurations
{
    public string? CSVFileName { get; set; }
    public string? SourceContainerOrShareName { get; set; }
    public string? SourceDirectory { get; set; }
    public string? DestinationContainerOrShareName { get; set; }
    public string? DestinationDirectory { get; set; }

    public CSVConfigurations()
    {
        
    }

    public CSVConfigurations(string? csvFileName, string? sourceContainerOrShareName, string? sourceDirectory, string? destinationContainerOrShareName, string? destinationDirectory)
    {
        CSVFileName = csvFileName;
        SourceContainerOrShareName = sourceContainerOrShareName;
        SourceDirectory = sourceDirectory;
        DestinationContainerOrShareName = destinationContainerOrShareName;
        DestinationDirectory = destinationDirectory;
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

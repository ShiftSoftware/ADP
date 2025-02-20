namespace ADP.SyncAgent.Services.Interfaces;

public interface IStorageService
{
    Task LoadOriginalFileAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName);
    Task LoadNewVersionAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName);
    Task StoreNewVersionAsync(string sourcePath, string destinationRelativePath, string? containerOrShareName, int ignoreFirstLines);
}

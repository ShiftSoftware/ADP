namespace ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

public interface IStorageService
{
    Task LoadOriginalFileAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName, CancellationToken cancellationToken);
    Task LoadNewVersionAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName, CancellationToken cancellationToken);
    Task StoreNewVersionAsync(string sourcePath, string destinationRelativePath, string? containerOrShareName, int ignoreFirstLines, CancellationToken cancellationToken);
}

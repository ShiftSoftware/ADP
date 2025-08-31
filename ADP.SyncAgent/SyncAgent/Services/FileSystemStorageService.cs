using Polly;
using Polly.Retry;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class FileSystemStorageService : IStorageService
{
    ResiliencePipeline ResiliencePipeline;

    private readonly string sourceBasePath;
    private readonly string destinationBasePath;

    public FileSystemStorageService(SyncAgentOptions syncAgentOptions)
    {
        ResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions()) // Upsert retry using the default options
            .Build(); // Builds the resilience pipeline

        this.sourceBasePath = syncAgentOptions.SourceBasePath;
        this.destinationBasePath = syncAgentOptions.DestinationBasePath;
    }

    private async Task LoadFileAsync(string sourcePath, string destinationPath, int ignoreFirstLines, CancellationToken cancellationToken)
    {
        try
        {
            await ResiliencePipeline.ExecuteAsync(async token =>
            {
                var linesToKeep = (await File.ReadAllLinesAsync(sourcePath, cancellationToken)).Skip(ignoreFirstLines);
                await File.WriteAllLinesAsync(destinationPath, linesToKeep, cancellationToken);
            }, cancellationToken);
        }
        catch (IOException ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
        {
            File.Create(destinationPath).Close();
        }
    }

    public async Task LoadNewVersionAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName, CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(this.sourceBasePath, sourceRelativePath);
        await LoadFileAsync(sourcePath, destinationPath, ignoreFirstLines, cancellationToken);
    }

    public async Task LoadOriginalFileAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName, CancellationToken cancellationToken)
    {
        var sourcePath = Path.Combine(destinationBasePath, sourceRelativePath);
        await LoadFileAsync(sourcePath, destinationPath, ignoreFirstLines, cancellationToken);
    }

    public async Task StoreNewVersionAsync(string sourcePath, string destinationRelativePath, string? containerOrShareName, int ignoreFirstLines, CancellationToken cancellationToken)
    {
        var destinationPath = Path.Combine(destinationBasePath, destinationRelativePath);
        var destinationDirectory = Path.GetDirectoryName(destinationPath);

        await ResiliencePipeline.ExecuteAsync(async token =>
        {
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory!);

            var lines = await File.ReadAllLinesAsync(sourcePath);

            for (int i = 0; i < ignoreFirstLines; i++)
                lines = lines.Prepend(string.Empty).ToArray();

            await File.WriteAllLinesAsync(destinationPath, lines);
        }, cancellationToken);
    }
}
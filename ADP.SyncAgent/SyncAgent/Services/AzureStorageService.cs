using ADP.SyncAgent.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace ADP.SyncAgent.Services;

public class AzureStorageService : IStorageService
{
    private readonly IConfiguration config;
    private readonly SyncAgentOptions options;
    ResiliencePipeline ResiliencePipeline;

    public AzureStorageService(IConfiguration config, SyncAgentOptions options)
    {
        this.config = config;
        this.options = options;
        ResiliencePipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions()) // Add retry using the default options
        .Build(); // Builds the resilience pipeline
    }

    private async Task<string> DownloadAsync(string path, string url, int ignoreFirstLines)
    {
        File.Delete(path);

        using var client = new HttpClient();
        await ResiliencePipeline.ExecuteAsync(async token =>
        {
            try
            {
                using var s = await client.GetStreamAsync(url);
                using var fs = new FileStream(path, FileMode.OpenOrCreate);
                await s.CopyToAsync(fs);
                fs.Close();

                var tempFile = Path.GetTempFileName();
                var linesToKeep = File.ReadLines(path).Skip(ignoreFirstLines);

                File.WriteAllLines(tempFile, linesToKeep);

                File.Delete(path);
                File.Move(tempFile, path);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                File.Create(path).Close();
            }
        });

        return path;
    }

    public async Task LoadOriginalFileAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName)
    {
        var url = this.GetBlobSasURL(containerOrShareName!, Path.GetDirectoryName(sourceRelativePath!)!, Path.GetFileName(sourceRelativePath));
        await DownloadAsync(destinationPath, url, ignoreFirstLines);
    }

    public async Task LoadNewVersionAsync(string sourceRelativePath, string destinationPath, int ignoreFirstLines, string? containerOrShareName)
    {
        var url = this.GetFileShareSASURL(containerOrShareName!, Path.GetDirectoryName(sourceRelativePath!)!, Path.GetFileName(sourceRelativePath));
        await DownloadAsync(destinationPath, url, ignoreFirstLines);
    }

    public async Task StoreNewVersionAsync(string sourcePath, string destinationRelativePath, string? containerOrShareName)
    {
        await UploadToAzureStorageBlobAsync(sourcePath, destinationRelativePath, containerOrShareName!);
    }

    private async Task UploadToAzureStorageBlobAsync(string sourcePath, string destinationRelativePath, string container)
    {
        try
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(options.AzureStorageAccountConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(container);
            BlobClient blobClient = containerClient.GetBlobClient(destinationRelativePath); // Include the folder path

            using (FileStream fileStream = File.OpenRead(sourcePath))
            {
                await blobClient.UploadAsync(fileStream, true);
            }

        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private string GetBlobSasURL(string containerName, string? folderName, string blobName)
    {
        var blobFullName = string.IsNullOrWhiteSpace(folderName) ? blobName : $"{folderName}/{blobName}";

        BlobSasBuilder sasBuilder = new BlobSasBuilder(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(3))
        {
            BlobContainerName = containerName,
            BlobName = blobFullName, // Include the folder path
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow,
            Protocol = SasProtocol.Https,
        };

        BlobServiceClient blobServiceClient = new BlobServiceClient(options.AzureStorageAccountConnectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        BlobClient blobClient = containerClient.GetBlobClient(blobFullName); // Include the folder path

        string sasURL = blobClient.GenerateSasUri(sasBuilder).ToString();

        return sasURL;
    }

    private string GetFileShareSASURL(string shareName, string directoryName, string fileName)
    {
        ShareClient shareClient = new ShareClient(options.AzureStorageAccountConnectionString, shareName);
        ShareDirectoryClient directoryClient = shareClient.GetDirectoryClient(directoryName);
        ShareFileClient fileClient = directoryClient.GetFileClient(Path.GetFileName(fileName));

        ShareSasBuilder sasBuilder = new ShareSasBuilder(ShareFileSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(3))
        {
            ShareName = shareName,
            Resource = "f",
            StartsOn = DateTimeOffset.UtcNow,
            Protocol = SasProtocol.Https,
        };

        string sasURL = fileClient.GenerateSasUri(sasBuilder).ToString();

        return sasURL;
    }
}

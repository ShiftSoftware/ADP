using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ShiftSoftware.ADP.WarrantyClaims.Web.Extensions;

/// <summary>
/// Saves a file that an API endpoint returned in its response body. Use it from an
/// <c>ActionButton.OnResponse</c> handler.
/// </summary>
public static class FileDownloadExtensions
{
    /// <summary>
    /// Sends the response body to the browser as a downloaded file.
    /// The file name comes from the Content-Disposition header when the browser can read it.
    /// When the API is on another origin and does not expose that header, <paramref name="fallbackFileName"/> is used.
    /// </summary>
    public static async Task DownloadResponseAsFileAsync(this IJSRuntime jsRuntime, HttpResponseMessage response, string fallbackFileName)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;

        var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName?.Trim('"') ?? fallbackFileName;

        var stream = await response.Content.ReadAsStreamAsync();

        using var streamReference = new DotNetStreamReference(stream);

        await jsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamReference);
    }
}

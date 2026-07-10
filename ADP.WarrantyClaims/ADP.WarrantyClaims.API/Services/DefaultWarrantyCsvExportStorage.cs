using Microsoft.Extensions.Hosting;
using ShiftSoftware.ADP.WarrantyClaims.Shared;

namespace ShiftSoftware.ADP.WarrantyClaims.API.Services;

/// <summary>
/// The module's default <see cref="IWarrantyCsvExportStorage"/> (Phase 3 Slice 3.6, D24): exports
/// live on local disk under <c>{ContentRootPath}\warranty-csv-exports</c>, exactly the placement
/// the original host application used — the relative path returned to the client
/// (<c>warranty-csv-exports\{fileName}</c>, backslash separator included) is byte-identical.
/// Registered TryAddScoped by the module API extension; a consumer registration made BEFORE the
/// module extension replaces this default (e.g. blob storage).
/// </summary>
public class DefaultWarrantyCsvExportStorage : IWarrantyCsvExportStorage
{
    private const string RelativeExportFolder = "warranty-csv-exports";

    private readonly IHostEnvironment hostEnvironment;

    public DefaultWarrantyCsvExportStorage(IHostEnvironment hostEnvironment)
    {
        this.hostEnvironment = hostEnvironment;
    }

    public Task<string> WriteAsync(string fileName, Stream content)
    {
        var absoluteExportFolder = $"{this.hostEnvironment.ContentRootPath}\\{RelativeExportFolder}";

        Directory.CreateDirectory(absoluteExportFolder);

        var relativeExportPath = $"{RelativeExportFolder}\\{fileName}";

        var absoluteExportPath = $"{this.hostEnvironment.ContentRootPath}\\{relativeExportPath}";

        using (var fileStream = File.Create(absoluteExportPath))
        {
            content.CopyTo(fileStream);
        }

        return Task.FromResult(relativeExportPath);
    }

    public Task<Stream> OpenAsync(string exportPath)
    {
        // DELIBERATE SECURITY FIX vs the verbatim-move doctrine (Phase 3 Slice 3.3): the dissolved
        // controller concatenated the anonymous catch-all route segment straight onto ContentRootPath,
        // so "..\"-style values (or plain names like "appsettings.json") could read ANY file the app
        // can — on an [AllowAnonymous] endpoint. The path is now canonicalized and confined to the
        // export folder; escapes fail exactly like a missing file does (FileNotFoundException -> the
        // same unhandled-500 the old code produced for a missing export). Legitimate paths under
        // warranty-csv-exports behave byte-identically.
        var exportRoot = Path.GetFullPath(Path.Combine(this.hostEnvironment.ContentRootPath, RelativeExportFolder));

        var fullPath = Path.GetFullPath(Path.Combine(this.hostEnvironment.ContentRootPath, exportPath));

        if (!fullPath.StartsWith(exportRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException($"Could not find file '{exportPath}'.", exportPath);

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }
}

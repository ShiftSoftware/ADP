namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam over the manufacturer-CSV export file placement (Phase 3 Slice 3.3). The module
/// owns the file-name convention (<c>warranty-claims-{yyyyMMddHHmmss}.csv</c>); the storage owns
/// where the bytes live and what relative path the client is given back. Resolved lazily from the
/// request services. Since Phase 3 Slice 3.6 (D24) the module ships a default implementation
/// writing under the content root's <c>warranty-csv-exports</c> folder (TryAdd — register your own
/// storage BEFORE the module extension to use e.g. blob storage instead).
/// </summary>
public interface IWarrantyCsvExportStorage
{
    /// <summary>
    /// Persists the export stream under <paramref name="fileName"/> and returns the relative export
    /// path handed to the client (echoed in <c>Additional["CSVExportPath"]</c> and later passed back
    /// to <see cref="OpenAsync"/> by the anonymous download endpoint).
    /// </summary>
    Task<string> WriteAsync(string fileName, Stream content);

    /// <summary>
    /// Opens a previously written export for the anonymous <c>DownloadCSV</c> endpoint.
    /// SECURITY CONTRACT: <paramref name="exportPath"/> comes raw from an anonymous catch-all route.
    /// Implementations MUST canonicalize it and confine it to the export root (e.g.
    /// <c>Path.GetFullPath</c> + a root-prefix check) and treat any escape exactly like a missing
    /// file — never serve bytes from outside the export folder.
    /// </summary>
    Task<Stream> OpenAsync(string exportPath);
}

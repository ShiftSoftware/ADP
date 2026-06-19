namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Host-supplied paths the framework core needs at composition time. A host sets these via
/// <c>AddRastgoCore</c>; the source connectors (DuckDB / Cosmos) take their own connection strings
/// through their own registration extensions.
/// </summary>
public sealed class RastgoOptions
{
    /// <summary>Base directory the <c>fileshare</c> source resolves relative paths against (the mounted Azure File Share in prod).</summary>
    public string FileShareBase { get; set; } = "";

    /// <summary>Root the <see cref="JsonlResultSink"/> writes partitioned results under (e.g. the mounted <c>/mnt/adp-health</c> share).</summary>
    public string ResultsRoot { get; set; } = "";
}

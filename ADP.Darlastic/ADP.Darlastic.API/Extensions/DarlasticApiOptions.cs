namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

public class DarlasticApiOptions
{
    /// <summary>Route prefix Darlastic controllers are mounted under (e.g. "api/Darlastic").</summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// SQL schema the registry lives under in the host database. Must match the schema the host's
    /// migrations created and the engine writes (DARLASTIC_SQL side) — "Darlastic" everywhere so far.
    /// </summary>
    public string Schema { get; set; } = "Darlastic";

    /// <summary>
    /// When true, Darlastic endpoints are protected by per-action DarlasticActionTree permissions.
    /// When false (default), only authentication is required — for hosts whose identity server
    /// doesn't grant the Darlastic tree yet.
    /// </summary>
    public bool EnableDarlasticActionTreeAuthorization { get; set; } = false;
}

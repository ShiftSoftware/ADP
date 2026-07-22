namespace ShiftSoftware.ADP.Darlastic.Web.Extensions;

public class DarlasticWebOptions
{
    /// <summary>
    /// Route prefix Darlastic API controllers are mounted under, relative to the HttpClient base
    /// address. For example if HttpClient.BaseAddress already contains "/api/" and the Darlastic
    /// API uses RoutePrefix "api/Darlastic", set this to "Darlastic" so calls resolve to
    /// "/api/Darlastic/...".
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="RoutePrefix"/> normalized with a trailing slash, or an empty string when no
    /// prefix is set. Pre-pend this to any relative URL (ShiftList EntitySet, HttpClient call)
    /// that targets the Darlastic API.
    /// </summary>
    public string ResolvedRoutePrefix =>
        string.IsNullOrWhiteSpace(RoutePrefix) ? string.Empty : RoutePrefix.Trim('/') + "/";

    /// <summary>
    /// When true, Darlastic pages gate their actions by DarlasticActionTree permissions.
    /// When false (default), only authentication is required — for hosts whose identity server
    /// doesn't grant the Darlastic tree yet.
    /// </summary>
    public bool EnableDarlasticActionTreeAuthorization { get; set; } = false;
}

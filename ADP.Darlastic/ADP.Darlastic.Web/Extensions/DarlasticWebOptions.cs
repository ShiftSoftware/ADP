using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.TypeAuth.Core.Actions;

// TypeAuth's Action collides with System.Action under ImplicitUsings.
using TypeAuthAction = ShiftSoftware.TypeAuth.Core.Actions.Action;

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
    /// When true, Darlastic pages gate their controls on <see cref="Actions"/>. When false
    /// (default), only authentication is required — for hosts whose identity server doesn't grant
    /// the Darlastic tree yet.
    /// </summary>
    /// <remarks>
    /// This is presentation, not protection — the endpoints are reachable directly whatever this
    /// says, so <c>DarlasticApiOptions.EnableDarlasticActionTreeAuthorization</c> is the flag that
    /// decides who can actually read the registry. Keep the two in agreement.
    /// </remarks>
    public bool EnableDarlasticActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// Lets the host gate the pages on <b>its own</b> action tree instead of
    /// <see cref="DarlasticActionTree"/>. Anything left null falls back to the module's own action.
    /// Mirrors <c>DarlasticApiOptions.Actions</c>; set both sides to the same actions.
    /// </summary>
    /// <remarks>
    /// Without this, a host that already has, say, a <c>Customers.Golden</c> action would have to
    /// deploy and grant a second tree purely because the module ships one — which is enough friction
    /// that deployments turn authorization off instead, which is exactly what has happened.
    /// </remarks>
    public DarlasticActionOverrides Actions { get; set; } = new();

    /// <summary>
    /// Whether <c>AddDarlasticBlazorServices</c> registers <see cref="DarlasticActionTree"/> with
    /// TypeAuth. Set false when the host gates entirely on its own actions via <see cref="Actions"/>,
    /// so the module's unused tree doesn't clutter the permissions UI. Default true.
    /// </summary>
    /// <remarks>
    /// TypeAuth is fail-closed for actions it wasn't given: turning this off while any gate still
    /// resolves to <see cref="DarlasticActionTree"/> hides that surface from everyone rather than
    /// leaving it ungated. Override every action before setting this false.
    /// </remarks>
    public bool RegisterDarlasticActionTree { get; set; } = true;

    /// <summary>
    /// Action the golden list gates on, or null when <see cref="EnableDarlasticActionTreeAuthorization"/>
    /// is off — ShiftBlazor reads a null <c>TypeAuthAction</c> as "no gate".
    /// </summary>
    public TypeAuthAction? GoldenCustomersAction =>
        EnableDarlasticActionTreeAuthorization ? Actions.ResolvedGoldenCustomers : null;

    /// <summary>Steward queue counterpart of <see cref="GoldenCustomersAction"/>.</summary>
    public TypeAuthAction? StewardQueueAction =>
        EnableDarlasticActionTreeAuthorization ? Actions.ResolvedStewardQueue : null;

    /// <summary>
    /// Action the golden grid's export button gates on, or null when authorization is off — null
    /// means "no gate", which is how every consumer behaved before this existed.
    /// </summary>
    public BooleanAction? ExportGoldenCustomersAction =>
        EnableDarlasticActionTreeAuthorization ? Actions.ResolvedExportGoldenCustomers : null;
}

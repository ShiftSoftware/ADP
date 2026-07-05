using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.Web.Extensions;

public class ClaimableItemsWebOptions
{
    /// <summary>
    /// TypeAuth node gating the ClaimableItem admin pages (List/Form). Consumer-supplied so existing
    /// grants keep resolving (decision D9) — e.g. TCA passes
    /// <c>TCA.ActionTrees.Services.ClaimableItems.ClaimableItemSetup</c>. Null → the pages are not gated.
    /// </summary>
    public ReadWriteDeleteAction? ClaimableItemSetupAction { get; set; }

    /// <summary>
    /// TypeAuth node gating the CampaignVinEntry admin pages (consumer-supplied, per D9). Null → not gated.
    /// </summary>
    public ReadWriteDeleteAction? CampaignVinEntriesAction { get; set; }

    /// <summary>
    /// Optional custom layout for Claimable Items pages. Must be a Blazor LayoutComponentBase.
    /// </summary>
    public Type? Layout { get; set; }

    /// <summary>
    /// When true, the Blazor UI hides actions/nav items the user lacks <c>ClaimableItemsActionTree</c>
    /// permission for. When false (default), UI behaves as authentication-only.
    /// </summary>
    public bool EnableClaimableItemsActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// When true, registers the module's own default <c>ClaimableItemsActionTree</c> into the Blazor
    /// TypeAuth options (for fresh consumers / the sample). Leave false (default) when the consumer already
    /// registers its own action tree with the catalog nodes (e.g. TCA), to avoid a duplicate group (D9).
    /// </summary>
    public bool RegisterModuleActionTree { get; set; } = false;

    /// <summary>
    /// Base URL of the ShiftIdentity API, if catalog pages need to resolve identity-owned lookups
    /// (brands, companies) without depending on ShiftBlazor's ExternalAddresses map.
    /// </summary>
    public string? IdentityApiUrl { get; set; }

    /// <summary>
    /// Route prefix the module's API controllers are mounted under, relative to the HttpClient base
    /// address (e.g. set "ClaimableItems" when the API uses RoutePrefix "api/ClaimableItems" and the
    /// HttpClient base already contains "/api/").
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="RoutePrefix"/> normalized with a trailing slash (empty when unset). Pre-pend to any
    /// relative URL (ShiftList EntitySet, ShiftEntityForm Endpoint) that targets the module API.
    /// </summary>
    public string ResolvedRoutePrefix =>
        string.IsNullOrWhiteSpace(RoutePrefix) ? string.Empty : RoutePrefix.Trim('/') + "/";
}

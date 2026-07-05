using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.API.Extensions;

public class ClaimableItemsApiOptions
{
    /// <summary>
    /// The TypeAuth action node gating the ClaimableItem catalog CRUD controller. The consumer supplies
    /// its OWN node (e.g. Toyota Central Asia passes <c>TCA.ActionTrees.Services.ClaimableItems.ClaimableItemSetup</c>)
    /// so existing permission grants keep resolving — the module never reproduces the node (decision D9).
    /// Only used when <see cref="EnableClaimableItemsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? ClaimableItemSetupAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the CampaignVinEntry controller (consumer-supplied, per D9).
    /// Only used when <see cref="EnableClaimableItemsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? CampaignVinEntriesAction { get; set; }

    /// <summary>
    /// Route prefix the Claimable Items API controllers are mounted under (e.g. "api" → "api/ClaimableItem").
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// When true, catalog endpoints are protected by per-action <c>ClaimableItemsActionTree</c> permissions.
    /// When false (default), endpoints require only authentication. A consumer using its own action tree
    /// (e.g. Toyota Iraq) can leave this false.
    /// </summary>
    public bool EnableClaimableItemsActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// When true, the module registers its own default <c>ClaimableItemsActionTree</c> into TypeAuth
    /// (for fresh consumers / the sample). Leave false (default) when the consumer already supplies its own
    /// action tree that owns the catalog permission nodes — e.g. Toyota Central Asia registers
    /// <c>TCA.ActionTrees.Services.ClaimableItems.*</c>, and registering the module tree too would create a
    /// duplicate permission group (see decision D9).
    /// </summary>
    public bool RegisterModuleActionTree { get; set; } = false;

    /// <summary>
    /// SQL schema every module-owned entity (and its temporal history table) is placed under.
    /// Defaults to "ClaimableItems" for the sample host. The Toyota Central Asia consumer passes
    /// <c>null</c> so the existing year-old <c>dbo</c> temporal tables are NOT renamed under
    /// SYSTEM_VERSIONING (extraction plan risk R1 / decision D3).
    /// </summary>
    public string? Schema { get; set; } = "ClaimableItems";
}

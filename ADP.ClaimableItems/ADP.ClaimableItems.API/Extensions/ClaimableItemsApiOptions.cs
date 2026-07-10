using ShiftSoftware.ADP.ClaimableItems.Data.Printing;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.API.Extensions;

public class ClaimableItemsApiOptions
{
    /// <summary>
    /// Optional absolute paths to consumer-supplied .frx templates replacing the module-embedded
    /// print defaults (the sanctioned rebranding hook). Leave the paths null (default) to print the
    /// byte-frozen embedded templates.
    /// </summary>
    public ClaimableItemsReportOverrides ReportOverrides { get; set; } = new();

    /// <summary>
    /// The TypeAuth action node gating the ClaimableItem catalog CRUD controller. The consumer supplies
    /// its OWN node (e.g. the original host application passes its existing <c>ClaimableItemSetup</c> node)
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
    /// The TypeAuth action node gating the ItemClaim controller (CRUD + the hot claim endpoint,
    /// which additionally requires Write on this node). Consumer-supplied, per D9 — the original
    /// host application passes its existing <c>Claiming</c> node.
    /// Only used when <see cref="EnableClaimableItemsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? ClaimingAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the ItemClaimCertificate controller (consumer-supplied, per
    /// D9 — the original host application passes <c>ClaimableItemCertifying</c>). Only used when
    /// <see cref="EnableClaimableItemsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? CertifyingAction { get; set; }

    /// <summary>
    /// The TypeAuth action node gating the ItemClaimInvoice controller (consumer-supplied, per D9 —
    /// the original host application passes <c>ClaimableItemInvoicing</c>). Only used when
    /// <see cref="EnableClaimableItemsActionTreeAuthorization"/> is true.
    /// </summary>
    public ReadWriteDeleteAction? InvoicingAction { get; set; }

    /// <summary>
    /// The TypeAuth boolean node that allows editing the otherwise-immutable claim fields of a Draft
    /// item claim (consumer-supplied, per D9 — the original host application passes its existing
    /// <c>PostClaimModification</c> node; same node its Web options pass for the form UI). Read by
    /// the ItemClaim repository's <c>CanModifyPostClaim</c> default (Phase 3 Slice 3.6). Null
    /// (default) → never allowed, unless a derived repository overrides the check.
    /// </summary>
    public BooleanAction? PostClaimModificationAction { get; set; }

    /// <summary>
    /// Route prefix the Claimable Items API controllers are mounted under (e.g. "api" → "api/ClaimableItem").
    /// </summary>
    public string RoutePrefix { get; set; } = "api";

    /// <summary>
    /// When true, catalog endpoints are protected by per-action <c>ClaimableItemsActionTree</c> permissions.
    /// When false (default), endpoints require only authentication. A consumer using its own action tree
    /// (e.g. a sync-fed consumer) can leave this false.
    /// </summary>
    public bool EnableClaimableItemsActionTreeAuthorization { get; set; } = false;

    /// <summary>
    /// When true, the module registers its own default <c>ClaimableItemsActionTree</c> into TypeAuth
    /// (for fresh consumers / the sample). Leave false (default) when the consumer already supplies its own
    /// action tree that owns the catalog permission nodes — e.g. the original host application registers
    /// its own action-tree catalog nodes, and registering the module tree too would create a
    /// duplicate permission group (see decision D9).
    /// </summary>
    public bool RegisterModuleActionTree { get; set; } = false;

    /// <summary>
    /// SQL schema every module-owned entity (and its temporal history table) is placed under.
    /// Defaults to "ClaimableItems" for the sample host. The original host application passes
    /// <c>null</c> so the existing year-old <c>dbo</c> temporal tables are NOT renamed under
    /// SYSTEM_VERSIONING (extraction plan risk R1 / decision D3).
    /// </summary>
    public string? Schema { get; set; } = "ClaimableItems";
}

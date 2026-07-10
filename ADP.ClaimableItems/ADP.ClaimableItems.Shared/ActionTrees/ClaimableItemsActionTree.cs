using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;

/// <summary>
/// TypeAuth permission nodes for the Claimable Items catalog module.
///
/// NOTE (Slice 2 reconciliation): the node identity here must be reconciled with the
/// the host's existing action-tree nodes (ClaimableItemSetup,
/// CampaignVinEntries) so that permissions already granted in the original host application continue to resolve
/// after the catalog controllers move into this module (see risk R8 in the extraction plan).
/// Only the catalog nodes live here — Claiming / ClaimableItemCertifying /
/// ClaimableItemInvoicing / PostClaimModification stay with the claim controllers in the host application.
/// </summary>
[ActionTree("Claimable Items", "Claimable Items Module Permissions")]
public class ClaimableItemsActionTree
{
    public readonly static ReadWriteDeleteAction ClaimableItemSetup = new("Claimable Item Setup");
    public readonly static ReadWriteDeleteAction CampaignVinEntries = new("Campaign Vin Entries");
}

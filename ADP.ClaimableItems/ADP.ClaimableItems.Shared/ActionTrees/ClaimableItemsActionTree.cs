using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.ActionTrees;

/// <summary>
/// TypeAuth permission nodes for the Claimable Items catalog module.
///
/// NOTE: the node identity here must be reconciled with a host's existing action-tree nodes
/// (ClaimableItemSetup, CampaignVinEntries) so that permissions already granted there continue to resolve.
/// Claiming / ClaimableItemCertifying / ClaimableItemInvoicing / PostClaimModification are not here;
/// they stay in the host's action tree.
/// </summary>
[ActionTree("Claimable Items", "Claimable Items Module Permissions")]
public class ClaimableItemsActionTree
{
    public readonly static ReadWriteDeleteAction ClaimableItemSetup = new("Claimable Item Setup");
    public readonly static ReadWriteDeleteAction CampaignVinEntries = new("Campaign Vin Entries");
    public readonly static ReadWriteDeleteAction ClaimableItemContracts = new("Service Contracts");
    public readonly static BooleanAction IssueClaimableItemContracts = new("Issue Service Contracts");
    public readonly static BooleanAction CancelClaimableItemContracts = new("Cancel Service Contracts");
    public readonly static BooleanAction PrintClaimableItemContracts = new("Print Service Contracts");
    public readonly static BooleanAction ClaimClaimableItemContracts = new("Claim Service Contracts");
}

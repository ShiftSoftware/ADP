using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ToyotaLoyaltyProgramRedeemableItemModelCostCosmosModel
{
    public long ToyotaLoyaltyProgramRedeemableItemModelCostId { get; set; }

    public long? ToyotaLoyaltyProgramRedeemableItemId { get; set; }

    public string Variant { get; set; }

    public long? CostInCents { get; set; }

    public string Katashiki { get; set; }
    public string MenuCode { get; set; }
}

using System;
using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class TLPPackageInvoiceTLPItemCosmosModel
{
    public long ID { get; set; }

    public long TLPPackageInvoiceId { get; set; }

    public long ToyotaLoyaltyProgramRedeemableItemId { get; set; }

    public decimal Cost { get; set; }

    public DateTime? ExpireDate { get; set; }

    public decimal PriceWithIncrement { get; set; }

    public string MenuCode { get; set; }

    public ToyotaLoyaltyProgramRedeemableItemCosmosModel RedeemableItem { get; set; }
}

using System;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ToyotaLoyaltyProgramTransactionLineCosmosModel
{
    public string id { get; set; } = default!;

    public long ToyotaLoyaltyProgramTransactionLineId { get; set; }

    public long? ToyotaLoyaltyProgramTransactionId { get; set; }

    public string DealerCode { get; set; }

    public string BranchCode { get; set; }

    public DateTime? EntryDate { get; set; }

    public int? TransactionStatus { get; set; }

    public decimal? TLP { get; set; }

    public decimal? CustomerDollars { get; set; }

    public int? TransactionType { get; set; }

    public decimal? RewardPercentage { get; set; }

    public decimal? RewardedDollars { get; set; }

    public int? PointType { get; set; }

    public string SerialNumber { get; set; }

    public int? InvoiceStatus { get; set; }

    public long? TLPInvoiceId { get; set; }

    public long? TLPPaymentId { get; set; }

    public long? ClaimedByDivisionId { get; set; }

    public long? ClaimedByUserBranchId { get; set; }

    public decimal? TLPRate { get; set; }

    public string Invoice { get; set; }

    public string WIP { get; set; }

    public string VIN { get; set; }

    public string CustomerName { get; set; }

    public string Mobile { get; set; }

    public string RedeemableItemIdRef { get; set; }

    public string RedeemableItemName { get; set; }

    public DateTime? ValidationDate { get; set; }

    public DateTime? CollectionDate { get; set; }

    public long? DivisionIdToCharge { get; set; }

    public int? PointSource { get; set; }

    public long? DivisionIdToPay { get; set; }

    public decimal? PayableItemCost { get; set; }

    public long? ToyotaAppUserTIQId { get; set; }

    public int? TLPContributor { get; set; }

    public int Franchise { get; set; }

    public Franchises Brand { get; set; }
    public string BrandIntegrationID { get; set; }

    public decimal? OriginalPayableItemCost { get; set; }

    public int? RedeemType { get; set; }

    public decimal? AvailableTLP { get; set; }

    public int? RedeemSubType { get; set; }

    public DateTime? LastReplicationDate { get; set; }

    public string ItemType => "ToyotaLoyaltyProgramTransactionLine";

    public virtual ToyotaLoyaltyProgramTransactionCosmosModel ToyotaLoyaltyProgramTransaction { get; set; }

    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}

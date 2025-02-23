using System;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ToyotaLoyaltyProgramTransactionCosmosModel
{
    public long ToyotaLoyaltyProgramTransactionId { get; set; }

    public DateTime? TransactionDate { get; set; }

    public int? SourceSystem { get; set; }

    public long? AutoExpertTrnsactionId { get; set; }

    public string? DealerCode { get; set; }

    public string? BranchCode { get; set; }

    public string? WIP { get; set; }

    public string? Invoice { get; set; }

    public int? TransactionStatus { get; set; }

    public DateTime? ValidationDate { get; set; }

    public decimal? TLP { get; set; }

    public decimal? RoundedTLP { get; set; }

    public int? TransactionType { get; set; }

    public decimal? LaborTotal { get; set; }

    public decimal? PartsTotal { get; set; }

    public decimal? VSTotal { get; set; }

    public int? InvoiceType { get; set; }

    public string? QrData { get; set; }

    public string? CustomerName { get; set; }

    public string? AutolineCustomerAddress1 { get; set; }

    public string? AutolineCustomerAddress2 { get; set; }

    public string? Mobile { get; set; }

    public string? AutolineAccount { get; set; }

    public string? AutolineSalesType { get; set; }

    public string? VIN { get; set; }

    public string? AutolineRegisterationNumber { get; set; }

    public DateTime? AutolineInvoiceDate { get; set; }

    public int? AutolineMileage { get; set; }

    public string? AutolineEnquiryNumber { get; set; }

    public int? InvoiceStatus { get; set; }

    public long? InvoiceId { get; set; }

    public long? OriginalToyotaLoyaltyProgramTransactionId { get; set; }

    public string? QRBarcodePath { get; set; }

    public string? QRBarcodeGuid { get; set; }

    public long? ToyotaLoyaltyProgramRedeemableItemId { get; set; }

    public string? RedeemableItemName { get; set; }

    public decimal? PayableToDealer { get; set; }

    public long? ClaimedByDivisionId { get; set; }

    public long? ClaimedByUserBranchId { get; set; }

    public decimal? TLPRate { get; set; }

    public decimal? LaborTLP { get; set; }

    public decimal? PartsTLP { get; set; }

    public decimal? VSTLP { get; set; }

    public decimal? LaborRewardDollars { get; set; }

    public decimal? PartsRewardDollars { get; set; }

    public decimal? VSRewardDollars { get; set; }

    public decimal? LaborRewardPercentage { get; set; }

    public decimal? PartsRewardPercentage { get; set; }

    public string? DealerName { get; set; }

    public string? BranchName { get; set; }

    public long? ToyotaAppUserTIQId { get; set; }

    public DateTime? TransactionTimeStamp_UpToSeconds { get; set; }

    public string? AutoExpertTransactionToken { get; set; }

    public decimal? RedeemableItemCost { get; set; }

    public string? VSVariantCode { get; set; }

    public string? VSModelCode { get; set; }

    public string? RewardedToyotaLoyaltyProgramTransactionLineSerialNumber { get; set; }

    public int? RedeemType { get; set; }

    public int? RedeemableAt { get; set; }

    public DateTime? ModificationDate { get; set; }

    public string? CampaingCode { get; set; }

    public int CreditNoteStatus { get; set; }

    public string? RedeemableItemBarcode { get; set; }

    public long? BrokerAppTransactionId { get; set; }

    public string? BrokerAppTransactionToken { get; set; }

    public decimal? ReportedInvoiceAmount { get; set; }

    public string? RedeemableItemDescription { get; set; }

    public int? PushNotificationStatus { get; set; }

    public string? OriginalInvoiceNumber { get; set; }

    public string? Notes { get; set; }

    public int Franchise { get; set; }

    public Brands Brand { get; set; }
    public string BrandIntegrationID { get; set; }

    public string? ServiceCodes { get; set; }

    public string? RedeemCardQRBarcodePath { get; set; }

    public string? RedeemCardQRBarcodeGuid { get; set; }

    public string? RedeemProgressDealerCode { get; set; }

    public string? RedeemProgressBranchCode { get; set; }

    public bool SkipZeroTrust { get; set; }

    public DateTime? RedeemExpiryDate { get; set; }

    public bool AppUserDealerLinkEvaluated { get; set; }

    public long? ScannedByPortalUserId { get; set; }

    public Guid? NPSSurveyId { get; set; }

    public string? RedeemedInvoice { get; set; }

    public long? PreValidatedTLPRecordId { get; set; }

    public decimal? AdditionalRewardedTLP { get; set; }

    public decimal? AdditionalDealerRewardedDollars { get; set; }

    public string? CustomerIdOnDMS { get; set; }

    public string? VSKatashiki { get; set; }

    public decimal? AdditionalDistributorRewardedDollars { get; set; }

    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}

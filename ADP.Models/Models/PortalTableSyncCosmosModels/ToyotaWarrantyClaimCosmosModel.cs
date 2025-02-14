using System;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ToyotaWarrantyClaimCosmosModel : IDealerDataCSV
{
    public string id { get; set; } = default!;

    public string VIN { get; set; } = default!;

    public string ItemType => "ToyotaWarrantyClaim";

    public long ToyotaWarrantyClaimId { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModificationDate { get; set; }

    public bool? Deleted { get; set; }

    public bool? ArchivedVersion { get; set; }

    public long? OriginalToyotaWarrantyClaimId { get; set; }

    public long? CreatedByUserId { get; set; }

    public long? ModifiedByUserId { get; set; }

    public long? DivisionId { get; set; }

    public string DistCode { get; set; }

    public string Twcno { get; set; }

    public string Sfx { get; set; }

    public string ClaimantCode { get; set; }

    public string InvoiceNo { get; set; }

    public string DealerCode { get; set; }

    public string DealerClaimNo { get; set; }

    public DateTime? DateOfReceipt { get; set; }

    public int? ProcessFlg { get; set; }

    public string WarrantyType { get; set; }

    public int? Franchise { get; set; }

    public Franchises? Brand { get; set; }
    public string? BrandIntegrationID { get; set; }

    public string Ap1 { get; set; }

    public string Ap2 { get; set; }

    public string Ap3 { get; set; }

    public string Ap4 { get; set; }

    public string Ap5 { get; set; }

    public int? Nv { get; set; }

    public int? Fv { get; set; }

    public string VinWmi { get; set; }

    public string VinVds { get; set; }

    public string VinCd { get; set; }

    public string VinVis { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? RepairDate { get; set; }

    public int? Odometer { get; set; }

    public int? Kmflg { get; set; }

    public string RepairOrderNo { get; set; }

    public int? InvoiceCurrency { get; set; }

    public decimal? ExchangeRate { get; set; }

    public string DataId { get; set; }

    public string LaborOperationNoMain { get; set; }

    public decimal? LaborRate { get; set; }

    public decimal? HourTotal { get; set; }

    public decimal? HourTotalTiq { get; set; }

    public decimal? LaborTotalAmount { get; set; }

    public decimal? LaborTotalAmountTiq { get; set; }

    public decimal? SubletTotalAmount { get; set; }

    public string SubletDescription { get; set; }

    public string T1 { get; set; }

    public string T2 { get; set; }

    public string T31 { get; set; }

    public string T32 { get; set; }

    public string T33 { get; set; }

    public string T34 { get; set; }

    public string T35 { get; set; }

    public string T36 { get; set; }

    public string T37 { get; set; }

    public string Condition { get; set; }

    public string Cause { get; set; }

    public string Remedy { get; set; }

    public string OfplocalFlag { get; set; }

    public string Ofp { get; set; }

    public decimal? Prr1 { get; set; }

    public decimal? PartsTotalAmount { get; set; }

    public decimal? PartsTotalAmountTiq { get; set; }

    public decimal? TotalClaimAmount { get; set; }

    public decimal? TotalClaimAmountTiq { get; set; }

    public DateTime? ProcessDate { get; set; }

    public DateTime? TiqprocessDate { get; set; }

    public int? LaborAdjustment { get; set; }

    public int? SubletAdjustment { get; set; }

    public int? PartsAdjustment { get; set; }

    public string DistComment1 { get; set; }

    public int? Twcstatus { get; set; }

    public int? TwctmcStatus { get; set; }

    public string TwcerrorMsg { get; set; }

    public DateTime? AcInstallDate { get; set; }

    public int? AcInstallKm { get; set; }

    public string AcpreviousRepairOrderNo { get; set; }

    public DateTime? AcPreviousRepairDate { get; set; }

    public int? AcPreviousRepairKm { get; set; }

    public string AcPreviousInvoiceNo { get; set; }

    public string AcCurrentInvoiceNo { get; set; }

    public int? Twctype { get; set; }

    public bool? SpecialServiceCampaign { get; set; }

    public long? ToyotaFreeServiceRegisteredVehicleId { get; set; }

    public int? ToyotaFreeServiceBreakPart { get; set; }

    public string DealerComments { get; set; }

    public string Vin { get; set; }

    public string ModelCode { get; set; }

    public int? ProductionYear { get; set; }

    public int? ProductionMonth { get; set; }

    public string Katashiki { get; set; }

    public string OperationType { get; set; }

    public DateTime? RepairCompletionDate { get; set; }

    public string BatteryTestCode11 { get; set; }

    public string BatteryTestCode12 { get; set; }

    public string BatteryTestCode21 { get; set; }

    public string BatteryTestCode22 { get; set; }

    public string Tsb { get; set; }

    public int? SupplierId { get; set; }

    public int? SupplierTwcstatus { get; set; }

    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}

using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[TemporalShiftEntity]
[Index(nameof(ClaimNumber))]
[Index(nameof(VIN))]
[Index(nameof(ProcessDate))]
public class WarrantyClaim : ShiftEntity<WarrantyClaim>,
    IClaim,
    IEntityHasCompany<WarrantyClaim>,
    IEntityHasCompanyBranch<WarrantyClaim>,
    IEntityHasUniqueHash<WarrantyClaim>,
    IShiftEntityReplication
{
    public string ClaimNumber { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string? DealerCode { get; set; } = default!;
    public string? DealerClaimNo { get; set; } = default!;
    public DateTime? DateOfReceipt { get; set; }
    public ProcessFlags ProcessFlg { get; set; }
    public string WarrantyType { get; set; } = default!;
    public string Franchise { get; set; } = default!;

    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP1 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP2 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP3 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP4 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP5 { get; set; }
    public bool NV { get; set; }
    public int? FV { get; set; }
    public string VIN_WMI { get; set; } = default!;
    public string VIN_VDS { get; set; } = default!;
    public string VIN_CD { get; set; } = default!;
    public string VIN_VIS { get; set; } = default!;
    public DateTime? DeliveryDate { get; set; }
    public bool PreDelivery { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }
    public KMFlags KMFlg { get; set; }
    public string RepairOrderNo { get; set; } = default!;
    public string DataID { get; set; } = default!;
    public string? LaborOperationNoMain { get; set; }
    public decimal LaborRate { get; set; }
    public decimal LaborRateJPY { get; set; }


    [Precision(18, 1)]
    public decimal? HourTotal { get; set; }
    [Precision(18, 1)]
    public decimal? HourTotalDistributor { get; set; }
    public decimal? LaborTotalAmount { get; set; }
    public decimal? LaborTotalAmountDistributor { get; set; }
    public decimal? SubletTotalAmount { get; set; }
    public decimal? SubletTotalAmountDistributor { get; set; }
    public string? SubletDescription { get; set; }
    public string? T1 { get; set; }
    public string? T2 { get; set; }
    public string? T3_1 { get; set; }
    public string? T3_2 { get; set; }
    public string? T3_3 { get; set; }
    public string? T3_4 { get; set; }
    public string? T3_5 { get; set; }
    public string? T3_6 { get; set; }
    public string? T3_7 { get; set; }
    public string Condition { get; set; } = default!;
    public string Cause { get; set; } = default!;
    public string Remedy { get; set; } = default!;
    public string? OFPLocalFlag { get; set; }
    public string? OFP { get; set; }
    public decimal? PartsTotalAmount { get; set; }
    public decimal? PartsTotalAmountDistributor { get; set; }
    public decimal? TotalClaimAmount { get; set; }
    public decimal? TotalClaimAmountDistributor { get; set; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? DistributorProcessDate { get; set; }
    public int? LaborAdjustment { get; set; }
    public int? SubletAdjustment { get; set; }
    public int? PartsAdjustment { get; set; }
    public string? DistComment1 { get; set; }
    public ClaimStatus? ClaimStatus { get; set; }
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }
    public DateTime? AcInstallDate { get; set; }
    public int? AcInstallKm { get; set; }
    public string? ACPreviousRepairOrderNo { get; set; }
    public DateTime? AcPreviousRepairDate { get; set; }
    public int? AcPreviousRepairKm { get; set; }
    public string? AcPreviousInvoiceNo { get; set; }
    public string? AcCurrentInvoiceNo { get; set; }
    public bool? SpecialServiceCampaign { get; set; }
    public string? SSCCampaignCode { get; set; }
    public long? FreeServiceRegisteredVehicleId { get; set; }
    public int? FreeServiceBreakPart { get; set; }
    public string? DealerComments { get; set; }
    public string VIN { get; set; } = default!;
    public string? ModelCode { get; set; }
    public int? YearModel { get; set; }
    public string? Katashiki { get; set; }

    public OperationTypes OperationType { get; set; }

    public string? BatteryTestCode11 { get; set; }
    public string? BatteryTestCode12 { get; set; }
    public string? BatteryTestCode21 { get; set; }
    public string? BatteryTestCode22 { get; set; }
    public string? TSB { get; set; }

    public virtual ICollection<WarrantyClaimLaborLine> WarrantyClaimLaborLines { get; set; } = new HashSet<WarrantyClaimLaborLine>();
    public virtual ICollection<WarrantyClaimSubletLine> WarrantyClaimSubletLines { get; set; } = new HashSet<WarrantyClaimSubletLine>();
    public virtual ICollection<WarrantyClaimPartLine> WarrantyClaimPartLines { get; set; } = new HashSet<WarrantyClaimPartLine>();
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }

    //public virtual ICollection<WarrantyClaimCSVEntry> WarrantyClaimCSVEntries { get; set; } = new HashSet<WarrantyClaimCSVEntry>();

    public long? CertificateID { get; set; }
    public virtual Certificate? Certificate { get; set; }



    public int? InvoiceCurrency { get; set; }
    public decimal? PRR1 { get; set; }
    public decimal? LaborExchangeRate { get; set; }
    public decimal? PartExchangeRate { get; set; }
    public decimal? SubletExchangeRate { get; set; }


    public decimal? LaborTotalAmountDistributorJPY { get; set; }
    public decimal? SubletTotalAmountJPY { get; set; }
    public decimal? SubletTotalAmountDistributorJPY { get; set; }
    public decimal? PartsTotalAmountDistributorJPY { get; set; }
    public decimal? TotalClaimAmountDistributorJPY { get; set; }

    public decimal? ManufacturerSettledLaborTotalAmountJPY { get; set; }
    public decimal? ManufacturerSettledSubletTotalAmountJPY { get; set; }
    public decimal? ManufacturerSettledPartsTotalAmountJPY { get; set; }
    public decimal? ManufacturerSettledTotalClaimAmountJPY { get; set; }
    public decimal? ManufacturerSettledTotalClaimAmount { get; set; }


    public long? ManufacturerSettlmentSheetID { get; set; }
    public virtual ManufacturerSettlmentSheet? ManufacturerSettlmentSheet { get; set; }


    /// <summary>
    /// Temp: Only to store cleared invoice number that were manually assigned before Manufacturer Settlement import feature
    /// </summary>
    public string? LegacyInvoiceNumber { get; set; }

    public string? ManufacturerErrorMessage { get; set; }

    public long? ReferenceWarrantyClaimID { get; set; }
    public virtual WarrantyClaim? ReferenceWarrantyClaim { get; set; }

    public string? Attachments { get; set; }
    public bool HasAttachment { get; set; }

    public string? LastReplicationStamp { get; set; }
    public DateTimeOffset? LastReplicationDate { get; set; }

    public string? CalculateUniqueHash()
    {
        return this.ClaimNumber;
    }

    public string GetClaimIdentifier()
    {
        return ClaimNumber;
    }
}

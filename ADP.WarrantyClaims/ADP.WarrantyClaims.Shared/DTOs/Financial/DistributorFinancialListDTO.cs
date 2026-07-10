using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;

/// <summary>
/// The distributor-side financial analytics projection over the warranty claim (list + PDF export).
/// Moved from the original host application (Phase 3 Slice 3.5, D23) with its members genericized to
/// the module vocabulary (ClaimNumber / Distributor* / Manufacturer*) — the entity and this DTO now
/// share names, so the entity -&gt; DTO map is convention-based again (see
/// Data/AutoMapperProfiles/Financial.cs for the few explicit pins that remain).
/// </summary>
public class DistributorFinancialListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string ClaimNumber { get; set; } = default!;
    public string? ReferenceWarrantyClaimNumber { get; set; }
    public string? InvoiceNo { get; set; }
    public string DealerCode { get; set; } = default!;
    public string? DealerClaimNo { get; set; }
    public string WarrantyType { get; set; } = default!;
    public string? RepairOrderNo { get; set; }
    public string? CertificateCertificateNo { get; set; }

    public DateTime? RepairDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool PreDelivery { get; set; }
    public DateTime RepairCompletionDate { get; set; }
    public DateTime? CertificateInvoiceDate { get; set; }
    public string? Franchise { get; set; }
    public decimal? HourTotal { get; set; }
    public decimal? HourTotalDistributor { get; set; }
    public decimal? TotalClaimAmount { get; set; }
    public decimal? TotalClaimAmountDistributor { get; set; }
    public string VIN { get; set; } = default!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus? ClaimStatus { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }


    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    public int? Odometer { get; set; }
    public DateTimeOffset? ProcessDate { get; set; }
    public DateTimeOffset? DistributorProcessDate { get; set; }
    public int? DealerRepairDays { get => ProcessDate is null || RepairDate is null ? null : (ProcessDate - RepairDate)?.Days; }
    public int? DistributorRepairDays { get => DistributorProcessDate is null || RepairDate is null ? null : (DistributorProcessDate - RepairDate)?.Days; }

    public string? LaborOperationNoMain { get; set; }
    public string? OFP { get; set; }
    public string? Condition { get; set; }
    public string? T1 { get; set; }
    public string? T2 { get; set; }
    public string? DealerComments { get; set; }
    public string? DistComment1 { get; set; }
    public decimal? LaborTotalAmount { get; set; }
    public decimal? LaborTotalAmountDistributor { get; set; }
    public decimal? SubletTotalAmount { get; set; }
    public decimal? SubletTotalAmountDistributor { get; set; }
    public decimal? PartsTotalAmount { get; set; }
    public decimal? PartsTotalAmountDistributor { get; set; }
    public decimal? DistributorMargin { get => TotalClaimAmountDistributor - TotalClaimAmount; }
    public decimal? ManufacturerSettledTotalClaimAmount { get; set; }
    public decimal? RealizedGainsAndLosses { get =>  ManufacturerSettledTotalClaimAmount - TotalClaimAmount; }
    public decimal? AllGainsAndLosses { get =>  (ManufacturerSettledTotalClaimAmount ?? 0m) - TotalClaimAmount; }
    public decimal? OutstandingDealerPayment { get => this.ClaimStatus == ShiftSoftware.ADP.Models.Enums.ClaimStatus.Invoiced ? 0m : TotalClaimAmount; }
}

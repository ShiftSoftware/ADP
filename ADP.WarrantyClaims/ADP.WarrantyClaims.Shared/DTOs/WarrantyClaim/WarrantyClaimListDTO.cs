using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

[ShiftEntityKeyAndName(nameof(ID), nameof(ClaimNumber))]
public class WarrantyClaimListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string ClaimNumber { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string DealerCode { get; set; } = default!;
    public string WarrantyType { get; set; } = default!;
    public DateTime? RepairDate { get; set; }
    public int? DealerRepairDays { get => ProcessDate is null || RepairDate is null ? null : (ProcessDate - RepairDate)?.Days; }
    public int? DistributorRepairDays { get => DistributorProcessDate is null || RepairDate is null ? null : (DistributorProcessDate - RepairDate)?.Days; }
    public DateTime RepairCompletionDate { get; set; }
    public decimal? HourTotal { get; set; }
    public decimal? HourTotalDistributor { get; set; }
    public decimal? TotalClaimAmount { get; set; }
    public decimal? TotalClaimAmountDistributor { get; set; }
    public DateTimeOffset? ProcessDate { get; set; }
    public DateTimeOffset? DistributorProcessDate { get; set; }
    public string VIN { get; set; } = default!;

    public DateTime? DeliveryDate { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus? ClaimStatus { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string? CompanyID { get; set; }
    public string? ManufacturerErrorMessage { get; set; }

    public string? SSCCampaignCode { get; set; }

    public string? ReferenceWarrantyClaimID { get; set; }
    public string? ReferenceWarrantyClaimNumber { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public YesNoOptions HasAttachment { get; set; }
}

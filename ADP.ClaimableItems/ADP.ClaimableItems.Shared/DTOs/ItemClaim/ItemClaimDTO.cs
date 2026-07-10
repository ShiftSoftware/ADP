using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;

/// <summary>
/// Item-claim form/view DTO. Moved VERBATIM from the original host application's Services.Shared.DTOs.ItemClaim (Phase 2
/// Slice 5) — property names/converters are frozen (admin UI + UpdateStatus flows).
/// </summary>
public class ItemClaimDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public int ClaimNumber { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public DateTimeOffset? ClaimDate { get; set; }
    public string? JobNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? QRCode { get; set; }
    public string? ModelDescription { get; set; }
    public string? Katashiki { get; set; }
    public ShiftEntitySelectDTO? Campaign { get; set; }
    public ShiftEntitySelectDTO? ClaimableItem { get; set; }
    public ShiftEntitySelectDTO? VehicleInspectionResult { get; set; }
    public ShiftEntitySelectDTO? CampaignVINEntry { get; set; }
    public decimal? Cost { get; set; }
    public string? PackageCode { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public ShiftEntitySelectDTO? Company { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public ShiftEntitySelectDTO? CompanyBranch { get; set; }

    public List<ShiftFileDTO>? Attachments { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus? ClaimStatus { get; set; }

    public string? DistributorErrorMessage { get; set; }

    public bool ReSubmitForDistributorReview { get; set; }
}

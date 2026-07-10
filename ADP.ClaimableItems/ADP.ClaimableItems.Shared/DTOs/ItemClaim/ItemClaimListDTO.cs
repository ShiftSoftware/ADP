using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;

/// <summary>
/// Item-claim list DTO. Moved VERBATIM from the original host application's Services.Shared.DTOs.ItemClaim (Phase 2 Slice 5).
/// NOTE: the VehicleInspectionResult* members flatten a CONSUMER-owned navigation the module entity
/// does not carry — the module's default list projection leaves them null; a consumer that needs
/// them (the original host application does) overrides <c>MapToList</c> in a derived repository with a manual join.
/// </summary>
public class ItemClaimListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public int ClaimNumber { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public DateTimeOffset ClaimDate { get; set; }
    public string? JobNumber { get; set; }
    public string? InvoiceNumber { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? CampaignName { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string ClaimableItemName { get; set; } = default!;


    public string ClaimableItemID { get; set; } = default!;
    public string CampaignID { get; set; } = default!;
    public string VehicleInspectionResultVehicleInspectionTypeID { get; set; } = default!;



    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? VehicleInspectionResultVehicleInspectionTypeName { get; set; }
    public decimal? Cost { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string? CompanyBranchID { get; set; }

    public string? ReimbursementCertificateID { get; set; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? ReimbursementCertificateCertificateDate { get; set; }
    public DateTime? ContributionCertificateCertificateDate { get; set; }
    public DateTime? ReimbursementCertificateInvoiceDate { get; set; }
    public DateTime? ContributionCertificateInvoiceDate { get; set; }

    public string? DistributorErrorMessage { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus? ClaimStatus { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public YesNoOptions HasAttachment { get; set; }
}

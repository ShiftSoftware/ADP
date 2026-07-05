using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;

public class ClaimableItemDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;
    public string? PrintoutTitle { get; set; }
    public string? PrintoutDescription { get; set; }
    public long? MaximumMileage { get; set; }
    public string? UniqueReference { get; set; }
    public string? PackageCode { get; set; }
    public ClaimableItemCostingType CostingType { get; set; }
    public decimal? FixedCost { get; set; }
    public ClaimableItemValidityMode ValidityMode { get; set; } = ClaimableItemValidityMode.RelativeToActivation;
    public int ActiveFor { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DurationType ActiveForDurationType { get; set; }
    public List<ClaimableItemCostDTO> Costs { get; set; } = new();

    [Required]
    public ShiftEntitySelectDTO? Campaign { get; set; }

    public ClaimableItemClaimingMethod ClaimingMethod { get; set; } = ClaimableItemClaimingMethod.ClaimByScanningQRCode;
    public ClaimableItemAttachmentFieldBehavior AttachmentFieldBehavior { get; set; } = ClaimableItemAttachmentFieldBehavior.Hidden;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class ClaimableItemCostDTO
{
    public string? Katashiki { get; set; }
    public string? Variant { get; set; }
    public decimal? Cost { get; set; }
    public string? PackageCode { get; set; }
}

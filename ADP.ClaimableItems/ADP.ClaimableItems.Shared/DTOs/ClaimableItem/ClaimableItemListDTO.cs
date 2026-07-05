using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ClaimableItemListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string? CampaignID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CampaignName { get; set; } = default!;
    public string? UniqueReference { get; set; } = default!;
    public DateTime? CampaignStartDate { get; set; }
    public DateTime? CampaignExpireDate { get; set; }
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? PrintoutTitle { get; set; }
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? PrintoutDescription { get; set; }
    public int ActiveFor { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DurationType ActiveForDurationType { get; set; }
    public long? MaximumMileage { get; set; }
    public string? PackageCode { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemValidityMode ValidityMode { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public string Validity
    {
        get =>
            ValidityMode == ClaimableItemValidityMode.RelativeToActivation ?
            $"{ActiveFor} {ActiveForDurationType}" :
            $"{ValidFrom?.ToString("yyyy-MM-dd")} -to- {ValidTo?.ToString("yyyy-MM-dd")}";
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }
    public string ActivationTriggerText { get => this.CampaignActivationTrigger.Describe(); }
    public string ActivationTypeText { get => this.CampaignActivationType.Describe(); }
    public string ValidityModeText { get => this.ValidityMode.Describe(); }
}

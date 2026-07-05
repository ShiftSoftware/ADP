using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;


[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CampaignListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? UniqueReference { get; set; } = default!;
    public DateTime? StartDate { get; set; }
    public DateTime? ExpireDate { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTrigger ActivationTrigger { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemCampaignActivationTypes ActivationType { get; set; }

    public string ActivationTriggerText { get => this.ActivationTrigger.Describe(); }
    public string ActivationTypeText { get => this.ActivationType.Describe(); }
}

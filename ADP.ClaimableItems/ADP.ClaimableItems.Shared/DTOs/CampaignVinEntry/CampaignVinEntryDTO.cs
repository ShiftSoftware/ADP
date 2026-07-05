using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;

public class CampaignVinEntryDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    public string VIN { get; set; } = default!;

    [Required]
    public ShiftEntitySelectDTO? Campaign { get; set; }

    [Required]
    public DateTime? RecordedDate { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public ShiftEntitySelectDTO? Company { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public ShiftEntitySelectDTO? CompanyBranch { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public ShiftEntitySelectDTO? Country { get; set; }

    [JsonIgnore]
    public bool DisableVinValidation { get; set; }
}

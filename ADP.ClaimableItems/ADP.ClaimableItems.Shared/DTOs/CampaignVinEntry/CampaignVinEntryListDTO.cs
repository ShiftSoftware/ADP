using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;

public class CampaignVinEntryListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    public string VIN { get; set; } = default!;

    public string CampaignID { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? CampaignName { get; set; }

    public string? CampaignUniqueReference { get; set; }

    public DateTime RecordedDate { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string? CompanyBranchID { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public string? CountryID { get; set; }
}

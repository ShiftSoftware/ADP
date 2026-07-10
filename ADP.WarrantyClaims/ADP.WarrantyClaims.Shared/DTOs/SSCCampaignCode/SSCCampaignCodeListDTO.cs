using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.SSCCampaignCode;

[ShiftEntityKeyAndName(nameof(ID), nameof(Code))]
public class SSCCampaignCodeListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string Code { get; set; } = default!;
}

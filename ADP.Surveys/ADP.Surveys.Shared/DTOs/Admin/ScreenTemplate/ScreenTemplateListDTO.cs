using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.ScreenTemplate;

public class ScreenTemplateListDTO : ShiftEntityListDTO
{
    [ScreenTemplateHashId]
    public override string? ID { get; set; }

    public string Key { get; set; } = default!;

    public int QuestionCount { get; set; }
}

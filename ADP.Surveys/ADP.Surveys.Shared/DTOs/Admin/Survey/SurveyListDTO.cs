using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Survey;

public class SurveyListDTO : ShiftEntityListDTO
{
    [SurveyHashId]
    public override string? ID { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>Optional friendly identifier for external integrations. Unique across non-deleted rows.</summary>
    public string? IntegrationId { get; set; }

    /// <summary>Null before the survey has ever been published.</summary>
    public int? PublishedVersionNumber { get; set; }
}

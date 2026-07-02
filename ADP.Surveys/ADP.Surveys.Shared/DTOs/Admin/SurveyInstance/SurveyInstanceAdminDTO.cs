using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;

/// <summary>
/// Exists only to satisfy the framework generics — <c>SurveyInstanceController</c>
/// blocks every single-item verb with 405. Instance detail for the dashboard flows
/// through the richer <c>SurveyResponses/instance/{publicId}</c> endpoint instead
/// (responses + answers + pinned ResolvedJson).
/// </summary>
public class SurveyInstanceAdminDTO : ShiftEntityViewAndUpsertDTO
{
    [SurveyInstanceHashId]
    public override string? ID { get; set; }
}

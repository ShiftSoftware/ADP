using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.SurveyInstance;

/// <summary>
/// OData list projection for the dashboard's per-survey Responses grid (ShiftList).
/// Read-only — instances are created by triggers or the test-run action and answered
/// through the public endpoints; see <c>SurveyInstanceController</c> which exposes
/// ONLY the list Get.
/// </summary>
public class SurveyInstanceListDTO : ShiftEntityListDTO
{
    [SurveyInstanceHashId]
    public override string? ID { get; set; }

    /// <summary>Hashed FK — the Responses page pins its ShiftList filter to this.</summary>
    [SurveyHashId]
    public string? SurveyID { get; set; }

    /// <summary>Public URL anchor; the row actions (answer / view answers / copy link) key off it.</summary>
    public Guid PublicID { get; set; }

    public DateTimeOffset TriggeredAt { get; set; }

    public string? TriggeredBy { get; set; }

    /// <summary>True when <see cref="TriggeredBy"/> is <c>SurveysConstants.DashboardTestTriggerSource</c>.</summary>
    public bool IsTest { get; set; }

    public string? Channel { get; set; }

    public string? RecipientAddress { get; set; }

    /// <summary>
    /// <c>SurveyInstanceStatus</c> ordinal (0 Pending, 1 Sent, 2 Opened, 3 Completed,
    /// 4 Expired). Kept numeric so OData filter/sort stay server-side — the enum lives
    /// in the Data assembly, which this Shared DTO can't reference.
    /// </summary>
    public int Status { get; set; }

    /// <summary>The pinned <c>SurveyVersion.Version</c> this instance validates against.</summary>
    public int SchemaVersion { get; set; }

    public int ResponseCount { get; set; }

    /// <summary>Latest response's completion time, when any.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.Responses;

/// <summary>
/// Transport DTOs for the dashboard's per-survey Responses view (inspect recorded
/// answers, create test instances). Grouped in one file — they are one feature's
/// wire shapes, not framework CRUD DTOs, and never round-trip through
/// <c>ShiftEntityForm</c>. (The instance LIST is a proper ShiftList/OData resource —
/// see <c>SurveyInstance.SurveyInstanceListDTO</c>.)
/// </summary>
public class PublicUrlTemplateDTO
{
    /// <summary><c>SurveyApiOptions.PublicSurveyUrlTemplate</c> with its <c>{publicId}</c> placeholder; null when unset.</summary>
    public string? Template { get; set; }
}

public class SurveyInstanceSummaryDTO
{
    public Guid PublicId { get; set; }

    /// <summary><c>SurveyInstanceStatus</c> name — string so the Blazor client doesn't need the Data assembly.</summary>
    public string Status { get; set; } = default!;

    public DateTimeOffset TriggeredAt { get; set; }

    public string? TriggeredBy { get; set; }

    /// <summary>True when <see cref="TriggeredBy"/> is <see cref="SurveysConstants.DashboardTestTriggerSource"/>.</summary>
    public bool IsTest { get; set; }

    public string? Channel { get; set; }
    public string? RecipientAddress { get; set; }
    public string? RecipientLocale { get; set; }
    public string? CustomerRef { get; set; }

    /// <summary>The pinned <c>SurveyVersion.Version</c> this instance validates against.</summary>
    public int SchemaVersion { get; set; }

    public int ResponseCount { get; set; }

    /// <summary>Latest response's completion time, when any.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Recipient-facing link composed from <c>SurveyApiOptions.PublicSurveyUrlTemplate</c>.</summary>
    public string? PublicUrl { get; set; }
}

public class SurveyInstanceDetailDTO
{
    public SurveyInstanceSummaryDTO Instance { get; set; } = default!;

    /// <summary>
    /// The pinned version's fully-resolved schema JSON, verbatim. The client
    /// deserializes it with <c>SurveySchemaSerializer.Options</c> and maps answers
    /// to question titles / option labels against it.
    /// </summary>
    public string ResolvedJson { get; set; } = default!;

    public List<SurveyResponseItemDTO> Responses { get; set; } = new();
}

public class SurveyResponseItemDTO
{
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? AgentId { get; set; }
    public List<SurveyAnswerItemDTO> Answers { get; set; } = new();
}

public class SurveyAnswerItemDTO
{
    /// <summary>Answer key as submitted (bank key or survey-local question id).</summary>
    public string Key { get; set; } = default!;

    /// <summary>Stable BI anchor for banked questions; null for inline.</summary>
    public Guid? BankEntryId { get; set; }

    /// <summary>Raw answer value JSON (scalar or array), exactly as stored.</summary>
    public string ValueJson { get; set; } = default!;
}

public class CreateTestInstanceResultDTO
{
    public Guid PublicId { get; set; }
    public int SchemaVersion { get; set; }
    public string? PublicUrl { get; set; }
}

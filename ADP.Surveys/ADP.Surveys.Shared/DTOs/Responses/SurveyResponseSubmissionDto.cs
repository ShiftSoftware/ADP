using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Responses;

/// <summary>
/// Body of <c>POST /surveys/instances/{instanceId}/responses</c>. Same shape regardless
/// of channel (public renderer, agent-assist iframe, deferred AI path).
/// </summary>
public class SurveyResponseSubmissionDto
{
    /// <summary>
    /// The schema version the renderer hydrated against. The API validates answers
    /// against this pinned version. Must match the version the instance was created with.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Answers keyed by question id (banked ids and survey-local ids coexist). Values
    /// are kept as <see cref="JsonElement"/> because the answer CLR type varies per
    /// question type (string, number, bool, string[], etc.).
    /// </summary>
    [JsonPropertyName("answers")]
    public Dictionary<string, JsonElement> Answers { get; set; } = new();

    [JsonPropertyName("meta")]
    public ResponseMetaDto? Meta { get; set; }
}

public class SurveyResponseSubmissionDtoValidator : AbstractValidator<SurveyResponseSubmissionDto>
{
    public SurveyResponseSubmissionDtoValidator()
    {
        RuleFor(x => x.SchemaVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Answers).NotNull();
        When(x => x.Meta is not null, () =>
            RuleFor(x => x.Meta!).SetValidator(new ResponseMetaDtoValidator()));
        // Per-answer shape checks (answer matches the question's type and validation)
        // live in the domain-layer AnswerValidator — those need the resolved schema,
        // which FluentValidation doesn't carry here.
    }
}

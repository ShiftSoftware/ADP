using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Logic;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

/// <summary>
/// Declarative trigger config embedded on a survey. One survey may carry many
/// triggers; each one matches a specific upstream <see cref="EventKind"/>, runs
/// its <see cref="Filter"/> against the candidate event payload, computes a
/// dedup key from <see cref="DedupRecipe"/>, materialises a <c>SurveyInstance</c>
/// (slice 2+), and is delivered via the <see cref="Channel"/> on the schedule
/// declared by <see cref="Schedule"/>.
/// </summary>
public class TriggerDto
{
    /// <summary>Stable within the survey. Used as <c>SurveyInstance.TriggerId</c>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>Off-switch — a published survey can carry inactive triggers.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>e.g. <c>"service-visit-closed"</c> — keyed against ingest dispatcher.</summary>
    [JsonPropertyName("eventKind")]
    public string EventKind { get; set; } = "";

    /// <summary>
    /// Optional condition evaluated against the candidate event payload + recipient.
    /// Reuses the navigation logic-rule condition shape so authors learn one syntax.
    /// </summary>
    [JsonPropertyName("filter")]
    public LogicConditionDto? Filter { get; set; }

    /// <summary>
    /// Field references whose resolved values are SHA-256'd to populate
    /// <c>SurveyInstance.UniqueHash</c>. Must include the natural key of the
    /// event for the recipient (e.g. <c>candidate.dealerId</c> + <c>candidate.wip</c>
    /// for service-visit events) — that's what makes "process each event at
    /// most once per recipient" the dedup semantic.
    /// </summary>
    [JsonPropertyName("dedupRecipe")]
    public List<string> DedupRecipe { get; set; } = new();

    [JsonPropertyName("schedule")]
    public TriggerScheduleDto Schedule { get; set; } = new();

    /// <summary>DI key resolved against registered <c>ISurveyChannel</c> implementations.</summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "";
}

/// <summary>
/// Draft-time validator. Structural checks only — empties are fine on a draft
/// so the builder can persist work-in-progress. Publish-gate rules live in
/// <see cref="TriggerPublishValidator"/>, which runs alongside this validator
/// on publish.
/// </summary>
public class TriggerDtoValidator : AbstractValidator<TriggerDto>
{
    public TriggerDtoValidator()
    {
        When(x => x.Filter is not null, () =>
            RuleFor(x => x.Filter!).SetValidator(new LogicConditionDtoValidator()));

        RuleFor(x => x.Schedule).NotNull().SetValidator(new TriggerScheduleDtoValidator());
    }
}

/// <summary>
/// Publish-time validator. Adds required-field rules — runs alongside
/// <see cref="TriggerDtoValidator"/>, not in place of it.
/// </summary>
public class TriggerPublishValidator : AbstractValidator<TriggerDto>
{
    public TriggerPublishValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Trigger id is required.");
        RuleFor(x => x.EventKind).NotEmpty().WithMessage("Trigger eventKind is required.");
        RuleFor(x => x.DedupRecipe).NotEmpty()
            .WithMessage("Trigger dedupRecipe must include at least one field reference.");
        RuleForEach(x => x.DedupRecipe).NotEmpty()
            .WithMessage("Trigger dedupRecipe entries must be non-empty.");
        RuleFor(x => x.Channel).NotEmpty().WithMessage("Trigger channel is required.");
        RuleFor(x => x.Schedule.InitialDelay).NotEmpty()
            .WithMessage("Trigger schedule.initialDelay is required.");
    }
}

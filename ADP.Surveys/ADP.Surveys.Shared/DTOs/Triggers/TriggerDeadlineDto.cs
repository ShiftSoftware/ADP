using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

/// <summary>
/// How late is too late. The event's own date (<see cref="Field"/>, e.g. the visit's close or the
/// vehicle's handover) plus <see cref="Within"/> is the last moment a survey from this trigger is
/// created or sent — whatever made it late: the record being back-entered, re-sent by the source,
/// or held up by a stalled pipeline.
///
/// It complements the pull cursor rather than replacing it. The cursor guarantees a record that
/// arrives late is still SEEN (a "survey whatever is exactly N days old today" job loses it for
/// good); the deadline decides whether it is still worth surveying. A fixed eligibility-floor
/// filter cannot do this job: it stops history from before go-live, but a record from after
/// go-live that turns up months late still passes it.
///
/// Measured from the event's date, never from when the record reached the system — otherwise a
/// bulk import of old records, all freshly arrived, would pass.
/// </summary>
public class TriggerDeadlineDto
{
    /// <summary>
    /// <c>candidate.X</c> — a top-level payload property holding the event's date as an ISO-8601
    /// string. A value without an offset is read as UTC.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";

    /// <summary>A duration like <c>7d</c> (see <see cref="TriggerDuration"/>).</summary>
    [JsonPropertyName("within")]
    public string Within { get; set; } = "";
}

/// <summary>
/// Publish-time rules. A deadline is optional, but a published one must be complete: a
/// half-authored deadline would refuse every event. Drafts only check the duration's format
/// (<see cref="TriggerDtoValidator"/>) so the builder can save work in progress.
/// </summary>
public class TriggerDeadlineDtoValidator : AbstractValidator<TriggerDeadlineDto>
{
    public const string CandidatePrefix = "candidate.";

    public TriggerDeadlineDtoValidator()
    {
        RuleFor(x => x.Field)
            .Must(f => f is not null
                       && f.StartsWith(CandidatePrefix, StringComparison.Ordinal)
                       && f.Length > CandidatePrefix.Length)
            .WithMessage("Deadline field must name the event's date as candidate.<field>.");

        RuleFor(x => x.Within)
            .Must(s => TriggerDuration.TryParse(s, out var d) && d > TimeSpan.Zero)
            .WithMessage("Deadline 'within' must be a positive duration like '7d'.");
    }
}

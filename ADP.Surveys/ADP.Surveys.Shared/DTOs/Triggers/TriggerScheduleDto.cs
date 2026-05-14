using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

/// <summary>
/// First-send delay (from event timestamp) plus optional reminder delays
/// (each is delay-since-previous-send). Durations parsed at instance creation
/// via <see cref="TriggerDuration.TryParse"/> and stored as <see cref="DateTimeOffset"/>
/// on the instance row — never re-parsed in the scheduler hot path.
/// </summary>
public class TriggerScheduleDto
{
    [JsonPropertyName("initialDelay")]
    public string InitialDelay { get; set; } = "0d";

    [JsonPropertyName("reminders")]
    public List<string> Reminders { get; set; } = new();
}

public class TriggerScheduleDtoValidator : AbstractValidator<TriggerScheduleDto>
{
    public TriggerScheduleDtoValidator()
    {
        RuleFor(x => x.InitialDelay)
            .Must(s => TriggerDuration.TryParse(s, out _))
            .When(x => !string.IsNullOrEmpty(x.InitialDelay))
            .WithMessage("initialDelay must be a duration like '2d', '4h', '15m', '0d'.");

        RuleForEach(x => x.Reminders)
            .Must(s => TriggerDuration.TryParse(s, out _))
            .WithMessage("Each reminder must be a duration like '5d', '4h', '15m'.");
    }
}

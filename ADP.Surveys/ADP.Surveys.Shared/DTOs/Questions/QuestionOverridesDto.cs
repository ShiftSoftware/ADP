using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

/// <summary>
/// Presentation-only overrides a survey may apply to a banked question.
/// Per Decision #9: ID, type, validation and answer shape are locked at the bank;
/// only these surface-level fields are overridable.
/// </summary>
public class QuestionOverridesDto
{
    [JsonPropertyName("title")]
    public LocalizedString? Title { get; set; }

    [JsonPropertyName("help")]
    public LocalizedString? Help { get; set; }

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }

    /// <summary>
    /// Per-option label overrides for choice types. Keyed by option id.
    /// </summary>
    [JsonPropertyName("optionLabels")]
    public Dictionary<string, LocalizedString>? OptionLabels { get; set; }

    /// <summary>
    /// Query-param overrides for a banked question's <c>optionsSource</c>, merged over
    /// the bank's own <c>queryParams</c> (override wins per key). This is how one bank
    /// entry (one BI column) serves endpoint variations — e.g. <c>services=body-and-paint</c>
    /// vs <c>services=new-vehicle-sale</c> on the same branch-list API. The URL and
    /// headers stay locked at the bank. Ignored when the banked question has no source.
    /// </summary>
    [JsonPropertyName("sourceParams")]
    public Dictionary<string, string>? SourceParams { get; set; }
}

public class QuestionOverridesDtoValidator : AbstractValidator<QuestionOverridesDto>
{
    public QuestionOverridesDtoValidator()
    {
        When(x => x.Title is not null, () =>
            RuleFor(x => x.Title!).SetValidator(new LocalizedStringValidator()));
        When(x => x.Help is not null, () =>
            RuleFor(x => x.Help!).SetValidator(new LocalizedStringValidator()));
        When(x => x.OptionLabels is not null, () =>
        {
            RuleForEach(x => x.OptionLabels!).ChildRules(pair =>
            {
                pair.RuleFor(p => p.Key).NotEmpty();
                pair.RuleFor(p => p.Value).NotNull().SetValidator(new LocalizedStringValidator());
            });
        });
        When(x => x.SourceParams is not null, () =>
            RuleForEach(x => x.SourceParams!).ChildRules(pair =>
                pair.RuleFor(p => p.Key).NotEmpty().WithMessage("sourceParams keys must be non-empty.")));
    }
}

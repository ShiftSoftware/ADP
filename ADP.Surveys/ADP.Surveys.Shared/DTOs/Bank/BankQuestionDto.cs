using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;

/// <summary>
/// A canonical banked question. The stable <see cref="Id"/> travels with every survey
/// that references the entry — that's what gives BI a consistent join key across surveys.
///
/// Once any published survey references a bank entry, it becomes effectively
/// append-only: corrections produce a new id, never a mutation of the existing one.
/// The <see cref="Locked"/> flag tracks whether this threshold has been crossed.
/// </summary>
public class BankQuestionDto
{
    /// <summary>
    /// Stable cross-survey id. Also the answer key in submitted responses.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// The full canonical question definition (type, title, validation, options, etc.).
    /// Presentation fields may be overridden per referencing survey per Decision #9;
    /// the type / validation / answer shape are frozen at the bank.
    /// </summary>
    [JsonPropertyName("question")]
    public QuestionDto Question { get; set; } = default!;

    /// <summary>
    /// Optional friendly BI column name. Defaults to the id if absent.
    /// </summary>
    [JsonPropertyName("biColumn")]
    public string? BiColumn { get; set; }

    /// <summary>
    /// True once any published survey references this entry. Enforced server-side on edits.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    /// <summary>
    /// Marked-retired entries stay in the bank for historical exports but are hidden
    /// from the "insert from bank" picker in the builder.
    /// </summary>
    [JsonPropertyName("retired")]
    public bool Retired { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class BankQuestionDtoValidator : AbstractValidator<BankQuestionDto>
{
    public BankQuestionDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Question).NotNull().SetInheritanceValidator(v =>
        {
            v.Add(new TextQuestionDtoValidator());
            v.Add(new ParagraphQuestionDtoValidator());
            v.Add(new NumberQuestionDtoValidator());
            v.Add(new RatingQuestionDtoValidator());
            v.Add(new NpsQuestionDtoValidator());
            v.Add(new SingleChoiceQuestionDtoValidator());
            v.Add(new MultiChoiceQuestionDtoValidator());
            v.Add(new DropdownQuestionDtoValidator());
            v.Add(new DateQuestionDtoValidator());
            v.Add(new DateTimeQuestionDtoValidator());
            v.Add(new FileQuestionDtoValidator());
            v.Add(new SignatureQuestionDtoValidator());
            v.Add(new YesNoQuestionDtoValidator());
            v.Add(new NavigationListQuestionDtoValidator());
        });
        RuleFor(x => x).Must(x => x.Question.Id == x.Id)
            .When(x => x.Question is not null && !string.IsNullOrEmpty(x.Id))
            .WithMessage("BankQuestion.Id must match its inner Question.Id — the id is the bank's stable anchor.");
    }
}

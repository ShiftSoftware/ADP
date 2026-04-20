using System.Text.Json.Serialization;
using FluentValidation;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;

/// <summary>
/// A slot inside a screen's <c>questions</c> array. Carries either an inline
/// <see cref="QuestionDto"/> (resolved form) or a <see cref="QuestionRefDto"/> (draft form).
///
/// Discriminated by presence of <c>bankRef</c> — see <see cref="QuestionEntryDtoConverter"/>.
/// Exactly one of <see cref="Inline"/> and <see cref="Ref"/> is non-null.
/// </summary>
[JsonConverter(typeof(QuestionEntryDtoConverter))]
public class QuestionEntryDto
{
    public QuestionDto? Inline { get; set; }

    public QuestionRefDto? Ref { get; set; }

    [JsonIgnore]
    public bool IsRef => Ref is not null;

    public static QuestionEntryDto FromInline(QuestionDto question) => new() { Inline = question };

    public static QuestionEntryDto FromRef(QuestionRefDto reference) => new() { Ref = reference };
}

public class QuestionEntryDtoValidator : AbstractValidator<QuestionEntryDto>
{
    public QuestionEntryDtoValidator()
    {
        RuleFor(x => x).Must(x => x.Inline is not null ^ x.Ref is not null)
            .WithMessage("QuestionEntry must carry exactly one of Inline or Ref.");

        When(x => x.Ref is not null, () =>
            RuleFor(x => x.Ref!).SetValidator(new QuestionRefDtoValidator()));

        When(x => x.Inline is not null, () =>
        {
            RuleFor(x => x.Inline!).SetInheritanceValidator(v =>
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
        });
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

namespace ShiftSoftware.ADP.Surveys.Shared.Answers;

/// <summary>
/// Runtime answer-shape validation. Given a resolved <see cref="SurveyDto"/> and a
/// submitted answers map (from <c>POST /surveys/instances/{instanceId}/responses</c>),
/// verifies each answer matches its question's type and validation.
///
/// Not FluentValidation-shaped because the check is polymorphic on question type AND
/// needs both the schema and the submitted values together — FluentValidation takes
/// exactly one DTO.
/// </summary>
public static class AnswerValidator
{
    public static IReadOnlyList<AnswerError> Validate(SurveyDto resolved, IReadOnlyDictionary<string, JsonElement> answers)
    {
        var errors = new List<AnswerError>();

        foreach (var screen in resolved.Screens)
        {
            if (screen is not InlineScreenDto inline) continue;
            foreach (var entry in inline.Questions)
            {
                if (entry.Inline is null) continue;
                ValidateAnswer(entry.Inline, answers, errors);
            }
        }

        return errors;
    }

    private static void ValidateAnswer(QuestionDto question, IReadOnlyDictionary<string, JsonElement> answers, List<AnswerError> errors)
    {
        var present = answers.TryGetValue(question.Id, out var value) && value.ValueKind != JsonValueKind.Null;

        if (!present)
        {
            if (question.Required)
                errors.Add(new AnswerError(question.Id, "Answer is required."));
            return;
        }

        switch (question)
        {
            case TextQuestionDto t: ValidateText(t, value, errors); break;
            case ParagraphQuestionDto p: ValidateParagraph(p, value, errors); break;
            case NumberQuestionDto n: ValidateNumber(n, value, errors); break;
            case RatingQuestionDto r: ValidateRating(r, value, errors); break;
            case NpsQuestionDto nps: ValidateNps(nps, value, errors); break;
            case SingleChoiceQuestionDto sc: ValidateOptionId(sc.Id, sc.Options.Select(o => o.Id), value, errors); break;
            case MultiChoiceQuestionDto mc: ValidateMultiChoice(mc, value, errors); break;
            case DropdownQuestionDto dd: ValidateOptionId(dd.Id, dd.Options.Select(o => o.Id), value, errors); break;
            case DateQuestionDto d: ValidateDate(d, value, errors); break;
            case DateTimeQuestionDto dt: ValidateDateTime(dt, value, errors); break;
            case FileQuestionDto f: ValidateString(f.Id, value, errors, "file reference"); break;
            case SignatureQuestionDto s: ValidateString(s.Id, value, errors, "signature data url"); break;
            case YesNoQuestionDto yn: ValidateYesNo(yn, value, errors); break;
            case NavigationListQuestionDto nl: ValidateOptionId(nl.Id, nl.Options.Select(o => o.Id), value, errors); break;
        }
    }

    private static void ValidateText(TextQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new AnswerError(q.Id, "Text answer must be a JSON string."));
            return;
        }
        var s = value.GetString()!;
        if (q.MinLength.HasValue && s.Length < q.MinLength.Value)
            errors.Add(new AnswerError(q.Id, $"Answer length {s.Length} is less than minLength {q.MinLength.Value}."));
        if (q.MaxLength.HasValue && s.Length > q.MaxLength.Value)
            errors.Add(new AnswerError(q.Id, $"Answer length {s.Length} exceeds maxLength {q.MaxLength.Value}."));
        if (!string.IsNullOrEmpty(q.Pattern) && !Regex.IsMatch(s, q.Pattern))
            errors.Add(new AnswerError(q.Id, "Answer does not match the required pattern."));
    }

    private static void ValidateParagraph(ParagraphQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new AnswerError(q.Id, "Paragraph answer must be a JSON string."));
            return;
        }
        var s = value.GetString()!;
        if (q.MinLength.HasValue && s.Length < q.MinLength.Value)
            errors.Add(new AnswerError(q.Id, $"Answer length {s.Length} is less than minLength {q.MinLength.Value}."));
        if (q.MaxLength.HasValue && s.Length > q.MaxLength.Value)
            errors.Add(new AnswerError(q.Id, $"Answer length {s.Length} exceeds maxLength {q.MaxLength.Value}."));
    }

    private static void ValidateNumber(NumberQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            errors.Add(new AnswerError(q.Id, "Number answer must be a JSON number."));
            return;
        }
        var n = value.GetDecimal();
        if (q.Min.HasValue && n < q.Min.Value)
            errors.Add(new AnswerError(q.Id, $"Answer {n} is less than min {q.Min.Value}."));
        if (q.Max.HasValue && n > q.Max.Value)
            errors.Add(new AnswerError(q.Id, $"Answer {n} exceeds max {q.Max.Value}."));
    }

    private static void ValidateRating(RatingQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            errors.Add(new AnswerError(q.Id, "Rating answer must be a JSON number."));
            return;
        }
        var n = value.GetDecimal();
        if (n < 0 || n > q.Max)
            errors.Add(new AnswerError(q.Id, $"Rating {n} is outside 0..{q.Max}."));
        if (!q.AllowHalf && n != Math.Floor(n))
            errors.Add(new AnswerError(q.Id, "Rating does not allow half values."));
    }

    private static void ValidateNps(NpsQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            errors.Add(new AnswerError(q.Id, "NPS answer must be a JSON number."));
            return;
        }
        var n = value.GetInt32();
        if (n < q.Min || n > q.Max)
            errors.Add(new AnswerError(q.Id, $"NPS answer {n} is outside {q.Min}..{q.Max}."));
    }

    private static void ValidateOptionId(string questionId, IEnumerable<string> validIds, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new AnswerError(questionId, "Choice answer must be a JSON string (option id)."));
            return;
        }
        var id = value.GetString()!;
        if (!validIds.Contains(id))
            errors.Add(new AnswerError(questionId, $"'{id}' is not a valid option id for this question."));
    }

    private static void ValidateMultiChoice(MultiChoiceQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new AnswerError(q.Id, "MultiChoice answer must be a JSON array of option ids."));
            return;
        }
        var validIds = q.Options.Select(o => o.Id).ToHashSet();
        var picked = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                errors.Add(new AnswerError(q.Id, "Each MultiChoice array entry must be a string option id."));
                return;
            }
            picked.Add(item.GetString()!);
        }
        foreach (var id in picked)
            if (!validIds.Contains(id))
                errors.Add(new AnswerError(q.Id, $"'{id}' is not a valid option id for this question."));
        if (q.MinSelected.HasValue && picked.Count < q.MinSelected.Value)
            errors.Add(new AnswerError(q.Id, $"At least {q.MinSelected.Value} option(s) must be selected."));
        if (q.MaxSelected.HasValue && picked.Count > q.MaxSelected.Value)
            errors.Add(new AnswerError(q.Id, $"At most {q.MaxSelected.Value} option(s) may be selected."));
    }

    private static void ValidateDate(DateQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new AnswerError(q.Id, "Date answer must be a JSON string in yyyy-MM-dd format."));
            return;
        }
        var raw = value.GetString()!;
        if (!DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            errors.Add(new AnswerError(q.Id, $"Date '{raw}' is not yyyy-MM-dd."));
            return;
        }
        if (DateQuestionDtoValidator.BeValidIsoDate(q.MinDate) &&
            d < DateOnly.ParseExact(q.MinDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            errors.Add(new AnswerError(q.Id, $"Date {raw} is before minDate {q.MinDate}."));
        if (DateQuestionDtoValidator.BeValidIsoDate(q.MaxDate) &&
            d > DateOnly.ParseExact(q.MaxDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            errors.Add(new AnswerError(q.Id, $"Date {raw} is after maxDate {q.MaxDate}."));
    }

    private static void ValidateDateTime(DateTimeQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new AnswerError(q.Id, "DateTime answer must be a JSON string in ISO 8601 format."));
            return;
        }
        var raw = value.GetString()!;
        if (!DateTimeOffset.TryParse(raw, out var dt))
        {
            errors.Add(new AnswerError(q.Id, $"DateTime '{raw}' is not valid ISO 8601."));
            return;
        }
        if (!string.IsNullOrEmpty(q.MinDateTime) && DateTimeOffset.TryParse(q.MinDateTime, out var min) && dt < min)
            errors.Add(new AnswerError(q.Id, $"DateTime is before minDateTime {q.MinDateTime}."));
        if (!string.IsNullOrEmpty(q.MaxDateTime) && DateTimeOffset.TryParse(q.MaxDateTime, out var max) && dt > max)
            errors.Add(new AnswerError(q.Id, $"DateTime is after maxDateTime {q.MaxDateTime}."));
    }

    private static void ValidateString(string questionId, JsonElement value, List<AnswerError> errors, string kind)
    {
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
            errors.Add(new AnswerError(questionId, $"Answer must be a non-empty {kind} string."));
    }

    private static void ValidateYesNo(YesNoQuestionDto q, JsonElement value, List<AnswerError> errors)
    {
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
            errors.Add(new AnswerError(q.Id, "Yes/No answer must be a JSON boolean."));
    }
}

using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Types;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.Resolver;

/// <summary>
/// Publish-time expansion of a draft <see cref="SurveyDto"/> into a fully self-contained,
/// inline form suitable for freezing as an immutable <c>SurveyVersion</c>.
///
/// Per Phase 1 Part B.3:
/// 1. Each <see cref="ScreenTemplateRefDto"/> becomes an <see cref="InlineScreenDto"/>
///    by pulling the template from <see cref="IBankSource"/>, applying template-level
///    overrides, then per-banked-question overrides.
/// 2. Each ref-form <see cref="QuestionEntryDto"/> becomes an inline <see cref="QuestionDto"/>
///    by pulling the bank entry, deep-cloning, applying presentation overrides per Decision #9
///    (title, help, required, option labels). Id / type / validation / answer shape are preserved.
/// 3. The banked id survives expansion — that's what gives BI a stable join key across surveys.
/// </summary>
public static class SchemaResolver
{
    public static ResolveResult Resolve(SurveyDto draft, IBankSource source)
    {
        var errors = new List<ResolveError>();
        var resolvedSurvey = DeepClone(draft);
        resolvedSurvey.Screens = new List<ScreenDto>(draft.Screens.Count);

        for (var i = 0; i < draft.Screens.Count; i++)
        {
            var path = $"screens[{i}]";
            var resolvedScreen = ResolveScreen(draft.Screens[i], source, path, errors);
            if (resolvedScreen is not null)
                resolvedSurvey.Screens.Add(resolvedScreen);
        }

        return errors.Count > 0
            ? ResolveResult.Fail(errors)
            : ResolveResult.Ok(resolvedSurvey);
    }

    private static InlineScreenDto? ResolveScreen(ScreenDto screen, IBankSource source, string path, List<ResolveError> errors)
    {
        return screen switch
        {
            InlineScreenDto inline => ResolveInlineScreen(inline, source, path, errors),
            ScreenTemplateRefDto templateRef => ResolveTemplateRef(templateRef, source, path, errors),
            _ => null,
        };
    }

    private static InlineScreenDto ResolveInlineScreen(InlineScreenDto screen, IBankSource source, string path, List<ResolveError> errors)
    {
        var resolved = DeepClone(screen);
        resolved.Questions = new List<QuestionEntryDto>(screen.Questions.Count);

        for (var i = 0; i < screen.Questions.Count; i++)
        {
            var entryPath = $"{path}.questions[{i}]";
            var entry = screen.Questions[i];
            var resolvedInline = ResolveQuestionEntry(entry, source, entryPath, errors);
            if (resolvedInline is not null)
                resolved.Questions.Add(QuestionEntryDto.FromInline(resolvedInline));
        }

        return resolved;
    }

    private static InlineScreenDto? ResolveTemplateRef(ScreenTemplateRefDto templateRef, IBankSource source, string path, List<ResolveError> errors)
    {
        var template = source.GetTemplate(templateRef.TemplateRef);
        if (template is null)
        {
            errors.Add(new ResolveError($"{path}.templateRef", $"Screen template '{templateRef.TemplateRef}' does not exist."));
            return null;
        }

        var resolved = new InlineScreenDto
        {
            // Convention: a resolved-from-template screen keeps the template id as its screen id
            // so logic rules that referenced the template by name keep working.
            Id = template.Id,
            Title = templateRef.Overrides?.Title ?? (template.Title is null ? null : DeepClone(template.Title)),
            Description = templateRef.Overrides?.Description ?? (template.Description is null ? null : DeepClone(template.Description)),
            NextScreen = templateRef.Overrides?.NextScreen,
            Questions = new List<QuestionEntryDto>(template.Questions.Count),
        };

        for (var i = 0; i < template.Questions.Count; i++)
        {
            var qPath = $"{path}.questions[{i}]";
            var bankRef = template.Questions[i];
            // Overrides from the survey's template usage are keyed by banked question id
            // and merge onto the template's own overrides (if any).
            var overrideFromSurvey = templateRef.Overrides?.Questions?.GetValueOrDefault(bankRef.BankRef);
            var merged = MergeQuestionOverrides(bankRef.Overrides, overrideFromSurvey);
            var resolvedInline = ResolveBankRef(bankRef.BankRef, merged, source, qPath, errors);
            if (resolvedInline is not null)
                resolved.Questions.Add(QuestionEntryDto.FromInline(resolvedInline));
        }

        return resolved;
    }

    private static QuestionDto? ResolveQuestionEntry(QuestionEntryDto entry, IBankSource source, string path, List<ResolveError> errors)
    {
        if (entry.Inline is not null)
            return DeepClone(entry.Inline);

        if (entry.Ref is not null)
            return ResolveBankRef(entry.Ref.BankRef, entry.Ref.Overrides, source, path, errors);

        errors.Add(new ResolveError(path, "QuestionEntry carries neither an inline question nor a bankRef."));
        return null;
    }

    private static QuestionDto? ResolveBankRef(string bankRef, QuestionOverridesDto? overrides, IBankSource source, string path, List<ResolveError> errors)
    {
        var bank = source.GetQuestion(bankRef);
        if (bank is null)
        {
            errors.Add(new ResolveError($"{path}.bankRef", $"Bank question '{bankRef}' does not exist."));
            return null;
        }

        var inline = DeepClone(bank.Question);
        // The banked id MUST survive resolution — that's the stable join key for BI (Decision #11).
        inline.Id = bank.Id;

        if (overrides is not null)
            ApplyOverrides(inline, overrides);

        return inline;
    }

    private static void ApplyOverrides(QuestionDto question, QuestionOverridesDto overrides)
    {
        // Per Decision #9: presentation only. Id, type, validation, answer shape are frozen.
        if (overrides.Title is not null) question.Title = DeepClone(overrides.Title);
        if (overrides.Help is not null) question.Help = DeepClone(overrides.Help);
        if (overrides.Required.HasValue) question.Required = overrides.Required.Value;

        if (overrides.OptionLabels is { Count: > 0 })
            ApplyOptionLabelOverrides(question, overrides.OptionLabels);
    }

    private static void ApplyOptionLabelOverrides(QuestionDto question, Dictionary<string, LocalizedString> overrides)
    {
        switch (question)
        {
            case SingleChoiceQuestionDto sc:
                foreach (var opt in sc.Options)
                    if (overrides.TryGetValue(opt.Id, out var label)) opt.Label = DeepClone(label);
                break;
            case MultiChoiceQuestionDto mc:
                foreach (var opt in mc.Options)
                    if (overrides.TryGetValue(opt.Id, out var label)) opt.Label = DeepClone(label);
                break;
            case DropdownQuestionDto dd:
                foreach (var opt in dd.Options)
                    if (overrides.TryGetValue(opt.Id, out var label)) opt.Label = DeepClone(label);
                break;
            case NavigationListQuestionDto nl:
                foreach (var opt in nl.Options)
                    if (overrides.TryGetValue(opt.Id, out var label)) opt.Label = DeepClone(label);
                break;
            // Non-choice types silently ignore option-label overrides — the FluentValidation
            // layer doesn't catch this, but it's harmless at resolve time.
        }
    }

    private static QuestionOverridesDto? MergeQuestionOverrides(QuestionOverridesDto? lower, QuestionOverridesDto? higher)
    {
        if (lower is null) return higher;
        if (higher is null) return lower;
        return new QuestionOverridesDto
        {
            Title = higher.Title ?? lower.Title,
            Help = higher.Help ?? lower.Help,
            Required = higher.Required ?? lower.Required,
            Order = higher.Order ?? lower.Order,
            OptionLabels = MergeDictionaries(lower.OptionLabels, higher.OptionLabels),
        };
    }

    private static Dictionary<string, LocalizedString>? MergeDictionaries(
        Dictionary<string, LocalizedString>? lower,
        Dictionary<string, LocalizedString>? higher)
    {
        if (lower is null) return higher;
        if (higher is null) return lower;
        var merged = new Dictionary<string, LocalizedString>(lower);
        foreach (var (k, v) in higher) merged[k] = v;
        return merged;
    }

    private static T DeepClone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, SurveySchemaSerializer.Options);
        return JsonSerializer.Deserialize<T>(json, SurveySchemaSerializer.Options)!;
    }
}

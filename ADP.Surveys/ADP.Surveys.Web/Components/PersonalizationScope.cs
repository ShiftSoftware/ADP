using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;
using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;

namespace ShiftSoftware.ADP.Surveys.Web.Components;

/// <summary>
/// The personalization vocabulary offered to every localized text field and every
/// request field on the current survey form, handed down as a cascading value.
/// </summary>
/// <remarks>
/// Cascading rather than a parameter because <c>LocalizedStringField</c> is reached
/// through a dozen editors (screen inspector, question common fields, option lists,
/// per-type placeholders, template-ref overrides…). Threading a list through all of
/// them would be churn on every signature for a value none of them care about, and
/// a new editor would silently lose the feature by forgetting to pass it on.
/// </remarks>
public sealed class PersonalizationScope
{
    /// <summary>
    /// Always-available recipient fields. These come from the instance itself rather
    /// than the event payload, so they are offered on every survey whether or not the
    /// author has declared anything.
    /// </summary>
    public static readonly string[] RecipientTokens =
    {
        "recipient.customerRef",
        "recipient.address",
        "recipient.locale",
    };

    /// <summary>Token names to offer, in the order they should be listed. Never includes answer tokens — see <see cref="Answers"/>.</summary>
    public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Names with no declared fallback. Flagged in the insert menu because that is the
    /// customer-facing risk: such a token renders as raw braces to a real recipient
    /// whose event happens to omit the field. A missing <i>example</i> is not flagged —
    /// it only affects how a test run looks.
    /// </summary>
    public IReadOnlySet<string> WithoutFallback { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// The survey's own questions, in screen order, as <c>{{answers.&lt;id&gt;}}</c>
    /// candidates. These resolve in the respondent's browser as they answer, so they
    /// need no example and no declared fallback to be safe — an inline
    /// <c>|fallback</c> covers the "not answered yet" case where it matters.
    /// </summary>
    public IReadOnlyList<AnswerToken> Answers { get; init; } = Array.Empty<AnswerToken>();

    public bool IsEmpty => Names.Count == 0 && Answers.Count == 0;

    /// <summary>One question the author can reference by answer.</summary>
    /// <param name="QuestionId">The question id — the <c>x</c> in <c>{{answers.x}}</c>.</param>
    /// <param name="Display">Menu text: screen number and id, question id, type.</param>
    /// <param name="HasLabel">
    /// Whether the answer has a display form distinct from its stored value (a choice's
    /// option label, a yes/no word, a formatted date). The copy menu inserts
    /// <c>{{answers.x.label}}</c> for these; request fields always take the raw value.
    /// </param>
    public sealed record AnswerToken(string QuestionId, string Display, bool HasLabel)
    {
        /// <summary>The token to write into display copy.</summary>
        public string CopyToken => HasLabel
            ? $"{PersonalizationTokens.AnswerTokenPrefix}{QuestionId}{PersonalizationTokens.AnswerLabelSuffix}"
            : $"{PersonalizationTokens.AnswerTokenPrefix}{QuestionId}";

        /// <summary>The token to write into a URL, parameter, header or body.</summary>
        public string ValueToken => $"{PersonalizationTokens.AnswerTokenPrefix}{QuestionId}";
    }

    /// <summary>
    /// Builds the vocabulary for a draft: the standing recipient fields, everything
    /// the author has declared, every token already written somewhere in the
    /// survey's copy, and every question as an answer token. That third group is what
    /// makes a token typed by hand into one screen show up as a suggestion on the next.
    /// </summary>
    public static PersonalizationScope FromDraft(SurveyDto? draft)
    {
        var declared = new List<string>();
        var withFallback = new HashSet<string>(StringComparer.Ordinal);
        if (draft?.Variables is not null)
        {
            foreach (var variable in draft.Variables)
            {
                if (string.IsNullOrWhiteSpace(variable.Name)) continue;
                var name = variable.Name.Trim();
                if (PersonalizationTokens.IsAnswerToken(name)) continue;
                declared.Add(name);
                if (variable.Fallback is { Count: > 0 }) withFallback.Add(name);
            }
        }

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Declared first: those are the ones with values behind them, so they are the
        // ones an author should reach for.
        foreach (var name in declared)
            if (seen.Add(name)) names.Add(name);

        foreach (var name in RecipientTokens)
            if (seen.Add(name)) names.Add(name);

        // Answer tokens have their own list; here they would only read as
        // "undeclared" variables that need a fallback, which they don't.
        foreach (var name in PersonalizationTokens.CollectTokenNames(draft)
                     .Where(n => !PersonalizationTokens.IsAnswerToken(n))
                     .OrderBy(x => x, StringComparer.Ordinal))
            if (seen.Add(name)) names.Add(name);

        // recipient.* always resolve from the instance itself, so they never need a
        // fallback and must not be flagged as if they did.
        var withoutFallback = names
            .Where(n => !withFallback.Contains(n))
            .Where(n => !RecipientTokens.Contains(n, StringComparer.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        return new PersonalizationScope
        {
            Names = names,
            WithoutFallback = withoutFallback,
            Answers = CollectAnswers(draft),
        };
    }

    /// <summary>
    /// Every question an answer token can name: inline questions with their type, and
    /// banked references by key (the key IS the resolved question id — the bank's
    /// stable anchor). Screen-template references are skipped: their questions live
    /// in the template, and the author can still type the token by hand.
    /// </summary>
    private static List<AnswerToken> CollectAnswers(SurveyDto? draft)
    {
        var answers = new List<AnswerToken>();
        if (draft is null) return answers;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < draft.Screens.Count; i++)
        {
            if (draft.Screens[i] is not InlineScreenDto screen) continue;
            var screenLabel = string.IsNullOrWhiteSpace(screen.Id) ? $"{i + 1}." : $"{i + 1}.{screen.Id}";

            foreach (var entry in screen.Questions)
            {
                if (entry.Inline is { } question)
                {
                    if (string.IsNullOrWhiteSpace(question.Id) || !seen.Add(question.Id)) continue;
                    var type = question.QuestionType;
                    answers.Add(new AnswerToken(
                        question.Id,
                        $"{screenLabel} / {question.Id} — {type.ToString().ToLowerInvariant()}",
                        HasDisplayForm(type)));
                }
                else if (entry.Ref is { BankRef.Length: > 0 } reference)
                {
                    if (!seen.Add(reference.BankRef)) continue;
                    // The banked question's type isn't known here; offering the label
                    // form is harmless — for a type without one it reads as the value.
                    answers.Add(new AnswerToken(reference.BankRef, $"{screenLabel} / {reference.BankRef} — banked", HasLabel: true));
                }
            }
        }
        return answers;
    }

    private static bool HasDisplayForm(QuestionType type) => type is
        QuestionType.SingleChoice or QuestionType.MultiChoice or QuestionType.Dropdown
        or QuestionType.NavigationList or QuestionType.YesNo
        or QuestionType.Date or QuestionType.DateTime;
}

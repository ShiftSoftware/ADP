using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.Personalization;

namespace ShiftSoftware.ADP.Surveys.Web.Components;

/// <summary>
/// The personalization vocabulary offered to every localized text field on the
/// current survey form, handed down as a cascading value.
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

    /// <summary>Token names to offer, in the order they should be listed.</summary>
    public IReadOnlyList<string> Names { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Names with no declared fallback. Flagged in the insert menu because that is the
    /// customer-facing risk: such a token renders as raw braces to a real recipient
    /// whose event happens to omit the field. A missing <i>example</i> is not flagged —
    /// it only affects how a test run looks.
    /// </summary>
    public IReadOnlySet<string> WithoutFallback { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool IsEmpty => Names.Count == 0;

    /// <summary>
    /// Builds the vocabulary for a draft: the standing recipient fields, everything
    /// the author has declared, and every token already written somewhere in the
    /// survey's copy. That last group is what makes a token typed by hand into one
    /// screen show up as a suggestion on the next.
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

        foreach (var name in PersonalizationTokens.CollectTokenNames(draft).OrderBy(x => x, StringComparer.Ordinal))
            if (seen.Add(name)) names.Add(name);

        // recipient.* always resolve from the instance itself, so they never need a
        // fallback and must not be flagged as if they did.
        var withoutFallback = names
            .Where(n => !withFallback.Contains(n))
            .Where(n => !RecipientTokens.Contains(n, StringComparer.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

        return new PersonalizationScope { Names = names, WithoutFallback = withoutFallback };
    }
}

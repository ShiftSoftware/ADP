using ShiftSoftware.ADP.Surveys.Shared.DTOs;

namespace ShiftSoftware.ADP.Surveys.Shared.Personalization;

/// <summary>
/// Which audience a schema is being served to, and therefore whether a variable's
/// <see cref="SurveyVariableDto.Example"/> is in play.
/// </summary>
public enum PersonalizationMode
{
    /// <summary>
    /// A real instance. Examples are ignored entirely — test data must never reach a
    /// recipient — and only the customer-facing <see cref="SurveyVariableDto.Fallback"/>
    /// backs a token up.
    /// </summary>
    Live,

    /// <summary>
    /// A dashboard test instance or the builder preview. The example leads, standing in
    /// for the event that isn't there. Everything behind it is identical to
    /// <see cref="Live"/>, so a variable with no example previews exactly what a real
    /// recipient with a missing field would get.
    /// </summary>
    Sample,
}

/// <summary>
/// Everything <see cref="PersonalizationTokens"/> needs to resolve one survey's
/// tokens: the values frozen on the instance, the author's declared variables, and
/// which audience is being served.
/// </summary>
/// <remarks>
/// Resolution order for <c>{{name|inline}}</c>, given the current locale:
/// <list type="number">
///   <item><b>Example</b> — <see cref="PersonalizationMode.Sample"/> only.</item>
///   <item>Instance snapshot (<see cref="Values"/>).</item>
///   <item>The inline <c>|fallback</c>, if the author wrote one for this sentence.</item>
///   <item><b>Fallback</b> — the survey-wide safety net.</item>
///   <item>Otherwise the token is left verbatim, so an author's typo shows itself.</item>
/// </list>
/// Only step 1 differs between the two modes, which is what makes a test run an honest
/// preview of the live path whenever the example is left blank.
/// </remarks>
public sealed class PersonalizationContext
{
    private static readonly Dictionary<string, string> NoValues = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, LocalizedString> NoLocalized = new(StringComparer.Ordinal);

    /// <summary>Values frozen on the instance — <c>recipient.*</c> plus top-level <c>candidate.*</c> fields.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Declared test-run stand-ins, keyed by token name. Consulted in <see cref="PersonalizationMode.Sample"/> only.</summary>
    public IReadOnlyDictionary<string, LocalizedString> Examples { get; }

    /// <summary>Declared customer-facing safety nets, keyed by token name.</summary>
    public IReadOnlyDictionary<string, LocalizedString> Fallbacks { get; }

    /// <summary>Locale to fall back to when a declared value has no entry for the locale being substituted.</summary>
    public string? DefaultLocale { get; }

    public PersonalizationMode Mode { get; }

    /// <summary>Nothing to substitute from. Inline fallbacks can still resolve — see <see cref="PersonalizationTokens.SubstituteString(string, PersonalizationContext, string?)"/>.</summary>
    public bool IsEmpty =>
        Values.Count == 0 && Fallbacks.Count == 0
        && (Mode == PersonalizationMode.Live || Examples.Count == 0);

    public PersonalizationContext(
        IReadOnlyDictionary<string, string>? values,
        IReadOnlyDictionary<string, LocalizedString>? examples = null,
        IReadOnlyDictionary<string, LocalizedString>? fallbacks = null,
        string? defaultLocale = null,
        PersonalizationMode mode = PersonalizationMode.Live)
    {
        Values = values ?? NoValues;
        Examples = examples ?? NoLocalized;
        Fallbacks = fallbacks ?? NoLocalized;
        DefaultLocale = defaultLocale;
        Mode = mode;
    }

    /// <summary>
    /// Context for a real instance: values from the instance's frozen snapshot, with
    /// the survey's declared fallbacks behind them. Examples are deliberately not
    /// carried — there is no path by which test data should reach a recipient.
    /// </summary>
    public static PersonalizationContext ForInstance(
        IReadOnlyDictionary<string, string>? snapshotValues,
        SurveyDto? survey) =>
        new(
            snapshotValues,
            examples: null,
            fallbacks: Index(survey, static v => v.Fallback),
            survey?.DefaultLocale,
            PersonalizationMode.Live);

    /// <inheritdoc cref="ForInstance(IReadOnlyDictionary{string,string}, SurveyDto)"/>
    public static PersonalizationContext ForInstance(
        string? customerRef,
        string? recipientAddress,
        string? recipientLocale,
        string? candidateMetadataJson,
        SurveyDto? survey) =>
        ForInstance(
            PersonalizationTokens.BuildContext(customerRef, recipientAddress, recipientLocale, candidateMetadataJson),
            survey);

    /// <summary>
    /// Context for a dashboard test instance or the builder preview: the declared
    /// examples lead. Any snapshot values that do exist sit behind them, and the live
    /// fallback chain backs the whole thing up unchanged.
    /// </summary>
    public static PersonalizationContext ForSample(
        SurveyDto? survey,
        IReadOnlyDictionary<string, string>? snapshotValues = null) =>
        new(
            snapshotValues,
            examples: Index(survey, static v => v.Example),
            fallbacks: Index(survey, static v => v.Fallback),
            survey?.DefaultLocale,
            PersonalizationMode.Sample);

    /// <summary>
    /// Keyed view of one side of <see cref="SurveyDto.Variables"/>. Unnamed rows and
    /// empty values are dropped — a half-filled row in the builder must not start
    /// blanking tokens. Later rows win on a duplicate name, matching how the editor
    /// stacks them.
    /// </summary>
    private static Dictionary<string, LocalizedString> Index(
        SurveyDto? survey,
        Func<SurveyVariableDto, LocalizedString?> select)
    {
        var map = new Dictionary<string, LocalizedString>(StringComparer.Ordinal);
        if (survey?.Variables is null) return map;

        foreach (var variable in survey.Variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Name)) continue;
            var value = select(variable);
            if (value is not { Count: > 0 }) continue;
            map[variable.Name.Trim()] = value;
        }
        return map;
    }
}

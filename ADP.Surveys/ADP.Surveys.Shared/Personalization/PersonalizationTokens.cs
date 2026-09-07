using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;

namespace ShiftSoftware.ADP.Surveys.Shared.Personalization;

/// <summary>
/// Serve-time personalization: <c>{{token}}</c> placeholders inside a resolved
/// schema's <see cref="LocalizedString"/> values are substituted from the
/// instance's snapshot fields when <c>PublicSurveyController.GetSchema</c>
/// serves it. Published versions stay frozen — substitution happens on the
/// copy being served, mirroring the branding overlay's discipline.
///
/// Token vocabulary matches the trigger filter / dedup-recipe path grammar so
/// authors learn one set of names:
///   {{recipient.address}} / {{recipient.locale}} / {{recipient.customerRef}}
///   {{candidate.&lt;field&gt;}} — top-level property of the ingested event payload
///                              (nested paths not supported, same as TriggerHasher).
///
/// A token may carry an inline fallback after a pipe —
/// <c>{{candidate.customerName|our valued customer}}</c> — used when the field is
/// missing from the payload. Behind that sits the survey's own
/// <see cref="SurveyDto.Variables"/>, each declaring a customer-facing
/// <see cref="SurveyVariableDto.Fallback"/> and — for test runs and the builder
/// preview only — an <see cref="SurveyVariableDto.Example"/>;
/// <see cref="PersonalizationContext"/> documents the full precedence.
///
/// A token that resolves to nothing at all is left verbatim (an author typo shows
/// itself rather than silently vanishing). Substitution targets ONLY LocalizedString
/// values — a typed tree-walk over the DTO graph, never a string-replace over raw
/// JSON — so ids, expressions, and URLs can never be corrupted.
/// </summary>
public static class PersonalizationTokens
{
    // Group 1 is the token path; group 2 is the optional inline fallback. The
    // fallback stops at '}' so a malformed token cannot swallow the rest of the
    // sentence — everything else, Arabic and Kurdish copy included, is fair game.
    private static readonly Regex TokenPattern = new(
        @"\{\{\s*([A-Za-z_][A-Za-z0-9_.\-]*)\s*(?:\|([^}]*))?\}\}",
        RegexOptions.Compiled);

    private static readonly Regex TokenNamePattern = new(
        @"^[A-Za-z_][A-Za-z0-9_.\-]*$",
        RegexOptions.Compiled);

    /// <summary>
    /// True when <paramref name="name"/> is shaped like a token path, i.e. would be
    /// matched by the token pattern if written between braces. Used by the builder to
    /// reject a variable row that could never resolve.
    /// </summary>
    public static bool IsValidTokenName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && TokenNamePattern.IsMatch(name!.Trim());

    /// <summary>
    /// Cheap pre-check so the serve path can skip deserialization entirely for
    /// the (common) token-free surveys. Conservative — a match only means "worth
    /// parsing", the walker still only rewrites LocalizedString values.
    /// </summary>
    public static bool MightContainTokens(string? text) =>
        text?.Contains("{{", StringComparison.Ordinal) == true;

    /// <summary>
    /// Every distinct token path written anywhere in <paramref name="survey"/>'s
    /// localized copy. The builder uses it to offer "add the variables this survey
    /// actually uses" rather than making the author retype each name.
    /// </summary>
    public static IReadOnlyCollection<string> CollectTokenNames(SurveyDto? survey)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (survey is null) return names;

        Walk(survey, localized =>
        {
            foreach (var value in localized.Values)
            {
                if (value is null) continue;
                foreach (Match match in TokenPattern.Matches(value))
                    names.Add(match.Groups[1].Value);
            }
        }, new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);

        return names;
    }

    /// <summary>
    /// Builds the token → value map from an instance's snapshot fields. The
    /// candidate payload is the raw <c>MetaDataJson</c> the trigger ingest froze;
    /// only top-level string/number/bool properties become tokens (objects and
    /// arrays aren't sensible display values). Malformed JSON yields no
    /// candidate tokens — the schema still serves, tokens stay verbatim.
    /// </summary>
    public static Dictionary<string, string> BuildContext(
        string? customerRef,
        string? recipientAddress,
        string? recipientLocale,
        string? candidateMetadataJson)
    {
        var context = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(recipientAddress)) context["recipient.address"] = recipientAddress!;
        if (!string.IsNullOrEmpty(recipientLocale)) context["recipient.locale"] = recipientLocale!;
        if (!string.IsNullOrEmpty(customerRef)) context["recipient.customerRef"] = customerRef!;

        if (!string.IsNullOrEmpty(candidateMetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(candidateMetadataJson!);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var rendered = RenderValue(prop.Value);
                        if (rendered is not null)
                            context[$"candidate.{prop.Name}"] = rendered;
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed snapshot — serve un-personalized rather than fail.
            }
        }

        return context;
    }

    private static string? RenderValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null,
    };

    /// <summary>Replaces known tokens in one string; unresolvable tokens stay verbatim.</summary>
    public static string SubstituteString(string input, IReadOnlyDictionary<string, string> context) =>
        SubstituteString(input, new PersonalizationContext(context), locale: null);

    /// <summary>
    /// Replaces tokens in one string, resolving each against <paramref name="context"/>
    /// for <paramref name="locale"/>. A token that resolves to nothing — no value, no
    /// inline fallback, no declared variable — is left verbatim.
    /// </summary>
    public static string SubstituteString(string input, PersonalizationContext context, string? locale)
    {
        if (!MightContainTokens(input)) return input;
        // An inline fallback resolves with nothing configured at all, so the
        // empty-context shortcut must not skip a string that carries one.
        if (context.IsEmpty && !input.Contains('|', StringComparison.Ordinal)) return input;

        return TokenPattern.Replace(input, match =>
        {
            var name = match.Groups[1].Value;
            var inline = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            return Resolve(name, inline, context, locale) ?? match.Value;
        });
    }

    /// <summary>Null means "nothing to substitute" — the caller keeps the token verbatim.</summary>
    private static string? Resolve(string name, string? inlineFallback, PersonalizationContext context, string? locale)
    {
        // Test runs and the preview only. The example stands in for the event that
        // isn't there; on a real instance it is not even consulted, so test data has
        // no path to a recipient.
        if (context.Mode == PersonalizationMode.Sample
            && TryLocalized(context.Examples, context, name, locale, out var example))
            return example;

        if (TryValue(context, name, out var actual)) return actual;

        // The sentence's own fallback is more specific than the survey-wide one.
        if (!string.IsNullOrEmpty(inlineFallback)) return inlineFallback;

        if (TryLocalized(context.Fallbacks, context, name, locale, out var fallback)) return fallback;

        return null;
    }

    private static bool TryValue(PersonalizationContext context, string name, out string value)
    {
        if (context.Values.TryGetValue(name, out var found) && !string.IsNullOrEmpty(found))
        {
            value = found;
            return true;
        }
        value = "";
        return false;
    }

    /// <summary>
    /// Reads one of the declared maps for the locale being substituted. Falls back to
    /// the survey's default locale, then any locale the author filled in — the same
    /// forgiving policy the renderer's own <c>localize()</c> applies, so a variable
    /// translated in only one language still beats a raw token.
    /// </summary>
    private static bool TryLocalized(
        IReadOnlyDictionary<string, LocalizedString> source,
        PersonalizationContext context,
        string name,
        string? locale,
        out string value)
    {
        value = "";
        if (!source.TryGetValue(name, out var localized) || localized is null) return false;

        if (locale is not null && localized.TryGetValue(locale, out var exact) && !string.IsNullOrEmpty(exact))
        {
            value = exact;
            return true;
        }
        if (context.DefaultLocale is not null
            && localized.TryGetValue(context.DefaultLocale, out var fallback)
            && !string.IsNullOrEmpty(fallback))
        {
            value = fallback;
            return true;
        }
        foreach (var candidate in localized.Values)
        {
            if (string.IsNullOrEmpty(candidate)) continue;
            value = candidate;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Walks the resolved DTO graph and substitutes tokens inside every
    /// <see cref="LocalizedString"/> value, in place. Returns the same instance
    /// for chaining.
    /// </summary>
    public static SurveyDto Substitute(SurveyDto resolved, IReadOnlyDictionary<string, string> context) =>
        Substitute(resolved, new PersonalizationContext(context));

    /// <summary>
    /// Walks the resolved DTO graph and substitutes tokens inside every
    /// <see cref="LocalizedString"/> value, in place, resolving each value against
    /// the locale it is keyed under. Returns the same instance for chaining.
    /// </summary>
    public static SurveyDto Substitute(SurveyDto resolved, PersonalizationContext context)
    {
        Walk(resolved, localized =>
        {
            foreach (var key in localized.Keys.ToList())
            {
                var value = localized[key];
                if (value is not null)
                    localized[key] = SubstituteString(value, context, key);
            }
        }, new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);

        return resolved;
    }

    // Generous bound — the deepest real path (survey → screen → question →
    // option → label) is ~6; the cap only exists to make pathological graphs
    // impossible to loop on.
    private const int MaxDepth = 16;

    /// <summary>
    /// Shared traversal for every operation that needs each <see cref="LocalizedString"/>
    /// in the graph. <paramref name="visit"/> may mutate the map it is handed.
    /// </summary>
    private static void Walk(object? node, Action<LocalizedString> visit, HashSet<object> seen, int depth)
    {
        if (node is null || depth > MaxDepth) return;

        // LocalizedString first — it IS a Dictionary, so this must run before
        // the generic IDictionary branch.
        if (node is LocalizedString localized)
        {
            visit(localized);
            return;
        }

        var type = node.GetType();
        if (node is string || type.IsPrimitive || type.IsEnum
            || node is JsonElement || node is DateTime || node is DateTimeOffset
            || node is Guid || node is decimal)
            return;

        if (!seen.Add(node)) return;

        if (node is IDictionary dictionary)
        {
            foreach (var value in dictionary.Values)
                Walk(value, visit, seen, depth + 1);
            return;
        }

        if (node is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                Walk(item, visit, seen, depth + 1);
            return;
        }

        // Only descend into our own DTO types — keeps the walker off framework
        // objects and future non-schema references.
        if (type.Namespace?.StartsWith("ShiftSoftware.ADP.Surveys", StringComparison.Ordinal) != true)
            return;

        foreach (var property in GetWalkableProperties(type))
        {
            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch
            {
                continue; // a throwing getter must never break schema serving
            }
            Walk(value, visit, seen, depth + 1);
        }
    }

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    private static PropertyInfo[] GetWalkableProperties(Type type) =>
        PropertiesCache.GetOrAdd(type, t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            // The variables ARE the substitution source. Walking into them would let
            // one variable's text be rewritten by another (or by itself), so the
            // declaration site is the one localized value left alone.
            .Where(p => !(t == typeof(SurveyDto) && p.Name == nameof(SurveyDto.Variables)))
            .ToArray());
}

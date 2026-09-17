using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.DTOs.Questions.Options;

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
/// A token that resolves to nothing at all is left verbatim in display copy (an
/// author typo shows itself rather than silently vanishing) and becomes empty inside a
/// request field (see <see cref="TokenSurface"/> — an endpoint must never be sent raw
/// braces). Substitution is a typed tree-walk over the DTO graph, never a
/// string-replace over raw JSON, and touches exactly two kinds of node: every
/// <see cref="LocalizedString"/>, and the request fields of every
/// <see cref="OptionsSourceDto"/>. Ids, expressions and everything else are never
/// rewritten.
///
/// <b>Answer tokens.</b> <c>{{answers.&lt;questionId&gt;}}</c> (and
/// <c>{{answers.&lt;questionId&gt;.label}}</c> for the option label the respondent saw)
/// refer to the respondent's own answers, which only exist in the browser. The server
/// therefore leaves them untouched — no example, no fallback, verbatim — and the
/// renderer's mirror of this engine (<c>survey-sdk/personalization.ts</c>) resolves
/// them at answer time, with the same grammar, the same fallback chain and the same
/// per-surface escaping. Keep the two in lock-step.
/// </summary>
public static class PersonalizationTokens
{
    /// <summary>Token path prefix reserved for the respondent's answers.</summary>
    public const string AnswerTokenPrefix = "answers.";

    /// <summary>Suffix selecting the display label of a choice answer instead of its stored value.</summary>
    public const string AnswerLabelSuffix = ".label";

    /// <summary>True for <c>answers.&lt;questionId&gt;</c> and <c>answers.&lt;questionId&gt;.label</c>.</summary>
    public static bool IsAnswerToken(string? name) =>
        name is not null
        && name.StartsWith(AnswerTokenPrefix, StringComparison.Ordinal)
        && name.Length > AnswerTokenPrefix.Length;

    /// <summary>
    /// The question id an answer token refers to — <c>answers.nps</c> and
    /// <c>answers.nps.label</c> both yield <c>nps</c>. Null for any other token.
    /// </summary>
    public static string? AnswerTokenQuestionId(string? name)
    {
        if (!IsAnswerToken(name)) return null;
        var path = name!.Substring(AnswerTokenPrefix.Length);
        if (path.EndsWith(AnswerLabelSuffix, StringComparison.Ordinal))
            path = path.Substring(0, path.Length - AnswerLabelSuffix.Length);
        return path.Length == 0 ? null : path;
    }

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
    /// parsing", the walker still only rewrites copy and request fields.
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

        void Collect(string? text)
        {
            if (text is null) return;
            foreach (Match match in TokenPattern.Matches(text))
                names.Add(match.Groups[1].Value);
        }

        Walk(survey,
            localized =>
            {
                foreach (var value in localized.Values) Collect(value);
            },
            source =>
            {
                Collect(source.Url);
                Collect(source.Body);
                if (source.QueryParams is not null) foreach (var v in source.QueryParams.Values) Collect(v);
                if (source.Headers is not null) foreach (var v in source.Headers.Values) Collect(v);
            },
            new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);

        return names;
    }

    /// <summary>
    /// Every question id referenced by an <c>{{answers.*}}</c> token anywhere in
    /// <paramref name="survey"/> — copy and request fields alike. The integrity
    /// validator checks each against the questions that actually exist.
    /// </summary>
    public static IReadOnlyCollection<string> CollectAnswerReferences(SurveyDto? survey)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in CollectTokenNames(survey))
        {
            var id = AnswerTokenQuestionId(name);
            if (id is not null) ids.Add(id);
        }
        return ids;
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
    /// Replaces tokens in one string of display copy, resolving each against
    /// <paramref name="context"/> for <paramref name="locale"/>. A token that resolves
    /// to nothing — no value, no inline fallback, no declared variable — is left verbatim.
    /// </summary>
    public static string SubstituteString(string input, PersonalizationContext context, string? locale) =>
        SubstituteString(input, context, locale, TokenSurface.Copy);

    /// <summary>
    /// Replaces tokens in one string destined for <paramref name="surface"/>. The
    /// resolved value is escaped for that surface; what happens to a token nothing can
    /// fill also depends on it — verbatim in copy, empty in a request field. Answer
    /// tokens are always left verbatim here: the renderer fills them.
    /// </summary>
    public static string SubstituteString(string input, PersonalizationContext context, string? locale, TokenSurface surface)
    {
        if (!MightContainTokens(input)) return input;
        // An inline fallback resolves with nothing configured at all, and a request
        // field blanks unresolvable tokens, so the empty-context shortcut only applies
        // to copy with no fallback in it.
        if (surface == TokenSurface.Copy && context.IsEmpty && !input.Contains('|', StringComparison.Ordinal)) return input;

        return TokenPattern.Replace(input, match =>
        {
            var name = match.Groups[1].Value;
            if (IsAnswerToken(name)) return match.Value;
            var inline = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            var resolved = Resolve(name, inline, context, locale);
            if (resolved is null)
                return surface == TokenSurface.Copy ? match.Value : "";
            return Encode(resolved, surface);
        });
    }

    /// <summary>
    /// Escapes a resolved value for the surface it is being written into. Mirrored by
    /// <c>encodeForSurface</c> in the SDK.
    /// </summary>
    public static string Encode(string value, TokenSurface surface) => surface switch
    {
        TokenSurface.Url or TokenSurface.FormBody => Uri.EscapeDataString(value),
        TokenSurface.JsonBody => JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString(),
        TokenSurface.HeaderValue => value.Replace("\r", "").Replace("\n", ""),
        _ => value,
    };

    /// <summary>
    /// Which escaping a request body wants, from its media type: JSON for
    /// <c>application/json</c> and any <c>+json</c> type, form encoding for
    /// <c>application/x-www-form-urlencoded</c>, raw otherwise.
    /// </summary>
    public static TokenSurface BodySurface(string? contentType)
    {
        var mediaType = (contentType ?? "").Split(';')[0].Trim();
        if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
            return TokenSurface.JsonBody;
        if (mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            return TokenSurface.FormBody;
        return TokenSurface.RawBody;
    }

    /// <summary>
    /// Substitutes tokens in every request field of an options source, in place: the
    /// URL (percent-encoded), query-parameter values (raw — the renderer encodes them
    /// when it builds the URL), header values (CR/LF stripped) and the body (escaped
    /// for its content type). Declared fallbacks resolve for the survey's default
    /// locale, since a request has no locale of its own.
    /// </summary>
    public static void SubstituteOptionsSource(OptionsSourceDto source, PersonalizationContext context)
    {
        var locale = context.DefaultLocale;
        source.Url = SubstituteString(source.Url, context, locale, TokenSurface.Url);
        if (source.Body is not null)
            source.Body = SubstituteString(source.Body, context, locale, BodySurface(source.EffectiveContentType));
        SubstituteValues(source.QueryParams, context, locale, TokenSurface.QueryValue);
        SubstituteValues(source.Headers, context, locale, TokenSurface.HeaderValue);
    }

    private static void SubstituteValues(Dictionary<string, string>? map, PersonalizationContext context, string? locale, TokenSurface surface)
    {
        if (map is null) return;
        foreach (var key in map.Keys.ToList())
        {
            var value = map[key];
            if (value is not null) map[key] = SubstituteString(value, context, locale, surface);
        }
    }

    /// <summary>Null means "nothing to substitute" — the caller decides what that means for its surface.</summary>
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
        Walk(resolved,
            localized =>
            {
                foreach (var key in localized.Keys.ToList())
                {
                    var value = localized[key];
                    if (value is not null)
                        localized[key] = SubstituteString(value, context, key);
                }
            },
            source => SubstituteOptionsSource(source, context),
            new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);

        return resolved;
    }

    // Generous bound — the deepest real path (survey → screen → question →
    // option → label) is ~6; the cap only exists to make pathological graphs
    // impossible to loop on.
    private const int MaxDepth = 16;

    /// <summary>
    /// Shared traversal for every operation that needs each <see cref="LocalizedString"/>
    /// and each <see cref="OptionsSourceDto"/> in the graph. Both visitors may mutate
    /// the node they are handed. The source visitor gets the whole DTO and the walk
    /// does not descend into it: its string maps are request fields, not copy.
    /// </summary>
    private static void Walk(
        object? node,
        Action<LocalizedString> visit,
        Action<OptionsSourceDto> visitSource,
        HashSet<object> seen,
        int depth)
    {
        if (node is null || depth > MaxDepth) return;

        // LocalizedString first — it IS a Dictionary, so this must run before
        // the generic IDictionary branch.
        if (node is LocalizedString localized)
        {
            visit(localized);
            return;
        }

        if (node is OptionsSourceDto source)
        {
            if (seen.Add(source)) visitSource(source);
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
                Walk(value, visit, visitSource, seen, depth + 1);
            return;
        }

        if (node is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                Walk(item, visit, visitSource, seen, depth + 1);
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
            Walk(value, visit, visitSource, seen, depth + 1);
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

/// <summary>
/// Where a substituted value is going, which decides both how it is escaped and what a
/// token nothing can fill turns into. Display copy keeps such a token verbatim so an
/// author's typo shows itself; a request field blanks it, because an endpoint must
/// never receive raw braces. Mirrored one-to-one by <c>TokenSurface</c> in the SDK.
/// </summary>
public enum TokenSurface
{
    /// <summary>A <see cref="LocalizedString"/> value. Raw; unresolvable stays verbatim.</summary>
    Copy,
    /// <summary>Inside the URL string — path or query. Percent-encoded; unresolvable → empty.</summary>
    Url,
    /// <summary>A query-parameter value the renderer encodes when it builds the URL. Raw; unresolvable → empty.</summary>
    QueryValue,
    /// <summary>A header value. CR/LF stripped; unresolvable → empty.</summary>
    HeaderValue,
    /// <summary>Inside a JSON body. JSON-string-escaped, no quotes added; unresolvable → empty.</summary>
    JsonBody,
    /// <summary>Inside an <c>application/x-www-form-urlencoded</c> body. Percent-encoded; unresolvable → empty.</summary>
    FormBody,
    /// <summary>Inside a body of any other media type. Raw; unresolvable → empty.</summary>
    RawBody,
}

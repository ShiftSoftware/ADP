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
/// Unknown tokens are left verbatim (an author typo shows itself rather than
/// silently vanishing). Substitution targets ONLY LocalizedString values — a
/// typed tree-walk over the DTO graph, never a string-replace over raw JSON —
/// so ids, expressions, and URLs can never be corrupted.
/// </summary>
public static class PersonalizationTokens
{
    private static readonly Regex TokenPattern = new(
        @"\{\{\s*([A-Za-z_][A-Za-z0-9_.\-]*)\s*\}\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Cheap pre-check so the serve path can skip deserialization entirely for
    /// the (common) token-free surveys. Conservative — a match only means "worth
    /// parsing", the walker still only rewrites LocalizedString values.
    /// </summary>
    public static bool MightContainTokens(string? text) =>
        text?.Contains("{{", StringComparison.Ordinal) == true;

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

    /// <summary>Replaces known tokens in one string; unknown tokens stay verbatim.</summary>
    public static string SubstituteString(string input, IReadOnlyDictionary<string, string> context)
    {
        if (context.Count == 0 || !MightContainTokens(input)) return input;
        return TokenPattern.Replace(input, m =>
            context.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
    }

    /// <summary>
    /// Walks the resolved DTO graph and substitutes tokens inside every
    /// <see cref="LocalizedString"/> value, in place. Returns the same instance
    /// for chaining. No-op when the context is empty.
    /// </summary>
    public static SurveyDto Substitute(SurveyDto resolved, IReadOnlyDictionary<string, string> context)
    {
        if (context.Count > 0)
            Walk(resolved, context, new HashSet<object>(ReferenceEqualityComparer.Instance), depth: 0);
        return resolved;
    }

    // Generous bound — the deepest real path (survey → screen → question →
    // option → label) is ~6; the cap only exists to make pathological graphs
    // impossible to loop on.
    private const int MaxDepth = 16;

    private static void Walk(object? node, IReadOnlyDictionary<string, string> context, HashSet<object> seen, int depth)
    {
        if (node is null || depth > MaxDepth) return;

        // LocalizedString first — it IS a Dictionary, so this must run before
        // the generic IDictionary branch.
        if (node is LocalizedString localized)
        {
            foreach (var key in localized.Keys.ToList())
            {
                var value = localized[key];
                if (value is not null)
                    localized[key] = SubstituteString(value, context);
            }
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
                Walk(value, context, seen, depth + 1);
            return;
        }

        if (node is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
                Walk(item, context, seen, depth + 1);
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
            Walk(value, context, seen, depth + 1);
        }
    }

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    private static PropertyInfo[] GetWalkableProperties(Type type) =>
        PropertiesCache.GetOrAdd(type, t => t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray());
}

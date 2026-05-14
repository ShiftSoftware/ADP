using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// Resolves a trigger's <c>dedupRecipe</c> against a concrete event + recipient
/// context, building the canonical hash string and SHA-256 bytes that populate
/// <c>SurveyInstance.UniqueHash</c>.
///
/// Path grammar (slice 2):
/// <list type="bullet">
///   <item><c>templateId</c> — the survey ID the recipe is being hashed for.</item>
///   <item><c>recipient.address</c> / <c>recipient.locale</c> / <c>recipient.customerRef</c> — fields on <see cref="TriggerRecipient"/>.</item>
///   <item><c>candidate.X</c> — a top-level property on the candidate payload JSON.</item>
/// </list>
/// Nested-path resolution (e.g. <c>candidate.address.city</c>) lands when a real
/// trigger needs it — first usage drives the extension. Unknown paths resolve
/// to empty string; the recipe author should design keys to avoid collisions.
///
/// Hash format: <c>path=valuepath=value...</c> in recipe order. The
/// path prefix prevents value-shift collisions, and the unit-separator (0x1F)
/// prevents accidental string-content collisions across slots.
/// </summary>
public static class TriggerHasher
{
    private const char SlotSeparator = '';

    public static string BuildHashString(
        IReadOnlyList<string> recipe,
        long templateId,
        TriggerRecipient recipient,
        JsonElement candidatePayload)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < recipe.Count; i++)
        {
            if (i > 0) sb.Append(SlotSeparator);
            var path = recipe[i];
            sb.Append(path).Append('=').Append(ResolvePath(path, templateId, recipient, candidatePayload));
        }
        return sb.ToString();
    }

    public static byte[] ComputeHashBytes(string hashString)
        => SHA256.HashData(Encoding.UTF8.GetBytes(hashString));

    public static byte[] BuildHashBytes(
        IReadOnlyList<string> recipe,
        long templateId,
        TriggerRecipient recipient,
        JsonElement candidatePayload)
        => ComputeHashBytes(BuildHashString(recipe, templateId, recipient, candidatePayload));

    private static string ResolvePath(string path, long templateId, TriggerRecipient recipient, JsonElement candidatePayload)
    {
        if (path == "templateId") return templateId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        if (path.StartsWith("recipient.", StringComparison.Ordinal))
        {
            return path["recipient.".Length..] switch
            {
                "address" => recipient.Address ?? "",
                "locale" => recipient.Locale ?? "",
                "customerRef" => recipient.CustomerRef ?? "",
                _ => "",
            };
        }

        if (path.StartsWith("candidate.", StringComparison.Ordinal))
        {
            var key = path["candidate.".Length..];
            if (candidatePayload.ValueKind != JsonValueKind.Object) return "";
            if (!candidatePayload.TryGetProperty(key, out var value)) return "";
            return JsonValueToHashString(value);
        }

        return "";
    }

    private static string JsonValueToHashString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "",
        _ => value.GetRawText(),
    };
}

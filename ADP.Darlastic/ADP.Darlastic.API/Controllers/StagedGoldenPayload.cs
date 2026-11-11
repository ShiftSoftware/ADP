using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Staged golden payloads read by key: <c>{"goldenId":N,"attrs":[{"t","v"}...],"members":[...]}</c>.
///
/// <para><b>Why not the view.</b> The <c>GoldenCustomer</c> view's ID is <c>CAST(ArtifactKey AS bigint)</c>, so a
/// filter on it cannot seek: reading fifty goldens by ID through the view unpacks every golden's payload (seconds
/// at a million goldens). The same fifty read from <c>ProjectionState</c> on its key
/// <c>(ArtifactType, ArtifactKey)</c> are fifty seeks.</para>
///
/// <para><b>Same values.</b> Last wins per attribute type, which is what the drain does and equals the view's
/// <c>MAX(v)</c>: the engine stages a golden's attributes sorted by (type, value). A tombstoned or payload-less
/// row is no golden, as in the view.</para>
/// </summary>
internal static class StagedGoldenPayload
{
    public const string ArtifactType = "golden";
    public const string TombstoneHash = "TOMBSTONE";

    /// <summary>The surviving attributes of each of <paramref name="ids"/> that has a staged golden, by ID.</summary>
    public static async Task<Dictionary<long, Dictionary<string, string?>>> AttributesAsync(
        ShiftDbContext db, IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        var result = new Dictionary<long, Dictionary<string, string?>>();
        if (ids.Count == 0) return result;

        var keys = ids.Select(id => id.ToString(CultureInfo.InvariantCulture)).Distinct().ToList();
        var staged = await db.Set<ProjectionState>().AsNoTracking()
            .Where(p => p.ArtifactType == ArtifactType && keys.Contains(p.ArtifactKey))
            .Select(p => new { p.ArtifactKey, p.ContentHash, p.Payload })
            .ToListAsync(ct);

        foreach (var row in staged)
        {
            // ContentHash is VARCHAR(64) and may come back space-padded on some collations; compare trimmed.
            if (row.Payload is null || row.ContentHash.TrimEnd() == TombstoneHash) continue;
            if (!long.TryParse(row.ArtifactKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id)) continue;
            result[id] = Attributes(row.Payload);
        }
        return result;
    }

    /// <summary>A payload's attributes, last wins per type.</summary>
    public static Dictionary<string, string?> Attributes(string payload)
    {
        var byType = new Dictionary<string, string?>(StringComparer.Ordinal);
        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
            foreach (var a in attrs.EnumerateArray())
                if (a.TryGetProperty("t", out var t) && a.TryGetProperty("v", out var v) && t.GetString() is { } type)
                    byType[type] = v.GetString();
        return byType;
    }
}

using System.Globalization;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Reader for the <c>vinowner</c> artifact — one per VIN, staged in <c>ProjectionState</c> by the
/// resolve's ownership pass, keyed by the canonical VIN:
///
/// <code>
/// {"vin":"…","owners":[{"vin":"…","goldenId":N,"name":"…","role":"owner","from":"yyyy-MM-dd","to":"",
///   "sales":N,"services":N,"firstSale":"…","lastSale":"…","firstService":"…","lastService":"…",
///   "sources":["src|rid",…]}]}
/// </code>
///
/// Dates are <c>yyyy-MM-dd</c> or empty. Entries come owners first in timeline order, then service
/// contacts by golden id. The writer is the resolve (the link element is shared with the per-golden
/// <c>vinlinks</c> family, so both always agree).
///
/// <para>The staged <c>name</c> is deliberately NOT read: it is a snapshot of the survived name at
/// resolve time, and a surface that shows a name must read it from the golden itself and protect it
/// like every other golden read does.</para>
/// </summary>
public static class VinOwnerPayload
{
    /// <summary>The <c>ProjectionState.ArtifactType</c> of the family. Key = the canonical VIN.</summary>
    public const string ArtifactType = "vinowner";

    /// <summary>One golden's link to the VIN, as staged.</summary>
    /// <param name="GoldenId">The golden the resolve linked — live at that resolve; a later merge may
    /// have redirected it.</param>
    /// <param name="Role"><see cref="VinPeriod.OwnerRole"/> or <see cref="VinPeriod.ContactRole"/>.</param>
    /// <param name="From">Owner: first sale. Service contact: first service seen.</param>
    /// <param name="To">Owner: the next owner's first sale, exclusive (null = still the owner).
    /// Service contact: last service seen, inclusive.</param>
    public sealed record Link(
        long GoldenId,
        string Role,
        DateOnly? From,
        DateOnly? To,
        int Sales,
        int Services,
        DateOnly? FirstSale,
        DateOnly? LastSale,
        DateOnly? FirstService,
        DateOnly? LastService,
        IReadOnlyList<string> Sources);

    /// <summary>
    /// Parse a staged payload. Entries without a golden id or role are skipped; the list keeps the
    /// staged order.
    /// </summary>
    public static (string? Vin, List<Link> Links) Parse(string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var vin = Str(root, "vin");
        var links = new List<Link>();
        if (!root.TryGetProperty("owners", out var owners) || owners.ValueKind != JsonValueKind.Array)
            return (vin, links);

        foreach (var l in owners.EnumerateArray())
        {
            if (l.ValueKind != JsonValueKind.Object) continue;
            if (!l.TryGetProperty("goldenId", out var g) || g.ValueKind != JsonValueKind.Number || !g.TryGetInt64(out var goldenId)) continue;
            if (Str(l, "role") is not { Length: > 0 } role) continue;
            var sources = l.TryGetProperty("sources", out var s) && s.ValueKind == JsonValueKind.Array
                ? s.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => x.Length > 0).ToList()
                : new List<string>();
            links.Add(new Link(goldenId, role, Date(l, "from"), Date(l, "to"), Int(l, "sales"), Int(l, "services"),
                Date(l, "firstSale"), Date(l, "lastSale"), Date(l, "firstService"), Date(l, "lastService"), sources));
        }
        return (vin, links);
    }

    /// <summary>
    /// One link per surviving identity. The payload is as fresh as the last resolve; when two of its
    /// goldens have since been merged into one survivor, a read that redirect-chases each link would
    /// list that survivor twice. This keeps the strongest link of each survivor (see
    /// <see cref="IsStronger"/>) and drops the others.
    ///
    /// <para>The kept links stay in staged order (owners in timeline order, then service contacts),
    /// each at its own staged position. A dropped link's counts, dates and sources are NOT folded into
    /// the kept one: every fact on a kept link comes from one staged link, so its period still means
    /// what the resolve computed. The next resolve restages the VIN with the survivor's combined
    /// evidence.</para>
    /// </summary>
    /// <param name="links">The staged links, in staged order.</param>
    /// <param name="liveId">Each link's golden id mapped to its surviving identity; an id it does
    /// not contain is its own survivor.</param>
    /// <returns>The kept links with their surviving identity, in staged order.</returns>
    public static List<(Link Link, long LiveId)> OnePerSurvivor(IReadOnlyList<Link> links, IReadOnlyDictionary<long, long> liveId)
    {
        var best = new Dictionary<long, int>();
        for (int i = 0; i < links.Count; i++)
        {
            var live = liveId.GetValueOrDefault(links[i].GoldenId, links[i].GoldenId);
            if (!best.TryGetValue(live, out var kept) || IsStronger(links[i], links[kept]))
                best[live] = i;
        }

        var result = new List<(Link, long)>(best.Count);
        for (int i = 0; i < links.Count; i++)
        {
            var live = liveId.GetValueOrDefault(links[i].GoldenId, links[i].GoldenId);
            if (best[live] == i)
                result.Add((links[i], live));
        }
        return result;
    }

    /// <summary>
    /// True when <paramref name="a"/> is a strictly stronger link to the vehicle than
    /// <paramref name="b"/>: an owner beats a service contact; within a role, the longer period wins.
    /// An open period (no end: still the owner) is the longest; a period with no known start is the
    /// shortest, since nothing says how long it was. Equal strength is not stronger, so the
    /// earlier staged link is kept.
    /// </summary>
    public static bool IsStronger(Link a, Link b)
    {
        int byRole = RoleRank(a.Role).CompareTo(RoleRank(b.Role));
        if (byRole != 0) return byRole > 0;
        return PeriodLength(a) > PeriodLength(b);
    }

    private static int RoleRank(string role) => role switch
    {
        VinPeriod.OwnerRole => 2,
        VinPeriod.ContactRole => 1,
        _ => 0,
    };

    /// <summary>Days from start to end; <see cref="long.MaxValue"/> when open-ended, -1 when the start
    /// is unknown.</summary>
    private static long PeriodLength(Link l) =>
        l.From is not { } from ? -1
        : l.To is { } to ? to.DayNumber - from.DayNumber
        : long.MaxValue;

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : 0;

    private static DateOnly? Date(JsonElement e, string name) =>
        Str(e, name) is { Length: > 0 } s
        && DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;
}

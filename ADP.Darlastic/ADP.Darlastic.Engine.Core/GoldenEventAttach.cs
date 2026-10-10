using System.Text;
using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// One golden's link to one VIN as the resolve's ownership timeline computed it — the same
/// (role, from, to) the <c>vinlinks</c> / <c>vinowner</c> artifacts carry.
/// </summary>
/// <param name="Role"><see cref="OwnerRole"/> or <see cref="ContactRole"/>.</param>
/// <param name="From">Owner: the first sale (null = unknown start). Contact: first service seen.</param>
/// <param name="To">Owner: the NEXT owner's first sale, exclusive (null = still the owner).
/// Contact: last service seen, inclusive.</param>
public sealed record VinPeriod(long GoldenId, string Role, DateOnly? From, DateOnly? To)
{
    public const string OwnerRole = "owner";
    public const string ContactRole = "service-contact";
}

/// <summary>Per-tenant attachment policy. The defaults are the conservative rule set.</summary>
public sealed record EventAttachOptions
{
    /// <summary>
    /// When a VIN-only event's VIN has NO owner link at all (only service contacts), attach it to the
    /// VIN's single service contact — or, with several, to the one whose service window covers the
    /// event date when exactly one does. Off by default: a service contact may be a driver or a
    /// relative, so this is an inference a tenant opts into knowingly. Owner links always win when
    /// the VIN has any.
    /// </summary>
    public bool VinContactFallback { get; init; }

    public static EventAttachOptions Default { get; } = new();
}

/// <summary>An event as attached to one golden, with how it got there.</summary>
public sealed record AttachedEvent(SourceEvent Event, GoldenEventLink Link);

/// <summary>What one attachment pass did — the numbers a run log reports.</summary>
public sealed class EventAttachResult
{
    /// <summary>Attached events per golden (unordered; the payload writer orders them).</summary>
    public Dictionary<long, List<AttachedEvent>> ByGolden { get; } = new();

    /// <summary>Distinct events read per type (after duplicate removal).</summary>
    public Dictionary<GoldenEventType, int> Read { get; } = new();

    /// <summary>Events attached to at least one golden, per type.</summary>
    public Dictionary<GoldenEventType, int> Attached { get; } = new();

    /// <summary>Events attached to no golden, per type.</summary>
    public Dictionary<GoldenEventType, int> Unattached { get; } = new();

    /// <summary>Unattached events by "Type/reason" (reasons: see the constants on <see cref="GoldenEventAttach"/>).</summary>
    public SortedDictionary<string, int> UnattachedReasons { get; } = new(StringComparer.Ordinal);

    /// <summary>Attachments (event × golden) by link kind.</summary>
    public Dictionary<GoldenEventLink, int> ByLink { get; } = new();

    /// <summary>Events dropped because an earlier event had the same (type, id).</summary>
    public int Duplicates { get; internal set; }

    /// <summary>VIN-only events that attached to more than one golden (co-owners with no ordering).</summary>
    public int MultiGolden { get; internal set; }

    public int TotalRead => Read.Values.Sum();
    public int TotalAttached => Attached.Values.Sum();
    public int TotalUnattached => Unattached.Values.Sum();

    /// <summary>One line per concern, no personal data — for run logs.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        sb.Append($"events read {TotalRead:N0} / attached {TotalAttached:N0} / unattached {TotalUnattached:N0}");
        if (Duplicates > 0) sb.Append($" / duplicates dropped {Duplicates:N0}");
        sb.Append($"; goldens with events {ByGolden.Count:N0}");
        foreach (var t in Enum.GetValues<GoldenEventType>())
        {
            int read = Read.GetValueOrDefault(t);
            if (read == 0) continue;
            sb.Append($"\n    {t,-14} read {read,7:N0}  attached {Attached.GetValueOrDefault(t),7:N0}  unattached {Unattached.GetValueOrDefault(t),7:N0}");
        }
        if (ByLink.Count > 0)
            sb.Append("\n    links: " + string.Join(" / ", ByLink.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value:N0}")));
        if (MultiGolden > 0) sb.Append($" / VIN-only events on several goldens {MultiGolden:N0}");
        if (UnattachedReasons.Count > 0)
            sb.Append("\n    unattached by reason: " + string.Join(" / ", UnattachedReasons.Select(kv => $"{kv.Key} {kv.Value:N0}")));
        return sb.ToString();
    }
}

/// <summary>
/// The attachment rules that turn a resolve's event feeds into per-golden timelines (pure — no
/// storage, so the rules are testable on synthetic data and identical wherever they run).
///
/// <para><b>Profile-linked events</b> follow the named profile's identity. The resolver supplies
/// that lookup; the engine's own resolver answers from this run's clusters first and falls back to
/// the registry's last assignment (redirect-chased) for a profile not in this run's input — a frozen
/// absent source, or a record the identity feed skipped — so an event never loses its customer just
/// because its source was down for one run. No assignment anywhere: unattached, reason
/// <see cref="NoProfile"/>.</para>
///
/// <para><b>VIN-only events</b> attach to the VIN's <i>owner-role</i> golden(s) whose ownership
/// period covers the event date. Periods are half-open — [first sale, next owner's first sale) —
/// so a transfer date belongs to the new owner. An UNDATED event (an open recall) attaches to the
/// current owner(s), whose period is open-ended. Uncovered cases:</para>
///   - the VIN is unknown to every golden: <see cref="UnknownVin"/>;
///   - the event predates every ownership period (pre-delivery work, dealer stock):
///     <see cref="BeforeOwnership"/> — deliberately NOT given to the first owner;
///   - the VIN has only service-contact links: <see cref="NoOwner"/>, unless
///     <see cref="EventAttachOptions.VinContactFallback"/> is on, which attaches to the VIN's single
///     contact, or the single contact whose service window covers the date; otherwise
///     <see cref="AmbiguousContacts"/>;
///   - no usable VIN on the event: <see cref="NoVin"/>.
/// </summary>
public static class GoldenEventAttach
{
    public const string NoProfile = "no-profile";
    public const string NoVin = "no-vin";
    public const string UnknownVin = "unknown-vin";
    public const string BeforeOwnership = "before-ownership";
    public const string NoOwner = "no-owner";
    public const string AmbiguousContacts = "ambiguous-contacts";

    /// <param name="events">Every feed's events, concatenated in a deterministic order.</param>
    /// <param name="profileGolden">(sourceSystem, sourceRecordId) → the live golden id, or null.</param>
    /// <param name="vinPeriods">VIN → every golden's link to it.</param>
    public static EventAttachResult Attach(
        IEnumerable<SourceEvent> events,
        Func<string, string, long?> profileGolden,
        IReadOnlyDictionary<string, IReadOnlyList<VinPeriod>> vinPeriods,
        EventAttachOptions? options = null)
    {
        options ??= EventAttachOptions.Default;
        var result = new EventAttachResult();
        var seen = new HashSet<(GoldenEventType, string)>();
        var targets = new List<long>(4);

        foreach (var e in events)
        {
            if (!seen.Add((e.Type, e.EventId))) { result.Duplicates++; continue; }
            Bump(result.Read, e.Type);

            targets.Clear();
            GoldenEventLink link;
            string? reason;
            if (e.IsProfileLinked)
            {
                link = GoldenEventLink.Profile;
                if (profileGolden(e.SourceSystem!, e.SourceRecordId!) is long id) { targets.Add(id); reason = null; }
                else reason = NoProfile;
            }
            else
                (link, reason) = ByVin(e, vinPeriods, options, targets);

            if (targets.Count == 0)
            {
                Bump(result.Unattached, e.Type);
                string key = $"{e.Type}/{reason ?? NoProfile}";
                result.UnattachedReasons[key] = result.UnattachedReasons.GetValueOrDefault(key) + 1;
                continue;
            }

            Bump(result.Attached, e.Type);
            if (targets.Count > 1) result.MultiGolden++;
            foreach (var gid in targets)
            {
                if (!result.ByGolden.TryGetValue(gid, out var list)) result.ByGolden[gid] = list = [];
                list.Add(new AttachedEvent(e, link));
                result.ByLink[link] = result.ByLink.GetValueOrDefault(link) + 1;
            }
        }
        return result;
    }

    private static (GoldenEventLink Link, string? Reason) ByVin(
        SourceEvent e, IReadOnlyDictionary<string, IReadOnlyList<VinPeriod>> vinPeriods,
        EventAttachOptions options, List<long> targets)
    {
        if (string.IsNullOrEmpty(e.Vin)) return (GoldenEventLink.VinOwner, NoVin);
        if (!vinPeriods.TryGetValue(e.Vin, out var periods) || periods.Count == 0)
            return (GoldenEventLink.VinOwner, UnknownVin);

        var owners = periods.Where(p => p.Role == VinPeriod.OwnerRole).ToList();
        if (owners.Count > 0)
        {
            foreach (var p in owners)
                if (e.Date is { } d ? OwnerCovers(p, d) : p.To is null)
                    AddDistinct(targets, p.GoldenId);
            return targets.Count > 0 ? (GoldenEventLink.VinOwner, null) : (GoldenEventLink.VinOwner, BeforeOwnership);
        }

        if (!options.VinContactFallback) return (GoldenEventLink.VinContact, NoOwner);

        var contacts = periods.Select(p => p.GoldenId).Distinct().ToList();
        if (contacts.Count == 1) { targets.Add(contacts[0]); return (GoldenEventLink.VinContact, null); }
        if (e.Date is { } date)
        {
            var covering = periods.Where(p => ContactCovers(p, date)).Select(p => p.GoldenId).Distinct().ToList();
            if (covering.Count == 1) { targets.Add(covering[0]); return (GoldenEventLink.VinContact, null); }
        }
        return (GoldenEventLink.VinContact, AmbiguousContacts);
    }

    /// <summary>Owner period [From, To): null bounds are open.</summary>
    public static bool OwnerCovers(VinPeriod p, DateOnly date) =>
        (p.From is null || p.From <= date) && (p.To is null || date < p.To);

    /// <summary>Service window [From, To], both inclusive: null bounds are open.</summary>
    public static bool ContactCovers(VinPeriod p, DateOnly date) =>
        (p.From is null || p.From <= date) && (p.To is null || date <= p.To);

    private static void AddDistinct(List<long> list, long id) { if (!list.Contains(id)) list.Add(id); }

    private static void Bump(Dictionary<GoldenEventType, int> d, GoldenEventType t) => d[t] = d.GetValueOrDefault(t) + 1;
}

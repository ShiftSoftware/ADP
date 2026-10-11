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
    /// the VIN has any: an event the owners leave unattached (before ownership, ambiguous owners)
    /// never falls through to a service contact.
    /// </summary>
    public bool VinContactFallback { get; init; }

    /// <summary>
    /// When more than one owner of a VIN can claim a VIN-only event, leave the event UNATTACHED with
    /// reason <see cref="GoldenEventAttach.AmbiguousOwners"/> instead of attaching it to every one of
    /// them. With this on, no event is attached to more than one golden
    /// (<see cref="EventAttachResult.MultiGolden"/> is 0 by construction).
    ///
    /// <para>"More than one owner" covers several owner periods covering the date, owners with no
    /// sale date, and a same-day sale to two goldens. The option therefore also shapes the ownership
    /// periods themselves (<see cref="OwnershipTimeline.OwnerPeriods"/>, which the resolve uses for
    /// the <c>vinlinks</c> / <c>vinowner</c> artifacts as well, so the vehicle card and the timeline
    /// keep agreeing): goldens whose first sale falls on the same day share one period instead of the
    /// tie going to the higher golden id, and owners with no sale date share the period before the
    /// first dated sale.</para>
    ///
    /// <para>Off by default: an event goes to every covering owner and is counted in
    /// <see cref="EventAttachResult.MultiGolden"/>, and the periods are built as they always were.</para>
    /// </summary>
    public bool AmbiguousOwnersUnattached { get; init; }

    public static EventAttachOptions Default { get; } = new();
}

/// <summary>An event as attached to one golden, with how it got there.</summary>
public sealed record AttachedEvent(SourceEvent Event, GoldenEventLink Link);

/// <summary>What one attachment pass did for ONE event type — the numbers a run log reports per type.</summary>
public sealed class EventTypeAttachStats
{
    /// <summary>Distinct events read (after duplicate removal).</summary>
    public int Read { get; internal set; }

    /// <summary>Events attached to at least one golden.</summary>
    public int Attached { get; internal set; }

    /// <summary>Events attached to no golden.</summary>
    public int Unattached { get; internal set; }

    /// <summary>Events dropped because an earlier event had the same (type, id).</summary>
    public int Duplicates { get; internal set; }

    /// <summary>Events attached to more than one golden (VIN-only events with several covering owners).</summary>
    public int MultiGolden { get; internal set; }

    /// <summary>Attached events by how they attached. An event on several goldens counts once.</summary>
    public SortedDictionary<GoldenEventLink, int> AttachedByLink { get; } = new();

    /// <summary>Unattached events by reason (see the constants on <see cref="GoldenEventAttach"/>).</summary>
    public SortedDictionary<string, int> UnattachedByReason { get; } = new(StringComparer.Ordinal);
}

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

    /// <summary>Everything above broken down per event type, plus duplicates and multi-golden
    /// attachments per type — one entry for every type that appeared in the input.</summary>
    public SortedDictionary<GoldenEventType, EventTypeAttachStats> ByType { get; } = new();

    /// <summary>Events dropped because an earlier event had the same (type, id).</summary>
    public int Duplicates { get; internal set; }

    /// <summary>VIN-only events that attached to more than one golden (co-owners with no ordering).
    /// Always 0 with <see cref="EventAttachOptions.AmbiguousOwnersUnattached"/>.</summary>
    public int MultiGolden { get; internal set; }

    public int TotalRead => Read.Values.Sum();
    public int TotalAttached => Attached.Values.Sum();
    public int TotalUnattached => Unattached.Values.Sum();

    /// <summary>The per-type entry for <paramref name="type"/>, created on first use.</summary>
    internal EventTypeAttachStats For(GoldenEventType type)
    {
        if (!ByType.TryGetValue(type, out var stats)) ByType[type] = stats = new EventTypeAttachStats();
        return stats;
    }

    /// <summary>One block per event type, no personal data — for run logs.</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        sb.Append($"events read {TotalRead:N0} / attached {TotalAttached:N0} / unattached {TotalUnattached:N0}");
        if (Duplicates > 0) sb.Append($" / duplicates dropped {Duplicates:N0}");
        sb.Append($"; goldens with events {ByGolden.Count:N0}");
        foreach (var (t, s) in ByType)
        {
            sb.Append($"\n    {t,-16} read {s.Read,9:N0}  attached {s.Attached,9:N0}  unattached {s.Unattached,9:N0}"
                      + $"  duplicates {s.Duplicates:N0}  on several goldens {s.MultiGolden:N0}");
            if (s.AttachedByLink.Count > 0)
                sb.Append("\n        attached: " + string.Join(" / ", s.AttachedByLink.Select(kv => $"{kv.Key} {kv.Value:N0}")));
            if (s.UnattachedByReason.Count > 0)
                sb.Append("\n        unattached: " + string.Join(" / ", s.UnattachedByReason.Select(kv => $"{kv.Key} {kv.Value:N0}")));
        }
        if (ByLink.Count > 0)
            sb.Append("\n    links (event x golden): " + string.Join(" / ", ByLink.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} {kv.Value:N0}")));
        if (MultiGolden > 0) sb.Append($"\n    VIN-only events on several goldens {MultiGolden:N0}");
        return sb.ToString();
    }
}

/// <summary>
/// The owner half of a VIN's ownership timeline: which golden held the vehicle when, from each
/// owner's first sale-grade date. Pure, so the period rule is testable on synthetic data; the
/// resolve builds the <c>vinlinks</c> / <c>vinowner</c> artifacts and the event attachment from
/// this one definition.
/// </summary>
public static class OwnershipTimeline
{
    /// <summary>
    /// One half-open period [From, To) per owner, in timeline order (first sale ascending with an
    /// unknown first sale first, then golden id).
    ///
    /// <para>Default (<paramref name="tiesShare"/> false): each owner's period ends where the next
    /// owner in timeline order starts. A same-day sale to two goldens gives the lower id the empty
    /// period [d, d) — the tie goes to the higher id — and two owners with no sale date leave the
    /// first one open-ended.</para>
    ///
    /// <para><paramref name="tiesShare"/> true (<see cref="EventAttachOptions.AmbiguousOwnersUnattached"/>):
    /// an owner's period ends at the first sale date strictly after its own. Goldens sharing a
    /// first-sale date share one period, and owners with no sale date share the period before the
    /// first dated sale, so every claim the evidence cannot order stays visible as an overlap.</para>
    /// </summary>
    /// <param name="owners">Every golden with sale-grade evidence on one VIN (each once) and its
    /// first sale date, null when unknown.</param>
    public static List<(long GoldenId, DateOnly? From, DateOnly? To)> OwnerPeriods(
        IEnumerable<(long GoldenId, DateOnly? FirstSale)> owners, bool tiesShare = false)
    {
        var ordered = owners.OrderBy(x => x.FirstSale ?? DateOnly.MinValue).ThenBy(x => x.GoldenId).ToList();
        var periods = new List<(long, DateOnly?, DateOnly?)>(ordered.Count);
        if (!tiesShare)
        {
            for (int i = 0; i < ordered.Count; i++)
                periods.Add((ordered[i].GoldenId, ordered[i].FirstSale, i + 1 < ordered.Count ? ordered[i + 1].FirstSale : null));
            return periods;
        }

        var starts = ordered.Where(x => x.FirstSale is not null).Select(x => x.FirstSale!.Value).Distinct().Order().ToList();
        foreach (var (id, first) in ordered)
        {
            DateOnly? to = null;
            foreach (var s in starts)
                if (first is null || s > first.Value) { to = s; break; }
            periods.Add((id, first, to));
        }
        return periods;
    }
}

/// <summary>
/// The attachment rules that turn a resolve's event feeds into per-golden timelines (pure — no
/// storage, so the rules are testable on synthetic data and identical wherever they run).
///
/// <para><b>Every event attaches at most once</b> by its (type, id): a later event with the same
/// pair is dropped (first wins) and counted as a duplicate.</para>
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
/// current owner(s), whose period is open-ended. Several covering owners: all of them by default
/// (counted in <see cref="EventAttachResult.MultiGolden"/>); none with
/// <see cref="EventAttachOptions.AmbiguousOwnersUnattached"/>, reason <see cref="AmbiguousOwners"/>
/// (a single covering owner whose first sale shares its day with another owner's is a same-day tie
/// and counts as ambiguous too). Uncovered cases:</para>
///   - the VIN is unknown to every golden: <see cref="UnknownVin"/>;
///   - the event predates every ownership period (pre-delivery work, dealer stock):
///     <see cref="BeforeOwnership"/> — deliberately NOT given to the first owner, unless the event
///     itself declares <see cref="SourceEvent.AttachBeforeFirstSaleWithinDays"/> and the VIN's first
///     sale starts within that many days after it: then it goes to that sale's owner as
///     <see cref="GoldenEventLink.VinPreDelivery"/> when the owner is unique, else it stays
///     unattached as <see cref="AmbiguousOwners"/>;
///   - the VIN has only service-contact links: <see cref="NoOwner"/>, unless
///     <see cref="EventAttachOptions.VinContactFallback"/> is on, which attaches to the VIN's single
///     contact, or the single contact whose service window covers the date; otherwise
///     <see cref="AmbiguousContacts"/>. The fallback never applies to a VIN that has an owner link;
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
    public const string AmbiguousOwners = "ambiguous-owners";

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
            var stats = result.For(e.Type);
            if (!seen.Add((e.Type, e.EventId))) { result.Duplicates++; stats.Duplicates++; continue; }
            Bump(result.Read, e.Type);
            stats.Read++;

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
                stats.Unattached++;
                reason ??= NoProfile;
                string key = $"{e.Type}/{reason}";
                result.UnattachedReasons[key] = result.UnattachedReasons.GetValueOrDefault(key) + 1;
                stats.UnattachedByReason[reason] = stats.UnattachedByReason.GetValueOrDefault(reason) + 1;
                continue;
            }

            Bump(result.Attached, e.Type);
            stats.Attached++;
            stats.AttachedByLink[link] = stats.AttachedByLink.GetValueOrDefault(link) + 1;
            if (targets.Count > 1) { result.MultiGolden++; stats.MultiGolden++; }
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
            if (targets.Count > 0)
            {
                if (options.AmbiguousOwnersUnattached && (targets.Count > 1 || SharesFirstSaleDay(owners, targets[0])))
                {
                    targets.Clear();
                    return (GoldenEventLink.VinOwner, AmbiguousOwners);
                }
                return (GoldenEventLink.VinOwner, null);
            }
            if (e.AttachBeforeFirstSaleWithinDays is int days && days > 0 && e.Date is { } date)
                return PreDelivery(owners, date, days, targets);
            return (GoldenEventLink.VinOwner, BeforeOwnership);
        }

        if (!options.VinContactFallback) return (GoldenEventLink.VinContact, NoOwner);

        var contacts = periods.Select(p => p.GoldenId).Distinct().ToList();
        if (contacts.Count == 1) { targets.Add(contacts[0]); return (GoldenEventLink.VinContact, null); }
        if (e.Date is { } date2)
        {
            var covering = periods.Where(p => ContactCovers(p, date2)).Select(p => p.GoldenId).Distinct().ToList();
            if (covering.Count == 1) { targets.Add(covering[0]); return (GoldenEventLink.VinContact, null); }
        }
        return (GoldenEventLink.VinContact, AmbiguousContacts);
    }

    /// <summary>
    /// A dated event no owner period covers, from a source that declared a pre-delivery window: it
    /// goes to the owner of the VIN's FIRST sale when that sale starts at most <paramref name="days"/>
    /// days after the event and has exactly one owner.
    /// </summary>
    private static (GoldenEventLink Link, string? Reason) PreDelivery(
        List<VinPeriod> owners, DateOnly date, int days, List<long> targets)
    {
        // Only an event that predates EVERY ownership period (an owner with no known start reaches
        // back indefinitely, so it never predates that one).
        if (owners.Any(p => p.From is not { } from || from <= date))
            return (GoldenEventLink.VinPreDelivery, BeforeOwnership);

        var firstSale = owners.Min(p => p.From!.Value);
        if (firstSale.DayNumber - date.DayNumber > days)
            return (GoldenEventLink.VinPreDelivery, BeforeOwnership);

        var firstOwners = owners.Where(p => p.From == firstSale).Select(p => p.GoldenId).Distinct().ToList();
        if (firstOwners.Count != 1)
            return (GoldenEventLink.VinPreDelivery, AmbiguousOwners);

        targets.Add(firstOwners[0]);
        return (GoldenEventLink.VinPreDelivery, null);
    }

    /// <summary>True when another golden's owner period starts on the same first-sale day (or both
    /// are unknown) as <paramref name="goldenId"/>'s — a same-day tie the periods broke by golden id.</summary>
    private static bool SharesFirstSaleDay(List<VinPeriod> owners, long goldenId)
    {
        foreach (var p in owners)
        {
            if (p.GoldenId != goldenId) continue;
            foreach (var q in owners)
                if (q.GoldenId != goldenId && q.From == p.From) return true;
        }
        return false;
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

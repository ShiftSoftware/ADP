using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// One dated customer event from a tenant source — a workshop visit, an inspection, a warranty
/// claim, a recall, an activation — as an <see cref="IEventFeed"/> yields it. The resolve attaches
/// it to golden customers and stages one <c>events</c> artifact per golden.
///
/// An event is linked one of two ways:
///   - <b>profile-linked</b>: <see cref="SourceSystem"/> + <see cref="SourceRecordId"/> name the
///     source profile the event belongs to (the same key an <see cref="ISourceFeed"/> emits), and the
///     event follows that profile's identity;
///   - <b>VIN-only</b>: both are null, and the event attaches through the VIN's ownership timeline
///     (see <see cref="GoldenEventAttach"/>).
///
/// Everything here is display data for a timeline, so it must carry NO personal data: no names,
/// phones, e-mails, addresses or ID numbers in <see cref="Title"/> or <see cref="Facts"/>. Dealer
/// references, VINs, dates, codes, statuses and mileages are the intended content.
/// </summary>
/// <param name="Type">What kind of event this is.</param>
/// <param name="EventId">The source's stable id for the event, unique within <paramref name="Type"/>
/// across every feed of one resolve. Duplicates are dropped (first wins) and counted.</param>
/// <param name="Date">The event's LOCAL calendar date in the tenant's time zone; null when the source
/// has none (an open recall). Feeds convert UTC instants before emitting — the engine never guesses
/// a zone.</param>
/// <param name="Vin">The vehicle, normalized like the identity feeds normalize it (so it meets the
/// VIN keys of the ownership timeline). Required for VIN-only events.</param>
/// <param name="SourceSystem">Profile link, half 1 — null for a VIN-only event.</param>
/// <param name="SourceRecordId">Profile link, half 2 — null for a VIN-only event.</param>
/// <param name="DealerId">The dealer / company the event happened at, in the tenant's own id space.</param>
/// <param name="DealerName">Display name for <paramref name="DealerId"/>; optional.</param>
/// <param name="Title">Short, non-personal description.</param>
/// <param name="Facts">Small non-personal facts (mileage, status, campaign code, claim type …).</param>
public sealed record SourceEvent(
    GoldenEventType Type,
    string EventId,
    DateOnly? Date,
    string? Vin,
    string? SourceSystem,
    string? SourceRecordId,
    string? DealerId,
    string? DealerName,
    string Title,
    IReadOnlyDictionary<string, string>? Facts = null)
{
    /// <summary>True when the event names a source profile; false for a VIN-only event.</summary>
    public bool IsProfileLinked => !string.IsNullOrEmpty(SourceSystem) && !string.IsNullOrEmpty(SourceRecordId);
}

/// <summary>
/// A source of <see cref="SourceEvent"/>s for one resolve — the event-side sibling of
/// <see cref="ISourceFeed"/>, with the same contract:
///   - DETERMINISTIC for unchanged source data (stable ids, stable order, no wall-clock), because
///     the staged events are content-hashed and a re-run must be zero-delta;
///   - THROW when the source is unreachable — an empty result would tombstone every golden's
///     timeline (the resolve refuses that as a mass change, but an explicit failure is clearer).
///
/// Supplying no event feeds at all is the default and changes nothing: no <c>events</c> artifact
/// is read, staged or tombstoned.
/// </summary>
public interface IEventFeed
{
    /// <summary>A stable name for logs and metrics.</summary>
    string Name { get; }

    IReadOnlyList<SourceEvent> Read();
}

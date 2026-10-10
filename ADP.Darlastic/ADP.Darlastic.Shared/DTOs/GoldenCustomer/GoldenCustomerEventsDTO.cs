using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// One page of a golden customer's timeline events, newest first — served from the
/// <c>events</c> artifact the resolve stages per golden in <c>ProjectionState</c>, so it is exactly
/// as fresh as the last resolve run and needs no host table of its own.
///
/// <para>Ordering is fixed: <see cref="GoldenCustomerEventDTO.Date"/> descending with undated
/// events LAST (an undated event is typically an open item, e.g. a recall not yet repaired, and
/// has no place on a dated timeline), then <see cref="GoldenCustomerEventDTO.Type"/>, then
/// <see cref="GoldenCustomerEventDTO.EventID"/>. Paging is applied after the type filter.</para>
///
/// <para>Carries no personal data: events hold dates, VINs, dealer references and short
/// non-personal facts only. The customer's own attributes stay behind the masked detail read.</para>
/// </summary>
public class GoldenCustomerEventsDTO
{
    /// <summary>The LIVE identity ID, after redirect-chasing.</summary>
    public string? ID { get; set; }

    /// <summary>The ID the caller asked for; differs from <see cref="ID"/> when it was merged away.</summary>
    public string? RequestedID { get; set; }

    public bool WasRedirected { get; set; }

    /// <summary>Events matching the type filter, before paging.</summary>
    public int Total { get; set; }

    public int Skip { get; set; }

    public int Top { get; set; }

    /// <summary>Event count per type over ALL of this golden's events (ignores the type filter),
    /// keyed by <see cref="GoldenEventType"/> name — enough to render filter chips in one call.</summary>
    public Dictionary<string, int> CountsByType { get; set; } = new();

    public List<GoldenCustomerEventDTO> Events { get; set; } = new();
}

/// <summary>One timeline event attached to a golden customer.</summary>
public class GoldenCustomerEventDTO
{
    public GoldenEventType Type { get; set; }

    /// <summary>The source's stable id for the event, unique within <see cref="Type"/>.</summary>
    public string EventID { get; set; } = default!;

    /// <summary>The event's local calendar date in the tenant's time zone. Null when the source has no
    /// date for it (e.g. a recall that is still open).</summary>
    public DateOnly? Date { get; set; }

    public string? VIN { get; set; }

    /// <summary>How this event reached the golden — see <see cref="GoldenEventLink"/>.</summary>
    public GoldenEventLink Link { get; set; }

    /// <summary>The source record the event names, for <see cref="GoldenEventLink.Profile"/> links.</summary>
    public string? SourceSystem { get; set; }

    public string? SourceRecordId { get; set; }

    /// <summary>The dealer / company the event happened at, as the tenant's company id.</summary>
    public string? DealerID { get; set; }

    /// <summary>The dealer's display name as the feed supplied it. Hosts with a localized company
    /// catalogue should prefer their own name for <see cref="DealerID"/>.</summary>
    public string? DealerName { get; set; }

    /// <summary>A short, non-personal description (e.g. a campaign code and title).</summary>
    public string Title { get; set; } = "";

    /// <summary>Small non-personal facts (mileage, status, campaign code, claim type …), by key.</summary>
    public Dictionary<string, string> Facts { get; set; } = new();
}

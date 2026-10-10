using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Darlastic.Shared;

/// <summary>
/// The kinds of dated event a resolve can attach to a golden customer — the vocabulary of the
/// customer timeline. Serialized by NAME everywhere (staged payloads, API responses), never by
/// number: the staged artifact outlives any one build, and a renumbered member must not silently
/// relabel history. Renaming a member is therefore a breaking change to staged data.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GoldenEventType>))]
public enum GoldenEventType
{
    /// <summary>A workshop visit (one repair order), from a dealer management system.</summary>
    ServiceVisit = 1,

    /// <summary>A vehicle inspection recorded by the distributor or a dealer.</summary>
    Inspection = 2,

    /// <summary>A warranty claim filed for the vehicle.</summary>
    WarrantyClaim = 3,

    /// <summary>A safety recall / special service campaign that affects the vehicle, repaired or open.</summary>
    SafetyRecall = 4,

    /// <summary>The vehicle's warranty activation (the customer-at-sale record).</summary>
    Activation = 5,
}

/// <summary>
/// How an event reached a golden. Consumers should show the difference: a profile link is the
/// source's own statement about who the customer was; a VIN link is the engine's inference from
/// who held the vehicle at the time.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GoldenEventLink>))]
public enum GoldenEventLink
{
    /// <summary>The event names a source record, and that record belongs to this golden.</summary>
    Profile = 1,

    /// <summary>VIN-only event; this golden held an owner-role link to the VIN covering the event date
    /// (or, for an undated event, holds the current ownership period).</summary>
    VinOwner = 2,

    /// <summary>VIN-only event on a VIN with no owner link at all; this golden is the VIN's only
    /// service contact (or the only one whose service window covers the date). Opt-in per tenant.</summary>
    VinContact = 3,
}

using System;

namespace ShiftSoftware.ADP.Surveys.Data.Entities;

/// <summary>
/// Durable progress cursor for pull-based trigger ingestion (<c>TriggerPullService</c> in
/// the API package). One row per pull source, keyed by the trigger <c>eventKind</c>;
/// <see cref="LastSourceRowID"/> is the highest upstream row ID already OFFERED to
/// trigger ingest (regardless of whether it matched a trigger).
///
/// Persisted in the DB — not in worker memory — so progress survives restarts, works
/// under short-lived runners (serverless timers), and never depends on the runner being
/// a singleton: two overlapping runners re-offer at most one batch and the engine's
/// DB-level dedup absorbs it.
///
/// Deliberately NOT a ShiftEntity: operational state, not domain data — no audit trail,
/// no soft delete, no hashids.
/// </summary>
public class TriggerPullCursor
{
    /// <summary>The stream key — the trigger <c>eventKind</c> the source feeds.</summary>
    public string ID { get; set; } = "";

    public long LastSourceRowID { get; set; }

    public DateTimeOffset LastAdvancedAt { get; set; }

    /// <summary>
    /// Upstream row currently blocking the drain (the batch's first Failed item), while it
    /// is inside its retry budget. Null when nothing is blocking.
    /// </summary>
    public long? RetryRowID { get; set; }

    /// <summary>How many consecutive scans <see cref="RetryRowID"/> has failed.</summary>
    public int RetryCount { get; set; }
}

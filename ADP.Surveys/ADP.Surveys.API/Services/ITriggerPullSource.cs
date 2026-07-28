using ShiftSoftware.ADP.Surveys.Shared.Triggers;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// The host seam for pull-based trigger ingestion. The engine
/// (<see cref="TriggerPullService"/> + <see cref="TriggerPullWorker"/>) owns the durable
/// cursor, batch pacing, advancement/retry semantics, and logging; the host implements
/// ONLY this interface — a query over its upstream data source plus a projection to
/// trigger candidates — and registers it via
/// <c>AddSurveysTriggerPull&lt;TSource&gt;</c>.
///
/// Contract:
///  - <see cref="EventKind"/> is the trigger eventKind these candidates match AND the
///    cursor row key. One source per eventKind; duplicate kinds are a config error.
///  - Upstream row IDs must be <b>monotonic <see langword="long"/>s</b> (identity /
///    append-only source) — the cursor is a bookmark in that sequence.
///  - <see cref="ReadBatchAsync"/> returns rows with <c>RowId &gt; afterRowId</c>,
///    ordered ascending by RowId, at most <paramref name="take"/> of them, and only rows
///    first recorded on/after <paramref name="windowFloor"/> (the first-seen age bound).
///  - Rows the host never wants offered (deleted, no reachable recipient) are filtered
///    INSIDE the source query; the cursor advancing past them is how they are
///    deliberately and permanently passed over.
/// </summary>
public interface ITriggerPullSource
{
    /// <summary>The trigger <c>eventKind</c> this source feeds. Also the cursor row key.</summary>
    string EventKind { get; }

    Task<IReadOnlyList<TriggerPullRow>> ReadBatchAsync(
        long afterRowId, DateTimeOffset windowFloor, int take, CancellationToken cancellationToken);
}

/// <summary>One upstream row offered to ingest: its monotonic source ID plus the mapped candidate.</summary>
public sealed record TriggerPullRow(long RowId, TriggerCandidate Candidate);

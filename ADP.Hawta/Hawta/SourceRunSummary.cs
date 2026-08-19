namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The newest <c>meta.SyncRuns</c> record for one source — "did this source run, when, and how did
/// it end", reduced to the fact a health framework needs.
///
/// <para><b>Why it has to leave the write DB.</b> <c>meta.SyncRuns</c> lives on the agent's
/// instance-local disk, which is the only place it can live: DuckDB on a share is a corruption
/// risk. So a health check that reads it directly works only where the agent itself runs, and
/// silently stops working anywhere else — a check that passes because it cannot see. Publishing the
/// summary is what makes "has ingest stopped running?" answerable from outside the box.</para>
///
/// <para><b>No error text.</b> The status is the fact; the exception message stays in the write
/// DB's own run log, where an operator reads it. A published artifact is the wrong place for
/// arbitrary exception strings, which carry server names and paths.</para>
///
/// <para>Hawta records this. It does not decide whether the age or the status is acceptable —
/// that is a per-source delivery expectation, and it belongs to the health framework.</para>
/// </summary>
public sealed record SourceRunSummary(
    string SourceKey,
    string TargetTable,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Status,
    long RowsStaged,
    long RowsInserted,
    long RowsUpdated,
    long RowsTombstoned);

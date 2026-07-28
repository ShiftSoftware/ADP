using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShiftSoftware.ADP.Surveys.Data.Entities;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// One pull-ingestion scan: read the next batch of upstream rows past the durable
/// cursor from an <see cref="ITriggerPullSource"/>, offer them to
/// <see cref="TriggerIngestService"/> in-process, and advance the cursor.
///
/// Progress model: a DURABLE CURSOR (<see cref="TriggerPullCursor"/>, one row keyed by
/// eventKind) tracks the highest upstream row ID already offered. Each scan reads
/// <c>RowId &gt; cursor</c> in ID order, so a backlog larger than
/// <see cref="TriggerPullOptions.MaxRowsPerScan"/> drains across scans instead of
/// re-reading the same first page forever, and steady state never re-offers old rows.
/// The cursor lives in the DB — not worker memory — so it survives restarts, tolerates
/// short-lived runners (serverless timers), and does not require the runner to be a
/// singleton: overlapping runners re-offer at most one batch and the engine's DB-level
/// dedup absorbs it.
///
/// Advancement rules:
///  - No published+enabled trigger for the eventKind (<c>result.PublishedTriggers == 0</c>):
///    HOLD the cursor — otherwise enabling the puller before publishing the survey would
///    silently consume the whole backlog as NoMatch.
///  - SYSTEMIC failure (every item in the batch Failed — config/connectivity): hold the
///    cursor WITHOUT consuming the retry budget; the error repeats until fixed, nothing
///    is lost.
///  - GENUINE POISON row (neighbors succeed, one row keeps failing): retried for
///    <see cref="TriggerPullOptions.MaxRowRetries"/> consecutive scans, then skipped
///    with an ERROR log carrying the exact re-offer statement — one bad row can't stall
///    the drain forever, and nothing is ever dropped silently.
///  - NoMatch due to the authored filter (e.g. an eligibility-floor date) ADVANCES —
///    those rows were deliberately rejected and must not be re-offered forever.
///
/// This service is the timer-agnostic core (mirrors <see cref="TriggerSchedulerService"/>'s
/// PollOnce shape): always-on hosts loop it via <see cref="TriggerPullWorker"/>; a
/// serverless host calls <see cref="ScanOnceAsync"/> directly from its timer trigger.
/// </summary>
public class TriggerPullService
{
    private readonly ShiftDbContext db;
    private readonly TriggerIngestService ingest;
    private readonly TriggerPullOptions options;
    private readonly ILogger<TriggerPullService> logger;

    public TriggerPullService(
        ShiftDbContext db,
        TriggerIngestService ingest,
        TriggerPullOptions options,
        ILogger<TriggerPullService> logger)
    {
        this.db = db;
        this.ingest = ingest;
        this.options = options;
        this.logger = logger;
    }

    public async Task ScanOnceAsync(ITriggerPullSource source, CancellationToken ct = default)
    {
        var eventKind = source.EventKind;

        var cursor = await db.Set<TriggerPullCursor>()
            .FirstOrDefaultAsync(c => c.ID == eventKind, ct);
        var lastOfferedId = cursor?.LastSourceRowID ?? 0;

        // LookbackDays bounds how old a row may be when FIRST seen (initial enablement /
        // cursor reset); the cursor carries all forward progress. Rows the source filters
        // out have IDs below whatever the cursor advances to — they are implicitly and
        // permanently passed over.
        var windowFloor = DateTimeOffset.UtcNow.AddDays(-options.LookbackDays);

        var rows = await source.ReadBatchAsync(lastOfferedId, windowFloor, options.MaxRowsPerScan, ct);

        if (rows.Count == 0)
        {
            logger.LogDebug("Trigger pull [{EventKind}]: no new upstream rows past cursor {Cursor}", eventKind, lastOfferedId);
            return;
        }

        var request = new TriggerIngestRequest
        {
            EventKind = eventKind,
            Items = rows.Select(r => r.Candidate).ToList(),
        };

        var result = await ingest.IngestAsync(request, ct);

        if (result.PublishedTriggers == 0)
        {
            logger.LogWarning(
                "Trigger pull [{EventKind}]: {Count} candidates offered but no published+enabled trigger carries this eventKind — " +
                "holding the cursor at {Cursor} so these rows are re-offered once a trigger goes live. " +
                "Publish the survey (with its trigger) to start consuming.",
                eventKind, rows.Count, lastOfferedId);
            return;
        }

        // Advance to the last row before the first Failed item (result.Items is 1:1 with
        // rows, same order). Rows after a failure may already have produced instances —
        // re-offering them next scan just re-reports Skipped.
        long advanceTo = lastOfferedId;
        int firstFailedIndex = -1;
        for (int i = 0; i < result.Items.Count && i < rows.Count; i++)
        {
            if (result.Items[i].Outcome == TriggerIngestOutcome.Failed)
            {
                firstFailedIndex = i;
                break;
            }
            advanceTo = rows[i].RowId;
        }

        long? retryRowId = null;
        var retryCount = 0;

        if (firstFailedIndex >= 0)
        {
            var failedRow = rows[firstFailedIndex];
            var error = result.Items[firstFailedIndex].Error;
            var anySucceeded = result.Items.Take(rows.Count).Any(x => x.Outcome != TriggerIngestOutcome.Failed);

            if (!anySucceeded)
            {
                logger.LogError(
                    "Trigger pull [{EventKind}]: every item in the batch failed (first: row {RowId}: {Error}) — " +
                    "systemic problem, not a poison row. Holding the cursor at {Cursor}; no retry budget consumed.",
                    eventKind, failedRow.RowId, error, lastOfferedId);
                return;
            }

            var attempts = (cursor?.RetryRowID == failedRow.RowId ? cursor.RetryCount : 0) + 1;
            if (attempts >= options.MaxRowRetries)
            {
                advanceTo = failedRow.RowId;
                logger.LogError(
                    "Trigger pull [{EventKind}]: giving up on upstream row {RowId} after {Attempts} scans ({Error}) — " +
                    "advancing past it so the drain continues. NO instance exists for it. To re-offer after fixing the cause: " +
                    "UPDATE [Surveys].[TriggerPullCursor] SET LastSourceRowID = {Previous} WHERE ID = '{EventKind}'",
                    eventKind, failedRow.RowId, attempts, error, failedRow.RowId - 1, eventKind);
            }
            else
            {
                retryRowId = failedRow.RowId;
                retryCount = attempts;
                logger.LogWarning(
                    "Trigger pull [{EventKind}]: item for upstream row {RowId} failed (attempt {Attempt}/{Max}): {Error} — " +
                    "cursor holds at {Cursor}; the row retries next scan, then gets skipped with an error.",
                    eventKind, failedRow.RowId, attempts, options.MaxRowRetries, error, advanceTo);
            }
        }

        var retryStateChanged = cursor?.RetryRowID != retryRowId || (cursor?.RetryCount ?? 0) != retryCount;
        if (advanceTo > lastOfferedId || retryStateChanged)
        {
            // Plain last-writer save. Overlapping runners (not the normal shape) could
            // briefly move the cursor backward; the cost is one re-offered batch reported
            // Skipped, never duplicates or losses.
            if (cursor is null)
            {
                cursor = new TriggerPullCursor { ID = eventKind };
                db.Add(cursor);
            }
            cursor.LastSourceRowID = advanceTo;
            cursor.RetryRowID = retryRowId;
            cursor.RetryCount = retryCount;
            if (advanceTo > lastOfferedId)
                cursor.LastAdvancedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var level = result.Created > 0 || result.Failed > 0 ? LogLevel.Information : LogLevel.Debug;
        logger.Log(level,
            "Trigger pull [{EventKind}] scan: read={Read} created={Created} skipped={Skipped} failed={Failed} triggers={Triggers} cursor={Cursor}",
            eventKind, rows.Count, result.Created, result.Skipped, result.Failed, result.PublishedTriggers, advanceTo);

        if (rows.Count == options.MaxRowsPerScan)
            logger.LogInformation(
                "Trigger pull [{EventKind}] hit MaxRowsPerScan ({MaxRows}); backlog continues draining next scan from cursor {Cursor}",
                eventKind, options.MaxRowsPerScan, advanceTo);
    }
}

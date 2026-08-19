namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The set-based merge at the heart of the snapshot: staging → one atomic transaction of
/// guardrail → inserts (anti-join) → updates (<c>IS DISTINCT FROM</c> on <c>_RowHash</c> —
/// unchanged rows are never touched, which is what keeps <c>_LastModified</c> a truthful
/// source-change stamp) → tombstones (scope-bounded anti-join, full-universe sources only) →
/// <c>meta.SyncRuns</c> record.
///
/// <para><c>_ReplicationHash</c> covers only the columns declared capable of changing the
/// destination document. A source-only update still refreshes the stored row and
/// <c>_LastModified</c>, but leaves <c>_ReplicationModified</c> and the Cosmos queue alone.</para>
///
/// <para>Both modified stamps are MONOTONIC per row: every stamp is
/// <c>greatest(candidate, previous + 1µs)</c>. Without this, an agent-clock tombstone on a
/// source-stamped row (or a source save-date that regresses) would land at or below the
/// replicated watermark and silently drop the change from the dirty predicate forever —
/// clock skew between source and agent must never be able to un-queue a change.</para>
///
/// Callers hold the write gate; staging rows carry <c>_PrimaryKey</c>, <c>_RowHash</c>,
/// <c>_ReplicationHash</c> (via <see cref="RowHash.Expression"/>), and optionally <c>_SourceModified</c> (the source
/// row's own save date — preferred as <c>_LastModified</c> when ahead, else the run's UTC time).
/// </summary>
public static class SnapshotMerge
{
    public static SnapshotMergeResult Execute(
        SnapshotStore store,
        SnapshotTableDefinition table,
        StagingTable stagingTable,
        SnapshotMergeOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Source);

        var runId = options.RunId ?? Guid.NewGuid().ToString("N");
        var startedAt = DateTime.UtcNow;
        var runTimestamp = startedAt;

        // The staging alias contains '$', which the identifier rules forbid in table names —
        // so it can never be ambiguous with a caller-chosen table (e.g. a table named "s").
        var staging = stagingTable.QualifiedName;
        const string stg = "\"hawta$stg\"";
        var target = table.QualifiedName;
        var targetRef = $"\"{table.Name}\"";

        var pk = $"\"{BookkeepingColumns.PrimaryKey}\"";
        var hash = $"\"{BookkeepingColumns.RowHash}\"";
        var replicationHash = $"\"{BookkeepingColumns.ReplicationHash}\"";
        var scope = $"\"{BookkeepingColumns.SourceScope}\"";
        var deleted = $"\"{BookkeepingColumns.Deleted}\"";
        var lastModified = $"\"{BookkeepingColumns.LastModified}\"";
        var replicationModified = $"\"{BookkeepingColumns.ReplicationModified}\"";
        var changeSequence = $"\"{BookkeepingColumns.ChangeSequence}\"";
        var changeRecordedAt = $"\"{BookkeepingColumns.ChangeRecordedAt}\"";

        long rowsStaged = Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {staging}"));

        // Hand-built staging used by extension points and older callers has always supplied
        // _RowHash only. Default the replication hash to it; model-driven ingestors overwrite
        // this with their narrower declarative hash before entering the merge.
        store.Execute($"UPDATE {staging} SET {replicationHash} = {hash} WHERE {replicationHash} IS NULL");

        var invalidRows = Convert.ToInt64(store.ExecuteScalar(
            $"SELECT count(*) FROM {staging} WHERE {pk} IS NULL OR {hash} IS NULL OR {replicationHash} IS NULL"));
        if (invalidRows > 0)
        {
            var invalid = new SnapshotMergeResult(runId, SnapshotMergeStatus.FailedInvalidStagingRows, rowsStaged, 0, 0, 0);
            InsertRunRecord(store, table, options, runId, startedAt, invalid,
                $"{invalidRows} staging row(s) with NULL identity/content hash — the ingestor contract was not met.");
            return invalid;
        }

        var duplicateKeys = Convert.ToInt64(store.ExecuteScalar(
            $"SELECT count(*) - count(DISTINCT {pk}) FROM {staging}"));
        if (duplicateKeys > 0)
        {
            var failed = new SnapshotMergeResult(runId, SnapshotMergeStatus.FailedDuplicateStagingKeys, rowsStaged, 0, 0, 0);
            InsertRunRecord(store, table, options, runId, startedAt, failed,
                $"{duplicateKeys} duplicate _PrimaryKey value(s) in staging — the ingestor must dedup before merging.");
            return failed;
        }

        store.Execute("BEGIN TRANSACTION");
        try
        {
            // Would-be tombstones and the guardrail — computed before any mutation.
            long pendingDeletes = 0;
            if (options.DeletesEnabled)
            {
                pendingDeletes = Convert.ToInt64(store.ExecuteScalar(
                    $"""
                    SELECT count(*) FROM {target} t
                    WHERE t.{deleted} = false AND t.{scope} IS NOT DISTINCT FROM ?
                      AND t.{pk} NOT IN (SELECT {pk} FROM {staging})
                    """,
                    options.SourceScope));

                var liveRows = Convert.ToInt64(store.ExecuteScalar(
                    $"SELECT count(*) FROM {target} t WHERE t.{deleted} = false AND t.{scope} IS NOT DISTINCT FROM ?",
                    options.SourceScope));

                // The absolute floor exists to wave through trivial churn on small tables —
                // it must never wave through a TOTAL wipe (an empty staging against a small
                // family: the review-confirmed 29-row NonJPM case). Deleting the entire
                // scope is categorically suspicious at any size; ForceDeletes is the
                // intentional-purge path.
                var wipesEntireScope = liveRows > 0 && pendingDeletes == liveRows;

                var guardTripped =
                    !options.ForceDeletes
                    && liveRows > 0
                    && (wipesEntireScope
                        || (pendingDeletes > options.MinDeletedRowsAbsolute
                            && (double)pendingDeletes / liveRows > options.MaxDeletedPercent));

                if (guardTripped)
                {
                    store.Execute("ROLLBACK");
                    var aborted = new SnapshotMergeResult(runId, SnapshotMergeStatus.AbortedMassDelete,
                        rowsStaged, 0, 0, 0, pendingDeletes);
                    InsertRunRecord(store, table, options, runId, startedAt, aborted,
                        $"Mass-delete guardrail: {pendingDeletes} of {liveRows} live rows would be tombstoned " +
                        $"(> {options.MaxDeletedPercent:P0} and > {options.MinDeletedRowsAbsolute}). " +
                        "Re-run with ForceDeletes for an intentional purge.");
                    return aborted;
                }
            }

            // Updates: rows whose content actually changed, that are being resurrected, or that
            // this source is ADOPTING from another scope (same key, different _SourceScope —
            // without adoption the old scope's next merge would tombstone the row and this
            // scope's would resurrect it, churning Cosmos with delete/re-add pairs forever).
            // Adoption is counted separately: persistent RowsRescoped > 0 across runs means two
            // sources both claim the key — a config error to alarm on, not to hide.
            // The stamp prefers the source's own save date but can never move the row backward;
            // a changed row is therefore always strictly above any replicated watermark. The
            // failure ledger resets — a new row version deserves fresh replication attempts.
            var rowsRescoped = Convert.ToInt64(store.ExecuteScalar(
                $"""
                SELECT count(*) FROM {target} t
                JOIN {staging} AS {stg} ON t.{pk} = {stg}.{pk}
                WHERE t.{deleted} = false AND t.{scope} IS DISTINCT FROM ?
                """,
                options.SourceScope));

            // Mass-adoption guardrail — the delete guardrail's mirror image. A mis-pasted
            // connection string (dealer B pointed at dealer A's database) stages A's whole
            // universe under B's scope: without this, every row is adopted, re-stamped, and
            // re-replicated under the WRONG dealer coordinates, and the two scopes flip-flop
            // each cadence. Small overlaps (a handful of migrating rows) pass; a bulk
            // takeover aborts. ForceAdoptions is the intentional-migration path.
            var adoptionGuardTripped =
                !options.ForceAdoptions
                && rowsRescoped > options.MinAdoptedRowsAbsolute
                && rowsStaged > 0
                && (double)rowsRescoped / rowsStaged > options.MaxAdoptedPercent;

            if (adoptionGuardTripped)
            {
                store.Execute("ROLLBACK");
                var abortedAdoption = new SnapshotMergeResult(runId, SnapshotMergeStatus.AbortedMassAdoption,
                    rowsStaged, 0, 0, 0, RowsRescoped: rowsRescoped);
                InsertRunRecord(store, table, options, runId, startedAt, abortedAdoption,
                    $"Mass-adoption guardrail: {rowsRescoped} of {rowsStaged} staged rows are live under a DIFFERENT " +
                    $"_SourceScope (> {options.MaxAdoptedPercent:P0} and > {options.MinAdoptedRowsAbsolute}). " +
                    "This is the mis-pasted-connection-string signature. Re-run with ForceAdoptions for an intentional scope migration.");
                return abortedAdoption;
            }

            var updatePredicate =
                $"({targetRef}.{hash} IS DISTINCT FROM {stg}.{hash} " +
                $"OR {targetRef}.{deleted} = true " +
                $"OR {targetRef}.{scope} IS DISTINCT FROM ? " +
                $"OR ownership.\"SourceKey\" IS DISTINCT FROM ? " +
                $"OR {targetRef}.{changeSequence} IS NULL " +
                $"OR {targetRef}.{changeRecordedAt} IS NULL)";

            var updateFrom =
                $"FROM {target} AS {targetRef} JOIN {staging} AS {stg} " +
                $"ON {targetRef}.{pk} = {stg}.{pk} " +
                $"LEFT JOIN meta.SourceOwnership AS ownership " +
                $"ON ownership.\"TableName\" = ? AND ownership.\"PrimaryKey\" = {targetRef}.{pk}";

            var rowsUpdatedPlanned = Convert.ToInt64(store.ExecuteScalar(
                $"SELECT count(*) {updateFrom} WHERE {updatePredicate}",
                table.Name, options.SourceScope, options.Source));

            if (rowsUpdatedPlanned > 0)
            {
                var firstUpdateSequence = store.ReserveChangeSequences(rowsUpdatedPlanned);
                store.Execute(
                    $"""
                    CREATE OR REPLACE TEMP TABLE "hawta$versions" AS
                    SELECT {stg}.{pk} AS {pk},
                           CAST(? + row_number() OVER (ORDER BY {stg}.{pk}) - 1 AS BIGINT) AS {changeSequence}
                    {updateFrom}
                    WHERE {updatePredicate}
                    """,
                    firstUpdateSequence, table.Name, options.SourceScope, options.Source);
            }

            var assignments = string.Join(",\n    ", table.Columns.Select(c => $"\"{c.Name}\" = {stg}.\"{c.Name}\""));
            long rowsUpdated = 0;
            if (rowsUpdatedPlanned > 0)
            {
                rowsUpdated = store.Execute(
                    $"""
                UPDATE {target}
                SET {assignments},
                    {hash} = {stg}.{hash},
                    {replicationHash} = {stg}.{replicationHash},
                    {scope} = ?,
                    {lastModified} = greatest(
                        coalesce({stg}."_SourceModified", ?),
                        {targetRef}.{lastModified} + INTERVAL 1 MICROSECOND),
                    {replicationModified} = CASE
                        WHEN {targetRef}.{replicationHash} IS DISTINCT FROM {stg}.{replicationHash}
                          OR {targetRef}.{deleted} = true
                          OR {targetRef}.{scope} IS DISTINCT FROM ?
                        THEN greatest(
                            coalesce({stg}."_SourceModified", ?),
                            {targetRef}.{replicationModified} + INTERVAL 1 MICROSECOND)
                        ELSE {targetRef}.{replicationModified}
                    END,
                    {changeSequence} = versions.{changeSequence},
                    {changeRecordedAt} = ?,
                    {deleted} = false,
                    "{BookkeepingColumns.DeletedAt}" = NULL,
                    "{BookkeepingColumns.ReplicationAttempts}" = CASE
                        WHEN {targetRef}.{replicationHash} IS DISTINCT FROM {stg}.{replicationHash}
                          OR {targetRef}.{deleted} = true
                          OR {targetRef}.{scope} IS DISTINCT FROM ? THEN 0
                        ELSE {targetRef}."{BookkeepingColumns.ReplicationAttempts}" END,
                    "{BookkeepingColumns.ReplicationError}" = CASE
                        WHEN {targetRef}.{replicationHash} IS DISTINCT FROM {stg}.{replicationHash}
                          OR {targetRef}.{deleted} = true
                          OR {targetRef}.{scope} IS DISTINCT FROM ? THEN NULL
                        ELSE {targetRef}."{BookkeepingColumns.ReplicationError}" END
                FROM {staging} AS {stg}
                JOIN "hawta$versions" AS versions ON versions.{pk} = {stg}.{pk}
                WHERE {targetRef}.{pk} = {stg}.{pk}
                """,
                    options.SourceScope, runTimestamp,
                    options.SourceScope, runTimestamp,
                    runTimestamp,
                    options.SourceScope, options.SourceScope);
            }

            if (rowsUpdated != rowsUpdatedPlanned)
                throw new InvalidOperationException(
                    $"Source-version update for '{options.Source}' planned {rowsUpdatedPlanned} row(s) but changed {rowsUpdated}.");

            // Inserts: staging rows with no existing key.
            var rowsInsertedPlanned = Convert.ToInt64(store.ExecuteScalar(
                $"SELECT count(*) FROM {staging} AS {stg} WHERE NOT EXISTS " +
                $"(SELECT 1 FROM {target} t WHERE t.{pk} = {stg}.{pk})"));
            if (rowsInsertedPlanned > 0)
            {
                var firstInsertSequence = store.ReserveChangeSequences(rowsInsertedPlanned);
                store.Execute(
                    $"""
                    CREATE OR REPLACE TEMP TABLE "hawta$versions" AS
                    SELECT {stg}.{pk} AS {pk},
                           CAST(? + row_number() OVER (ORDER BY {stg}.{pk}) - 1 AS BIGINT) AS {changeSequence}
                    FROM {staging} AS {stg}
                    WHERE NOT EXISTS (SELECT 1 FROM {target} t WHERE t.{pk} = {stg}.{pk})
                    """,
                    firstInsertSequence);
            }

            long rowsInserted = 0;
            if (rowsInsertedPlanned > 0)
            {
                rowsInserted = store.Execute(
                    $"""
                INSERT INTO {target} ({table.QuotedColumnList}, {pk}, {hash}, {replicationHash}, {scope}, {lastModified}, {replicationModified}, {deleted},
                                      {changeSequence}, {changeRecordedAt})
                SELECT {string.Join(", ", table.Columns.Select(c => $"{stg}.\"{c.Name}\""))},
                       {stg}.{pk}, {stg}.{hash}, {stg}.{replicationHash}, ?,
                       coalesce({stg}."_SourceModified", ?), coalesce({stg}."_SourceModified", ?), false,
                       versions.{changeSequence}, ?
                FROM {staging} AS {stg}
                JOIN "hawta$versions" AS versions ON versions.{pk} = {stg}.{pk}
                """,
                    options.SourceScope, runTimestamp, runTimestamp, runTimestamp);
            }

            if (rowsInserted != rowsInsertedPlanned)
                throw new InvalidOperationException(
                    $"Source-version insert for '{options.Source}' planned {rowsInsertedPlanned} row(s) but changed {rowsInserted}.");

            // Tombstones: in-scope live rows absent from this full-universe staging.
            // NOT IN is NULL-safe here: the Failed:InvalidStagingRows pre-check guarantees
            // no NULL _PrimaryKey survives to this point. The failure ledger resets like the
            // update leg's — a tombstone IS a new row version, and a row that dead-lettered
            // on its content pushes must still get its Cosmos delete attempted.
            long rowsTombstoned = 0;
            if (options.DeletesEnabled && pendingDeletes > 0)
            {
                var firstDeleteSequence = store.ReserveChangeSequences(pendingDeletes);
                store.Execute(
                    $"""
                    CREATE OR REPLACE TEMP TABLE "hawta$versions" AS
                    SELECT {pk},
                           CAST(? + row_number() OVER (ORDER BY {pk}) - 1 AS BIGINT) AS {changeSequence}
                    FROM {target}
                    WHERE {deleted} = false
                      AND {scope} IS NOT DISTINCT FROM ?
                      AND {pk} NOT IN (SELECT {pk} FROM {staging})
                    """,
                    firstDeleteSequence, options.SourceScope);

                rowsTombstoned = store.Execute(
                    $"""
                    UPDATE {target} AS {targetRef}
                    SET {deleted} = true,
                        "{BookkeepingColumns.DeletedAt}" = ?,
                        {lastModified} = greatest(?, {lastModified} + INTERVAL 1 MICROSECOND),
                        {replicationModified} = greatest(?, {replicationModified} + INTERVAL 1 MICROSECOND),
                        {changeSequence} = versions.{changeSequence},
                        {changeRecordedAt} = ?,
                        "{BookkeepingColumns.ReplicationAttempts}" = 0,
                        "{BookkeepingColumns.ReplicationError}" = NULL
                    FROM "hawta$versions" AS versions
                    WHERE {targetRef}.{pk} = versions.{pk}
                    """,
                    runTimestamp, runTimestamp, runTimestamp, runTimestamp);

                if (rowsTombstoned != pendingDeletes)
                    throw new InvalidOperationException(
                        $"Source-version tombstone for '{options.Source}' planned {pendingDeletes} row(s) but changed {rowsTombstoned}.");
            }

            // The internal owner changes in the SAME transaction as the accepted row versions.
            // Missing/different ownership participated in updatePredicate, so this repair cannot
            // happen without the corresponding sequence advance.
            store.Execute(
                $"""
                DELETE FROM meta.SourceOwnership
                WHERE "TableName" = ?
                  AND "PrimaryKey" IN (SELECT {pk} FROM {staging})
                """,
                table.Name);
            store.Execute(
                $"""
                INSERT INTO meta.SourceOwnership ("TableName", "PrimaryKey", "SourceKey")
                SELECT ?, {pk}, ? FROM {staging}
                """,
                table.Name, options.Source);

            if (rowsTombstoned > 0)
            {
                store.Execute(
                    "DELETE FROM meta.SourceOwnership WHERE \"TableName\" = ? " +
                    "AND \"PrimaryKey\" IN (SELECT \"_PrimaryKey\" FROM \"hawta$versions\")",
                    table.Name);
                store.Execute(
                    "INSERT INTO meta.SourceOwnership (\"TableName\", \"PrimaryKey\", \"SourceKey\") " +
                    "SELECT ?, \"_PrimaryKey\", ? FROM \"hawta$versions\"",
                    table.Name, options.Source);
            }

            var result = new SnapshotMergeResult(runId, SnapshotMergeStatus.Succeeded,
                rowsStaged, rowsInserted, rowsUpdated, rowsTombstoned, RowsRescoped: rowsRescoped);

            InsertRunRecord(store, table, options, runId, startedAt, result, error: null);

            store.Execute("COMMIT");
            return result;
        }
        catch (Exception exception)
        {
            try { store.Execute("ROLLBACK"); } catch { /* connection-level failure; original exception wins */ }

            // The run record IS the alarm surface — a crashed run must be visible, not absent.
            try
            {
                InsertRunRecord(store, table, options, runId, startedAt,
                    new SnapshotMergeResult(runId, SnapshotMergeStatus.Failed, rowsStaged, 0, 0, 0),
                    exception.Message);
            }
            catch { /* recording must never mask the original failure */ }

            throw;
        }
    }

    internal static void InsertRunRecord(
        SnapshotStore store, SnapshotTableDefinition table, SnapshotMergeOptions options,
        string runId, DateTime startedAt, SnapshotMergeResult result, string? error)
    {
        store.Execute(
            """
            INSERT INTO meta.SyncRuns
            ("RunId", "Source", "TargetTable", "StartedAt", "FinishedAt",
             "RowsStaged", "RowsInserted", "RowsUpdated", "RowsTombstoned", "Status", "Error")
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            runId, options.Source, table.Name, startedAt, DateTime.UtcNow,
            result.RowsStaged, result.RowsInserted, result.RowsUpdated, result.RowsTombstoned,
            StatusText(result.Status), error);
    }

    private static string StatusText(SnapshotMergeStatus status) => status switch
    {
        SnapshotMergeStatus.Succeeded => "Succeeded",
        SnapshotMergeStatus.AbortedMassDelete => "Aborted:MassDelete",
        SnapshotMergeStatus.AbortedMassAdoption => "Aborted:MassAdoption",
        SnapshotMergeStatus.FailedDuplicateStagingKeys => "Failed:DuplicateStagingKeys",
        SnapshotMergeStatus.FailedInvalidStagingRows => "Failed:InvalidStagingRows",
        SnapshotMergeStatus.Failed => "Failed:Exception",
        SnapshotMergeStatus.SkippedSourceAbsent => "Skipped:SourceAbsent",
        SnapshotMergeStatus.SkippedSourceEmpty => "Skipped:SourceEmpty",
        SnapshotMergeStatus.SkippedSourceUnchanged => "Skipped:SourceUnchanged",
        _ => status.ToString(),
    };
}

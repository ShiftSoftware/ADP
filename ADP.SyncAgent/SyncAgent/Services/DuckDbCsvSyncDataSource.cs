using CsvHelper.Configuration.Attributes;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
// IStorageService is intentionally not referenced: the data source reads <see cref="DuckDbCsvSyncDataSourceConfigurations{TCsv}.CsvFilePath"/>
// directly. Fetching the CSV from a remote store, if needed, is the caller's responsibility.

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// CSV sync data source that uses DuckDB to detect changes between the previous source snapshot
/// and a new CSV file, persisting the resulting work queue across runs. Replaces the
/// LibGit2Sharp-based diff used by <see cref="CsvHelperCsvSyncDataSource{TCsv, TDestination}"/>
/// for large files where the in-memory diff causes OOM.
/// </summary>
/// <remarks>
/// Layout per sync (one DuckDB file per sync, single-writer):
/// <list type="bullet">
///   <item><c>{TCsv}_source</c> — last successfully promoted snapshot. Diff baseline.</item>
///   <item><c>{TCsv}_changes</c> — work queue (model columns + change metadata).
///         <c>_PrimaryKey</c> is the table's PK so coalescing across runs is automatic.</item>
///   <item><c>{TCsv}_staging</c> — TEMP table loaded each run via DuckDB COPY.</item>
/// </list>
/// </remarks>
public class DuckDbCsvSyncDataSource<TCsv, TDestination>
    : ISyncDataAdapter<TCsv, TDestination, DuckDbCsvSyncDataSourceConfigurations<TCsv, TDestination>, DuckDbCsvSyncDataSource<TCsv, TDestination>>
    where TCsv : SyncCsvBase, new()
    where TDestination : class
{
    private const string PrimaryKeyColumn = "_PrimaryKey";
    private const string RowHashColumn = "_RowHash";
    private const string LoadedAtColumn = "_LoadedAt";
    private const string ChangeTypeColumn = "_ChangeType";
    private const string DetectedAtColumn = "_DetectedAt";
    private const string AttemptCountColumn = "_AttemptCount";
    private const string LastAttemptAtColumn = "_LastAttemptAt";
    private const string LastErrorColumn = "_LastError";
    private const string StatusColumn = "_Status";
    private const string BatchIdColumn = "_BatchId";

    private DuckDBConnection? connection;

    // Reflection is resolved once per closed generic type (TCsv is fixed per closed type), not per
    // batch/row: the model/materializable property arrays and the compiled per-property setters used
    // to hydrate a TCsv. Building setters via Expression replaces per-row PropertyInfo.SetValue
    // (reflection + boxing) with direct delegate calls — the only rows×columns cost in the pipeline.
    private static PropertyInfo[]? cachedModelProperties;
    private static PropertyInfo[]? cachedMaterializableProperties;
    private static Action<TCsv, object?>[]? cachedMaterializeSetters;

    private Guid? currentBatchId;
    private SyncActionType? currentBatchActionType;

    // Lazily-compiled accessors for DestinationKey (one per key component). Built once per run.
    private Func<TDestination, object?>[]? destinationKeyAccessors;
    private bool destinationKeyAccessorsBuilt;

    // Lazily-compiled accessor for the optional SourceKey expression. Built once per run.
    private Func<TCsv, object?>? sourceKeyAccessor;
    private bool sourceKeyAccessorBuilt;

    public DuckDbCsvSyncDataSourceConfigurations<TCsv, TDestination>? Configurations { get; private set; }
    public ISyncEngine<TCsv, TDestination> SyncService { get; private set; } = default!;

    public DuckDbCsvSyncDataSource()
    {
    }

    public DuckDbCsvSyncDataSource<TCsv, TDestination> SetSyncService(ISyncEngine<TCsv, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    public ISyncEngine<TCsv, TDestination> Configure(DuckDbCsvSyncDataSourceConfigurations<TCsv, TDestination> configurations, bool configureSyncService = true)
    {
        this.Configurations = configurations;

        if (!configureSyncService)
            return SyncService;

        var previousPreparing = SyncService.Preparing;
        var previousActionStarted = SyncService.ActionStarted;
        var previousActionCompleted = SyncService.ActionCompleted;
        var previousBatchCompleted = SyncService.BatchCompleted;
        var previousSucceeded = SyncService.Succeeded;
        var previousFinished = SyncService.Finished;

        SyncService
            .SetupPreparing(async input =>
            {
                var prevResult = SyncPreparingResponseAction.Succeeded;
                if (previousPreparing is not null)
                    prevResult = await previousPreparing(input);

                var current = await Preparing(input);

                if (prevResult == SyncPreparingResponseAction.Skiped || current == SyncPreparingResponseAction.Skiped)
                    return SyncPreparingResponseAction.Skiped;
                if (prevResult == SyncPreparingResponseAction.Succeeded && current == SyncPreparingResponseAction.Succeeded)
                    return SyncPreparingResponseAction.Succeeded;
                return SyncPreparingResponseAction.Failed;
            })
            .SetupActionStarted(async input =>
            {
                var prev = previousActionStarted is null || await previousActionStarted(input);
                return prev && await ActionStarted(input);
            })
            .SetupSourceTotalItemCount(SourceTotalItemCount)
            .SetupGetSourceBatchItems(GetSourceBatchItems)
            .SetupBatchCompleted(async input =>
            {
                var prev = previousBatchCompleted is null || await previousBatchCompleted(input);
                var current = await BatchCompleted(input);
                return prev && current;
            })
            .SetupActionCompleted(async input =>
            {
                var prev = previousActionCompleted is null || await previousActionCompleted(input);
                return prev && await ActionCompleted(input);
            })
            .SetupSucceeded(async input =>
            {
                if (previousSucceeded is not null)
                    await previousSucceeded(input);
                await Succeeded(input);
            })
            .SetupFinished(async input =>
            {
                if (previousFinished is not null)
                    await previousFinished(input);
                await Finished(input);
            });

        return SyncService;
    }

    #region Preparing pipeline

    public async ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        var config = RequireConfig();

        try
        {
            await input.SyncProgressIndicators.LogInformation("Opening DuckDB connection.");
            OpenConnection();

            await input.SyncProgressIndicators.LogInformation("Ensuring source/changes tables exist.");
            EnsureSourceTable();
            EnsureChangesTable();

            await input.SyncProgressIndicators.LogInformation("Validating schema against model.");
            ValidateSchema();

            await input.SyncProgressIndicators.LogInformation("Resetting any in-flight rows from a previous crashed run.");
            ResetInFlightRows();

            var revived = RequeueExpiredDeadRows();
            if (revived > 0)
                await input.SyncProgressIndicators.LogWarning(
                    "Re-queued {0} Dead change(s) that had rested longer than DeadRetryAfter ({1}); they get a fresh set of attempts this run.",
                    revived, config.DeadRetryAfter);

            await input.SyncProgressIndicators.LogInformation($"Loading CSV '{config.CsvFilePath}' into DuckDB staging table.");
            CreateStagingTable();
            CopyCsvIntoStaging();
            ComputeStagingKeyAndHash();

            var duplicateRowsCollapsed = DeduplicateStagingByPrimaryKey();
            if (duplicateRowsCollapsed > 0)
                await input.SyncProgressIndicators.LogInformation(
                    $"Collapsed {duplicateRowsCollapsed} duplicate CSV row(s) sharing a primary key — kept the latest occurrence (override).");

            await input.SyncProgressIndicators.LogInformation("Cancelling pending changes invalidated by the new source, then computing diff.");
            // Group the stale-cancel DELETE and the diff INSERTs into one transaction so the whole
            // diff lands with a single checkpoint instead of one fsync per statement.
            // Add/Update are key-based: a row that exists in staging is never enqueued as Delete,
            // so a deletion that "comes back" or a row that moved between buckets cannot appear as
            // both Delete and Add/Update in the same diff. The git-diff ProccessDeletedItems hook
            // is therefore unnecessary here — dedup is implicit.
            InTransaction(() =>
            {
                CancelStaleChanges();
                if (IsActionEnabled(SyncActionType.Add))
                    InsertAdds();
                if (IsActionEnabled(SyncActionType.Update))
                    InsertUpdates();
                if (IsActionEnabled(SyncActionType.Delete))
                    InsertDeletes();
            });

            DropStaging();

            var pendingCount = CountPendingChanges();
            await input.SyncProgressIndicators.LogInformation($"Diff complete. {pendingCount} pending change(s) in queue.");

            return pendingCount == 0
                ? SyncPreparingResponseAction.Skiped
                : SyncPreparingResponseAction.Succeeded;
        }
        catch (Exception ex)
        {
            await input.SyncProgressIndicators.LogError(ex, "DuckDbCsvSyncDataSource.Preparing failed.");
            return SyncPreparingResponseAction.Failed;
        }
    }

    private void OpenConnection()
    {
        var config = RequireConfig();

        var connectionString = config.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (string.IsNullOrWhiteSpace(config.DuckDbFilePath))
                throw new InvalidOperationException(
                    "DuckDbCsvSyncDataSource requires either ConnectionString or DuckDbFilePath to be set.");

            var filePath = config.DuckDbFilePath!;
            var fileDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(fileDir))
                Directory.CreateDirectory(fileDir);

            connectionString = $"Data Source={filePath}";
        }

        connection = new DuckDBConnection(connectionString);
        connection.Open();

        ApplyDuckDbRuntimeSettings();
    }

    private void ApplyDuckDbRuntimeSettings()
    {
        var config = RequireConfig();

        // Only memory_limit is configured here. DuckDB picks its own spill directory next to the
        // database file when the limit is exceeded — managing that location is an environment
        // concern, not a sync-engine concern.
        if (config.MemoryLimitMB > 0)
            ExecuteNonQuery($"SET memory_limit='{config.MemoryLimitMB}MB'");
    }

    private void EnsureSourceTable()
    {
        // _PrimaryKey is the natural-key fingerprint for the row. Declaring it as the table's
        // PRIMARY KEY enables INSERT OR REPLACE during promotion and indexes diff joins.
        var sql = DuckDbSchemaHelpers.BuildCreateTableSql(
            GetSourceTableName(),
            GetModelProperties(),
            extraColumns: new[]
            {
                $"{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} VARCHAR PRIMARY KEY",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)} VARCHAR",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn)} TIMESTAMP"
            });

        ExecuteNonQuery(sql);
    }

    private void EnsureChangesTable()
    {
        var sql = DuckDbSchemaHelpers.BuildCreateTableSql(
            GetChangesTableName(),
            GetModelProperties(),
            extraColumns: new[]
            {
                $"{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} VARCHAR PRIMARY KEY",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)} VARCHAR",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn)} TIMESTAMP",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn)} INTEGER",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(DetectedAtColumn)} TIMESTAMP",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn)} INTEGER",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(LastAttemptAtColumn)} TIMESTAMP",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(LastErrorColumn)} VARCHAR",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)} INTEGER",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn)} UUID"
            });

        ExecuteNonQuery(sql);

        // Composite index covering the per-batch claim query
        //   WHERE _Status=Pending AND _ChangeType=X ORDER BY _DetectedAt LIMIT N
        // Without _DetectedAt in the index, each batch re-scans + re-sorts every still-Pending
        // row of the action — O(N²/batch_size) total work over a full-table sync.
        ExecuteNonQuery(
            $"CREATE INDEX IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName() + "_status_idx")} " +
            $"ON {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} ({DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)}, {DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn)}, {DuckDbSchemaHelpers.QuoteIdentifier(DetectedAtColumn)})");

        ExecuteNonQuery(
            $"CREATE INDEX IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName() + "_batch_idx")} " +
            $"ON {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} ({DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn)})");
    }

    private void ValidateSchema()
    {
        // Schema evolution is intentionally out of scope. If existing source columns differ from
        // the model, abort early with a clear message — operator deletes the file and re-syncs.
        var expected = GetModelProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        expected.Add(PrimaryKeyColumn);
        expected.Add(RowHashColumn);
        expected.Add(LoadedAtColumn);

        var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = connection!.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({DuckDbSchemaHelpers.QuoteIdentifier(GetSourceTableName())})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            actual.Add(reader.GetString(reader.GetOrdinal("name")));

        var missingFromTable = expected.Except(actual).ToList();
        var unexpectedInTable = actual.Except(expected).ToList();
        if (missingFromTable.Count > 0 || unexpectedInTable.Count > 0)
        {
            throw new InvalidOperationException(
                $"DuckDB source table '{GetSourceTableName()}' schema does not match model {typeof(TCsv).Name}. " +
                $"Missing columns: [{string.Join(", ", missingFromTable)}]. " +
                $"Unexpected columns: [{string.Join(", ", unexpectedInTable)}]. " +
                $"Schema evolution is not supported — delete the DuckDB file and re-run to rebuild from scratch.");
        }
    }

    private void ResetInFlightRows()
    {
        ExecuteNonQuery(
            $"UPDATE {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} " +
            $"SET {DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)} = {(int)SyncChangeStatus.Pending}, " +
            $"    {DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn)} = NULL " +
            $"WHERE {DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)} = {(int)SyncChangeStatus.InFlight}");
    }

    private void CreateStagingTable()
    {
        var sql = DuckDbSchemaHelpers.BuildCreateTableSql(
            GetStagingTableName(),
            GetModelProperties(),
            extraColumns: new[]
            {
                $"{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} VARCHAR",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)} VARCHAR",
                $"{DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn)} TIMESTAMP"
            });

        // CREATE TABLE IF NOT EXISTS is fine — but we want a fresh staging each run.
        ExecuteNonQuery($"DROP TABLE IF EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName())}");
        // Replace IF NOT EXISTS with plain CREATE for the staging table.
        sql = sql.Replace("CREATE TABLE IF NOT EXISTS", "CREATE TEMP TABLE", StringComparison.Ordinal);
        ExecuteNonQuery(sql);
    }

    private void CopyCsvIntoStaging()
    {
        var config = RequireConfig();
        var modelCols = string.Join(", ", GetModelProperties().Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name)));

        var skip = config.IgnoreFirstLines;
        // Strict-by-default: IGNORE_ERRORS is intentionally NOT set (DuckDB defaults it to false).
        // Any unparseable row — wrong column count, type-cast failure, or a byte sequence that
        // doesn't decode in the configured encoding — aborts the entire COPY, the exception
        // propagates to Preparing, and the sync returns Failed. We never silently skip rows.
        // Do not flip this without a deliberate plan for surfacing the rejects.
        //
        // When HasHeaderRecord=true the header row is consumed by HEADER=true and is *not*
        // counted toward SKIP — that matches DuckDB's COPY semantics.
        var optionParts = new List<string>
        {
            $"HEADER {(config.HasHeaderRecord ? "true" : "false")}",
            $"DELIMITER {DuckDbSchemaHelpers.QuoteString(config.Delimiter)}",
            $"QUOTE {DuckDbSchemaHelpers.QuoteString(config.Quote)}",
            // Pin ESCAPE — without it the sniffer can decide escape=empty when the sample
            // window happens not to contain any "" sequences, and later rows like
            // "...""C""..." then fail with "Value with unterminated quote found".
            $"ESCAPE {DuckDbSchemaHelpers.QuoteString(config.Escape)}",
            "NULL_PADDING true"
        };

        if (!string.IsNullOrWhiteSpace(config.Encoding))
            optionParts.Add($"ENCODING {DuckDbSchemaHelpers.QuoteString(config.Encoding)}");

        // Column types are pinned from the model, so DuckDB does NOT apply the sniffer's
        // auto-detected date/timestamp format — without these options a locale-formatted column
        // (e.g. "11/7/2024" into a TIMESTAMP) aborts the COPY with a conversion error. The format is
        // global to the file; a file mixing two date formats should type the odd column as string on
        // the model and convert it in the mapping.
        if (!string.IsNullOrWhiteSpace(config.DateFormat))
            optionParts.Add($"DATEFORMAT {DuckDbSchemaHelpers.QuoteString(config.DateFormat)}");

        if (!string.IsNullOrWhiteSpace(config.TimestampFormat))
            optionParts.Add($"TIMESTAMPFORMAT {DuckDbSchemaHelpers.QuoteString(config.TimestampFormat)}");

        if (skip > 0)
            optionParts.Add($"SKIP {skip}");

        var sql = $"COPY {DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName())}({modelCols}) " +
                  $"FROM {DuckDbSchemaHelpers.QuoteString(config.CsvFilePath)} " +
                  $"({string.Join(", ", optionParts)})";

        ExecuteNonQuery(sql);
    }

    private void ComputeStagingKeyAndHash()
    {
        var config = RequireConfig();
        var keyExpr = config.CustomKeyExpression
            ?? DuckDbSchemaHelpers.BuildCompositeKeySqlExpression(GetKeyColumnNames());
        var hashExpr = config.CustomHashExpression
            ?? DuckDbSchemaHelpers.BuildRowHashSqlExpression(GetHashColumnNames());

        ExecuteNonQuery(
            $"UPDATE {DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName())} SET " +
            $"  {DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} = {keyExpr}, " +
            $"  {DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)} = {hashExpr}, " +
            $"  {DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn)} = now()");
    }

    /// <summary>
    /// Collapses staging rows that share a <see cref="PrimaryKeyColumn"/> down to a single row,
    /// keeping the last occurrence (highest <c>rowid</c>, i.e. the row that appeared later in the
    /// CSV). A snapshot is meant to carry one row per natural key; when the upstream export repeats
    /// a key, the diff must treat the latest line as authoritative and override the earlier ones —
    /// the same "latest wins" upsert the DuckDB destination applies.
    /// <para>
    /// Without this, both duplicate rows reach the <c>INSERT OR REPLACE</c> diff statements together.
    /// DuckDB then resolves the in-batch conflict arbitrarily (observed: it keeps the <em>first</em>
    /// row), so the work queue can capture a stale value; on a plain-INSERT code path the same
    /// duplicate would instead raise "Duplicate key ... violates primary key constraint". Removing the
    /// duplicates here makes the result deterministic and eliminates that failure mode outright.
    /// </para>
    /// Returns the number of duplicate rows removed.
    /// </summary>
    private int DeduplicateStagingByPrimaryKey()
    {
        var staging = DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);

        // rowid is DuckDB's implicit per-row identifier; the COPY appends rows in file order, so the
        // highest rowid for a key is its last occurrence in the CSV. _PrimaryKey is md5(...) and never
        // null, and rowid is never null, so the NOT IN anti-join has no null pitfalls.
        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"DELETE FROM {staging} " +
            $"WHERE rowid NOT IN (SELECT max(rowid) FROM {staging} GROUP BY {pk})";
        return cmd.ExecuteNonQuery();
    }

    private void CancelStaleChanges()
    {
        // A pending change is stale when:
        //  - it's an Add/Update but staging no longer has that key with that hash, OR
        //  - it's a Delete but the key is back in staging.
        // Still-valid pending entries are left untouched so attempt counters are preserved.
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var staging = DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var hash = DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn);
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);
        var changeType = DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn);

        ExecuteNonQuery($@"
            DELETE FROM {changes}
            WHERE {status} = {(int)SyncChangeStatus.Pending}
              AND (
                ({changeType} IN ({(int)SyncActionType.Add}, {(int)SyncActionType.Update})
                 AND NOT EXISTS (
                   SELECT 1 FROM {staging} s
                   WHERE s.{pk} = {changes}.{pk}
                     AND s.{hash} = {changes}.{hash}
                 ))
                OR
                ({changeType} = {(int)SyncActionType.Delete}
                 AND EXISTS (
                   SELECT 1 FROM {staging} s
                   WHERE s.{pk} = {changes}.{pk}
                 ))
              )");

        // A Dead row is kept only while it still describes the source: an Add/Update whose key has
        // left the file, or a Delete whose key is back, would otherwise sit in the queue forever. (A Dead
        // Add/Update whose row CHANGED is replaced by the diff below — INSERT OR REPLACE on the key.)
        ExecuteNonQuery($@"
            DELETE FROM {changes}
            WHERE {status} = {(int)SyncChangeStatus.Dead}
              AND (
                ({changeType} IN ({(int)SyncActionType.Add}, {(int)SyncActionType.Update})
                 AND NOT EXISTS (SELECT 1 FROM {staging} s WHERE s.{pk} = {changes}.{pk}))
                OR
                ({changeType} = {(int)SyncActionType.Delete}
                 AND EXISTS (SELECT 1 FROM {staging} s WHERE s.{pk} = {changes}.{pk}))
              )");
    }

    private void InsertAdds()
    {
        InsertChangeRows(SyncActionType.Add, sourceTable: GetStagingTableName(), wherePredicateBuilder: (alias, sourceAlias, _) =>
            $"{sourceAlias}.{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} IS NULL");
    }

    private void InsertUpdates()
    {
        InsertChangeRows(SyncActionType.Update, sourceTable: GetStagingTableName(), wherePredicateBuilder: (alias, sourceAlias, _) =>
            $"{sourceAlias}.{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} IS NOT NULL " +
            $"AND s.{DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)} IS DISTINCT FROM {sourceAlias}.{DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn)}");
    }

    private void InsertDeletes()
    {
        // Deletes pull payload from <source> (the row no longer exists in staging).
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var source = DuckDbSchemaHelpers.QuoteIdentifier(GetSourceTableName());
        var staging = DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var hash = DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn);
        var loadedAt = DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn);
        var changeType = DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn);
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);

        var modelCols = string.Join(", ", GetModelProperties().Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name)));
        var modelColsAliased = string.Join(", ", GetModelProperties().Select(p => $"o.{DuckDbSchemaHelpers.QuoteIdentifier(p.Name)}"));

        var insertCols = $"{modelCols}, {pk}, {hash}, {loadedAt}, {changeType}, " +
                         $"{DuckDbSchemaHelpers.QuoteIdentifier(DetectedAtColumn)}, " +
                         $"{DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn)}, " +
                         $"{status}";

        // Dedup against any tracked (non-Done) row for the same key. Dead rows count too —
        // a Dead Delete should not be re-queued just because the key is still missing from
        // the source.
        ExecuteNonQuery($@"
            INSERT OR REPLACE INTO {changes} ({insertCols})
            SELECT {modelColsAliased}, o.{pk}, o.{hash}, o.{loadedAt},
                   {(int)SyncActionType.Delete}, now(), 0, {(int)SyncChangeStatus.Pending}
            FROM {source} o
            LEFT JOIN {staging} s ON s.{pk} = o.{pk}
            WHERE s.{pk} IS NULL
              AND NOT EXISTS (
                SELECT 1 FROM {changes} c
                WHERE c.{pk} = o.{pk}
                  AND c.{status} IN ({(int)SyncChangeStatus.Pending}, {(int)SyncChangeStatus.InFlight}, {(int)SyncChangeStatus.Dead})
                  AND c.{changeType} = {(int)SyncActionType.Delete}
              )");
    }

    private void InsertChangeRows(SyncActionType type, string sourceTable, Func<string, string, string, string> wherePredicateBuilder)
    {
        // Common pattern for Add/Update: payload comes from <staging>.
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var staging = DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName());
        var source = DuckDbSchemaHelpers.QuoteIdentifier(GetSourceTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var hash = DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn);
        var loadedAt = DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn);
        var changeType = DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn);
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);

        var modelCols = string.Join(", ", GetModelProperties().Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name)));
        var modelColsAliased = string.Join(", ", GetModelProperties().Select(p => $"s.{DuckDbSchemaHelpers.QuoteIdentifier(p.Name)}"));

        var insertCols = $"{modelCols}, {pk}, {hash}, {loadedAt}, {changeType}, " +
                         $"{DuckDbSchemaHelpers.QuoteIdentifier(DetectedAtColumn)}, " +
                         $"{DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn)}, " +
                         $"{status}";

        var wherePred = wherePredicateBuilder("s", "o", "c");

        // Dedup includes Dead rows so we don't re-queue a row whose data hasn't changed since
        // it was given up on. INSERT OR REPLACE still replaces a Dead row when the hash differs
        // (auto-recovery: source has changed → fresh attempt with attempt_count = 0).
        ExecuteNonQuery($@"
            INSERT OR REPLACE INTO {changes} ({insertCols})
            SELECT {modelColsAliased}, s.{pk}, s.{hash}, s.{loadedAt},
                   {(int)type}, now(), 0, {(int)SyncChangeStatus.Pending}
            FROM {staging} s
            LEFT JOIN {source} o ON s.{pk} = o.{pk}
            WHERE {wherePred}
              AND NOT EXISTS (
                SELECT 1 FROM {changes} c
                WHERE c.{pk} = s.{pk}
                  AND c.{status} IN ({(int)SyncChangeStatus.Pending}, {(int)SyncChangeStatus.InFlight}, {(int)SyncChangeStatus.Dead})
                  AND c.{hash} = s.{hash}
              )");
    }

    private void DropStaging()
    {
        ExecuteNonQuery($"DROP TABLE IF EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName())}");
    }

    private long CountPendingChanges()
    {
        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"SELECT COUNT(*) FROM {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} " +
            $"WHERE {DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)} = {(int)SyncChangeStatus.Pending}";
        return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    #endregion

    #region Per-action lifecycle

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        return new(true);
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        var changeType = MapSupportedAction(input.Input);
        if (changeType is null || !IsActionEnabled(changeType.Value))
            return new((long?)0);

        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"SELECT COUNT(*) FROM {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} " +
            $"WHERE {DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)} = {(int)SyncChangeStatus.Pending} " +
            $"  AND {DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn)} = {(int)changeType}";

        var count = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        return new((long?)count);
    }

    public ValueTask<IEnumerable<TCsv?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TCsv>> input)
    {
        if (input.Input.Status.CurrentRetryCount > 0 && input.Input.PreviousItems is not null)
            return new(input.Input.PreviousItems);

        var changeType = MapSupportedAction(input.Input.Status.ActionType);
        if (changeType is null)
            return new((IEnumerable<TCsv?>?)Array.Empty<TCsv?>());

        var batchId = Guid.NewGuid();
        var batchSize = input.Input.Status.BatchSize;

        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);
        var changeTypeCol = DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);
        var detectedAt = DuckDbSchemaHelpers.QuoteIdentifier(DetectedAtColumn);

        // Single-statement claim: tag the next page of pending rows for this action with a fresh
        // batch id in one UPDATE driven by an ORDER BY ... LIMIT subquery. The previous two-step form
        // (SELECT the keys to the client, then UPDATE ... WHERE pk IN (<batchSize keys>)) made an extra
        // round-trip and shipped a batch-sized IN-list as SQL text every batch; this is one round-trip
        // with no app-built key list. MaterializeBatch then reads the claimed rows by batch id; an
        // empty result means nothing was pending.
        currentBatchId = batchId;
        currentBatchActionType = input.Input.Status.ActionType;

        using (var claimCmd = connection!.CreateCommand())
        {
            claimCmd.CommandText =
                $"UPDATE {changes} SET " +
                $"  {status} = {(int)SyncChangeStatus.InFlight}, " +
                $"  {batchIdCol} = '{batchId}' " +
                $"WHERE {pk} IN (" +
                $"  SELECT {pk} FROM {changes} " +
                $"  WHERE {status} = {(int)SyncChangeStatus.Pending} " +
                $"    AND {changeTypeCol} = {(int)changeType} " +
                $"  ORDER BY {detectedAt} " +
                $"  LIMIT {batchSize})";
            claimCmd.ExecuteNonQuery();
        }

        var items = MaterializeBatch();
        if (items.Count == 0)
        {
            currentBatchId = null;
            currentBatchActionType = null;
            return new((IEnumerable<TCsv?>?)Array.Empty<TCsv?>());
        }

        return new((IEnumerable<TCsv?>?)items);
    }

    private List<TCsv?> MaterializeBatch()
    {
        var props = GetMaterializableProperties();
        var setters = GetMaterializeSetters();
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        var selectCols = string.Join(", ", props.Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name)));

        // batch_id is unique per claim and indexed (_batch_idx) — no need to re-filter by the
        // 10K-key IN list we used to tag the rows. That filter was ~250 KB of SQL text per call.
        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"SELECT {selectCols} FROM {changes} " +
            $"WHERE {batchIdCol} = '{currentBatchId}'";

        var items = new List<TCsv?>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var item = new TCsv();
            for (var i = 0; i < props.Length; i++)
            {
                var value = DuckDbSchemaHelpers.ReadValue(reader, i, props[i].PropertyType);
                // Skip nulls: a fresh TCsv already has default/null for every property, so this is
                // equivalent to assigning null — and it avoids unboxing null into a value-type setter.
                if (value is not null)
                    setters[i](item, value);
            }
            items.Add(item);
        }
        return items;
    }

    public async ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TCsv, TDestination>> input)
    {
        if (currentBatchId is null)
            return true;

        var batchId = currentBatchId.Value;
        var actionType = currentBatchActionType ?? input.Input.Status.ActionType;
        var storeResult = input.Input.StoreDataResult;
        var resultType = storeResult?.ResultType ?? SyncStoreDataResultType.Failed;

        if (resultType == SyncStoreDataResultType.Succeeded)
        {
            // Whole batch landed: promote every claimed row and drop it from the queue — one
            // set-based query each, keyed by _BatchId (no per-row work). One transaction so the
            // promote + delete commit with a single checkpoint instead of two fsyncs.
            InTransaction(() =>
            {
                PromoteToSource(batchId, actionType);
                DeleteFromChanges(batchId);
            });
            ClearCurrentBatch();
            return true;
        }

        // A bumped row must always carry a reason: a store that reports failed items without an
        // exception still says which — and a row that goes Dead with an empty last error is exactly
        // the invisible failure this queue exists to make visible.
        var error = input.Input.Exception?.Message
            ?? storeResult?.RetryException?.Message
            ?? $"Store reported {resultType}: {storeResult?.FailedItems?.Count() ?? 0} failed, {storeResult?.SucceededItems?.Count() ?? 0} succeeded, {storeResult?.SkippedItems?.Count() ?? 0} skipped, without an exception.";

        // Partial batch + a DestinationKey → split it: promote the succeeded subset (one query) and
        // leave only the unsucceeded rows under this batch id for BumpAttempts to retry. Without a
        // DestinationKey we can't tell which change rows the succeeded destination items came from,
        // so we fall back to all-or-nothing (whole batch retried).
        if (resultType == SyncStoreDataResultType.Partial && RequireConfig().DestinationKey is not null)
        {
            var matchPredicate = BuildSucceededKeyMatchPredicate(
                storeResult!.SucceededItems ?? Enumerable.Empty<TDestination?>(),
                input.Input.SourceItems,
                ChangesAlias);

            // Promote-succeeded + delete-succeeded + bump-the-rest commit together in one transaction.
            long partialDead = 0;
            InTransaction(() =>
            {
                if (matchPredicate is not null)
                {
                    PromoteToSource(batchId, actionType, matchPredicate);
                    DeleteFromChanges(batchId, matchPredicate);
                }

                // Whatever is still tagged with this batch id is exactly the failed/skipped rows.
                partialDead = BumpAttempts(batchId, error);
            });
            ClearCurrentBatch();
            await ReportDeadRows(input.SyncProgressIndicators, partialDead, error);
            return true;
        }

        // Failed / Skipped, or Partial without a DestinationKey → bump the whole batch and (maybe) Dead.
        var dead = BumpAttempts(batchId, error);
        ClearCurrentBatch();
        await ReportDeadRows(input.SyncProgressIndicators, dead, error);
        return true;
    }

    private static async Task ReportDeadRows(IEnumerable<ISyncEngineLogger>? loggers, long dead, string? error)
    {
        if (dead > 0 && loggers is not null)
            await loggers.LogError("{0} change(s) reached MaxAttempts and are now Dead; they will not be retried until they rest for DeadRetryAfter, their CSV row changes, or RequeueDeadChangesAsync is called. Last error: {1}", dead, error);
    }

    // Stable alias for the changes table in promote/delete queries; the partial-promotion
    // predicate built from DestinationKey references the changes row through it.
    private const string ChangesAlias = "c";

    /// <summary>
    /// Promotes change rows to the source table. <paramref name="matchPredicate"/> null promotes
    /// the whole batch; a non-null predicate (built from DestinationKey, referencing the changes
    /// row as <see cref="ChangesAlias"/>) restricts promotion to the succeeded subset of a partial
    /// batch.
    /// </summary>
    private void PromoteToSource(Guid batchId, SyncActionType actionType, string? matchPredicate = null)
    {
        var source = DuckDbSchemaHelpers.QuoteIdentifier(GetSourceTableName());
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        var where = $"{ChangesAlias}.{batchIdCol} = '{batchId}'"
                    + (matchPredicate is null ? string.Empty : $" AND {matchPredicate}");

        if (actionType == SyncActionType.Delete)
        {
            ExecuteNonQuery(
                $"DELETE FROM {source} WHERE {pk} IN " +
                $"(SELECT {ChangesAlias}.{pk} FROM {changes} AS {ChangesAlias} WHERE {where})");
        }
        else
        {
            // Add and Update both upsert into source.
            var sourceColumns = GetModelProperties()
                .Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name))
                .Concat(new[] {
                    DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn),
                    DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn),
                    DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn) })
                .ToList();
            var sourceColList = string.Join(", ", sourceColumns);
            var selectColList = string.Join(", ", sourceColumns.Select(c => $"{ChangesAlias}.{c}"));

            ExecuteNonQuery(
                $"INSERT OR REPLACE INTO {source} ({sourceColList}) " +
                $"SELECT {selectColList} FROM {changes} AS {ChangesAlias} WHERE {where}");
        }
    }

    /// <summary>
    /// Removes change rows from the queue. <paramref name="matchPredicate"/> null deletes the whole
    /// batch; a non-null predicate restricts deletion to the rows it matches.
    /// </summary>
    private void DeleteFromChanges(Guid batchId, string? matchPredicate = null)
    {
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        if (matchPredicate is null)
        {
            ExecuteNonQuery($"DELETE FROM {changes} WHERE {batchIdCol} = '{batchId}'");
            return;
        }

        // Resolve to a PK set first so the DELETE target needs no alias (portable across DuckDB).
        ExecuteNonQuery(
            $"DELETE FROM {changes} WHERE {pk} IN " +
            $"(SELECT {ChangesAlias}.{pk} FROM {changes} AS {ChangesAlias} " +
            $" WHERE {ChangesAlias}.{batchIdCol} = '{batchId}' AND {matchPredicate})");
    }

    /// <summary>
    /// Builds a correlated EXISTS predicate that matches a changes row (aliased
    /// <paramref name="changesAlias"/>) against the natural key read off the succeeded destination
    /// items via <see cref="DuckDbCsvSyncDataSourceConfigurations{TCsv, TDestination}.DestinationKey"/>.
    /// Returns null when there are no succeeded items to match.
    /// </summary>
    private string? BuildSucceededKeyMatchPredicate(
        IEnumerable<TDestination?> succeededItems,
        IEnumerable<TCsv?>? sourceItems,
        string changesAlias)
    {
        var accessors = GetDestinationKeyAccessors();
        if (accessors is null)
            return null;

        // When a SourceKey is configured, correlate in memory: compare DestinationKey (on the succeeded
        // destination item) with SourceKey (on the source row), and promote the matched rows by their
        // primary key. Both sides run in C#, so the values always format identically.
        var sourceKey = GetSourceKeyAccessor();
        if (sourceKey is not null)
        {
            if (accessors.Length != 1)
                throw new InvalidOperationException(
                    "DuckDbCsvSyncDataSource: SourceKey pairs with a single-component DestinationKey.");

            var succeededKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in succeededItems)
            {
                if (item is null)
                    continue;
                var value = accessors[0](item);
                if (value is not null)
                    succeededKeys.Add(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            if (succeededKeys.Count == 0)
                return null;

            var primaryKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in sourceItems ?? Enumerable.Empty<TCsv?>())
            {
                if (row is null || string.IsNullOrEmpty(row._PrimaryKey))
                    continue;
                var value = sourceKey(row);
                if (value is not null && succeededKeys.Contains(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty))
                    primaryKeys.Add(row._PrimaryKey!);
            }

            if (primaryKeys.Count == 0)
                return null;

            var pkList = string.Join(", ", primaryKeys.Select(DuckDbSchemaHelpers.QuoteString));
            return $"{changesAlias}.{DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn)} IN ({pkList})";
        }

        var keyColumns = GetKeyColumnNames();
        if (accessors.Length != keyColumns.Count)
            throw new InvalidOperationException(
                $"DuckDbCsvSyncDataSource: DestinationKey has {accessors.Length} component(s) but KeyColumns has " +
                $"{keyColumns.Count}. They must align 1:1 and in the same order.");

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var tuples = new List<string>();
        foreach (var item in succeededItems)
        {
            if (item is null)
                continue;

            var parts = new string[accessors.Length];
            for (var i = 0; i < accessors.Length; i++)
            {
                var value = accessors[i](item);
                parts[i] = value is null
                    ? "NULL"
                    : DuckDbSchemaHelpers.QuoteString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            var tuple = "(" + string.Join(", ", parts) + ")";
            if (seen.Add(tuple))
                tuples.Add(tuple);
        }

        if (tuples.Count == 0)
            return null;

        var tupleColumns = string.Join(", ", keyColumns.Select((_, i) => $"k{i}"));
        var conditions = string.Join(
            " AND ",
            keyColumns.Select((col, i) =>
                $"t.k{i} IS NOT DISTINCT FROM CAST({changesAlias}.{DuckDbSchemaHelpers.QuoteIdentifier(col)} AS VARCHAR)"));

        return $"EXISTS (SELECT 1 FROM (VALUES {string.Join(", ", tuples)}) AS t({tupleColumns}) WHERE {conditions})";
    }

    private Func<TDestination, object?>[]? GetDestinationKeyAccessors()
    {
        if (!destinationKeyAccessorsBuilt)
        {
            var expression = RequireConfig().DestinationKey;
            destinationKeyAccessors = expression is null ? null : BuildDestinationKeyAccessors(expression);
            destinationKeyAccessorsBuilt = true;
        }

        return destinationKeyAccessors;
    }

    private Func<TCsv, object?>? GetSourceKeyAccessor()
    {
        if (!sourceKeyAccessorBuilt)
        {
            var expression = RequireConfig().SourceKey;
            if (expression is null)
            {
                sourceKeyAccessor = null;
            }
            else
            {
                var compiled = expression.Compile();
                sourceKeyAccessor = row => compiled(row);
            }
            sourceKeyAccessorBuilt = true;
        }

        return sourceKeyAccessor;
    }

    /// <summary>
    /// Compiles a DestinationKey expression into one accessor per key component, in declaration
    /// order — so a composite <c>x =&gt; new { x.A, x.B }</c> yields two accessors aligned with the
    /// two <see cref="KeyColumns"/> components, and a scalar <c>x =&gt; x.A</c> yields one.
    /// </summary>
    private static Func<TDestination, object?>[] BuildDestinationKeyAccessors(Expression<Func<TDestination, object>> expression)
    {
        var parameter = expression.Parameters[0];
        var body = expression.Body;
        if (body is UnaryExpression unary && unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            body = unary.Operand;

        var components = body is NewExpression newExpression && newExpression.Arguments.Count > 0
            ? (IEnumerable<Expression>)newExpression.Arguments
            : new[] { body };

        return components
            .Select(component => Expression
                .Lambda<Func<TDestination, object?>>(Expression.Convert(component, typeof(object)), parameter)
                .Compile())
            .ToArray();
    }

    private void ClearCurrentBatch()
    {
        currentBatchId = null;
        currentBatchActionType = null;
    }

    private long BumpAttempts(Guid batchId, string? error)
    {
        var maxAttempts = RequireConfig().MaxAttempts;
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);
        var attemptCount = DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn);
        var lastAttemptAt = DuckDbSchemaHelpers.QuoteIdentifier(LastAttemptAtColumn);
        var lastError = DuckDbSchemaHelpers.QuoteIdentifier(LastErrorColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        long goingDead;
        using (var countCmd = connection!.CreateCommand())
        {
            countCmd.CommandText =
                $"SELECT count(*) FROM {changes} WHERE {batchIdCol} = '{batchId}' AND {attemptCount} + 1 >= {maxAttempts}";
            goingDead = Convert.ToInt64(countCmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"UPDATE {changes} SET " +
            $"  {attemptCount} = {attemptCount} + 1, " +
            $"  {lastAttemptAt} = now(), " +
            $"  {lastError} = $error, " +
            $"  {status} = CASE WHEN {attemptCount} + 1 >= {maxAttempts} THEN {(int)SyncChangeStatus.Dead} ELSE {(int)SyncChangeStatus.Pending} END, " +
            $"  {batchIdCol} = NULL " +
            $"WHERE {batchIdCol} = '{batchId}'";

        var p = cmd.CreateParameter();
        p.ParameterName = "error";
        p.Value = (object?)error ?? DBNull.Value;
        cmd.Parameters.Add(p);

        cmd.ExecuteNonQuery();
        return goingDead;
    }

    /// <summary>
    /// Gives every Dead row a fresh set of attempts, now. For a host that knows the cause is gone —
    /// typically wired to <c>DuckDBSyncDataDestinationConfigurations.SchemaChanged</c>, so rows that
    /// died while the destination table was behind on its schema are re-sent as soon as it is fixed.
    /// Works before, during (from Preparing on) and after a run; outside a run it opens the diff
    /// database itself.
    /// </summary>
    public async ValueTask<long> RequeueDeadChangesAsync(string reason)
    {
        var ownsConnection = connection is null;
        if (ownsConnection)
            OpenConnection();

        try
        {
            if (!TableExists(GetChangesTableName()))
                return 0;

            var revived = RequeueDeadRows(olderThan: null);
            lastRequeueReason = revived > 0 ? reason : lastRequeueReason;
            return revived;
        }
        finally
        {
            if (ownsConnection)
                CloseConnection();
        }
    }

    private string? lastRequeueReason;

    private long RequeueExpiredDeadRows()
    {
        var retryAfter = RequireConfig().DeadRetryAfter;
        return retryAfter is null ? 0 : RequeueDeadRows(olderThan: retryAfter.Value);
    }

    private long RequeueDeadRows(TimeSpan? olderThan)
    {
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);
        var attemptCount = DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn);
        var lastAttemptAt = DuckDbSchemaHelpers.QuoteIdentifier(LastAttemptAtColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        var age = olderThan is null
            ? string.Empty
            : $" AND ({lastAttemptAt} IS NULL OR {lastAttemptAt} < now() - INTERVAL '{(long)olderThan.Value.TotalSeconds}' SECOND)";

        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"UPDATE {changes} SET {status} = {(int)SyncChangeStatus.Pending}, {attemptCount} = 0, {batchIdCol} = NULL " +
            $"WHERE {status} = {(int)SyncChangeStatus.Dead}{age}";
        return cmd.ExecuteNonQuery();
    }

    private bool TableExists(string tableName)
    {
        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"SELECT count(*) FROM duckdb_tables() WHERE database_name = current_database() AND schema_name = 'main' AND table_name = {DuckDbSchemaHelpers.QuoteString(tableName)}";
        return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
    }

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        return new(input.Input.Succeeded);
    }

    #endregion

    #region Cleanup

    public ValueTask Succeeded(SyncFunctionInput input) => default;

    /// <summary>The queue as it stood at the end of the last run: what is still waiting, and what has been given up on.</summary>
    public DuckDbCsvSyncQueueStats? LastRunQueueStats { get; private set; }

    public async ValueTask Finished(SyncFunctionInput input)
    {
        if (connection is null || !TableExists(GetChangesTableName()))
            return;

        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);

        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"SELECT " +
            $"  count(*) FILTER (WHERE {status} = {(int)SyncChangeStatus.Pending}), " +
            $"  count(*) FILTER (WHERE {status} = {(int)SyncChangeStatus.InFlight}), " +
            $"  count(*) FILTER (WHERE {status} = {(int)SyncChangeStatus.Dead}) " +
            $"FROM {changes}";

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return;

        var stats = new DuckDbCsvSyncQueueStats(
            Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture),
            lastRequeueReason);
        LastRunQueueStats = stats;
        lastRequeueReason = null;

        if (stats.Dead > 0)
            await input.SyncProgressIndicators.LogWarning("Queue after run: {0} pending, {1} in flight, {2} Dead (given up on; see the _LastError column).", stats.Pending, stats.InFlight, stats.Dead);
        else
            await input.SyncProgressIndicators.LogInformation("Queue after run: {0} pending, {1} in flight, 0 Dead.", stats.Pending, stats.InFlight);
    }

    public ValueTask Reset()
    {
        CloseConnection();
        currentBatchId = null;
        currentBatchActionType = null;
        destinationKeyAccessors = null;
        destinationKeyAccessorsBuilt = false;
        sourceKeyAccessor = null;
        sourceKeyAccessorBuilt = false;
        return default;
    }

    public async ValueTask DisposeAsync()
    {
        await Reset();
    }

    private void CloseConnection()
    {
        try
        {
            connection?.Close();
            connection?.Dispose();
        }
        catch { }
        connection = null;
    }

    #endregion

    #region Helpers

    private DuckDbCsvSyncDataSourceConfigurations<TCsv, TDestination> RequireConfig()
    {
        return Configurations
            ?? throw new InvalidOperationException("DuckDbCsvSyncDataSource is not configured. Call Configure(...) first.");
    }

    private string GetSourceTableName() => Configurations?.SourceTableName ?? $"{typeof(TCsv).Name}_source";

    private string GetChangesTableName() => Configurations?.ChangesTableName ?? $"{typeof(TCsv).Name}_changes";

    private string GetStagingTableName() => $"{typeof(TCsv).Name}_staging";

    /// <summary>
    /// Model properties = public instance properties of TCsv that map to columns in the CSV.
    /// Excludes: <see cref="SyncCsvBase"/> metadata columns, computed properties without a
    /// public setter, and properties marked with CsvHelper's <see cref="IgnoreAttribute"/>.
    /// </summary>
    private static PropertyInfo[] GetModelProperties()
    {
        // Cached per closed generic type — the result is identical for every call with the same TCsv.
        if (cachedModelProperties is not null)
            return cachedModelProperties;

        var metadataNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(SyncCsvBase._PrimaryKey),
            nameof(SyncCsvBase._RowHash),
            nameof(SyncCsvBase._LoadedAt)
        };

        return cachedModelProperties = DuckDbSchemaHelpers.GetPropertiesWithChildPriority<TCsv>()
            .Where(p => !metadataNames.Contains(p.Name))
            .Where(p => p.CanWrite)
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null)
            .ToArray();
    }

    /// <summary>
    /// Properties used for materializing a TCsv instance from the changes table — model
    /// columns + metadata columns. Computed properties (no setter) and CsvHelper
    /// <see cref="IgnoreAttribute"/>-marked properties are excluded so the SELECT only
    /// references columns that actually exist in the table.
    /// </summary>
    private static PropertyInfo[] GetMaterializableProperties()
    {
        // Cached per closed generic type — see GetModelProperties.
        return cachedMaterializableProperties ??= DuckDbSchemaHelpers.GetPropertiesWithChildPriority<TCsv>()
            .Where(p => p.CanWrite)
            .Where(p => p.GetCustomAttribute<IgnoreAttribute>() is null)
            .ToArray();
    }

    /// <summary>
    /// Compiled setters aligned 1:1 with <see cref="GetMaterializableProperties"/>, built once per
    /// closed generic type. Each is an <c>(entity, value) =&gt; entity.Prop = (PropType)value</c> lambda
    /// — a direct assignment that replaces per-row <see cref="PropertyInfo.SetValue"/> (reflection +
    /// boxing) in the materialization hot loop.
    /// </summary>
    private static Action<TCsv, object?>[] GetMaterializeSetters()
    {
        if (cachedMaterializeSetters is not null)
            return cachedMaterializeSetters;

        var props = GetMaterializableProperties();
        var setters = new Action<TCsv, object?>[props.Length];
        for (var i = 0; i < props.Length; i++)
        {
            var entity = Expression.Parameter(typeof(TCsv), "e");
            var value = Expression.Parameter(typeof(object), "v");
            var assign = Expression.Assign(
                Expression.Property(entity, props[i]),
                Expression.Convert(value, props[i].PropertyType));
            setters[i] = Expression.Lambda<Action<TCsv, object?>>(assign, entity, value).Compile();
        }

        return cachedMaterializeSetters = setters;
    }

    private List<string> GetKeyColumnNames()
    {
        var config = RequireConfig();
        return DuckDbSchemaHelpers.GetPropertyNamesFromExpression(config.KeyColumns).ToList();
    }

    private List<string> GetHashColumnNames()
    {
        var config = RequireConfig();
        if (config.HashColumns is not null)
            return DuckDbSchemaHelpers.GetPropertyNamesFromExpression(config.HashColumns).ToList();

        var keyNames = new HashSet<string>(GetKeyColumnNames(), StringComparer.OrdinalIgnoreCase);
        return GetModelProperties()
            .Where(p => p.GetCustomAttribute<IgnoreInHashAttribute>() is null)
            .Where(p => !keyNames.Contains(p.Name))
            .Select(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// Returns the action only when it is one of the three supported change types.
    /// <see cref="SyncActionType.Upsert"/> is not handled by this source.
    /// </summary>
    private static SyncActionType? MapSupportedAction(SyncActionType action) => action switch
    {
        SyncActionType.Add or SyncActionType.Update or SyncActionType.Delete => action,
        _ => null
    };

    /// <summary>
    /// True when the engine is configured to execute the given action. Used to skip the diff
    /// for action types the caller has opted out of — e.g. labor-line / part-line syncs do not
    /// emit Deletes because the upstream system moves removed rows to an archive table rather
    /// than removing them from the active CSV.
    /// </summary>
    private bool IsActionEnabled(SyncActionType action)
    {
        var actions = SyncService.Configurations?.ActionExecutionAndOrder;
        if (actions is null)
            return true;
        return actions.Contains(action);
    }


    private void ExecuteNonQuery(string sql)
    {
        using var cmd = connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Runs <paramref name="body"/> inside a single explicit DuckDB transaction. On a persistent
    /// (on-disk) database every autocommit statement is independently checkpointed/fsync'd; grouping
    /// the per-batch bookkeeping writes (and the Preparing diff) into one transaction collapses those
    /// 2-3 synchronous disk flushes into one — the dominant per-batch cost. Rolls back on failure so
    /// a half-applied batch never lands.
    /// </summary>
    private void InTransaction(Action body)
    {
        ExecuteNonQuery("BEGIN TRANSACTION");
        try
        {
            body();
            ExecuteNonQuery("COMMIT");
        }
        catch
        {
            try { ExecuteNonQuery("ROLLBACK"); } catch { /* connection is being torn down anyway */ }
            throw;
        }
    }

    #endregion

    #region Not implemented (data source does not own these stages)

    public ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TCsv, TDestination>> input)
        => throw new NotImplementedException();

    public ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TCsv?>? sourceItems, SyncActionType actionType)
        => throw new NotImplementedException();

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
        => throw new NotImplementedException();

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TCsv, TDestination>> input)
        => throw new NotImplementedException();

    public ValueTask Failed(SyncFunctionInput input) => default;

    #endregion
}

/// <summary>Row counts of a CSV diff queue, by status.</summary>
/// <param name="RequeueReason">Why Dead rows were re-queued during the run, when <c>RequeueDeadChangesAsync</c> was called.</param>
public sealed record DuckDbCsvSyncQueueStats(long Pending, long InFlight, long Dead, string? RequeueReason);

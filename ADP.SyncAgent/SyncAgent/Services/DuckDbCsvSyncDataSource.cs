using System.Globalization;
using System.Reflection;
using System.Text;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

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
    : ISyncDataAdapter<TCsv, TDestination, DuckDbCsvSyncDataSourceConfigurations<TCsv>, DuckDbCsvSyncDataSource<TCsv, TDestination>>
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

    private readonly IStorageService storageService;

    private DuckDBConnection? connection;
    private string? localCsvPath;
    private string? workingDirectory;

    private Guid? currentBatchId;
    private SyncActionType? currentBatchActionType;

    public DuckDbCsvSyncDataSourceConfigurations<TCsv>? Configurations { get; private set; }
    public ISyncEngine<TCsv, TDestination> SyncService { get; private set; } = default!;

    public DuckDbCsvSyncDataSource(IStorageService storageService)
    {
        this.storageService = storageService;
    }

    public DuckDbCsvSyncDataSource<TCsv, TDestination> SetSyncService(ISyncEngine<TCsv, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    public ISyncEngine<TCsv, TDestination> Configure(DuckDbCsvSyncDataSourceConfigurations<TCsv> configurations, bool configureSyncService = true)
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

            await input.SyncProgressIndicators.LogInformation("Downloading new CSV to local working directory.");
            await DownloadCsvAsync(input.CancellationToken);

            await input.SyncProgressIndicators.LogInformation("Loading CSV into DuckDB staging table.");
            CreateStagingTable();
            CopyCsvIntoStaging();
            ComputeStagingKeyAndHash();

            await input.SyncProgressIndicators.LogInformation("Cancelling pending changes invalidated by the new source.");
            CancelStaleChanges();

            await input.SyncProgressIndicators.LogInformation("Computing diff and writing to changes table.");
            InsertAdds();
            InsertUpdates();
            InsertDeletes();

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
        connection = new DuckDBConnection(config.ConnectionString);
        connection.Open();
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

        ExecuteNonQuery(
            $"CREATE INDEX IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName() + "_status_idx")} " +
            $"ON {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} ({DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn)}, {DuckDbSchemaHelpers.QuoteIdentifier(ChangeTypeColumn)})");

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

    private async Task DownloadCsvAsync(CancellationToken cancellationToken)
    {
        var config = RequireConfig();

        workingDirectory = Path.Combine(
            config.LocalWorkingDirectory ?? Path.GetTempPath(),
            "duckdb-csv-sync",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(workingDirectory);
        localCsvPath = Path.Combine(workingDirectory, config.CsvFileName);

        await storageService.LoadNewVersionAsync(
            Path.Combine(config.SourceDirectory ?? "", config.CsvFileName),
            localCsvPath,
            ignoreFirstLines: 0, // we let DuckDB COPY handle skipping via SKIP=N
            config.SourceContainerOrShareName,
            cancellationToken);
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
        // When HasHeaderRecord=true the header row is consumed by HEADER=true and is *not*
        // counted toward SKIP — that matches DuckDB's COPY semantics.
        var optionParts = new List<string>
        {
            $"HEADER {(config.HasHeaderRecord ? "true" : "false")}",
            $"DELIMITER {DuckDbSchemaHelpers.QuoteString(config.Delimiter)}",
            $"QUOTE {DuckDbSchemaHelpers.QuoteString(config.Quote)}",
            "NULL_PADDING true"
        };

        if (skip > 0)
            optionParts.Add($"SKIP {skip}");

        var sql = $"COPY {DuckDbSchemaHelpers.QuoteIdentifier(GetStagingTableName())}({modelCols}) " +
                  $"FROM {DuckDbSchemaHelpers.QuoteString(localCsvPath!)} " +
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
        if (changeType is null)
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

        // Two-step claim: select the keys to claim, then update them. Avoids depending on
        // RETURNING semantics for cross-version DuckDB.NET compatibility.
        var keysToClaim = new List<string>();
        using (var selectCmd = connection!.CreateCommand())
        {
            selectCmd.CommandText =
                $"SELECT {pk} FROM {changes} " +
                $"WHERE {status} = {(int)SyncChangeStatus.Pending} " +
                $"  AND {changeTypeCol} = {(int)changeType} " +
                $"ORDER BY {detectedAt} " +
                $"LIMIT {batchSize}";
            using var reader = selectCmd.ExecuteReader();
            while (reader.Read())
                keysToClaim.Add(reader.GetString(0));
        }

        if (keysToClaim.Count == 0)
        {
            currentBatchId = null;
            currentBatchActionType = null;
            return new((IEnumerable<TCsv?>?)Array.Empty<TCsv?>());
        }

        var keyList = string.Join(", ", keysToClaim.Select(DuckDbSchemaHelpers.QuoteString));
        using (var updateCmd = connection!.CreateCommand())
        {
            updateCmd.CommandText =
                $"UPDATE {changes} SET " +
                $"  {status} = {(int)SyncChangeStatus.InFlight}, " +
                $"  {batchIdCol} = '{batchId}' " +
                $"WHERE {pk} IN ({keyList})";
            updateCmd.ExecuteNonQuery();
        }

        currentBatchId = batchId;
        currentBatchActionType = input.Input.Status.ActionType;

        var items = MaterializeBatch(keyList);
        return new((IEnumerable<TCsv?>?)items);
    }

    private List<TCsv?> MaterializeBatch(string keyList)
    {
        var props = GetMaterializableProperties();
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        var selectCols = string.Join(", ", props.Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name)));

        using var cmd = connection!.CreateCommand();
        cmd.CommandText =
            $"SELECT {selectCols} FROM {changes} " +
            $"WHERE {batchIdCol} = '{currentBatchId}' " +
            $"  AND {pk} IN ({keyList})";

        var items = new List<TCsv?>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var item = new TCsv();
            for (var i = 0; i < props.Length; i++)
            {
                var value = DuckDbSchemaHelpers.ReadValue(reader, i, props[i].PropertyType);
                if (props[i].CanWrite)
                    props[i].SetValue(item, value);
            }
            items.Add(item);
        }
        return items;
    }

    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TCsv, TDestination>> input)
    {
        if (currentBatchId is null)
            return new(true);

        var batchId = currentBatchId.Value;
        var actionType = currentBatchActionType ?? input.Input.Status.ActionType;
        var resultType = input.Input.StoreDataResult?.ResultType ?? SyncStoreDataResultType.Failed;

        if (resultType == SyncStoreDataResultType.Succeeded)
        {
            PromoteBatchToSource(batchId, actionType);
            DeleteBatchFromChanges(batchId);
            currentBatchId = null;
            currentBatchActionType = null;
            return new(true);
        }

        // Anything other than fully succeeded → bump attempts and (maybe) Dead.
        var error = input.Input.Exception?.Message ?? input.Input.StoreDataResult?.RetryException?.ToString();
        BumpAttempts(batchId, error);
        currentBatchId = null;
        currentBatchActionType = null;
        return new(true);
    }

    private void PromoteBatchToSource(Guid batchId, SyncActionType actionType)
    {
        var source = DuckDbSchemaHelpers.QuoteIdentifier(GetSourceTableName());
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var pk = DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

        var sourceColumns = GetModelProperties()
            .Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name))
            .Concat(new[] {
                DuckDbSchemaHelpers.QuoteIdentifier(PrimaryKeyColumn),
                DuckDbSchemaHelpers.QuoteIdentifier(RowHashColumn),
                DuckDbSchemaHelpers.QuoteIdentifier(LoadedAtColumn) })
            .ToList();
        var sourceColList = string.Join(", ", sourceColumns);

        if (actionType == SyncActionType.Delete)
        {
            ExecuteNonQuery(
                $"DELETE FROM {source} WHERE {pk} IN " +
                $"(SELECT {pk} FROM {changes} WHERE {batchIdCol} = '{batchId}')");
        }
        else
        {
            // Add and Update both upsert into source.
            ExecuteNonQuery(
                $"INSERT OR REPLACE INTO {source} ({sourceColList}) " +
                $"SELECT {sourceColList} FROM {changes} WHERE {batchIdCol} = '{batchId}'");
        }
    }

    private void DeleteBatchFromChanges(Guid batchId)
    {
        ExecuteNonQuery(
            $"DELETE FROM {DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName())} " +
            $"WHERE {DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn)} = '{batchId}'");
    }

    private void BumpAttempts(Guid batchId, string? error)
    {
        var maxAttempts = RequireConfig().MaxAttempts;
        var changes = DuckDbSchemaHelpers.QuoteIdentifier(GetChangesTableName());
        var status = DuckDbSchemaHelpers.QuoteIdentifier(StatusColumn);
        var attemptCount = DuckDbSchemaHelpers.QuoteIdentifier(AttemptCountColumn);
        var lastAttemptAt = DuckDbSchemaHelpers.QuoteIdentifier(LastAttemptAtColumn);
        var lastError = DuckDbSchemaHelpers.QuoteIdentifier(LastErrorColumn);
        var batchIdCol = DuckDbSchemaHelpers.QuoteIdentifier(BatchIdColumn);

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
    }

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        return new(input.Input.Succeeded);
    }

    #endregion

    #region Cleanup

    public ValueTask Succeeded(SyncFunctionInput input) => default;

    public ValueTask Finished(SyncFunctionInput input)
    {
        CleanupLocalFiles();
        return default;
    }

    public ValueTask Reset()
    {
        CloseConnection();
        CleanupLocalFiles();
        currentBatchId = null;
        currentBatchActionType = null;
        return default;
    }

    public async ValueTask DisposeAsync()
    {
        await Reset();
    }

    private void CleanupLocalFiles()
    {
        if (string.IsNullOrEmpty(workingDirectory))
            return;

        try
        {
            if (Directory.Exists(workingDirectory))
                Directory.Delete(workingDirectory, recursive: true);
        }
        catch { }

        workingDirectory = null;
        localCsvPath = null;
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

    private DuckDbCsvSyncDataSourceConfigurations<TCsv> RequireConfig()
    {
        return Configurations
            ?? throw new InvalidOperationException("DuckDbCsvSyncDataSource is not configured. Call Configure(...) first.");
    }

    private string GetSourceTableName() => Configurations?.SourceTableName ?? $"{typeof(TCsv).Name}_source";

    private string GetChangesTableName() => Configurations?.ChangesTableName ?? $"{typeof(TCsv).Name}_changes";

    private string GetStagingTableName() => $"{typeof(TCsv).Name}_staging";

    /// <summary>
    /// Model properties = all public instance properties of TCsv excluding the metadata columns
    /// declared on <see cref="SyncCsvBase"/>.
    /// </summary>
    private static PropertyInfo[] GetModelProperties()
    {
        var metadataNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(SyncCsvBase._PrimaryKey),
            nameof(SyncCsvBase._RowHash),
            nameof(SyncCsvBase._LoadedAt)
        };

        return DuckDbSchemaHelpers.GetPropertiesWithChildPriority<TCsv>()
            .Where(p => !metadataNames.Contains(p.Name))
            .ToArray();
    }

    /// <summary>
    /// Properties used for materializing a TCsv instance from the changes table — model
    /// columns + metadata columns. Order must match the SELECT projection in
    /// <see cref="MaterializeBatch"/>.
    /// </summary>
    private static PropertyInfo[] GetMaterializableProperties()
    {
        return DuckDbSchemaHelpers.GetPropertiesWithChildPriority<TCsv>();
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

    private void ExecuteNonQuery(string sql)
    {
        using var cmd = connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
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

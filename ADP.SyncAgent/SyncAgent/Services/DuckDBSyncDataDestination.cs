using System.Globalization;
using System.Reflection;
using System.Text;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Extensions;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// Writes mapped rows into a DuckDB table whose columns follow <typeparamref name="TDestination"/>.
/// <para>
/// The table is created from the model the first time it is met and, on every run after that,
/// reconciled with the model BY NAME before anything is written: a property the model gained is added
/// to the table as a new column (wherever it sits in the model), a column the model no longer has is
/// kept and left alone, and a type or primary-key difference — which no automatic rewrite can settle
/// without losing something — fails the run loudly with the exact columns named. Rows are then staged
/// in a table built from the model, appended one row at a time so a value the engine rejects never
/// leaves a torn row behind, and merged into the live table by column name. A batch that cannot be
/// stored is reported as a failure with its cause, never as success.
/// </para>
/// <para>
/// This replaced a positional write into a clone of the live table. That older shape silently lost
/// every row after the first of a batch whenever a model gained a property the table did not have,
/// and wrote values into the wrong columns when the property was added mid-model; the runs reported
/// success throughout. See <see cref="DuckDBSchemaChange"/> for the signal hosts use to re-queue
/// rows lost that way.
/// </para>
/// </summary>
public class DuckDBSyncDataDestination<TSource, TDestination, DuckDB>
    : ISyncDataAdapter<TSource, TDestination, DuckDBSyncDataDestinationConfigurations<TSource, TDestination>, DuckDBSyncDataDestination<TSource, TDestination, DuckDB>>
    where TSource : class
    where TDestination : class
    where DuckDB : DuckDBConnection
{
    private readonly DuckDB db;

    // One property walk per adapter: the same list feeds the DDL, the staging table and the appender,
    // so a column can never be written in a different order than it was declared.
    private static readonly PropertyInfo[] Properties = DuckDbSchemaHelpers.GetPropertiesWithChildPriority<TDestination>();

    public virtual DuckDBSyncDataDestinationConfigurations<TSource, TDestination>? Configurations { get; private set; }

    public ISyncEngine<TSource, TDestination> SyncService { get; private set; }

    /// <summary>
    /// What the last <see cref="Preparing"/> changed on the live table — null when it already matched
    /// the model. Also delivered to <see cref="DuckDBSyncDataDestinationConfigurations{TSource, TDestination}.SchemaChanged"/>.
    /// </summary>
    public DuckDBSchemaChange? LastSchemaChange { get; private set; }

    public DuckDBSyncDataDestination(DuckDB db)
    {
        this.db = db;
    }

    public DuckDBSyncDataDestination<TSource, TDestination, DuckDB> SetSyncService(ISyncEngine<TSource, TDestination> syncService)
    {
        this.SyncService = syncService;
        return this;
    }

    public ISyncEngine<TSource, TDestination> Configure(DuckDBSyncDataDestinationConfigurations<TSource, TDestination> configurations, bool configureSyncService = true)
    {
        Configurations = configurations;

        // Resolved (and so validated) HERE, at wiring time, rather than only inside Preparing: a
        // misdeclared index is a programming error, and Preparing's catch-all would turn it into a
        // bare "prepare failed" with the offending column nowhere in sight.
        ResolveIndexes();

        var previousPreparing = SyncService.Preparing;

        if (configureSyncService)
            SyncService
                .SetupPreparing(async x =>
                {
                    // The source prepares first (it computes what there is to sync). Its verdict is
                    // kept: a source that failed stays failed, and a source with nothing to do stays
                    // skipped — this destination only ever downgrades the outcome, never masks it.
                    var sourceResult = previousPreparing is null
                        ? SyncPreparingResponseAction.Succeeded
                        : await previousPreparing(x);

                    if (sourceResult == SyncPreparingResponseAction.Failed)
                        return SyncPreparingResponseAction.Failed;

                    // Prepared even on a skipped run: reconciling the table (and telling the host
                    // about it) must not wait for the next change to arrive.
                    var destinationResult = await Preparing(x);

                    if (destinationResult == SyncPreparingResponseAction.Failed)
                        return SyncPreparingResponseAction.Failed;

                    // A source with nothing to do decided that BEFORE the table was reconciled. When the
                    // reconciliation changed something, a SchemaChanged handler may just have re-queued
                    // rows — so the actions run this time and pick them up rather than waiting a cycle.
                    return LastSchemaChange is not null
                        ? SyncPreparingResponseAction.Succeeded
                        : sourceResult;
                })
                .SetupStoreBatchData(async x => await StoreBatchData(x));

        return SyncService;
    }

    /// <summary>
    /// Creates the table when missing, then reconciles it with the model (see the class remarks) and
    /// creates the configured indexes. <paramref name="input"/> may be null for a bare probe call; it
    /// is only used for logging.
    /// </summary>
    public async ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        var loggers = input?.SyncProgressIndicators;
        LastSchemaChange = null;

        try
        {
            var tableName = GetTableName();
            var primaryKeyNames = GetPrimaryKeyPropertyNames();

            ExecuteNonQuery(BuildCreateTableSql(tableName, primaryKeyNames));

            var change = await ReconcileSchemaAsync(tableName, primaryKeyNames, loggers);

            if (change is null)
                return SyncPreparingResponseAction.Failed;

            CreateIndexes(tableName);

            if (change.AddedColumns.Count > 0)
            {
                LastSchemaChange = change;

                if (Configurations?.SchemaChanged is not null)
                    await Configurations.SchemaChanged(change);
            }

            return SyncPreparingResponseAction.Succeeded;
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (loggers is not null)
                await loggers.LogError(exception, "Preparing DuckDB table {0} for {1} failed.", GetTableName(), typeof(TDestination).Name);

            return SyncPreparingResponseAction.Failed;
        }
    }

    #region Schema reconciliation

    private async Task<DuckDBSchemaChange?> ReconcileSchemaAsync(string tableName, IReadOnlyList<string> primaryKeyNames, IEnumerable<ISyncEngineLogger>? loggers)
    {
        var live = ReadLiveColumns(tableName);
        var expected = ReadModelColumns(tableName);

        var liveByName = live.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var expectedByName = expected.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var typeMismatches = live
            .Where(x => expectedByName.TryGetValue(x.Name, out var e) && !string.Equals(e.Type, x.Type, StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{x.Name}: table {x.Type}, model {expectedByName[x.Name].Type}")
            .ToList();

        var livePrimaryKey = ReadPrimaryKeyColumns(tableName);
        var expectedPrimaryKey = Properties
            .Where(p => primaryKeyNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();
        var primaryKeyDiffers = !livePrimaryKey.ToHashSet(StringComparer.OrdinalIgnoreCase)
            .SetEquals(expectedPrimaryKey);

        if (typeMismatches.Count > 0 || primaryKeyDiffers)
        {
            // Nothing here can be fixed without a decision: converting a column may lose values, and a
            // different key changes what "the same row" means. Refuse, and name everything.
            if (loggers is not null)
                await loggers.LogError(
                    "DuckDB table {0} does not match {1} and cannot be reconciled automatically. " +
                    "Type mismatches: [{2}]. Table primary key: [{3}]; model primary key: [{4}]. " +
                    "Table columns: [{5}]. Model columns: [{6}].",
                    tableName, typeof(TDestination).Name,
                    string.Join(", ", typeMismatches),
                    string.Join(", ", livePrimaryKey), string.Join(", ", expectedPrimaryKey),
                    string.Join(", ", live.Select(x => x.ToString())), string.Join(", ", expected.Select(x => x.ToString())));

            return null;
        }

        var missing = expected.Where(x => !liveByName.ContainsKey(x.Name)).ToList();
        var extra = live.Where(x => !expectedByName.ContainsKey(x.Name)).Select(x => x.Name).ToList();

        foreach (var column in missing)
        {
            ExecuteNonQuery($"ALTER TABLE {tableName} ADD COLUMN IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(column.Name)} {column.Type}");

            if (loggers is not null)
                await loggers.LogWarning("DuckDB table {0} was missing model column {1} {2}; added it. Existing rows hold NULL there until they are re-sent.", tableName, column.Name, column.Type);
        }

        if (extra.Count > 0 && loggers is not null)
            await loggers.LogWarning("DuckDB table {0} has columns the model {1} no longer declares: [{2}]. They are kept and not written.", tableName, typeof(TDestination).Name, string.Join(", ", extra));

        return new DuckDBSchemaChange(tableName, missing.Select(x => x.Name).ToList(), extra);
    }

    private IReadOnlyList<DuckDBColumn> ReadLiveColumns(string tableName)
    {
        var (databaseFilter, schemaName, bareName) = ResolveTableIdentity(tableName);

        using var cmd = db.CreateCommand();
        cmd.CommandText =
            "SELECT column_name, data_type FROM duckdb_columns() " +
            $"WHERE {databaseFilter} AND schema_name = {DuckDbSchemaHelpers.QuoteString(schemaName)} AND table_name = {DuckDbSchemaHelpers.QuoteString(bareName)} " +
            "ORDER BY column_index";

        return ReadColumns(cmd);
    }

    // The model's columns with the types DuckDB itself reports for them — not the mapping's spelling
    // ("DECIMAL(38, 10)" vs "DECIMAL(38,10)") — so they compare exactly against the live table's.
    // A TEMP table is connection-local, so a concurrent run on another connection never sees it.
    private IReadOnlyList<DuckDBColumn> ReadModelColumns(string tableName)
    {
        var probeTable = $"__adp_model_probe_{SanitizeForIdentifier(tableName)}";

        ExecuteNonQuery($"CREATE OR REPLACE TEMP TABLE {DuckDbSchemaHelpers.QuoteIdentifier(probeTable)} ({BuildColumnDefinitions()})");

        try
        {
            using var cmd = db.CreateCommand();
            cmd.CommandText =
                "SELECT column_name, data_type FROM duckdb_columns() " +
                $"WHERE database_name = 'temp' AND table_name = {DuckDbSchemaHelpers.QuoteString(probeTable)} " +
                "ORDER BY column_index";

            return ReadColumns(cmd);
        }
        finally
        {
            ExecuteNonQuery($"DROP TABLE IF EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(probeTable)}");
        }
    }

    private IReadOnlyList<string> ReadPrimaryKeyColumns(string tableName)
    {
        var (databaseFilter, schemaName, bareName) = ResolveTableIdentity(tableName);

        using var cmd = db.CreateCommand();
        cmd.CommandText =
            "SELECT unnest(constraint_column_names) FROM duckdb_constraints() " +
            $"WHERE {databaseFilter} AND schema_name = {DuckDbSchemaHelpers.QuoteString(schemaName)} AND table_name = {DuckDbSchemaHelpers.QuoteString(bareName)} " +
            "AND constraint_type = 'PRIMARY KEY'";

        var columns = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns.Add(reader.GetString(0));

        return columns;
    }

    private static IReadOnlyList<DuckDBColumn> ReadColumns(DuckDBCommand cmd)
    {
        var columns = new List<DuckDBColumn>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            columns.Add(new DuckDBColumn(reader.GetString(0), reader.GetString(1)));
        return columns;
    }

    // A configured table name is usually bare; "schema.table" and "catalog.schema.table" are honoured
    // so the catalog lookups land on the same table the DDL and DML address.
    private static (string DatabaseFilter, string SchemaName, string TableName) ResolveTableIdentity(string tableName)
    {
        var parts = tableName.Split('.');

        return parts.Length switch
        {
            3 => ($"database_name = {DuckDbSchemaHelpers.QuoteString(parts[0])}", parts[1], parts[2]),
            2 => ("database_name = current_database()", parts[0], parts[1]),
            _ => ("database_name = current_database()", "main", tableName),
        };
    }

    private sealed record DuckDBColumn(string Name, string Type)
    {
        public override string ToString() => $"{Name} {Type}";
    }

    #endregion

    #region DDL

    private string GetTableName()
    {
        return Configurations?.TableName ?? typeof(TDestination).Name;
    }

    private IReadOnlyList<string> GetPrimaryKeyPropertyNames()
    {
        if (Configurations?.PrimaryKey is null)
            return [];

        return DuckDbSchemaHelpers.GetPropertyNamesFromExpression(Configurations.PrimaryKey);
    }

    private string BuildCreateTableSql(string tableName, IReadOnlyList<string> primaryKeyNames)
    {
        var keyColumns = Properties
            .Where(p => primaryKeyNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p => DuckDbSchemaHelpers.QuoteIdentifier(p.Name))
            .ToList();

        var definitions = BuildColumnDefinitions();

        if (keyColumns.Count > 0)
            definitions += $",\n    PRIMARY KEY ({string.Join(", ", keyColumns)})";

        return $"CREATE TABLE IF NOT EXISTS {tableName} (\n{definitions}\n)";
    }

    private static string BuildColumnDefinitions()
        => string.Join(",\n", Properties.Select(p => $"    {DuckDbSchemaHelpers.QuoteIdentifier(p.Name)} {DuckDbSchemaHelpers.MapCSharpTypeToDuckDB(p.PropertyType)}"));

    /// <summary>
    /// Creates every configured secondary index, after the table exists. <c>IF NOT EXISTS</c> makes
    /// this a no-op from the second run on — indexes live in the database file, so only the run that
    /// first meets a table pays for building them.
    ///
    /// <para>Before the batches, not after: an incremental run inherits the indexes from the run
    /// that created the table anyway, so deferring would only ever save the FIRST load's index
    /// maintenance — and would leave a table that failed mid-run with no indexes at all, which is
    /// exactly when a reader is most likely to meet it.</para>
    /// </summary>
    private void CreateIndexes(string tableName)
    {
        foreach (var index in ResolveIndexes())
        {
            ExecuteNonQuery(
                $"CREATE INDEX IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(index.Name)} " +
                $"ON {tableName} ({string.Join(", ", index.Expressions)})");
        }
    }

    /// <summary>
    /// Turns the configured <see cref="DuckDBIndexDefinition{TDestination}"/>s into named lists of
    /// SQL expressions. Column names come from the destination's properties (so an index can only
    /// name a column the table actually has, and quoting keeps a property called <c>Order</c> or
    /// <c>Group</c> from becoming a syntax error); raw SQL expressions pass through verbatim.
    /// </summary>
    private IReadOnlyList<ResolvedIndex> ResolveIndexes()
    {
        var definitions = Configurations?.Indexes;

        if (definitions is null || definitions.Count == 0)
            return [];

        var tableName = GetTableName();
        var resolved = new List<ResolvedIndex>(definitions.Count);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (definition is null)
                throw new InvalidOperationException($"A null index definition was configured for DuckDB table '{tableName}'.");

            var expressions = new List<string>();
            var nameParts = new List<string>();

            if (definition.Columns is not null)
            {
                var memberNames = DuckDbSchemaHelpers.GetPropertyNamesFromExpression(definition.Columns);

                if (memberNames.Count == 0)
                    throw new InvalidOperationException(
                        $"An index on DuckDB table '{tableName}' has a Columns expression that names no property of {typeof(TDestination).Name}.");

                foreach (var memberName in memberNames)
                {
                    var property = Properties.FirstOrDefault(x => string.Equals(x.Name, memberName, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException(
                            $"An index on DuckDB table '{tableName}' names '{memberName}', which is not a column of {typeof(TDestination).Name}.");

                    expressions.Add(DuckDbSchemaHelpers.QuoteIdentifier(property.Name));
                    nameParts.Add(property.Name);
                }
            }

            foreach (var sqlExpression in definition.SqlExpressions ?? [])
            {
                if (string.IsNullOrWhiteSpace(sqlExpression))
                    throw new InvalidOperationException($"An index on DuckDB table '{tableName}' has a blank SQL expression.");

                expressions.Add(sqlExpression.Trim());
                nameParts.Add(sqlExpression.Trim());
            }

            if (expressions.Count == 0)
                throw new InvalidOperationException(
                    $"An index on DuckDB table '{tableName}' declares no columns. Set Columns, SqlExpressions, or both.");

            var name = string.IsNullOrWhiteSpace(definition.Name)
                ? BuildIndexName(tableName, nameParts)
                : definition.Name.Trim();

            if (!usedNames.Add(name))
                throw new InvalidOperationException(
                    $"Two indexes on DuckDB table '{tableName}' resolve to the name '{name}'. Give one of them an explicit Name.");

            resolved.Add(new ResolvedIndex(name, expressions));
        }

        return resolved;
    }

    /// <summary>
    /// <c>IX_{table}_{parts}</c>, with anything that is not a letter, a digit or an underscore
    /// folded to a single underscore — the name has to survive being quoted as an identifier, and it
    /// has to come out the SAME on every run, because <c>CREATE INDEX IF NOT EXISTS</c> recognises
    /// an existing index by nothing but its name.
    /// </summary>
    private static string BuildIndexName(string tableName, IEnumerable<string> nameParts)
    {
        var sb = new StringBuilder("IX");

        AppendSanitized(sb, tableName);

        foreach (var part in nameParts)
            AppendSanitized(sb, part);

        // A part that ends in punctuation — an expression like lower("Code") — would otherwise
        // trail an underscore.
        return sb.ToString().TrimEnd('_');
    }

    private static void AppendSanitized(StringBuilder sb, string value)
    {
        sb.Append('_');

        var lastWasSeparator = true;

        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                sb.Append('_');
                lastWasSeparator = true;
            }
        }
    }

    private static string SanitizeForIdentifier(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        return sb.ToString();
    }

    private sealed record ResolvedIndex(string Name, IReadOnlyList<string> Expressions);

    #endregion

    #region Store

    public async ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        var result = new SyncStoreDataResult<TDestination>();
        var succeededItems = new List<TDestination?>();
        var failedItems = new List<TDestination?>();
        var skippedItems = new List<TDestination?>();

        var items = input.Input.Items;
        if (input.Input.Status.CurrentRetryCount > 0 && (input.Input.PreviousResult?.IsEligibleToUseItAsRetryInput(input.Input?.Items?.LongCount()) ?? false))
            items = input.Input?.PreviousResult?.FailedItems;

        if (items is null || !items.Any())
        {
            result.SucceededItems = succeededItems;
            result.FailedItems = failedItems;
            result.SkippedItems = skippedItems;
            return result;
        }

        var tableName = GetTableName();
        var continueAfterFail = Configurations?.ContinueAfterFail ?? false;

        if (input!.Input.Status.ActionType == SyncActionType.Delete)
        {
            ProcessDeleteBatch(tableName, items, succeededItems, failedItems, skippedItems, continueAfterFail);
        }
        else
        {
            var stagingTableName = GenerateScratchTableName(tableName, "staging");

            try
            {
                // Built from the model, not cloned from the live table: exactly as wide as the appender
                // writes, whatever the live table looks like today.
                ExecuteNonQuery($"CREATE OR REPLACE TEMP TABLE {DuckDbSchemaHelpers.QuoteIdentifier(stagingTableName)} ({BuildColumnDefinitions()})");

                var abort = BulkLoadToStagingTable(stagingTableName, items, succeededItems, failedItems, continueAfterFail);

                if (abort is not null)
                {
                    // A batch that stops part-way is not stored at all: nothing reaches the live table,
                    // every row is reported failed, and the cause travels with the result so the engine
                    // logs it and the queue records it — the opposite of a quiet partial write.
                    var (failedIndex, exception) = abort.Value;
                    failedItems.Clear();
                    failedItems.AddRange(items);
                    succeededItems.Clear();

                    var message =
                        $"Batch into DuckDB table {tableName} aborted at item {failedIndex + 1} of {items.Count()}: {exception.Message} " +
                        "(nothing from this batch was written; set ContinueAfterFail to skip bad rows instead).";

                    if (input.SyncProgressIndicators is not null)
                        await input.SyncProgressIndicators.LogError(exception, message);

                    result.RetryException = new RetryException(new InvalidOperationException(message, exception));
                }
                else if (succeededItems.Count > 0)
                {
                    UpsertFromStagingTable(tableName, stagingTableName);
                }
            }
            finally
            {
                DropScratchTable(stagingTableName);
            }
        }

        result.SucceededItems = succeededItems;
        result.FailedItems = failedItems;
        result.SkippedItems = skippedItems;
        return result;
    }

    /// <summary>
    /// Appends every item to the staging table, one atomic row at a time. Returns null when the batch
    /// may be stored, or the index and cause of the failure that aborted it (only with
    /// <c>ContinueAfterFail</c> off; with it on, bad rows go to <paramref name="failedItems"/> and the
    /// rest continue).
    /// </summary>
    private (int Index, Exception Exception)? BulkLoadToStagingTable(
        string stagingTableName,
        IEnumerable<TDestination?> items,
        List<TDestination?> succeededItems,
        List<TDestination?> failedItems,
        bool continueAfterFail)
    {
        DuckDBAppender? appender = null;
        var index = -1;

        try
        {
            appender = db.CreateAppender(stagingTableName);

            foreach (var item in items)
            {
                index++;

                if (item is null)
                {
                    failedItems.Add(item);
                    if (!continueAfterFail)
                        return (index, new InvalidOperationException("The batch contains a null item."));
                    continue;
                }

                // Every conversion happens before the appender is touched, so a value that cannot be
                // represented fails the item and nothing else.
                object?[] values;
                try
                {
                    values = Materialize(item);
                }
                catch (Exception exception)
                {
                    failedItems.Add(item);
                    if (!continueAfterFail)
                        return (index, exception);
                    continue;
                }

                try
                {
                    // AppendRow commits the row only once every column is in; a failure inside leaves
                    // no half-row in the staging table.
                    appender.AppendRow(row =>
                    {
                        for (var i = 0; i < Properties.Length; i++)
                            DuckDbSchemaHelpers.AppendValue(row, values[i], Properties[i].PropertyType);
                    });

                    succeededItems.Add(item);
                }
                catch (Exception exception)
                {
                    failedItems.Add(item);
                    if (!continueAfterFail)
                        return (index, exception);

                    // The appender does not accept further rows after a failed one; carry on with a fresh one.
                    appender.Dispose();
                    appender = db.CreateAppender(stagingTableName);
                }
            }

            return null;
        }
        finally
        {
            appender?.Dispose();
        }
    }

    private static object?[] Materialize(TDestination item)
    {
        var values = new object?[Properties.Length];
        for (var i = 0; i < Properties.Length; i++)
            values[i] = Properties[i].GetValue(item);
        return values;
    }

    // Merged by column NAME: the live table may carry columns the model no longer has (kept as they
    // are), and its columns may sit in any order.
    private void UpsertFromStagingTable(string tableName, string stagingTableName)
    {
        ExecuteNonQuery($"INSERT OR REPLACE INTO {tableName} BY NAME SELECT * FROM {DuckDbSchemaHelpers.QuoteIdentifier(stagingTableName)}");
    }

    #endregion

    #region Delete

    private void ProcessDeleteBatch(
        string tableName,
        IEnumerable<TDestination?> items,
        List<TDestination?> succeededItems,
        List<TDestination?> failedItems,
        List<TDestination?> skippedItems,
        bool continueAfterFail)
    {
        var keyProperties = GetPrimaryKeyProperties();
        if (keyProperties.Length == 0)
            throw new InvalidOperationException("DuckDB delete action requires PrimaryKey configuration.");

        var deleteKeysTableName = GenerateScratchTableName(tableName, "delete_keys");

        try
        {
            var keyDefinitions = string.Join(", ", keyProperties.Select(p => $"{DuckDbSchemaHelpers.QuoteIdentifier(p.Name)} {DuckDbSchemaHelpers.MapCSharpTypeToDuckDB(p.PropertyType)}"));
            ExecuteNonQuery($"CREATE OR REPLACE TEMP TABLE {DuckDbSchemaHelpers.QuoteIdentifier(deleteKeysTableName)} ({keyDefinitions})");

            var hasAnyDeleteKey = BulkLoadDeleteKeys(deleteKeysTableName, keyProperties, items, succeededItems, failedItems, skippedItems, continueAfterFail);

            if (hasAnyDeleteKey)
                DeleteFromDeleteKeysTable(tableName, deleteKeysTableName, keyProperties);
        }
        finally
        {
            DropScratchTable(deleteKeysTableName);
        }
    }

    private PropertyInfo[] GetPrimaryKeyProperties()
    {
        var keyNames = GetPrimaryKeyPropertyNames()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (keyNames.Count == 0)
            return [];

        var resolved = new List<PropertyInfo>();
        foreach (var keyName in keyNames)
        {
            var property = Properties.FirstOrDefault(p => string.Equals(p.Name, keyName, StringComparison.OrdinalIgnoreCase));
            if (property is not null)
                resolved.Add(property);
        }

        return resolved.ToArray();
    }

    private bool BulkLoadDeleteKeys(
        string deleteKeysTableName,
        IReadOnlyList<PropertyInfo> keyProperties,
        IEnumerable<TDestination?> items,
        List<TDestination?> succeededItems,
        List<TDestination?> failedItems,
        List<TDestination?> skippedItems,
        bool continueAfterFail)
    {
        using var appender = db.CreateAppender(deleteKeysTableName);
        var uniqueKeySet = new HashSet<string>(StringComparer.Ordinal);
        var hasAnyDeleteKey = false;

        foreach (var item in items)
        {
            if (item is null)
            {
                failedItems.Add(item);
                if (!continueAfterFail)
                    return hasAnyDeleteKey;

                continue;
            }

            try
            {
                var keyValues = new object?[keyProperties.Count];
                for (var i = 0; i < keyProperties.Count; i++)
                    keyValues[i] = keyProperties[i].GetValue(item);

                var dedupeKey = BuildDeleteDedupeKey(keyValues);
                if (!uniqueKeySet.Add(dedupeKey))
                {
                    skippedItems.Add(item);
                    continue;
                }

                appender.AppendRow(row =>
                {
                    for (var i = 0; i < keyProperties.Count; i++)
                        DuckDbSchemaHelpers.AppendValue(row, keyValues[i], keyProperties[i].PropertyType);
                });

                succeededItems.Add(item);
                hasAnyDeleteKey = true;
            }
            catch
            {
                failedItems.Add(item);

                if (!continueAfterFail)
                    return hasAnyDeleteKey;
            }
        }

        return hasAnyDeleteKey;
    }

    private static string BuildDeleteDedupeKey(object?[] keyValues)
    {
        return string.Join("", keyValues.Select(ToInvariantKeyPart));
    }

    private static string ToInvariantKeyPart(object? value)
    {
        if (value is null)
            return "<NULL>";

        if (value is DateTime dateTime)
            return dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        if (value is byte[] bytes)
            return Convert.ToBase64String(bytes);

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString() ?? string.Empty;
    }

    private void DeleteFromDeleteKeysTable(string tableName, string deleteKeysTableName, IReadOnlyList<PropertyInfo> keyProperties)
    {
        var joinPredicate = string.Join(
            " AND ",
            keyProperties.Select(x => $"t.{DuckDbSchemaHelpers.QuoteIdentifier(x.Name)} IS NOT DISTINCT FROM k.{DuckDbSchemaHelpers.QuoteIdentifier(x.Name)}"));

        ExecuteNonQuery($@"
            DELETE FROM {tableName} AS t
            USING {DuckDbSchemaHelpers.QuoteIdentifier(deleteKeysTableName)} AS k
            WHERE {joinPredicate}");
    }

    #endregion

    #region SQL helpers

    // Scratch tables are TEMP (connection-local) and uniquely named, so two batches on two connections
    // to the same file never collide; the table part is sanitised because a configured name may be
    // catalog-qualified.
    private static string GenerateScratchTableName(string tableName, string purpose)
        => $"{SanitizeForIdentifier(tableName)}_{purpose}_{Guid.NewGuid():N}";

    private void DropScratchTable(string scratchTableName)
    {
        ExecuteNonQuery($"DROP TABLE IF EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(scratchTableName)}");
    }

    private void ExecuteNonQuery(string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Not Implemented

    public ValueTask<bool> ActionCompleted(SyncFunctionInput<SyncActionCompletedInput> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ActionStarted(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> AdvancedMapping(SyncFunctionInput<SyncMappingInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> BatchCompleted(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<RetryAction> BatchRetry(SyncFunctionInput<SyncBatchCompleteRetryInput<TSource, TDestination>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public ValueTask Failed(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Finished(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TSource?>?> GetSourceBatchItems(SyncFunctionInput<SyncGetBatchDataInput<TSource>> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<TDestination?>?> Mapping(IEnumerable<TSource?>? sourceItems, SyncActionType actionType)
    {
        throw new NotImplementedException();
    }

    public ValueTask Reset()
    {
        throw new NotImplementedException();
    }

    public ValueTask<long?> SourceTotalItemCount(SyncFunctionInput<SyncActionType> input)
    {
        throw new NotImplementedException();
    }

    public ValueTask Succeeded(SyncFunctionInput input)
    {
        throw new NotImplementedException();
    }

    #endregion
}

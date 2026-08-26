using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.SyncAgent.Configurations;
using ShiftSoftware.ADP.SyncAgent.Services.Interfaces;

namespace ShiftSoftware.ADP.SyncAgent.Services;

public class DuckDBSyncDataDestination<TSource, TDestination, DuckDB>
    : ISyncDataAdapter<TSource, TDestination, DuckDBSyncDataDestinationConfigurations<TSource, TDestination>, DuckDBSyncDataDestination<TSource, TDestination, DuckDB>>
    where TSource : class
    where TDestination : class
    where DuckDB : DuckDBConnection
{
    private readonly DuckDB db;

    public virtual DuckDBSyncDataDestinationConfigurations<TSource, TDestination>? Configurations { get; private set; }

    public ISyncEngine<TSource, TDestination> SyncService { get; private set; }

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
                    if (previousPreparing is not null)
                        await previousPreparing(x);

                    return await Preparing(x);
                })
                .SetupStoreBatchData(async x => await StoreBatchData(x));

        return SyncService;
    }

    public ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        try
        {
            var tableName = GetTableName();
            var primaryKeyNames = GetPrimaryKeyPropertyNames();
            var createTableSql = GenerateCreateTableSql(tableName, primaryKeyNames);

            using var createTableCmd = db.CreateCommand();
            createTableCmd.CommandText = createTableSql;
            createTableCmd.ExecuteNonQuery();

            CreateIndexes(tableName);

            return ValueTask.FromResult(SyncPreparingResponseAction.Succeeded);
        }
        catch (IOException)
        {
            throw;
        }
        catch
        {
            return ValueTask.FromResult(SyncPreparingResponseAction.Failed);
        }
    }

    private string GetTableName()
    {
        return Configurations?.TableName ?? typeof(TDestination).Name;
    }

    private IReadOnlyList<string> GetPrimaryKeyPropertyNames()
    {
        if (Configurations?.PrimaryKey is null)
            return [];

        return GetPropertyNamesFromExpression(Configurations.PrimaryKey);
    }

    private static IReadOnlyList<string> GetPropertyNamesFromExpression(Expression<Func<TDestination, object>> expression)
    {
        var members = new List<string>();
        CollectMemberNames(expression.Body, members);
        return members;
    }

    private static void CollectMemberNames(Expression expression, ICollection<string> members)
    {
        switch (expression)
        {
            case MemberExpression memberExpression:
                members.Add(memberExpression.Member.Name);
                return;

            case UnaryExpression { Operand: var unaryOperand }:
                CollectMemberNames(unaryOperand, members);
                return;

            case NewExpression newExpression:
                foreach (var argument in newExpression.Arguments)
                    CollectMemberNames(argument, members);
                return;

            case NewArrayExpression newArrayExpression:
                foreach (var arrayExpression in newArrayExpression.Expressions)
                    CollectMemberNames(arrayExpression, members);
                return;
        }
    }

    /// <summary>
    /// Gets properties from TDestination with child class properties taking priority over parent class properties.
    /// When both parent and child have a property with the same name, only the child's property is included.
    /// </summary>
    private static PropertyInfo[] GetPropertiesWithChildPriority()
    {
        var allProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return allProperties
            .GroupBy(p => p.Name)
            .Select(g => g.OrderByDescending(p => GetInheritanceDepth(p.DeclaringType)).First())
            .ToArray();
    }

    private static int GetInheritanceDepth(Type? type)
    {
        var depth = 0;
        while (type is not null)
        {
            depth++;
            type = type.BaseType;
        }
        return depth;
    }

    private string GenerateCreateTableSql(string tableName, IReadOnlyList<string> primaryKeyNames)
    {
        var properties = GetPropertiesWithChildPriority();
        var keySet = primaryKeyNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var columnDefinitions = new List<string>();

        foreach (var property in properties)
        {
            var columnName = property.Name;
            var duckDbType = MapCSharpTypeToDuckDB(property.PropertyType);
            var columnDef = $"    {columnName} {duckDbType}";
            columnDefinitions.Add(columnDef);
        }

        if (keySet.Count > 0)
        {
            var keyColumns = properties
                .Where(p => keySet.Contains(p.Name))
                .Select(p => p.Name)
                .ToList();

            if (keyColumns.Count > 0)
                columnDefinitions.Add($"    PRIMARY KEY ({string.Join(", ", keyColumns)})");
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.Append(')');

        return sb.ToString();
    }

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
            using var createIndexCmd = db.CreateCommand();
            createIndexCmd.CommandText =
                $"CREATE INDEX IF NOT EXISTS {DuckDbSchemaHelpers.QuoteIdentifier(index.Name)} " +
                $"ON {tableName} ({string.Join(", ", index.Expressions)})";
            createIndexCmd.ExecuteNonQuery();
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
        var properties = GetPropertiesWithChildPriority();
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
                var memberNames = GetPropertyNamesFromExpression(definition.Columns);

                if (memberNames.Count == 0)
                    throw new InvalidOperationException(
                        $"An index on DuckDB table '{tableName}' has a Columns expression that names no property of {typeof(TDestination).Name}.");

                foreach (var memberName in memberNames)
                {
                    var property = properties.FirstOrDefault(x => string.Equals(x.Name, memberName, StringComparison.OrdinalIgnoreCase))
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

    private sealed record ResolvedIndex(string Name, IReadOnlyList<string> Expressions);

    private static string MapCSharpTypeToDuckDB(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch
        {
            _ when underlyingType == typeof(bool) => "BOOLEAN",
            _ when underlyingType == typeof(byte) => "UTINYINT",
            _ when underlyingType == typeof(sbyte) => "TINYINT",
            _ when underlyingType == typeof(short) => "SMALLINT",
            _ when underlyingType == typeof(ushort) => "USMALLINT",
            _ when underlyingType == typeof(int) => "INTEGER",
            _ when underlyingType == typeof(uint) => "UINTEGER",
            _ when underlyingType == typeof(long) => "BIGINT",
            _ when underlyingType == typeof(ulong) => "UBIGINT",
            _ when underlyingType == typeof(float) => "FLOAT",
            _ when underlyingType == typeof(double) => "DOUBLE",
            _ when underlyingType == typeof(decimal) => "DECIMAL(38, 10)",
            _ when underlyingType == typeof(string) => "VARCHAR",
            _ when underlyingType == typeof(char) => "VARCHAR",
            _ when underlyingType == typeof(DateTime) => "TIMESTAMP",
            _ when underlyingType == typeof(DateTimeOffset) => "TIMESTAMPTZ",
            _ when underlyingType == typeof(DateOnly) => "DATE",
            _ when underlyingType == typeof(TimeOnly) => "TIME",
            _ when underlyingType == typeof(TimeSpan) => "INTERVAL",
            _ when underlyingType == typeof(Guid) => "UUID",
            _ when underlyingType == typeof(byte[]) => "BLOB",
            _ when underlyingType.IsEnum => "INTEGER",
            _ when IsComplexType(underlyingType) => "JSON",
            _ => "VARCHAR"
        };
    }

    private static bool IsComplexType(Type type)
    {
        return type.IsClass && type != typeof(string) && type != typeof(byte[])
            || type.IsInterface
            || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace != "System");
    }

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
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
            return ValueTask.FromResult(result);
        }

        var tableName = GetTableName();
        var continueAfterFail = Configurations?.ContinueAfterFail ?? false;

        if (input.Input.Status.ActionType == SyncActionType.Delete)
        {
            ProcessDeleteBatch(
                tableName,
                items,
                succeededItems,
                failedItems,
                skippedItems,
                continueAfterFail);
        }
        else
        {
            var stagingTableName = GenerateStagingTableName(tableName);
            var properties = GetPropertiesWithChildPriority();

            try
            {
                CreateStagingTable(tableName, stagingTableName);

                BulkLoadToStagingTable(
                    stagingTableName,
                    properties,
                    items,
                    succeededItems,
                    failedItems,
                    continueAfterFail);

                UpsertFromStagingTable(tableName, stagingTableName);
            }
            finally
            {
                DropStagingTable(stagingTableName);
            }
        }

        result.SucceededItems = succeededItems;
        result.FailedItems = failedItems;
        result.SkippedItems = skippedItems;
        return ValueTask.FromResult(result);
    }

    private static string GenerateStagingTableName(string tableName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return $"{tableName}_staging_{uniqueId}";
    }

    private static string GenerateDeleteKeysTableName(string tableName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return $"{tableName}_delete_keys_{uniqueId}";
    }

    private void CreateStagingTable(string tableName, string stagingTableName)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"CREATE OR REPLACE TEMP TABLE {stagingTableName} AS SELECT * FROM {tableName} WHERE 1=0";
        cmd.ExecuteNonQuery();
    }

    private void BulkLoadToStagingTable(
        string stagingTableName,
        PropertyInfo[] properties,
        IEnumerable<TDestination?> items,
        List<TDestination?> succeededItems,
        List<TDestination?> failedItems,
        bool continueAfterFail)
    {
        using var appender = db.CreateAppender(stagingTableName);

        foreach (var item in items)
        {
            if (item is null)
            {
                failedItems.Add(item);
                continue;
            }

            try
            {
                var row = appender.CreateRow();

                foreach (var property in properties)
                {
                    var value = property.GetValue(item);
                    AppendValue(row, value, property.PropertyType);
                }

                row.EndRow();
                succeededItems.Add(item);
            }
            catch
            {
                failedItems.Add(item);

                if (!continueAfterFail)
                    return;
            }
        }
    }

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

        var deleteKeysTableName = GenerateDeleteKeysTableName(tableName);
        var hasAnyDeleteKey = false;

        try
        {
            CreateDeleteKeysTable(tableName, deleteKeysTableName, keyProperties);

            hasAnyDeleteKey = BulkLoadDeleteKeys(
                deleteKeysTableName,
                keyProperties,
                items,
                succeededItems,
                failedItems,
                skippedItems,
                continueAfterFail);

            if (hasAnyDeleteKey)
                DeleteFromDeleteKeysTable(tableName, deleteKeysTableName, keyProperties);
        }
        finally
        {
            DropStagingTable(deleteKeysTableName);
        }
    }

    private PropertyInfo[] GetPrimaryKeyProperties()
    {
        var properties = GetPropertiesWithChildPriority();
        var keyNames = GetPrimaryKeyPropertyNames()
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (keyNames.Count == 0)
            return [];

        var resolved = new List<PropertyInfo>();
        foreach (var keyName in keyNames)
        {
            var property = properties.FirstOrDefault(p => string.Equals(p.Name, keyName, StringComparison.OrdinalIgnoreCase));
            if (property is not null)
                resolved.Add(property);
        }

        return resolved.ToArray();
    }

    private void CreateDeleteKeysTable(string tableName, string deleteKeysTableName, IReadOnlyList<PropertyInfo> keyProperties)
    {
        using var cmd = db.CreateCommand();
        var keyColumns = string.Join(", ", keyProperties.Select(x => x.Name));
        cmd.CommandText = $"CREATE OR REPLACE TEMP TABLE {deleteKeysTableName} AS SELECT {keyColumns} FROM {tableName} WHERE 1=0";
        cmd.ExecuteNonQuery();
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

                var row = appender.CreateRow();
                for (var i = 0; i < keyProperties.Count; i++)
                    AppendValue(row, keyValues[i], keyProperties[i].PropertyType);

                row.EndRow();
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
        return string.Join("\u001F", keyValues.Select(ToInvariantKeyPart));
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

    private static void AppendValue(IDuckDBAppenderRow row, object? value, Type propertyType)
    {
        if (value is null)
        {
            row.AppendNullValue();
            return;
        }

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType.IsEnum)
        {
            row.AppendValue(Convert.ToInt32(value));
            return;
        }

        if (IsComplexType(underlyingType))
        {
            row.AppendValue(JsonSerializer.Serialize(value, underlyingType));
            return;
        }

        switch (value)
        {
            case bool b:
                row.AppendValue(b);
                break;
            case byte by:
                row.AppendValue(by);
                break;
            case sbyte sb:
                row.AppendValue(sb);
                break;
            case short s:
                row.AppendValue(s);
                break;
            case ushort us:
                row.AppendValue(us);
                break;
            case int i:
                row.AppendValue(i);
                break;
            case uint ui:
                row.AppendValue(ui);
                break;
            case long l:
                row.AppendValue(l);
                break;
            case ulong ul:
                row.AppendValue(ul);
                break;
            case float f:
                row.AppendValue(f);
                break;
            case double d:
                row.AppendValue(d);
                break;
            case decimal dec:
                row.AppendValue(dec);
                break;
            case string str:
                row.AppendValue(str);
                break;
            case char c:
                row.AppendValue(c.ToString());
                break;
            case DateTime dt:
                row.AppendValue(dt);
                break;
            case DateTimeOffset dto:
                row.AppendValue(dto);
                break;
            case DateOnly dateOnly:
                row.AppendValue((DateOnly?)dateOnly);
                break;
            case TimeOnly timeOnly:
                row.AppendValue((TimeOnly?)timeOnly);
                break;
            case TimeSpan ts:
                row.AppendValue(ts);
                break;
            case Guid g:
                row.AppendValue(g);
                break;
            case byte[] bytes:
                row.AppendValue(bytes);
                break;
            default:
                row.AppendValue(value.ToString());
                break;
        }
    }

    private void DeleteFromDeleteKeysTable(string tableName, string deleteKeysTableName, IReadOnlyList<PropertyInfo> keyProperties)
    {
        using var cmd = db.CreateCommand();

        var joinPredicate = string.Join(
            " AND ",
            keyProperties.Select(x => $"t.{x.Name} IS NOT DISTINCT FROM k.{x.Name}"));

        cmd.CommandText = $@"
            DELETE FROM {tableName} AS t
            USING {deleteKeysTableName} AS k
            WHERE {joinPredicate}";

        cmd.ExecuteNonQuery();
    }

    private void UpsertFromStagingTable(string tableName, string stagingTableName)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $@"
            INSERT OR REPLACE INTO {tableName}
            SELECT * FROM {stagingTableName}";
        cmd.ExecuteNonQuery();
    }

    private void DropStagingTable(string stagingTableName)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {stagingTableName}";
        cmd.ExecuteNonQuery();
    }

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

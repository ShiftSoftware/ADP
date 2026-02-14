using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
        var previousPreparing = SyncService.Preparing;

        if (configureSyncService)
            SyncService
                .SetupPreparing(async x =>
                {
                    if (previousPreparing is not null)
                        await previousPreparing(x);

                    return await Preparing(x);
                })
                .SetupStoreBatchData(async (x) => await this.StoreBatchData(x));

        return SyncService;
    }

    public ValueTask<SyncPreparingResponseAction> Preparing(SyncFunctionInput input)
    {
        try
        {
            var tableName = GetTableName();
            var primaryKeyName = GetPrimaryKeyPropertyName();
            var createTableSql = GenerateCreateTableSql(tableName, primaryKeyName);

            using var createTableCmd = db.CreateCommand();
            createTableCmd.CommandText = createTableSql;
            createTableCmd.ExecuteNonQuery();

            return ValueTask.FromResult(SyncPreparingResponseAction.Succeeded);
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(SyncPreparingResponseAction.Failed);
        }
    }

    private string GetTableName()
    {
        return this.Configurations?.TableName ?? typeof(TDestination).Name;
    }

    private string? GetPrimaryKeyPropertyName()
    {
        if (Configurations?.PrimaryKey is null)
            return null;

        return GetPropertyNameFromExpression(Configurations.PrimaryKey);
    }

    private static string? GetPropertyNameFromExpression(Expression<Func<TDestination, object>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;

        return null;
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

    private string GenerateCreateTableSql(string tableName, string? primaryKeyName)
    {
        var properties = GetPropertiesWithChildPriority();
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var columnDefinitions = new List<string>();

        foreach (var property in properties)
        {
            var columnName = property.Name;
            var duckDbType = MapCSharpTypeToDuckDB(property.PropertyType);
            var isPrimaryKey = string.Equals(columnName, primaryKeyName, StringComparison.OrdinalIgnoreCase);

            var columnDef = isPrimaryKey
                ? $"    {columnName} {duckDbType} PRIMARY KEY"
                : $"    {columnName} {duckDbType}";

            columnDefinitions.Add(columnDef);
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.Append(')');

        return sb.ToString();
    }

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
            _ => "VARCHAR"
        };
    }

    public ValueTask<SyncStoreDataResult<TDestination>> StoreBatchData(SyncFunctionInput<SyncStoreDataInput<TDestination>> input)
    {
        var result = new SyncStoreDataResult<TDestination>();
        var succeededItems = new List<TDestination?>();
        var failedItems = new List<TDestination?>();

        var items = input.Input.Items;
        if (items is null || !items.Any())
        {
            result.SucceededItems = succeededItems;
            result.FailedItems = failedItems;
            return ValueTask.FromResult(result);
        }

        var tableName = GetTableName();
        var stagingTableName = GenerateStagingTableName(tableName);
        var properties = GetPropertiesWithChildPriority();
        var continueAfterFail = Configurations?.ContinueAfterFail ?? false;

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

        result.SucceededItems = succeededItems;
        result.FailedItems = failedItems;
        return ValueTask.FromResult(result);
    }

    private static string GenerateStagingTableName(string tableName)
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return $"{tableName}_staging_{uniqueId}";
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

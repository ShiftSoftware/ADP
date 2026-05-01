using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.SyncAgent.Services;

/// <summary>
/// Shared helpers for generating DuckDB schema and SQL expressions from CLR models.
/// Used by both <see cref="DuckDBSyncDataDestination{TSource, TDestination, DuckDB}"/> and
/// <see cref="DuckDbCsvSyncDataSource{TCsv, TDestination}"/>.
/// </summary>
internal static class DuckDbSchemaHelpers
{
    /// <summary>
    /// Properties from <typeparamref name="T"/> with child class properties taking priority over
    /// parent class properties (when both declare the same name).
    /// </summary>
    public static PropertyInfo[] GetPropertiesWithChildPriority<T>()
    {
        var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

    public static IReadOnlyList<string> GetPropertyNamesFromExpression<T>(Expression<Func<T, object>> expression)
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

    public static string MapCSharpTypeToDuckDB(Type type)
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

    public static bool IsComplexType(Type type)
    {
        return type.IsClass && type != typeof(string) && type != typeof(byte[])
            || type.IsInterface
            || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace != "System");
    }

    /// <summary>
    /// Builds a CREATE TABLE statement from the given properties. Optionally appends extra
    /// metadata columns (already formatted as "name TYPE [constraints]").
    /// </summary>
    public static string BuildCreateTableSql(
        string tableName,
        IEnumerable<PropertyInfo> properties,
        IEnumerable<string>? extraColumns = null,
        IEnumerable<string>? primaryKeyColumns = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} (");

        var columnDefinitions = new List<string>();

        foreach (var property in properties)
        {
            var duckDbType = MapCSharpTypeToDuckDB(property.PropertyType);
            columnDefinitions.Add($"    {QuoteIdentifier(property.Name)} {duckDbType}");
        }

        if (extraColumns is not null)
        {
            foreach (var col in extraColumns)
                columnDefinitions.Add($"    {col}");
        }

        if (primaryKeyColumns is not null)
        {
            var keyColumns = primaryKeyColumns
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(QuoteIdentifier)
                .ToList();

            if (keyColumns.Count > 0)
                columnDefinitions.Add($"    PRIMARY KEY ({string.Join(", ", keyColumns)})");
        }

        sb.AppendLine(string.Join(",\n", columnDefinitions));
        sb.Append(')');

        return sb.ToString();
    }

    /// <summary>
    /// Builds a SQL expression that produces a deterministic key string from the given columns.
    /// NULL values become a sentinel so different combinations cannot collide.
    /// </summary>
    public static string BuildCompositeKeySqlExpression(IEnumerable<string> columnNames, string tableAlias = "")
    {
        var prefix = string.IsNullOrEmpty(tableAlias) ? "" : QuoteIdentifier(tableAlias) + ".";
        var parts = columnNames
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => $"COALESCE(CAST({prefix}{QuoteIdentifier(c)} AS VARCHAR), '<NULL>')")
            .ToList();

        if (parts.Count == 0)
            throw new InvalidOperationException("At least one column is required to build a composite key expression.");

        return $"md5(concat_ws(CHR(31), {string.Join(", ", parts)}))";
    }

    /// <summary>
    /// Same as <see cref="BuildCompositeKeySqlExpression(IEnumerable{string}, string)"/> but for
    /// the row-hash. Identical structure; kept separate so callers can read at call site.
    /// </summary>
    public static string BuildRowHashSqlExpression(IEnumerable<string> columnNames, string tableAlias = "")
    {
        return BuildCompositeKeySqlExpression(columnNames, tableAlias);
    }

    public static string QuoteIdentifier(string name)
    {
        // DuckDB identifier quoting; defends against names that collide with keywords or contain
        // characters that would otherwise be invalid bare identifiers.
        return "\"" + name.Replace("\"", "\"\"") + "\"";
    }

    public static string QuoteString(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }

    /// <summary>
    /// Appends a CLR value into a DuckDB appender row using the property's type.
    /// </summary>
    public static void AppendValue(IDuckDBAppenderRow row, object? value, Type propertyType)
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
            case bool b: row.AppendValue(b); break;
            case byte by: row.AppendValue(by); break;
            case sbyte sb: row.AppendValue(sb); break;
            case short s: row.AppendValue(s); break;
            case ushort us: row.AppendValue(us); break;
            case int i: row.AppendValue(i); break;
            case uint ui: row.AppendValue(ui); break;
            case long l: row.AppendValue(l); break;
            case ulong ul: row.AppendValue(ul); break;
            case float f: row.AppendValue(f); break;
            case double d: row.AppendValue(d); break;
            case decimal dec: row.AppendValue(dec); break;
            case string str: row.AppendValue(str); break;
            case char c: row.AppendValue(c.ToString()); break;
            case DateTime dt: row.AppendValue(dt); break;
            case DateTimeOffset dto: row.AppendValue(dto); break;
            case DateOnly dateOnly: row.AppendValue((DateOnly?)dateOnly); break;
            case TimeOnly timeOnly: row.AppendValue((TimeOnly?)timeOnly); break;
            case TimeSpan ts: row.AppendValue(ts); break;
            case Guid g: row.AppendValue(g); break;
            case byte[] bytes: row.AppendValue(bytes); break;
            default: row.AppendValue(value.ToString()); break;
        }
    }

    /// <summary>
    /// Reads a column value from a DuckDB reader and converts it into the target CLR property type.
    /// </summary>
    public static object? ReadValue(DuckDBDataReader reader, int ordinal, Type propertyType)
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType.IsEnum)
        {
            var raw = reader.GetValue(ordinal);
            return Enum.ToObject(underlyingType, Convert.ToInt32(raw, CultureInfo.InvariantCulture));
        }

        if (IsComplexType(underlyingType))
        {
            var json = reader.GetString(ordinal);
            if (string.IsNullOrEmpty(json))
                return null;
            return JsonSerializer.Deserialize(json, underlyingType);
        }

        return underlyingType switch
        {
            _ when underlyingType == typeof(bool) => reader.GetBoolean(ordinal),
            _ when underlyingType == typeof(byte) => reader.GetByte(ordinal),
            _ when underlyingType == typeof(sbyte) => Convert.ToSByte(reader.GetValue(ordinal), CultureInfo.InvariantCulture),
            _ when underlyingType == typeof(short) => reader.GetInt16(ordinal),
            _ when underlyingType == typeof(ushort) => Convert.ToUInt16(reader.GetValue(ordinal), CultureInfo.InvariantCulture),
            _ when underlyingType == typeof(int) => reader.GetInt32(ordinal),
            _ when underlyingType == typeof(uint) => Convert.ToUInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture),
            _ when underlyingType == typeof(long) => reader.GetInt64(ordinal),
            _ when underlyingType == typeof(ulong) => Convert.ToUInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture),
            _ when underlyingType == typeof(float) => reader.GetFloat(ordinal),
            _ when underlyingType == typeof(double) => reader.GetDouble(ordinal),
            _ when underlyingType == typeof(decimal) => reader.GetDecimal(ordinal),
            _ when underlyingType == typeof(string) => reader.GetString(ordinal),
            _ when underlyingType == typeof(char) => reader.GetString(ordinal) is { Length: > 0 } s ? s[0] : default(char),
            _ when underlyingType == typeof(DateTime) => reader.GetDateTime(ordinal),
            _ when underlyingType == typeof(DateTimeOffset) => Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture),
            _ when underlyingType == typeof(DateOnly) => DateOnly.FromDateTime(reader.GetDateTime(ordinal)),
            _ when underlyingType == typeof(TimeOnly) => TimeOnly.FromDateTime(reader.GetDateTime(ordinal)),
            _ when underlyingType == typeof(TimeSpan) => (TimeSpan)reader.GetValue(ordinal),
            _ when underlyingType == typeof(Guid) => reader.GetGuid(ordinal),
            _ when underlyingType == typeof(byte[]) => (byte[])reader.GetValue(ordinal),
            _ => Convert.ChangeType(reader.GetValue(ordinal), underlyingType, CultureInfo.InvariantCulture)
        };
    }
}

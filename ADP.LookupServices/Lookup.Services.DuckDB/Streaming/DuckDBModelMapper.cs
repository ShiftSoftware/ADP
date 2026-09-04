using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;

/// <summary>
/// Maps DuckDB rows to a model by column name, with exactly the value semantics the storage's
/// <c>ExecuteQueryAsync</c> has always applied (strings through <c>ToString</c>, numbers through
/// <c>Convert</c>, enums from their integer, complex members from JSON text, <c>DateTimeOffset</c>
/// from the stored <c>TIMESTAMP</c>) — but with each property's setter compiled ONCE per
/// (model, result shape) instead of reflected per cell. Reading six million rows through
/// <c>PropertyInfo.SetValue</c> inside a try/catch is most of what made the per-VIN path slow.
/// </summary>
internal sealed class DuckDBModelMapper<T> where T : new()
{
    private static readonly ConcurrentDictionary<string, DuckDBModelMapper<T>> Cache =
        new ConcurrentDictionary<string, DuckDBModelMapper<T>>(StringComparer.Ordinal);

    private readonly Column[] columns;

    private sealed class Column
    {
        public int Ordinal;
        public Func<DbDataReader, int, object> Read;
        public Action<T, object> Set;
    }

    private DuckDBModelMapper(Column[] columns) => this.columns = columns;

    /// <summary>The mapper for this reader's column set (cached by the column names, in order).</summary>
    public static DuckDBModelMapper<T> For(DbDataReader reader)
    {
        var names = new string[reader.FieldCount];
        for (var i = 0; i < names.Length; i++)
            names[i] = reader.GetName(i);
        return Cache.GetOrAdd(string.Join("", names), _ => Build(names));
    }

    private static DuckDBModelMapper<T> Build(string[] names)
    {
        var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Length; i++)
        {
            if (!byName.ContainsKey(names[i]))
                byName[names[i]] = i;
        }

        var columns = new List<Column>();
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || property.GetSetMethod() is null || property.GetIndexParameters().Length > 0)
                continue;
            if (!byName.TryGetValue(property.Name, out var ordinal))
                continue;
            columns.Add(new Column { Ordinal = ordinal, Read = ReaderFor(property.PropertyType), Set = SetterFor(property) });
        }
        return new DuckDBModelMapper<T>(columns.ToArray());
    }

    public T Read(DbDataReader reader)
    {
        var model = new T();
        foreach (var column in columns)
        {
            if (reader.IsDBNull(column.Ordinal))
                continue;
            object value;
            try
            {
                value = column.Read(reader, column.Ordinal);
            }
            catch (Exception)
            {
                // The storage read every cell inside a try/catch: a timestamp .NET cannot represent, a
                // number that does not convert, leave the member at its default. Same here, counted.
                DuckDBModelMapperDiagnostics.CountUnreadableCell();
                continue;
            }
            if (value is null)
                continue;
            column.Set(model, value);
        }
        return model;
    }

    private static Action<T, object> SetterFor(PropertyInfo property)
    {
        var model = Expression.Parameter(typeof(T), "model");
        var value = Expression.Parameter(typeof(object), "value");
        var assign = Expression.Assign(
            Expression.Property(model, property),
            Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<Action<T, object>>(assign, model, value).Compile();
    }

    private static Func<DbDataReader, int, object> ReaderFor(Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(string))
            return (reader, ordinal) => reader.GetValue(ordinal)?.ToString();
        if (type == typeof(bool))
            return (reader, ordinal) => reader.GetBoolean(ordinal);
        if (type == typeof(int))
            return (reader, ordinal) => Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        if (type == typeof(long))
            return (reader, ordinal) => Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        if (type == typeof(decimal))
            return (reader, ordinal) => Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        if (type == typeof(double))
            return (reader, ordinal) => Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        if (type == typeof(float))
            return (reader, ordinal) => Convert.ToSingle(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        if (type == typeof(DateTime))
            return (reader, ordinal) => reader.GetDateTime(ordinal);
        if (type == typeof(DateTimeOffset))
        {
            return (reader, ordinal) =>
            {
                var value = reader.GetValue(ordinal);
                if (value is DateTimeOffset offset) return offset;
                // The stored value is an instant (TIMESTAMPTZ), handed back as its UTC wall-clock with
                // no kind. Read it as UTC: `new DateTimeOffset(dateTime)` stamps the machine's own
                // offset on it, shifting every claim by that offset on any host outside UTC.
                if (value is DateTime dateTime) return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
                return DateTimeOffset.Parse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            };
        }
        if (type == typeof(Guid))
        {
            return (reader, ordinal) =>
            {
                var value = reader.GetValue(ordinal);
                return value is Guid guid ? guid : Guid.Parse(value.ToString());
            };
        }
        if (type.IsEnum)
            return (reader, ordinal) => Enum.ToObject(type, Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
        if (IsComplex(type))
        {
            // The storage read these inside a per-property try/catch: an empty or malformed JSON cell
            // left the member at its default. Same here, without the exception per cell.
            return (reader, ordinal) =>
            {
                var text = reader.GetValue(ordinal)?.ToString();
                if (string.IsNullOrWhiteSpace(text))
                    return null;
                try
                {
                    return JsonSerializer.Deserialize(text, targetType);
                }
                catch (JsonException)
                {
                    return null;
                }
            };
        }
        return (reader, ordinal) => reader.GetValue(ordinal);
    }

    private static bool IsComplex(Type type) =>
        (type.IsClass && type != typeof(string) && type != typeof(byte[]))
        || type.IsInterface
        || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace != "System");
}

/// <summary>What the mappers could not read, process-wide: a data-quality number, never a failure.</summary>
public static class DuckDBModelMapperDiagnostics
{
    private static long unreadableCells;

    public static long UnreadableCells => System.Threading.Interlocked.Read(ref unreadableCells);

    internal static void CountUnreadableCell() => System.Threading.Interlocked.Increment(ref unreadableCells);
}

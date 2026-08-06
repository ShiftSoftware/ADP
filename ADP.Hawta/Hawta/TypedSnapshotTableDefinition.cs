using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Declares the exact DuckDB precision and scale for a decimal property on a typed
/// snapshot-row model. The CLR property supplies the value type; numeric constructor
/// arguments avoid exposing a free-form SQL type string at the mapping site.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SnapshotDecimalAttribute : Attribute
{
    public SnapshotDecimalAttribute(int precision, int scale)
    {
        if (precision is < 1 or > 38)
            throw new ArgumentOutOfRangeException(nameof(precision), "Decimal precision must be between 1 and 38.");
        if (scale < 0 || scale > precision)
            throw new ArgumentOutOfRangeException(nameof(scale), "Decimal scale must be between 0 and precision.");

        DuckDbType = $"DECIMAL({precision},{scale})";
    }

    internal string DuckDbType { get; }
}

/// <summary>
/// A snapshot table whose source schema and row access are anchored to a CLR model. Table
/// columns are generated from the model's writable public properties in declaration order,
/// and <see cref="Read"/> uses a cached compiled materializer (reflection happens once per
/// model, never once per row).
/// </summary>
public sealed class SnapshotTableDefinition<TRow> : SnapshotTableDefinition
    where TRow : new()
{
    private static readonly IReadOnlyList<PropertyInfo> ModelProperties = GetModelProperties();
    private static readonly IReadOnlyList<SnapshotColumn> ModelColumns =
        [.. ModelProperties.Select(property => new SnapshotColumn(property.Name, DuckDbType(property)))];
    private static readonly Func<IReadOnlyDictionary<string, object?>, TRow> Materialize = BuildMaterializer();

    public SnapshotTableDefinition(string name)
        : base(name, ModelColumns)
    {
    }

    /// <summary>Materializes one stored row as <typeparamref name="TRow"/> through a cached compiled delegate.</summary>
    public TRow Read(DirtyRow row) => Materialize(row.Values);

    /// <summary>
    /// Returns the snapshot column named by a model property. Use this for primary-key,
    /// source-modified, sort, and projection configuration instead of a string literal.
    /// </summary>
    public string Column<TValue>(Expression<Func<TRow, TValue>> property)
    {
        var member = property.Body switch
        {
            MemberExpression direct => direct,
            UnaryExpression { Operand: MemberExpression converted } => converted,
            _ => throw new ArgumentException("A direct row-model property is required.", nameof(property)),
        };

        if (member.Member is not PropertyInfo info || member.Expression != property.Parameters[0])
            throw new ArgumentException("A direct row-model property is required.", nameof(property));
        if (!ModelProperties.Contains(info))
            throw new ArgumentException($"'{info.Name}' is not a stored property on {typeof(TRow).Name}.", nameof(property));

        return info.Name;
    }

    private static IReadOnlyList<PropertyInfo> GetModelProperties()
    {
        var properties = typeof(TRow)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.SetMethod is { IsPublic: true }
                               && property.GetMethod is { IsPublic: true }
                               && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.MetadataToken)
            .ToList();

        if (properties.Count == 0)
            throw new InvalidOperationException($"Typed snapshot row {typeof(TRow).Name} has no writable public properties.");

        var duplicates = properties
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                $"Typed snapshot row {typeof(TRow).Name} has case-insensitive duplicate properties: {string.Join(", ", duplicates)}.");

        return properties;
    }

    private static string DuckDbType(PropertyInfo property)
    {
        var declared = property.GetCustomAttribute<SnapshotDecimalAttribute>()?.DuckDbType;
        if (!string.IsNullOrWhiteSpace(declared))
            return declared;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(string) || type == typeof(Guid)) return "VARCHAR";
        if (type == typeof(byte[])) return "BLOB";
        if (type == typeof(DateTime)) return "TIMESTAMP";
        if (type == typeof(DateTimeOffset)) return "TIMESTAMPTZ";
        if (type == typeof(bool)) return "BOOLEAN";
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int)) return "INTEGER";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(float)) return "FLOAT";
        if (type == typeof(double)) return "DOUBLE";
        if (type == typeof(decimal))
            throw new InvalidOperationException(
                $"Decimal property {typeof(TRow).Name}.{property.Name} must declare [SnapshotDecimal(precision, scale)].");

        throw new InvalidOperationException(
            $"No DuckDB type inference exists for {typeof(TRow).Name}.{property.Name} ({property.PropertyType.Name}).");
    }

    private static Func<IReadOnlyDictionary<string, object?>, TRow> BuildMaterializer()
    {
        var values = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "values");
        var readMethod = typeof(SnapshotTableDefinition<TRow>)
            .GetMethod(nameof(ReadValue), BindingFlags.Static | BindingFlags.NonPublic)!;

        var bindings = ModelProperties.Select(property =>
            Expression.Bind(
                property,
                Expression.Call(
                    readMethod.MakeGenericMethod(property.PropertyType),
                    values,
                    Expression.Constant(property.Name))));

        return Expression.Lambda<Func<IReadOnlyDictionary<string, object?>, TRow>>(
            Expression.MemberInit(Expression.New(typeof(TRow)), bindings),
            values).Compile();
    }

    private static TValue ReadValue<TValue>(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            throw new InvalidDataException($"Stored row is missing typed column '{name}' for {typeof(TRow).Name}.");

        if (value is null or DBNull)
        {
            if (default(TValue) is null)
                return default!;
            throw new InvalidDataException($"Stored NULL cannot populate required {typeof(TRow).Name}.{name}.");
        }

        if (value is TValue exact)
            return exact;

        var target = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        object converted = target switch
        {
            _ when target == typeof(Guid) => value is Guid guid ? guid : Guid.Parse(value.ToString()!),
            _ when target == typeof(DateTimeOffset) => value is DateTimeOffset offset
                ? offset
                : new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)),
            _ when target.IsEnum => Enum.ToObject(target, value),
            _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture),
        };

        return (TValue)converted;
    }
}

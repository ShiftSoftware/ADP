using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

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
/// Marks a persisted source property that cannot affect the mapped destination document.
/// Hawta still stores and publishes changes to the property, but excludes it from the
/// replication hash so source-only metadata cannot create Cosmos churn.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SnapshotIgnoreForReplicationAttribute : Attribute
{
}

/// <summary>
/// Marks the optional typed property that receives an explicitly requested verbatim source
/// line for audit purposes. Raw capture is off by default and this property is always excluded
/// from replication change detection.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SnapshotRawSourceAttribute : Attribute
{
}

/// <summary>
/// How a typed table stores the CLR shapes that have no single obvious DuckDB column: decimals
/// without a declared storage shape, enums, <see cref="DateTimeOffset"/>, and complex members.
///
/// <para>The default is the strict posture a source row contract wants — a decimal must say its
/// precision, an enum or a nested object is a modelling mistake — because a snapshot row that
/// gets a column it did not ask for is a silent contract change. <see cref="ForExternalModel"/>
/// is the posture for a class this package does not own — a canonical ADP model that a SERVING
/// table is typed from — where every shape must land somewhere sensible and the class cannot
/// carry Hawta's attributes.</para>
/// </summary>
public sealed class SnapshotTypedTableOptions
{
    /// <summary>Strict: decimals need <see cref="SnapshotDecimalAttribute"/>; enums, offsets and complex members are refused.</summary>
    public static SnapshotTypedTableOptions Strict { get; } = new();

    /// <summary>
    /// Lenient, for classes this package does not own: undeclared decimals store as
    /// <c>DECIMAL(18,6)</c>, enums as their underlying integer, <see cref="DateTimeOffset"/> as a
    /// UTC <c>TIMESTAMP</c> (never <c>TIMESTAMPTZ</c>, whose rendering follows the session time
    /// zone and would make the row hash depend on the machine), and every complex member —
    /// nested object, list, dictionary — as JSON text.
    /// </summary>
    public static SnapshotTypedTableOptions ForExternalModel { get; } = new()
    {
        DefaultDecimalPrecision = 18,
        DefaultDecimalScale = 6,
        EnumsAsIntegers = true,
        DateTimeOffsetAsUtcTimestamp = true,
        ComplexMembersAsJson = true,
    };

    /// <summary>Precision for a decimal property with no attribute. Null keeps the strict rule.</summary>
    public int? DefaultDecimalPrecision { get; init; }

    public int? DefaultDecimalScale { get; init; }

    /// <summary>Per-property decimal shapes, keyed by property name — the attribute for classes that cannot carry one.</summary>
    public IReadOnlyDictionary<string, (int Precision, int Scale)> DecimalOverrides { get; init; } =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal);

    public bool EnumsAsIntegers { get; init; }

    public bool DateTimeOffsetAsUtcTimestamp { get; init; }

    public bool ComplexMembersAsJson { get; init; }

    /// <summary>Property names left out of the table entirely (a computed member the model exposes with a setter, say).</summary>
    public IReadOnlyCollection<string> ExcludedProperties { get; init; } = [];

    /// <summary>
    /// <see cref="DateTime"/> properties whose stored <c>TIMESTAMP</c> materializes with
    /// <see cref="DateTimeKind.Utc"/>. DuckDB timestamps are naive, so a value reads back
    /// <see cref="DateTimeKind.Unspecified"/> by default — which is what a document built from
    /// a <c>datetime2</c> source column carries. A document the app built from a
    /// <c>datetimeoffset</c> column (<c>.UtcDateTime</c>) carries a UTC kind and serializes with a
    /// trailing <c>Z</c>; naming those properties here makes the serving row serialize the same
    /// text, so document parity does not fail on a suffix.
    /// </summary>
    public IReadOnlyCollection<string> UtcDateTimeProperties { get; init; } = [];

    /// <summary>A copy with the named <see cref="DateTime"/> properties materializing as UTC.</summary>
    public SnapshotTypedTableOptions AsUtc(params string[] propertyNames) =>
        Copy(utc: [.. UtcDateTimeProperties, .. propertyNames]);

    /// <summary>A copy with one decimal property's storage shape declared.</summary>
    public SnapshotTypedTableOptions WithDecimal(string propertyName, int precision, int scale)
    {
        _ = new SnapshotDecimalAttribute(precision, scale); // validates the shape
        var overrides = new Dictionary<string, (int, int)>(DecimalOverrides, StringComparer.Ordinal)
        {
            [propertyName] = (precision, scale),
        };
        return Copy(overrides: overrides);
    }

    /// <summary>A copy with the named properties excluded from the table.</summary>
    public SnapshotTypedTableOptions Excluding(params string[] propertyNames) =>
        Copy(excluded: [.. ExcludedProperties, .. propertyNames]);

    private SnapshotTypedTableOptions Copy(
        IReadOnlyDictionary<string, (int Precision, int Scale)>? overrides = null,
        IReadOnlyCollection<string>? excluded = null,
        IReadOnlyCollection<string>? utc = null) => new()
    {
        DefaultDecimalPrecision = DefaultDecimalPrecision,
        DefaultDecimalScale = DefaultDecimalScale,
        DecimalOverrides = overrides ?? DecimalOverrides,
        EnumsAsIntegers = EnumsAsIntegers,
        DateTimeOffsetAsUtcTimestamp = DateTimeOffsetAsUtcTimestamp,
        ComplexMembersAsJson = ComplexMembersAsJson,
        ExcludedProperties = excluded ?? ExcludedProperties,
        UtcDateTimeProperties = utc ?? UtcDateTimeProperties,
    };
}

/// <summary>
/// A snapshot table whose source schema and row access are anchored to a CLR model. Table
/// columns are generated from the model's writable public properties in declaration order,
/// and <see cref="Read"/> uses a cached compiled materializer (reflection happens once per
/// definition, never once per row).
/// </summary>
public sealed class SnapshotTableDefinition<TRow> : SnapshotTableDefinition
    where TRow : new()
{
    private static readonly IReadOnlyList<PropertyInfo> AllModelProperties = GetModelProperties();

    /// <summary>Deserialization for JSON-text columns is forgiving about key case: the SQL that writes them is hand-authored.</summary>
    internal static readonly JsonSerializerOptions JsonColumnOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyList<PropertyInfo> modelProperties;
    private readonly Func<IReadOnlyDictionary<string, object?>, TRow> materialize;

    public SnapshotTableDefinition(string name)
        : this(name, SnapshotTypedTableOptions.Strict)
    {
    }

    public SnapshotTableDefinition(string name, SnapshotTypedTableOptions options)
        : this(name, options, Shape(options))
    {
    }

    private SnapshotTableDefinition(string name, SnapshotTypedTableOptions options, TableShape shape)
        : base(name, shape.Columns, shape.ReplicationColumns, shape.RawSourceColumn)
    {
        Options = options;
        modelProperties = shape.Properties;
        materialize = BuildMaterializer(shape.Properties, shape.JsonProperties, options.UtcDateTimeProperties);
        JsonColumns = shape.JsonProperties.Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public SnapshotTypedTableOptions Options { get; }

    /// <summary>The columns stored as JSON text under <see cref="SnapshotTypedTableOptions.ComplexMembersAsJson"/>.</summary>
    public IReadOnlySet<string> JsonColumns { get; }

    /// <summary>Materializes one stored row as <typeparamref name="TRow"/> through a cached compiled delegate.</summary>
    public TRow Read(DirtyRow row) => materialize(row.Values);

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
        if (!modelProperties.Contains(info))
            throw new ArgumentException($"'{info.Name}' is not a stored property on {typeof(TRow).Name}.", nameof(property));

        return info.Name;
    }

    /// <summary>
    /// Declares an external file-header binding for a typed property. The target property is
    /// compiler-checked; only the external header remains a string at the adapter boundary.
    /// </summary>
    public FileColumnBinding Bind<TValue>(
        Expression<Func<TRow, TValue>> property,
        string sourceColumn,
        FileValueNormalization normalization = FileValueNormalization.None) =>
        new(Column(property), sourceColumn, normalization);

    private sealed record TableShape(
        IReadOnlyList<PropertyInfo> Properties,
        IReadOnlyList<SnapshotColumn> Columns,
        IReadOnlyList<SnapshotColumn> ReplicationColumns,
        string? RawSourceColumn,
        IReadOnlyList<PropertyInfo> JsonProperties);

    private static TableShape Shape(SnapshotTypedTableOptions options)
    {
        var properties = AllModelProperties
            .Where(property => !options.ExcludedProperties.Contains(property.Name, StringComparer.Ordinal))
            .ToList();
        if (properties.Count == 0)
            throw new InvalidOperationException($"Typed snapshot row {typeof(TRow).Name} has no stored properties left.");

        var jsonProperties = new List<PropertyInfo>();
        var columns = new List<SnapshotColumn>(properties.Count);
        foreach (var property in properties)
        {
            var (duckDbType, isJson) = DuckDbType(property, options);
            columns.Add(new SnapshotColumn(property.Name, duckDbType));
            if (isJson)
                jsonProperties.Add(property);
        }

        var replicationColumns = properties
            .Where(property => property.GetCustomAttribute<SnapshotIgnoreForReplicationAttribute>() is null
                               && property.GetCustomAttribute<SnapshotRawSourceAttribute>() is null)
            .Select(property => columns.Single(column => column.Name == property.Name))
            .ToList();

        return new TableShape(properties, columns, replicationColumns, GetRawSourceColumn(properties), jsonProperties);
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

    private static string? GetRawSourceColumn(IReadOnlyList<PropertyInfo> properties)
    {
        var raw = properties
            .Where(property => property.GetCustomAttribute<SnapshotRawSourceAttribute>() is not null)
            .ToList();
        if (raw.Count > 1)
            throw new InvalidOperationException(
                $"Typed snapshot row {typeof(TRow).Name} has more than one [SnapshotRawSource] property.");

        var property = raw.SingleOrDefault();
        if (property is not null && property.PropertyType != typeof(string))
            throw new InvalidOperationException(
                $"Raw source property {typeof(TRow).Name}.{property.Name} must be a nullable string.");

        return property?.Name;
    }

    private static (string DuckDbType, bool IsJson) DuckDbType(PropertyInfo property, SnapshotTypedTableOptions options)
    {
        var declared = property.GetCustomAttribute<SnapshotDecimalAttribute>()?.DuckDbType;
        if (!string.IsNullOrWhiteSpace(declared))
            return (declared, false);

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(string) || type == typeof(Guid)) return ("VARCHAR", false);
        if (type == typeof(byte[])) return ("BLOB", false);
        if (type == typeof(DateTime)) return ("TIMESTAMP", false);
        if (type == typeof(DateTimeOffset))
            return (options.DateTimeOffsetAsUtcTimestamp ? "TIMESTAMP" : "TIMESTAMPTZ", false);
        if (type == typeof(bool)) return ("BOOLEAN", false);
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int)) return ("INTEGER", false);
        if (type == typeof(long)) return ("BIGINT", false);
        if (type == typeof(float)) return ("FLOAT", false);
        if (type == typeof(double)) return ("DOUBLE", false);
        if (type == typeof(decimal))
        {
            if (options.DecimalOverrides.TryGetValue(property.Name, out var shape))
                return ($"DECIMAL({shape.Precision},{shape.Scale})", false);
            if (options is { DefaultDecimalPrecision: { } precision, DefaultDecimalScale: { } scale })
                return ($"DECIMAL({precision},{scale})", false);
            throw new InvalidOperationException(
                $"Decimal property {typeof(TRow).Name}.{property.Name} must declare [SnapshotDecimal(precision, scale)].");
        }
        if (type.IsEnum)
        {
            if (options.EnumsAsIntegers)
                return (Enum.GetUnderlyingType(type) == typeof(long) ? "BIGINT" : "INTEGER", false);
            throw new InvalidOperationException(
                $"Enum property {typeof(TRow).Name}.{property.Name} needs SnapshotTypedTableOptions.EnumsAsIntegers " +
                "or an integer property on the row model.");
        }
        if (options.ComplexMembersAsJson && IsComplex(type))
            return ("VARCHAR", true);

        throw new InvalidOperationException(
            $"No DuckDB type inference exists for {typeof(TRow).Name}.{property.Name} ({property.PropertyType.Name}).");
    }

    internal static bool IsComplex(Type type) =>
        type != typeof(string)
        && type != typeof(byte[])
        && (type.IsClass || type.IsInterface || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace != "System"));

    private static Func<IReadOnlyDictionary<string, object?>, TRow> BuildMaterializer(
        IReadOnlyList<PropertyInfo> properties, IReadOnlyList<PropertyInfo> jsonProperties,
        IReadOnlyCollection<string> utcProperties)
    {
        var unknownUtc = utcProperties.Where(name => !properties.Any(p => p.Name == name)).ToList();
        if (unknownUtc.Count > 0)
            throw new InvalidOperationException(
                $"UtcDateTimeProperties names properties {typeof(TRow).Name} does not store: {string.Join(", ", unknownUtc)}.");
        var notDates = utcProperties
            .Where(name => properties.Any(p => p.Name == name
                && (Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType) != typeof(DateTime)))
            .ToList();
        if (notDates.Count > 0)
            throw new InvalidOperationException(
                $"UtcDateTimeProperties applies to DateTime properties only; not {string.Join(", ", notDates)} on {typeof(TRow).Name}.");
        var values = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "values");
        var readMethod = typeof(SnapshotTableDefinition<TRow>)
            .GetMethod(nameof(ReadValue), BindingFlags.Static | BindingFlags.NonPublic)!;
        var readJsonMethod = typeof(SnapshotTableDefinition<TRow>)
            .GetMethod(nameof(ReadJsonValue), BindingFlags.Static | BindingFlags.NonPublic)!;

        var readUtcMethod = typeof(SnapshotTableDefinition<TRow>)
            .GetMethod(nameof(ReadUtcDateTime), BindingFlags.Static | BindingFlags.NonPublic)!;

        var bindings = properties.Select(property =>
        {
            var reader = jsonProperties.Contains(property) ? readJsonMethod
                : utcProperties.Contains(property.Name) ? readUtcMethod
                : readMethod;
            return Expression.Bind(
                property,
                Expression.Call(
                    reader.MakeGenericMethod(property.PropertyType),
                    values,
                    Expression.Constant(property.Name)));
        });

        return Expression.Lambda<Func<IReadOnlyDictionary<string, object?>, TRow>>(
            Expression.MemberInit(Expression.New(typeof(TRow)), bindings),
            values).Compile();
    }

    private static TValue ReadJsonValue<TValue>(IReadOnlyDictionary<string, object?> values, string name)
    {
        if (!values.TryGetValue(name, out var value))
            throw new InvalidDataException($"Stored row is missing typed column '{name}' for {typeof(TRow).Name}.");

        if (value is null or DBNull)
            return default!;

        var text = value as string ?? value.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return default!;

        try
        {
            return JsonSerializer.Deserialize<TValue>(text, JsonColumnOptions)!;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Stored JSON column '{name}' on {typeof(TRow).Name} does not deserialize as {typeof(TValue).Name}: {exception.Message}",
                exception);
        }
    }

    private static TValue ReadUtcDateTime<TValue>(IReadOnlyDictionary<string, object?> values, string name)
    {
        var value = ReadValue<TValue>(values, name);
        return value switch
        {
            DateTime stamp => (TValue)(object)DateTime.SpecifyKind(stamp, DateTimeKind.Utc),
            _ => value,
        };
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
            return NormalizeDecimal(exact);

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

        return NormalizeDecimal((TValue)converted);
    }

    /// <summary>
    /// A DECIMAL(18,6) column hands back <c>12.500000</c> for a stored <c>12.5</c>; the model
    /// serializes that with its trailing zeros. Strip them so a document built from a serving row
    /// renders the number the way the model would have written it in the first place.
    /// </summary>
    private static TValue NormalizeDecimal<TValue>(TValue value) => value switch
    {
        decimal number => (TValue)(object)(number / 1.000000000000000000000000000000000m),
        _ => value,
    };
}

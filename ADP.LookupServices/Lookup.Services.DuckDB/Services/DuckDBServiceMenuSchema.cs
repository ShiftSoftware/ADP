using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Service.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// The one description of how the menu documents lay out as DuckDB tables — the contract between
/// whatever populates the tables (the menu sync, to be implemented — see
/// <see cref="DuckDBServiceMenuSyncService"/>) and the lookup reader (which verifies and queries
/// them), so the two cannot disagree about a name or a column.
///
/// <para><b>One table per ItemType, named by the ItemType.</b> The four document shapes that share the
/// Cosmos partition each get their own table (<c>MenuVariant</c>, <c>MenuPeriod</c>, <c>MenuLabour</c>,
/// <c>MenuItem</c>), following the vehicle-lookup convention of one DuckDB table per model type. The
/// discriminator column itself is not stored: on the Cosmos models <c>ItemType</c> is a computed
/// get-only property, so it falls out of the writable-property column set naturally — the table IS the
/// discriminator.</para>
///
/// <para><b>Columns are the model's writable public properties, in declaration order; embedded shapes
/// are JSON.</b> Scalars map to native DuckDB types. Everything nested (embedded master copies, parts,
/// country rates) is serialized into a VARCHAR column as JSON — the same convention the vehicle DuckDB
/// tables use — and the reader's materializer deserializes it back onto the Cosmos model, so the
/// documents round-trip storage-identically and the generation aggregator downstream cannot tell which
/// backend they came from.</para>
///
/// <para>One accepted flattening: a column has no "absent vs explicit null" distinction, so a document
/// property that was explicitly null and one that was never written both store as NULL — and NULL list
/// columns materialize as the model's initialized empty list, where Cosmos would return null for an
/// explicit null. Replication always writes these collections (the models initialize them), so no
/// produced document hits the difference.</para>
/// </summary>
internal static class DuckDBServiceMenuSchema
{
    /// <summary>Table names double as the Cosmos ItemType discriminators, deliberately.</summary>
    internal static readonly IReadOnlyList<(string TableName, Type ModelType)> Tables =
    [
        (ModelTypes.MenuVariant, typeof(MenuVariantCosmosModel)),
        (ModelTypes.MenuPeriod, typeof(MenuPeriodCosmosModel)),
        (ModelTypes.MenuLabour, typeof(MenuLabourCosmosModel)),
        (ModelTypes.MenuItem, typeof(MenuItemCosmosModel)),
    ];

    /// <summary>
    /// The stored columns of a menu document model: public read/write instance properties, in
    /// declaration order (sorted by metadata token — reflection order is otherwise unspecified).
    /// Get-only properties (the ItemType discriminator) are not stored.
    /// </summary>
    internal static IReadOnlyList<PropertyInfo> GetColumns(Type modelType) =>
        modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.MetadataToken)
            .ToList();

    internal static string BuildCreateTableSql(string tableName, Type modelType)
    {
        var columns = GetColumns(modelType)
            .Select(property => $"{QuoteIdentifier(property.Name)} {DuckDBType(property.PropertyType)}");

        // id is the Cosmos document id — the source SQL row's id, unique per document type — so each
        // per-type table can key on it directly.
        return $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", columns)}, PRIMARY KEY (\"id\"))";
    }

    /// <summary>
    /// Scalar CLR type → DuckDB column type; anything non-scalar is stored as JSON in a VARCHAR.
    /// DECIMAL(38, 12) comfortably covers every menu money/quantity precision (the widest source
    /// column is DECIMAL(12, 3)) without a per-property declaration — the appender rounds anything
    /// beyond the declared scale silently, so the scale errs wide.
    /// </summary>
    internal static string DuckDBType(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (type == typeof(string)) return "VARCHAR";
        if (type == typeof(bool)) return "BOOLEAN";
        if (type == typeof(int)) return "INTEGER";
        if (type == typeof(long)) return "BIGINT";
        if (type == typeof(decimal)) return "DECIMAL(38,12)";
        if (type == typeof(double)) return "DOUBLE";
        // Offsets must be stored as their UTC instant — plain TIMESTAMP, not TIMESTAMPTZ, so what the
        // reader gets back is exactly what the writer stored.
        if (type == typeof(DateTime)) return "TIMESTAMP";
        if (type == typeof(DateTimeOffset)) return "TIMESTAMP";
        if (type.IsEnum) return "INTEGER";

        return "VARCHAR";
    }

    /// <summary>True when the property stores as JSON rather than as a native scalar column.</summary>
    internal static bool IsJsonColumn(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return !(type == typeof(string)
            || type == typeof(bool)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(decimal)
            || type == typeof(double)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type.IsEnum);
    }

    internal static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    internal static string EscapeLiteral(string value) => value?.Replace("'", "''");
}

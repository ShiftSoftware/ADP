using Parquet;
using Parquet.Schema;
using Parquet.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Lookup.Services.Services;

/// <summary>
/// The vehicle reports' parquet shape: one column per public property of the report model, enums
/// as their underlying integer, <see cref="DateTimeOffset"/> as its UTC instant. Shared by the
/// per-VIN report service and the bulk engine so a file from either reads the same.
/// </summary>
public static class VehicleReportParquet
{
    public sealed class PropertyMapping
    {
        public PropertyMapping(PropertyInfo property, Field field, Type parquetType, bool isEnum, bool isDateTimeOffset)
        {
            Property = property;
            Field = field;
            ParquetType = parquetType;
            IsEnum = isEnum;
            IsDateTimeOffset = isDateTimeOffset;
        }

        public PropertyInfo Property { get; }
        public Field Field { get; }
        public Type ParquetType { get; }
        public bool IsEnum { get; }
        public bool IsDateTimeOffset { get; }
    }

    public static (ParquetSchema Schema, List<PropertyMapping> Mappings) BuildSchema<TModel>()
    {
        var propertyMappings = new List<PropertyMapping>();

        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
        {
            if (!TryGetParquetType(property.PropertyType, out var parquetType, out var isNullable, out var isEnum, out var isDateTimeOffset))
                continue;

            var fieldType = typeof(DataField<>).MakeGenericType(parquetType);
            var field = (Field)Activator.CreateInstance(fieldType, property.Name, (bool?)isNullable);
            propertyMappings.Add(new PropertyMapping(property, field, parquetType, isEnum, isDateTimeOffset));
        }

        var schema = new ParquetSchema(propertyMappings.Select(x => x.Field).ToArray());
        return (schema, propertyMappings);
    }

    public static List<IDictionary<string, object>> ToRecords<TModel>(IReadOnlyList<TModel> rows, List<PropertyMapping> propertyMappings)
    {
        var records = new List<IDictionary<string, object>>(rows.Count);

        foreach (var row in rows)
        {
            var record = new Dictionary<string, object>(propertyMappings.Count, StringComparer.Ordinal);

            foreach (var mapping in propertyMappings)
            {
                var value = mapping.Property.GetValue(row);
                if (value is not null && mapping.IsEnum)
                    value = Convert.ChangeType(value, mapping.ParquetType, CultureInfo.InvariantCulture);
                else if (value is not null && mapping.IsDateTimeOffset)
                    value = ((DateTimeOffset)value).UtcDateTime;

                record[mapping.Property.Name] = value;
            }

            records.Add(record);
        }

        return records;
    }

    public static bool TryGetParquetType(Type propertyType, out Type parquetType, out bool isNullable, out bool isEnum, out bool isDateTimeOffset)
    {
        parquetType = propertyType;
        isNullable = false;
        isEnum = false;
        isDateTimeOffset = false;

        var underlying = Nullable.GetUnderlyingType(propertyType);
        if (underlying is not null)
        {
            propertyType = underlying;
            isNullable = true;
        }
        else if (!propertyType.IsValueType)
        {
            isNullable = true;
        }

        if (propertyType.IsEnum)
        {
            parquetType = Enum.GetUnderlyingType(propertyType);
            isEnum = true;
            return true;
        }

        if (propertyType == typeof(DateTimeOffset))
        {
            parquetType = typeof(DateTime);
            isDateTimeOffset = true;
            return true;
        }

        if (propertyType == typeof(string) ||
            propertyType == typeof(bool) ||
            propertyType == typeof(byte) ||
            propertyType == typeof(sbyte) ||
            propertyType == typeof(short) ||
            propertyType == typeof(ushort) ||
            propertyType == typeof(int) ||
            propertyType == typeof(uint) ||
            propertyType == typeof(long) ||
            propertyType == typeof(ulong) ||
            propertyType == typeof(float) ||
            propertyType == typeof(double) ||
            propertyType == typeof(decimal) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(DateTimeOffset) ||
            propertyType == typeof(Guid) ||
            propertyType == typeof(byte[]))
        {
            parquetType = propertyType;
            return true;
        }

        return false;
    }
}

/// <summary>
/// One report file being written in batches: every <see cref="AppendAsync"/> adds a row group in
/// the order it is called, so rows land in the file in the order the caller produced them. The
/// file exists once the first non-empty batch is appended; <see cref="CompleteAsync"/> makes sure
/// an empty report still leaves a readable file with the report's columns.
/// </summary>
public sealed class ParquetReportFile<TModel>
{
    private readonly string fileFullPath;
    private readonly ParquetSchema schema;
    private readonly List<VehicleReportParquet.PropertyMapping> mappings;
    private bool firstChunk = true;

    public ParquetReportFile(string fileFullPath)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            throw new ArgumentException("Parquet output file path is required.", nameof(fileFullPath));

        this.fileFullPath = fileFullPath;
        var outputDirectory = Path.GetDirectoryName(fileFullPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        (schema, mappings) = VehicleReportParquet.BuildSchema<TModel>();
    }

    public string FileFullPath => fileFullPath;
    public long RowCount { get; private set; }

    public async Task AppendAsync(IReadOnlyList<TModel> rows)
    {
        if (rows is null || rows.Count == 0)
            return;

        var records = VehicleReportParquet.ToRecords(rows, mappings);
        using var fileStream = firstChunk
            ? File.Create(fileFullPath)
            : new FileStream(fileFullPath, FileMode.Open, FileAccess.ReadWrite);
        await ParquetSerializer.SerializeUntypedAsync(records, schema, fileStream, new ParquetOptions { Append = !firstChunk });
        firstChunk = false;
        RowCount += rows.Count;
    }

    /// <summary>Writes the file with its columns and no rows if nothing was ever appended.</summary>
    public async Task CompleteAsync()
    {
        if (!firstChunk)
            return;

        using var fileStream = File.Create(fileFullPath);
        await ParquetSerializer.SerializeUntypedAsync(new List<IDictionary<string, object>>(), schema, fileStream, new ParquetOptions());
        firstChunk = false;
    }
}

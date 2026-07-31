using System.Data;
using System.Diagnostics;

using Microsoft.Data.SqlClient;

using ShiftSoftware.ADP.Menus.Sample.Web.DTOs.DevTools;

namespace ShiftSoftware.ADP.Menus.Sample.API.DevTools;

/// <summary>
/// DEVELOPMENT-ONLY. Replaces the contents of one SQL schema in the sample database with the contents
/// of the same schema in another database, preserving primary keys.
///
/// This is a throwaway developer convenience for loading a realistic dataset into a local sample
/// database. It is NOT a migration tool, it has no rollback, and it must never be pointed at anything
/// but a local sample target. It is gated to <c>IsDevelopment()</c> at the controller.
///
/// The table list, column list, foreign keys and temporal status are all DISCOVERED AT RUNTIME from
/// the SQL catalog rather than hard-coded, so the copy keeps working when the schema changes and
/// silently skips anything the two databases do not have in common — which is the normal case when a
/// source database was provisioned from an older model.
/// </summary>
public class SchemaCopyService
{
    private readonly ILogger<SchemaCopyService> logger;

    public SchemaCopyService(ILogger<SchemaCopyService> logger)
    {
        this.logger = logger;
    }

    /// <param name="sourceConnectionString">Database to read from. Never written to.</param>
    /// <param name="targetConnectionString">The sample database. Rows in <paramref name="schema"/> are DELETED.</param>
    /// <param name="schema">SQL schema to replace, e.g. "Menu".</param>
    /// <param name="preserve">
    /// Optional per-table rule for rows that must survive in the target — used to keep the seeded
    /// superuser intact. See <see cref="PreserveRule"/> for how it is applied to each side.
    /// </param>
    public async Task<DevDataImportResultDTO> CopySchemaAsync(
        string sourceConnectionString,
        string targetConnectionString,
        string schema,
        IReadOnlyDictionary<string, PreserveRule>? preserve = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DevDataImportResultDTO();
        preserve ??= new Dictionary<string, PreserveRule>(StringComparer.OrdinalIgnoreCase);

        await using var source = new SqlConnection(sourceConnectionString);
        await using var target = new SqlConnection(targetConnectionString);
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        var sourceTables = await GetTablesAsync(source, schema, cancellationToken);
        var targetTables = await GetTablesAsync(target, schema, cancellationToken);

        if (sourceTables.Count == 0)
            throw new InvalidOperationException($"The source database has no tables in schema [{schema}].");

        if (targetTables.Count == 0)
            throw new InvalidOperationException($"The sample database has no tables in schema [{schema}]. Start the API once so it creates the schema, then retry.");

        // Only tables BOTH databases have. A table missing on either side is reported, never guessed at.
        var copyTables = targetTables.Keys.Intersect(sourceTables.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        foreach (var missing in targetTables.Keys.Except(sourceTables.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            result.Warnings.Add($"[{schema}].[{missing}] exists here but not in the source — left empty.");

        foreach (var extra in sourceTables.Keys.Except(targetTables.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            result.Warnings.Add($"[{schema}].[{extra}] exists in the source but not here — skipped.");

        // Foreign keys have to be relaxed in two directions for the duration:
        //  • tables that point AT this schema (from any schema) — otherwise deleting parents fails;
        //  • the copied tables themselves — otherwise inserting a row whose parent has not been
        //    written yet fails, and a table whose only foreign keys leave the schema would be missed.
        var referencingTables = await GetReferencingTablesAsync(target, schema, cancellationToken);
        referencingTables = referencingTables
            .Union(copyTables.Select(t => (Schema: schema, Table: t)))
            .Distinct()
            .ToList();
        var temporalTables = copyTables.Where(t => targetTables[t].IsSystemVersioned).ToList();

        try
        {
            // System versioning must come off before the base table can be emptied or written to:
            // its period columns are GENERATED ALWAYS and reject explicit values.
            foreach (var table in temporalTables)
                await ExecuteAsync(target, $"ALTER TABLE [{schema}].[{table}] SET (SYSTEM_VERSIONING = OFF);", cancellationToken);

            foreach (var (refSchema, refTable) in referencingTables)
                await ExecuteAsync(target, $"ALTER TABLE [{refSchema}].[{refTable}] NOCHECK CONSTRAINT ALL;", cancellationToken);

            // ---- empty the target ---------------------------------------------------------------
            // Constraints are relaxed, so delete order does not matter.
            var deleted = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var preservedKeys = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in copyTables)
            {
                var predicate = preserve.TryGetValue(table, out var rule) ? $" WHERE NOT ({rule.FlagColumn} = 1)" : string.Empty;
                deleted[table] = await ExecuteAsync(target, $"DELETE FROM [{schema}].[{table}]{predicate};", cancellationToken);

                // Remember exactly which rows survived, so the source cannot re-introduce their keys.
                if (rule is not null)
                    preservedKeys[table] = await GetPreservedKeysAsync(target, schema, table, rule, cancellationToken);
            }

            // ---- copy ----------------------------------------------------------------------------
            foreach (var table in copyTables)
            {
                var sourceColumns = await GetColumnsAsync(source, schema, table, cancellationToken);
                var targetColumns = await GetColumnsAsync(target, schema, table, cancellationToken);
                var sourceByName = sourceColumns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

                var tableResult = new DevDataImportTableResultDTO
                {
                    Table = table,
                    RowsDeleted = deleted.TryGetValue(table, out var d) ? d : 0,
                };

                // Build the projection column by column against the TARGET's shape, so a source that
                // predates a column (or allows NULL where the target no longer does) still loads.
                var projections = new List<string>();
                var mappings = new List<string>();
                var substituted = new List<string>();
                var unsupported = new List<string>();

                foreach (var targetColumn in targetColumns)
                {
                    if (sourceByName.TryGetValue(targetColumn.Name, out var sourceColumn))
                    {
                        // Present in both. Guard the one case that fails at insert time: the source
                        // permits NULL but the target no longer does.
                        if (!targetColumn.IsNullable && sourceColumn.IsNullable)
                        {
                            var literal = ZeroLiteral(targetColumn.TypeName);
                            if (literal is null)
                            {
                                unsupported.Add($"{targetColumn.Name} ({targetColumn.TypeName})");
                                continue;
                            }

                            projections.Add($"ISNULL([{targetColumn.Name}], {literal}) AS [{targetColumn.Name}]");
                            substituted.Add($"{targetColumn.Name} (NULLs)");
                        }
                        else
                        {
                            projections.Add($"[{targetColumn.Name}]");
                        }

                        mappings.Add(targetColumn.Name);
                        continue;
                    }

                    // Absent from the source. A nullable column or one with a DEFAULT can simply be
                    // left out — SQL Server fills it. A NOT NULL column with no default cannot, so
                    // supply a zero value rather than failing the whole copy.
                    if (targetColumn.IsNullable || targetColumn.HasDefault)
                        continue;

                    var fallback = ZeroLiteral(targetColumn.TypeName);
                    if (fallback is null)
                    {
                        unsupported.Add($"{targetColumn.Name} ({targetColumn.TypeName})");
                        continue;
                    }

                    projections.Add($"{fallback} AS [{targetColumn.Name}]");
                    mappings.Add(targetColumn.Name);
                    substituted.Add(targetColumn.Name);
                }

                if (unsupported.Count > 0)
                    throw new InvalidOperationException(
                        $"[{schema}].[{table}] cannot be copied: {string.Join(", ", unsupported)} is NOT NULL here but unavailable from the source, and no default value can be inferred for that type.");

                if (mappings.Count == 0)
                {
                    tableResult.Note = "no columns in common — skipped";
                    result.Warnings.Add($"[{schema}].[{table}] has no columns in common with the source — skipped.");
                    result.Tables.Add(tableResult);
                    continue;
                }

                if (substituted.Count > 0)
                    result.Warnings.Add($"[{schema}].[{table}]: the source cannot supply {string.Join(", ", substituted)} — filled with a zero value.");

                var ignored = sourceColumns.Select(c => c.Name)
                    .Except(targetColumns.Select(c => c.Name), StringComparer.OrdinalIgnoreCase).ToList();
                if (ignored.Count > 0)
                    result.Warnings.Add($"[{schema}].[{table}]: the source's {string.Join(", ", ignored)} {(ignored.Count == 1 ? "has" : "have")} no counterpart here — ignored.");

                // The preserve rule describes the TARGET. Only the parts of it the SOURCE can actually
                // evaluate may appear in this query — an older source may not have the flag column at
                // all, which is what "Invalid column name" means here.
                var conditions = new List<string>();
                if (preserve.TryGetValue(table, out var preserveRule))
                {
                    if (sourceByName.ContainsKey(preserveRule.FlagColumn))
                    {
                        conditions.Add($"NOT ([{preserveRule.FlagColumn}] = 1)");
                    }
                    else if (preserveRule.LegacyFlagColumn is not null && sourceByName.ContainsKey(preserveRule.LegacyFlagColumn))
                    {
                        conditions.Add($"NOT ([{preserveRule.LegacyFlagColumn}] = 1)");
                        result.Warnings.Add($"[{schema}].[{table}]: the source has no {preserveRule.FlagColumn}; used its {preserveRule.LegacyFlagColumn} instead to identify rows to skip.");
                    }
                    else
                    {
                        result.Warnings.Add($"[{schema}].[{table}]: the source has neither {preserveRule.FlagColumn} nor a known equivalent, so its own protected rows could not be identified — only key collisions were avoided.");
                    }

                    // Belt and braces: never re-introduce a key that survived in the target, whatever
                    // the source calls its flag column. This is what actually prevents a PK violation.
                    if (preservedKeys.TryGetValue(table, out var keys) && keys.Count > 0 && keys.Count <= 1000
                        && sourceByName.ContainsKey(preserveRule.KeyColumn))
                    {
                        conditions.Add($"[{preserveRule.KeyColumn}] NOT IN ({string.Join(", ", keys)})");
                    }
                }

                var filter = conditions.Count > 0 ? $" WHERE {string.Join(" AND ", conditions)}" : string.Empty;

                await using var read = new SqlCommand(
                    $"SELECT {string.Join(", ", projections)} FROM [{schema}].[{table}]{filter};", source)
                {
                    CommandTimeout = 0,
                };
                await using var reader = await read.ExecuteReaderAsync(cancellationToken);

                // KeepIdentity is essential: every table keys on an IDENTITY column and every foreign
                // key points at it, so letting the target re-number rows would silently corrupt them.
                // KeepNulls is deliberately NOT set — it would write NULL into columns the projection
                // omits, defeating the DEFAULT constraints relied on above.
                using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity, externalTransaction: null)
                {
                    DestinationTableName = $"[{schema}].[{table}]",
                    BulkCopyTimeout = 0,
                    BatchSize = 5000,
                };

                foreach (var column in mappings)
                    bulk.ColumnMappings.Add(column, column);

                await bulk.WriteToServerAsync(reader, cancellationToken);

                tableResult.RowsCopied = GetRowsCopied(bulk);
                if (filter.Length > 0)
                    tableResult.Note = "preserved rows kept";

                result.Tables.Add(tableResult);
            }

            // ---- put the target back the way it was ----------------------------------------------
            foreach (var table in temporalTables)
            {
                var meta = targetTables[table];
                await ExecuteAsync(
                    target,
                    $"ALTER TABLE [{schema}].[{table}] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [{meta.HistorySchema}].[{meta.HistoryTable}], DATA_CONSISTENCY_CHECK = ON));",
                    cancellationToken);
            }

            foreach (var (refSchema, refTable) in referencingTables)
            {
                try
                {
                    await ExecuteAsync(target, $"ALTER TABLE [{refSchema}].[{refTable}] WITH CHECK CHECK CONSTRAINT ALL;", cancellationToken);
                }
                catch (SqlException ex)
                {
                    // Re-enable untrusted rather than leaving the constraint off entirely, and say so.
                    // Expected when preserved rows (the superuser) reference rows the source did not supply.
                    await ExecuteAsync(target, $"ALTER TABLE [{refSchema}].[{refTable}] WITH NOCHECK CHECK CONSTRAINT ALL;", cancellationToken);
                    result.Warnings.Add($"[{refSchema}].[{refTable}]: foreign keys re-enabled WITHOUT validation — some rows reference missing parents ({ex.Message.Split('.')[0]}).");
                }
            }

            // Realign each IDENTITY seed with the copied data, or the next insert collides.
            foreach (var table in copyTables)
                await ExecuteAsync(target, $"IF OBJECTPROPERTY(OBJECT_ID('[{schema}].[{table}]'), 'TableHasIdentity') = 1 DBCC CHECKIDENT('[{schema}].[{table}]', RESEED) WITH NO_INFOMSGS;", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema copy of [{Schema}] failed", schema);
            result.Succeeded = false;
            result.Message = $"Copy of [{schema}] failed: {ex.Message}";
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.Warnings.Add("The sample database may be half-populated. Re-run the copy once the cause is fixed.");
            return result;
        }

        stopwatch.Stop();
        result.Succeeded = true;
        result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        result.Message = $"Copied {result.TotalRowsCopied:N0} rows into {result.Tables.Count} [{schema}] tables.";
        return result;
    }

    /// <summary>
    /// Rows that must survive a copy, described in terms of the TARGET schema.
    ///
    /// The two sides are treated differently on purpose: the target is known to be current, but the
    /// source may predate the column entirely — so the flag is applied to the source only when it
    /// exists there (falling back to <see cref="LegacyFlagColumn"/>), while the key exclusion always
    /// applies and is what actually guarantees no primary-key collision.
    /// </summary>
    /// <param name="FlagColumn">Bit column marking preserved rows in the target, e.g. IsProtected.</param>
    /// <param name="KeyColumn">Primary key column used to exclude preserved keys from the source, e.g. ID.</param>
    /// <param name="LegacyFlagColumn">Older name for the same concept, tried when the source lacks <paramref name="FlagColumn"/>.</param>
    public sealed record PreserveRule(string FlagColumn, string KeyColumn, string? LegacyFlagColumn = null);

    private sealed record TableInfo(bool IsSystemVersioned, string? HistorySchema, string? HistoryTable);

    /// <summary>Keys of the rows that survived the delete — the set the source must not re-introduce.</summary>
    private static async Task<List<long>> GetPreservedKeysAsync(
        SqlConnection connection, string schema, string table, PreserveRule rule, CancellationToken cancellationToken)
    {
        var keys = new List<long>();

        await using var command = new SqlCommand(
            $"SELECT [{rule.KeyColumn}] FROM [{schema}].[{table}] WHERE [{rule.FlagColumn}] = 1;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                keys.Add(Convert.ToInt64(reader.GetValue(0)));
        }

        return keys;
    }

    /// <summary>Base tables in the schema. History tables (temporal_type 1) are managed by SQL Server and excluded.</summary>
    private static async Task<Dictionary<string, TableInfo>> GetTablesAsync(SqlConnection connection, string schema, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT t.name AS TableName,
       t.temporal_type AS TemporalType,
       hs.name AS HistorySchema,
       h.name AS HistoryTable
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.tables h ON h.object_id = t.history_table_id
LEFT JOIN sys.schemas hs ON hs.schema_id = h.schema_id
WHERE s.name = @schema AND t.is_ms_shipped = 0 AND t.temporal_type <> 1;";

        var tables = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            tables[reader.GetString(0)] = new TableInfo(
                IsSystemVersioned: reader.GetByte(1) == 2,
                HistorySchema: reader.IsDBNull(2) ? null : reader.GetString(2),
                HistoryTable: reader.IsDBNull(3) ? null : reader.GetString(3));
        }

        return tables;
    }

    private sealed record ColumnInfo(string Name, bool IsNullable, bool HasDefault, string TypeName);

    /// <summary>Columns that can actually be written: not computed, not GENERATED ALWAYS (temporal period columns).</summary>
    private static async Task<List<ColumnInfo>> GetColumnsAsync(SqlConnection connection, string schema, string table, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT c.name,
       c.is_nullable,
       CASE WHEN c.default_object_id <> 0 THEN 1 ELSE 0 END AS HasDefault,
       ty.name AS TypeName
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE s.name = @schema AND t.name = @table AND c.is_computed = 0 AND c.generated_always_type = 0
ORDER BY c.column_id;";

        var columns = new List<ColumnInfo>();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schema", schema);
        command.Parameters.AddWithValue("@table", table);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            columns.Add(new ColumnInfo(reader.GetString(0), reader.GetBoolean(1), reader.GetInt32(2) == 1, reader.GetString(3)));

        return columns;
    }

    /// <summary>
    /// A neutral value for a NOT NULL column the source cannot supply — the schema-drift case, e.g. a
    /// flag column added after the source database was provisioned. Returns null for types where no
    /// sensible zero exists, so the copy reports the incompatibility instead of inventing data.
    /// </summary>
    private static string? ZeroLiteral(string sqlTypeName) => sqlTypeName.ToLowerInvariant() switch
    {
        "bit" => "CAST(0 AS bit)",
        "tinyint" or "smallint" or "int" or "bigint" => "0",
        "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "0",
        "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => "''",
        "date" or "datetime" or "datetime2" or "smalldatetime" => "SYSUTCDATETIME()",
        "datetimeoffset" => "SYSDATETIMEOFFSET()",
        "time" => "CAST(SYSUTCDATETIME() AS time)",
        "uniqueidentifier" => "CAST(0x AS uniqueidentifier)",
        "binary" or "varbinary" or "image" => "0x",
        _ => null,
    };

    /// <summary>Every table with a foreign key INTO this schema — including from other schemas.</summary>
    private static async Task<List<(string Schema, string Table)>> GetReferencingTablesAsync(SqlConnection connection, string schema, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT DISTINCT rs.name AS ReferencingSchema, rt.name AS ReferencingTable
FROM sys.foreign_keys fk
INNER JOIN sys.tables rt ON rt.object_id = fk.parent_object_id
INNER JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
INNER JOIN sys.tables pt ON pt.object_id = fk.referenced_object_id
INNER JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
WHERE ps.name = @schema;";

        var tables = new List<(string, string)>();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            tables.Add((reader.GetString(0), reader.GetString(1)));

        return tables;
    }

    private static async Task<int> ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// SqlBulkCopy only exposes its row count through a private field; RowsCopied64 is public from
    /// .NET 8 onward, so prefer it and fall back to 0 rather than reflecting.
    /// </summary>
    private static int GetRowsCopied(SqlBulkCopy bulk) => (int)Math.Min(bulk.RowsCopied64, int.MaxValue);
}

using Microsoft.Data.SqlClient;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Reads metrics from SQL Server, read-only. Scalar measures must select a <c>v</c> column; grouped
/// measures must select <c>k</c> and <c>v</c>.
/// <para>
/// This is the source for <b>flow / backlog / funnel</b> checks whose data lives in a relational store
/// (e.g. the Ticket System's temporal tables). There are no dedicated funnel/backlog assert types: the
/// SLA window, the join, and the "stuck"/"missing"/"unmatched" predicate all live in the measure's
/// <c>WHERE</c>, and the resulting scalar/grouped number is asserted with the existing
/// <c>threshold</c> / <c>diff</c> / <c>age</c> asserts. Ratios (delivery rate, etc.) are computed in the
/// SQL itself (<c>CAST(x AS float) / NULLIF(y, 0)</c>) and asserted as a <c>threshold</c>.
/// </para>
/// <para>
/// For <c>valueKind: timestamp</c> (freshness/age) the selected value is treated as <b>UTC</b>. A
/// <c>datetimeoffset</c> column is converted to UTC as-is; a naive <c>datetime2</c> is assumed already
/// UTC, so normalize local columns in the measure SQL (e.g.
/// <c>... AT TIME ZONE 'Arabian Standard Time' AT TIME ZONE 'UTC'</c>).
/// </para>
/// <para>
/// A null/blank connection string registers a no-op source: sql measures report a source error but the
/// run still completes (mirrors <see cref="CosmosCheckSource"/>'s null-client behaviour), so a host that
/// is missing the read-only connection string does not abort every other check in the pack.
/// </para>
/// </summary>
public sealed class SqlCheckSource : ICheckSource, ICheckSourceCatalog
{
    private readonly string? connectionString;
    private readonly int commandTimeoutSeconds;

    /// <summary>Guards a single slow/blocked query from stalling the whole run.</summary>
    public const int DefaultCommandTimeoutSeconds = 60;

    /// <summary>
    /// Creates the original, unqualified <c>sql</c> source. This constructor is retained
    /// explicitly for binary compatibility with existing Rastgo hosts.
    /// </summary>
    public SqlCheckSource(string? connectionString)
        : this(connectionString, qualifier: null, DefaultCommandTimeoutSeconds)
    {
    }

    /// <summary>
    /// Creates a SQL source, optionally addressed as <c>sql:&lt;qualifier&gt;</c>. Raising
    /// <paramref name="commandTimeoutSeconds"/> is useful for measures that wrap wide views.
    /// </summary>
    public SqlCheckSource(
        string? connectionString,
        string? qualifier,
        int commandTimeoutSeconds = DefaultCommandTimeoutSeconds)
    {
        if (commandTimeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(commandTimeoutSeconds),
                "SQL command timeout must be positive.");

        this.connectionString = connectionString;
        this.commandTimeoutSeconds = commandTimeoutSeconds;
        Name = SourceName.Compose("sql", qualifier);
    }

    public string Name { get; }

    public async Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new MeasureOutcome { Error = "SQL is not configured (read-only connection string missing)." };

        if (string.IsNullOrWhiteSpace(spec.Sql))
            return new MeasureOutcome { Error = "sql measure requires 'sql'." };

        try
        {
            var cells = new Dictionary<string, MeasureCell>();
            var isTimestamp = string.Equals(spec.ValueKind, "timestamp", StringComparison.OrdinalIgnoreCase);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = spec.Sql;
            cmd.CommandTimeout = commandTimeoutSeconds;
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var vOrd = reader.GetOrdinal("v");
            int? kOrd = grouped ? reader.GetOrdinal("k") : null;

            while (await reader.ReadAsync(ct))
            {
                var key = kOrd is { } ko && !await reader.IsDBNullAsync(ko, ct)
                    ? reader.GetValue(ko)?.ToString() ?? MeasureOutcome.ScalarKey
                    : MeasureOutcome.ScalarKey;

                MeasureCell cell;
                if (await reader.IsDBNullAsync(vOrd, ct))
                    cell = new MeasureCell(null, null);
                else if (isTimestamp)
                    cell = new MeasureCell(null, ToUtc(reader.GetValue(vOrd)));
                else
                    cell = new MeasureCell(Convert.ToDouble(reader.GetValue(vOrd)), null);

                cells[key] = cell;
            }

            return new MeasureOutcome { Cells = cells, AsOfUtc = DateTimeOffset.UtcNow };
        }
        catch (Exception ex)
        {
            return new MeasureOutcome { Error = ex.Message };
        }
    }

    public async Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return new(Name, "SQL Server", [], DateTimeOffset.UtcNow, Error: "SQL metadata is unavailable because this source is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = commandTimeoutSeconds;
        command.CommandText = """
            WITH objects AS
            (
                SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE,
                       ROW_NUMBER() OVER (ORDER BY TABLE_SCHEMA, TABLE_NAME) AS dataset_rank
                FROM INFORMATION_SCHEMA.TABLES
            ), fields AS
            (
                SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, ORDINAL_POSITION,
                       ROW_NUMBER() OVER (PARTITION BY TABLE_SCHEMA, TABLE_NAME ORDER BY ORDINAL_POSITION) AS field_rank
                FROM INFORMATION_SCHEMA.COLUMNS
            )
            SELECT o.TABLE_SCHEMA, o.TABLE_NAME, o.TABLE_TYPE,
                   f.COLUMN_NAME, f.DATA_TYPE, f.IS_NULLABLE
            FROM objects o
            LEFT JOIN fields f ON f.TABLE_SCHEMA = o.TABLE_SCHEMA AND f.TABLE_NAME = o.TABLE_NAME
                              AND f.field_rank <= @maxFields
            WHERE o.dataset_rank <= @maxDatasets
            ORDER BY o.dataset_rank, f.field_rank
            """;
        command.Parameters.AddWithValue("@maxDatasets", request.MaxDatasets);
        command.Parameters.AddWithValue("@maxFields", request.MaxFieldsPerDataset);

        var datasets = new List<CatalogDataset>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            var current = datasets.LastOrDefault();
            if (current is null || current.Name != name || current.Namespace != schema)
            {
                current = new(name, reader.GetString(2).Replace(' ', '_').ToLowerInvariant(), schema, new List<CatalogField>());
                datasets.Add(current);
            }

            if (!await reader.IsDBNullAsync(3, ct))
            {
                var list = (List<CatalogField>)current.Fields;
                list.Add(new(reader.GetString(3), reader.GetString(4),
                    string.Equals(reader.GetString(5), "YES", StringComparison.OrdinalIgnoreCase)));
            }
        }

        return new(
            Name,
            "SQL Server",
            datasets,
            DateTimeOffset.UtcNow,
            Truncated: datasets.Count >= request.MaxDatasets || datasets.Any(d => d.Fields.Count >= request.MaxFieldsPerDataset),
            Note: "Schemas, tables/views, and columns only. Row counts and record values are not queried.");
    }

    /// <summary>
    /// Normalizes a SQL date/time value to UTC. <c>datetimeoffset</c> arrives as a <see cref="DateTimeOffset"/>
    /// (converted as-is); a naive <c>datetime2</c>/<c>datetime</c> is assumed already UTC. Anything else is
    /// coerced through <see cref="Convert.ToDateTime(object)"/> and stamped UTC.
    /// </summary>
    private static DateTimeOffset ToUtc(object raw) => raw switch
    {
        DateTimeOffset o => o.ToUniversalTime(),
        DateTime d => new DateTimeOffset(DateTime.SpecifyKind(d, DateTimeKind.Utc)),
        _ => new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(raw), DateTimeKind.Utc)),
    };
}

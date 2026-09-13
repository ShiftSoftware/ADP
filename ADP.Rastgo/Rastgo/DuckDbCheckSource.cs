using DuckDB.NET.Data;

namespace ShiftSoftware.ADP.Rastgo;

/// <summary>
/// Reads metrics from a DuckDB database (typically the published read snapshot). Scalar measures
/// must select a <c>v</c> column; grouped measures must select <c>k</c> and <c>v</c>.
/// <para>
/// For <c>valueKind: timestamp</c> (freshness) the selected value is treated as <b>UTC</b>. If a
/// source column stores local/naive time, normalize it in the measure SQL — e.g.
/// <c>MAX(InvoiceDate) AT TIME ZONE 'Asia/Baghdad' AT TIME ZONE 'UTC'</c>.
/// </para>
/// <para>
/// A fresh connection is opened per measure and disposed right after, with the connection string
/// resolved through a factory each time. Two reasons: hosts that publish versioned snapshots (a
/// new file per publish, resolved latest-by-name) always measure the newest one, and no long-lived
/// handle pins a snapshot file open on an SMB share — a held-open target can turn a fixed-name
/// snapshot replacement into a delete. Measures within one run may
/// therefore span a publish boundary; for health checks that read-consistency trade is fine.
/// </para>
/// </summary>
public sealed class DuckDbCheckSource : ICheckSource, ICheckSourceCatalog
{
    private readonly Func<string> connectionStringFactory;

    public string Name => "duckdb";

    /// <summary>Fixed connection string — a single static database file.</summary>
    public DuckDbCheckSource(string connectionString)
        : this(() => connectionString)
    {
    }

    /// <summary>
    /// Connection string resolved per measure — for hosts whose database path changes between
    /// runs (e.g. versioned read snapshots resolved latest-by-name). A factory failure (no
    /// snapshot published yet) surfaces as that measure's source error; the run still completes.
    /// </summary>
    public DuckDbCheckSource(Func<string> connectionStringFactory)
        => this.connectionStringFactory = connectionStringFactory;

    public Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(spec.Sql))
            return Task.FromResult(new MeasureOutcome { Error = "duckdb measure requires 'sql'." });

        try
        {
            var cells = new Dictionary<string, MeasureCell>();
            var isTimestamp = string.Equals(spec.ValueKind, "timestamp", StringComparison.OrdinalIgnoreCase);

            using var connection = new DuckDBConnection(connectionStringFactory());
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = spec.Sql;
            using var reader = cmd.ExecuteReader();

            var vOrd = reader.GetOrdinal("v");
            int? kOrd = grouped ? reader.GetOrdinal("k") : null;

            while (reader.Read())
            {
                var key = kOrd is { } ko && !reader.IsDBNull(ko)
                    ? reader.GetValue(ko)?.ToString() ?? MeasureOutcome.ScalarKey
                    : MeasureOutcome.ScalarKey;

                MeasureCell cell;
                if (reader.IsDBNull(vOrd))
                    cell = new MeasureCell(null, null);
                else if (isTimestamp)
                    cell = new MeasureCell(null, new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(reader.GetValue(vOrd)), DateTimeKind.Utc)));
                else
                    cell = new MeasureCell(Convert.ToDouble(reader.GetValue(vOrd)), null);

                cells[key] = cell;
            }

            return Task.FromResult(new MeasureOutcome { Cells = cells, AsOfUtc = DateTimeOffset.UtcNow });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new MeasureOutcome { Error = ex.Message });
        }
    }

    public Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var connection = new DuckDBConnection(connectionStringFactory());
        connection.Open();

        var datasets = new List<CatalogDataset>();
        using (var tables = connection.CreateCommand())
        {
            tables.CommandText = $"""
                SELECT table_schema, table_name, table_type
                FROM information_schema.tables
                WHERE table_schema NOT IN ('information_schema', 'pg_catalog')
                ORDER BY table_schema, table_name
                LIMIT {request.MaxDatasets}
                """;
            using var tableReader = tables.ExecuteReader();
            while (tableReader.Read())
            {
                ct.ThrowIfCancellationRequested();
                datasets.Add(new(
                    tableReader.GetString(1),
                    tableReader.GetString(2).Replace(' ', '_').ToLowerInvariant(),
                    tableReader.GetString(0),
                    []));
            }
        }

        var fields = new Dictionary<(string Schema, string Table), List<CatalogField>>();
        using var columns = connection.CreateCommand();
        columns.CommandText = $"""
            WITH selected AS
            (
                SELECT table_schema, table_name
                FROM information_schema.tables
                WHERE table_schema NOT IN ('information_schema', 'pg_catalog')
                ORDER BY table_schema, table_name
                LIMIT {request.MaxDatasets}
            ), ranked AS
            (
                SELECT c.table_schema, c.table_name, c.column_name, c.data_type, c.is_nullable, c.ordinal_position,
                       ROW_NUMBER() OVER (PARTITION BY c.table_schema, c.table_name ORDER BY c.ordinal_position) AS field_rank
                FROM information_schema.columns c
                INNER JOIN selected s ON s.table_schema = c.table_schema AND s.table_name = c.table_name
            )
            SELECT table_schema, table_name, column_name, data_type, is_nullable, ordinal_position
            FROM ranked
            WHERE field_rank <= {request.MaxFieldsPerDataset}
            ORDER BY table_schema, table_name, ordinal_position
            """;
        using var columnReader = columns.ExecuteReader();
        while (columnReader.Read())
        {
            ct.ThrowIfCancellationRequested();
            var key = (columnReader.GetString(0), columnReader.GetString(1));
            if (!fields.TryGetValue(key, out var list)) fields[key] = list = [];
            if (list.Count < request.MaxFieldsPerDataset)
                list.Add(new(columnReader.GetString(2), columnReader.GetString(3),
                    string.Equals(columnReader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase)));
        }

        datasets = datasets.Select(d => d with
        {
            Fields = fields.GetValueOrDefault((d.Namespace ?? "", d.Name)) ?? [],
        }).ToList();

        return Task.FromResult(new SourceCatalogSnapshot(
            Name,
            "DuckDB",
            datasets,
            DateTimeOffset.UtcNow,
            Truncated: datasets.Count >= request.MaxDatasets || fields.Values.Any(f => f.Count >= request.MaxFieldsPerDataset),
            Note: "Schemas, tables/views, and columns only. Row counts and record values are not queried."));
    }
}

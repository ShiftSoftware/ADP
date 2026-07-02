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
/// handle pins a snapshot file open on an SMB share — a held-open target is what turned the
/// sync-agent's old fixed-name snapshot replacement into a delete. Measures within one run may
/// therefore span a publish boundary; for health checks that read-consistency trade is fine.
/// </para>
/// </summary>
public sealed class DuckDbCheckSource : ICheckSource
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
}

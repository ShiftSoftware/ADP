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
/// </summary>
public sealed class DuckDbCheckSource(string connectionString) : ICheckSource, IDisposable
{
    public string Name => "duckdb";

    private DuckDBConnection? _conn;

    private DuckDBConnection Connection
    {
        get
        {
            if (_conn is null)
            {
                _conn = new DuckDBConnection(connectionString);
                _conn.Open();
            }
            return _conn;
        }
    }

    public Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(spec.Sql))
            return Task.FromResult(new MeasureOutcome { Error = "duckdb measure requires 'sql'." });

        try
        {
            var cells = new Dictionary<string, MeasureCell>();
            var isTimestamp = string.Equals(spec.ValueKind, "timestamp", StringComparison.OrdinalIgnoreCase);

            using var cmd = Connection.CreateCommand();
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

    public void Dispose() => _conn?.Dispose();
}

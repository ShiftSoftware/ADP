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
public sealed class SqlCheckSource(string? connectionString) : ICheckSource
{
    /// <summary>Guards a single slow/blocked query from stalling the whole run.</summary>
    private const int CommandTimeoutSeconds = 60;

    public string Name => "sql";

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
            cmd.CommandTimeout = CommandTimeoutSeconds;
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

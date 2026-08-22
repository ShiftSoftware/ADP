namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Where one snapshot table's rows live right now.
///
/// <para><b>Resident</b> — the rows are in the write database. The only behaviour that existed
/// before lazy residency, and still the default for every table.</para>
///
/// <para><b>Deferred</b> — the rows live only in the newest committed published parquet; the
/// write database keeps the table definition, this state, and the manifest entry needed to
/// load the rows back. A table is deferred only at cold start, only when every source feeding
/// it can answer "unchanged" without reading data (a gate-wired file source), and only when
/// its committed copy is provably not owed to the replication pump. The first merge that
/// actually has to touch the table hydrates it back — after which it is Resident until the
/// process ends. There is deliberately no evict step: a restart is the only way rows leave.</para>
///
/// <para>The state is recorded explicitly in the write database's meta schema and is never
/// inferred from <c>count(*)</c>: zero rows is ambiguous (a legitimately empty resident table
/// reads the same as a deferred one), and a process restart that reopens an existing write DB
/// must find a previously deferred table still deferred.</para>
/// </summary>
public enum SnapshotResidency
{
    Resident,
    Deferred,
}

/// <summary>
/// What the write database remembers about a Deferred table: which manifest its committed
/// copy came from, the resolved parquet file list, and the row count the copy carries.
/// Recorded at the cold start that skipped loading; deleted when hydration takes the table
/// Resident. This is everything hydration needs, so ingest never has to reach the publish
/// tier's listing or manifest machinery — the store's own connection reads the files directly.
/// </summary>
internal sealed record DeferredTableRecord(
    string TableName,
    string ManifestFile,
    IReadOnlyList<string> ParquetPaths,
    long RowCount)
{
    /// <summary>
    /// A DuckDB relation over the deferred copy's explicit file list — never a glob pattern.
    /// Explicit names are what make hydration fail loud: a missing, zero-byte, or unreachable
    /// file raises a DuckDB error, while a glob that matches nothing silently reads as empty.
    /// </summary>
    public string ReadParquetSql() =>
        $"read_parquet([{string.Join(", ", ParquetPaths.Select(path => $"'{path.Replace("'", "''")}'"))}])";
}

/// <summary>
/// The shared content-identity recipe for the ingest-time guard: the aggregate computed from
/// resident rows at export time (recorded in the manifest and carried into the write DB when
/// the table defers) must be byte-comparable with the one computed from staged rows at ingest
/// time, so both sides use this one expression.
/// </summary>
internal static class SnapshotContentHash
{
    /// <summary>
    /// Row count plus an order-independent XOR fold of <c>md5(_PrimaryKey ‖ chr(31) ‖ _RowHash)</c>.
    /// Keyed by <c>_PrimaryKey</c> so equal contents on different rows cannot cancel; the count
    /// rides along so an empty set can never alias a set whose hashes XOR to zero. md5 rather
    /// than DuckDB's internal <c>hash()</c> because the two sides of the comparison can be
    /// computed by different engine builds, and md5 is stable across them. Same trust grade as
    /// the publisher's <c>stateHash</c>: an aggregate collision reads as "unchanged" — accepted,
    /// exactly as it is for the signature that gates re-export.
    /// </summary>
    internal static string AggregateSql =>
        $"""
        concat(count(*), ':', coalesce(CAST(bit_xor(md5_number("{BookkeepingColumns.PrimaryKey}" || chr(31) || "{BookkeepingColumns.RowHash}")) AS VARCHAR), '0'))
        """;
}

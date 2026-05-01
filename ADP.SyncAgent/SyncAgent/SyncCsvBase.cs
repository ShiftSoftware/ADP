namespace ShiftSoftware.ADP.SyncAgent;

/// <summary>
/// Base class for CSV-backed sync models that participate in DuckDB-based change detection.
/// The metadata columns are populated by the data source after rows are loaded into DuckDB
/// and are reused across the source, staging, and changes tables.
/// </summary>
public abstract class SyncCsvBase
{
    /// <summary>
    /// Deterministic key derived from the model's KeyColumns. Computed in DuckDB after COPY,
    /// used for joins between source, staging, and changes tables.
    /// </summary>
    public string? _PrimaryKey { get; set; }

    /// <summary>
    /// Hash of the model's value columns used to detect updates. Computed in DuckDB after COPY.
    /// </summary>
    public string? _RowHash { get; set; }

    /// <summary>
    /// Timestamp the row was loaded into DuckDB.
    /// </summary>
    public DateTime? _LoadedAt { get; set; }
}

using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

/// <summary>
/// Configuration for the DuckDB-backed CSV sync data source.
/// </summary>
/// <typeparam name="TCsv">
/// The CSV row model. Must inherit from <see cref="SyncCsvBase"/> so the metadata columns
/// (_PrimaryKey, _RowHash, _LoadedAt) are part of the schema.
/// </typeparam>
public class DuckDbCsvSyncDataSourceConfigurations<TCsv> where TCsv : SyncCsvBase
{
    /// <summary>
    /// DuckDB connection string. A persisted file is required so the changes queue survives
    /// between runs. One file per sync (do not share across concurrent functions — DuckDB is
    /// single-writer per file).
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// CSV file location in the storage service.
    /// </summary>
    public required string CsvFileName { get; set; }

    public string? SourceContainerOrShareName { get; set; }
    public string? SourceDirectory { get; set; }

    /// <summary>
    /// Whether the CSV has a header row. Default true.
    /// </summary>
    public bool HasHeaderRecord { get; set; } = true;

    /// <summary>
    /// Number of leading lines to skip (in addition to the header, for files with banners).
    /// </summary>
    public int IgnoreFirstLines { get; set; } = 0;

    /// <summary>
    /// CSV delimiter. Default ','.
    /// </summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>
    /// CSV quote character. Default '"'.
    /// </summary>
    public string Quote { get; set; } = "\"";

    /// <summary>
    /// Composite or single-column natural key that identifies a logical row across versions.
    /// Used to detect Add vs Update vs Delete and to coalesce pending changes across runs.
    /// Required.
    /// </summary>
    public required Expression<Func<TCsv, object>> KeyColumns { get; set; }

    /// <summary>
    /// Optional override of which columns participate in the row hash. When null, all model
    /// columns are used except those marked <see cref="IgnoreInHashAttribute"/>, the
    /// <see cref="SyncCsvBase"/> metadata columns, and the key columns.
    /// </summary>
    public Expression<Func<TCsv, object>>? HashColumns { get; set; }

    /// <summary>
    /// Optional raw SQL expression that overrides the generated key expression. Use for
    /// normalization (trim, upper, date formatting) when the default cast/concat isn't right.
    /// Reference columns by their unquoted property names.
    /// </summary>
    public string? CustomKeyExpression { get; set; }

    /// <summary>
    /// Optional raw SQL expression that overrides the generated hash expression.
    /// </summary>
    public string? CustomHashExpression { get; set; }

    /// <summary>
    /// Number of failed sync attempts before a change row is marked Dead and stops being
    /// retried. Default 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Override the source table name. Default: <c>{TCsv.Name}_source</c>.
    /// </summary>
    public string? SourceTableName { get; set; }

    /// <summary>
    /// Override the changes table name. Default: <c>{TCsv.Name}_changes</c>.
    /// </summary>
    public string? ChangesTableName { get; set; }

    /// <summary>
    /// Local working directory for staging the downloaded CSV file before COPY. Defaults to
    /// the OS temp directory when not set.
    /// </summary>
    public string? LocalWorkingDirectory { get; set; }
}

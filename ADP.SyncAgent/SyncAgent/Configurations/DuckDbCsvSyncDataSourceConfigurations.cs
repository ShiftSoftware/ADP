using System.Linq.Expressions;

namespace ShiftSoftware.ADP.SyncAgent.Configurations;

/// <summary>
/// Configuration for the DuckDB-backed CSV sync data source.
/// </summary>
/// <typeparam name="TCsv">
/// The CSV row model. Must inherit from <see cref="SyncCsvBase"/> so the metadata columns
/// (_PrimaryKey, _RowHash, _LoadedAt) are part of the schema.
/// </typeparam>
/// <typeparam name="TDestination">
/// The destination model the engine stores. Only referenced by
/// <see cref="DestinationPrimaryKeySelector"/>; everything else is driven by <typeparamref name="TCsv"/>.
/// </typeparam>
public class DuckDbCsvSyncDataSourceConfigurations<TCsv, TDestination>
    where TCsv : SyncCsvBase
    where TDestination : class
{
    /// <summary>
    /// DuckDB connection string. Optional when <see cref="DuckDbFilePath"/> is provided —
    /// the source will build a default <c>Data Source={path}</c> string in that case.
    /// One DuckDB file per sync (DuckDB is single-writer per file).
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Absolute path to the persisted DuckDB file. Used when <see cref="ConnectionString"/>
    /// is not provided.
    /// </summary>
    public string? DuckDbFilePath { get; set; }

    /// <summary>
    /// Absolute path of the source CSV file. The data source reads it in-place — it does not
    /// download, copy, or stage the file. Callers that need to fetch the file from a remote
    /// store should do that before calling <c>Configure</c> and pass the resolved local path
    /// here.
    /// </summary>
    public required string CsvFilePath { get; set; }

    /// <summary>
    /// DuckDB memory limit in megabytes. Applied via <c>SET memory_limit</c> after the
    /// connection opens so DuckDB spills to its default temp directory instead of growing to
    /// the system ceiling. Default 2000 MB.
    /// </summary>
    public long MemoryLimitMB { get; set; } = 2000;

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
    /// CSV escape character used inside quoted fields. RFC 4180 doubles the quote (e.g.
    /// <c>""</c> inside <c>"..."</c>), which is what the default — same as <see cref="Quote"/> —
    /// gives you. We pin this explicitly because DuckDB's CSV sniffer can override the default
    /// to empty when the sample window happens not to contain any escaped quotes, after which
    /// any later row with <c>""</c> fails parsing with "Value with unterminated quote found".
    /// </summary>
    public string Escape { get; set; } = "\"";

    /// <summary>
    /// Encoding of the source CSV file. Passed to DuckDB's <c>COPY ... (ENCODING '...')</c>.
    /// Default <c>utf-8</c>, picked because it can represent every Unicode character — Arabic,
    /// Kurdish, accented Latin, Chinese, emoji — without loss. Plain ASCII files (the common
    /// case for ERP exports) are a valid subset of UTF-8 and decode cleanly.
    /// <para>
    /// UTF-8 is strict: an invalid byte sequence aborts the <c>COPY</c> rather than silently
    /// reading garbage. If you hit that on a file you can't fix upstream, set <c>Encoding</c> to
    /// the file's actual encoding — <c>"utf-16"</c> for UTF-16 dumps, or <c>"latin-1"</c> as a
    /// last-resort fallback that accepts any byte sequence (but will mojibake non-Latin
    /// characters since latin-1 only covers 256 codepoints).
    /// </para>
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// strptime-style format passed to DuckDB's <c>COPY ... (DATEFORMAT '...')</c> and applied to
    /// every <c>DATE</c> column read from the CSV. Leave null (default) to let DuckDB parse ISO
    /// <c>YYYY-MM-DD</c>. Set this when the source exports dates in a locale format, e.g.
    /// <c>"%m/%d/%Y"</c> for <c>11/7/2024</c>.
    /// <para>
    /// Because the staging column types are pinned from the model, DuckDB does NOT fall back to the
    /// CSV sniffer's auto-detected format — an unset format means strict ISO parsing, so a
    /// locale-formatted date column aborts the whole COPY with a "Could not convert string ... to
    /// 'DATE'" conversion error.
    /// </para>
    /// <para>
    /// This format is global to the file (DuckDB's COPY supports only one date/timestamp format). On
    /// the rare file with two date columns in different formats, type the odd column as
    /// <see cref="string"/> on the model and convert it in the sync engine's mapping instead.
    /// </para>
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// strptime-style format passed to DuckDB's <c>COPY ... (TIMESTAMPFORMAT '...')</c> and applied
    /// to every <c>TIMESTAMP</c> column read from the CSV. A model property typed <c>DateTime</c>/
    /// <c>DateTime?</c> maps to <c>TIMESTAMP</c>, so a date-only value like <c>11/7/2024</c> in such a
    /// column needs this set (e.g. <c>"%m/%d/%Y"</c>) — DuckDB parses it to a timestamp at midnight.
    /// Leave null (default) for strict ISO parsing. See <see cref="DateFormat"/> for why the sniffer's
    /// auto-detected format does not apply and how to handle two columns with different formats.
    /// </summary>
    public string? TimestampFormat { get; set; }

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
    /// Enables partial-batch promotion. Mirrors <c>EFCoreSyncDataSourceConfigurations.DestinationKey</c>:
    /// it reads the natural key off a destination item the same way <see cref="KeyColumns"/> reads it
    /// off a CSV row. The components must align 1:1 and in the same order as <see cref="KeyColumns"/>
    /// (single column: <c>x =&gt; x.Vin</c>; composite: <c>x =&gt; new { x.Vin, x.CampaignCode }</c>).
    /// <para>
    /// When set and a batch comes back <see cref="SyncStoreDataResultType.Partial"/>, the data source
    /// reads the key off each succeeded destination item, matches it against the key columns of the
    /// rows claimed for the batch, and — in one query — promotes just those rows to source and
    /// removes them from the changes table; only the unsucceeded rows are retried. When null
    /// (default), partial batches keep the all-or-nothing behavior — the whole batch is retried.
    /// Fully-succeeded and fully-failed batches behave the same either way.
    /// </para>
    /// <para>
    /// Matching is by key <em>value</em> (<c>CAST(... AS VARCHAR)</c>), so it is exact when the
    /// destination carries the key unchanged. If your Mapping transforms a key value (e.g. trims it)
    /// a mismatch simply leaves that row pending to be re-sent next run — it never promotes the
    /// wrong row.
    /// </para>
    /// </summary>
    public Expression<Func<TDestination, object>>? DestinationKey { get; set; }

    /// <summary>
    /// Optional key selector on the CSV/source side, paired with <see cref="DestinationKey"/>, for
    /// partial-batch matching. When set, the data source correlates a succeeded destination item to its
    /// source row by comparing <see cref="DestinationKey"/> (evaluated on the destination item) with
    /// this expression (evaluated on the source row) — both in memory — and promotes the matched rows by
    /// their primary key. Use when the destination key isn't a plain column, e.g. a concatenated id:
    /// <c>SourceKey = x =&gt; x.ID</c> (the CSV's computed id) paired with <c>DestinationKey = x =&gt; x.id</c>.
    /// Because both sides are evaluated in C#, the formatting always agrees (no SQL <c>CAST</c> mismatch,
    /// e.g. for decimals). When null, partial matching falls back to the default per-<see cref="KeyColumns"/>
    /// comparison. Requires <see cref="DestinationKey"/> to be a single component.
    /// </summary>
    public Expression<Func<TCsv, object>>? SourceKey { get; set; }
}

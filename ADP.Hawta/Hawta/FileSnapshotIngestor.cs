namespace ShiftSoftware.ADP.Hawta;

public enum FileSourceFormat
{
    /// <summary>Resolve from the file extension: <c>.parquet</c> or <c>.csv</c> — anything else must be explicit.</summary>
    Auto,
    Csv,
    Parquet,
}

/// <summary>
/// CSV read behavior. Every CSV column is read as VARCHAR with sniffing disabled — parsing is
/// fully determined by this config, never by what the file happens to contain today. Typing
/// happens where it is uniform across source formats: at the staging insert (the staging
/// table's typed columns) or via explicit CASTs in the projection.
/// </summary>
public sealed class CsvReadOptions
{
    public string Delimiter { get; init; } = ",";

    public bool HasHeader { get; init; } = true;

    /// <summary>
    /// Column names for headerless files, in file order. When set with
    /// <see cref="HasHeader"/> = true, the names override the file's header row.
    /// </summary>
    public IReadOnlyList<string>? ColumnNames { get; init; }

    /// <summary>
    /// Pad rows that end early with NULLs instead of failing the read. Off by default —
    /// a short row in a machine-produced feed usually means truncation, and loud beats padded.
    /// </summary>
    public bool NullPadding { get; init; }
}

public sealed class FileSnapshotIngestorOptions
{
    /// <summary>The snapshot table this source feeds.</summary>
    public required SnapshotTableDefinition Table { get; init; }

    public required string FilePath { get; init; }

    public FileSourceFormat Format { get; init; } = FileSourceFormat.Auto;

    /// <summary>CSV read behavior; ignored for parquet sources.</summary>
    public CsvReadOptions Csv { get; init; } = new();

    /// <summary>
    /// Optional projection over the raw file relation. Must contain the <c>{source}</c>
    /// placeholder as its FROM target, e.g.
    /// <c>SELECT "Part No" AS PARTNO, … FROM {source}</c> — the place to rename the file's
    /// verbatim column headers (spaces and all), compute composite keys, CAST, filter, and
    /// dedup (QUALIFY). Null = <c>SELECT * FROM {source}</c>; either way the result must
    /// contain every column in <see cref="Table"/> by name.
    /// <para>May also use <c>{sourcePath}</c>: the current file's path as a ready-quoted,
    /// glob-escaped SQL string literal — for projections that need to read the SAME file a
    /// second way (e.g. a raw-line read joined by row number to carry the verbatim line
    /// alongside the parsed columns).</para>
    /// </summary>
    public string? SelectSql { get; init; }

    /// <summary>
    /// Expose a <c>"hawta$file_row_number"</c> column (1-based scan-order index) to the
    /// projection — the stable row position that makes "first occurrence in the file wins"
    /// dedup deterministic (a nondeterministic winner would flap <c>_RowHash</c> and fake a
    /// change every run). Format-agnostic: the same projection works unchanged when a feed
    /// flips CSV → parquet. The <c>$</c> name cannot collide with snapshot columns
    /// (identifier rules forbid it); a source file carrying the same literal column name is
    /// rejected loudly rather than silently shadowing the synthesized index.
    /// </summary>
    public bool IncludeFileRowNumber { get; init; }

    /// <summary>Result column whose trimmed text value becomes <c>_PrimaryKey</c> (blank → NULL → a loud <c>Failed:InvalidStagingRows</c>).</summary>
    public required string PrimaryKeyColumn { get; init; }

    /// <summary>Optional result column carrying the row's own save date (becomes <c>_SourceModified</c>).</summary>
    public string? SourceModifiedColumn { get; init; }

    public required SnapshotMergeOptions MergeOptions { get; init; }
}

/// <summary>
/// Full-pull ingestion for file feeds — parquet first, CSV per-source config. The file is read
/// by DuckDB's native readers straight into staging (no .NET row loop — the 400 MB catalog
/// files are why), <c>_RowHash</c> is computed in-database AFTER the values land in the typed
/// staging columns — so a CSV's <c>"1"</c> and a parquet's <c>1</c> hash identically — then
/// hands off to <see cref="SnapshotMerge"/>.
///
/// <para><b>Format-flip parity, precisely.</b> Typing at the staging insert makes a format flip
/// free for TYPED columns. It does NOT make a blank text field equal to a NULL text field:
/// under the blank-field policy below, a blank CSV cell is <c>''</c> in a VARCHAR column, and
/// <c>''</c> and NULL are deliberately distinct to <see cref="RowHash"/>. So a feed whose
/// snapshot columns are all VARCHAR (raw-text capture — which is every CSV feed reproducing
/// another pipeline's documents) re-hashes once if its Phase-B parquet producer encodes blanks
/// as NULL. That cost is bounded, schedulable, and measured by the format-flip drill's
/// vendor-shaped leg — it is a trade for automatic blank-vs-null document parity against the
/// incumbent, which is the gate that actually blocks cutover.</para>
///
/// <para>File-level outcomes are run records, not silence: a missing file returns
/// <see cref="SnapshotMergeStatus.SkippedSourceAbsent"/> (a renamed/unmounted feed must never
/// tombstone its family), and a failed read (share offline, torn upload) records
/// <c>Failed:Exception</c> before rethrowing.</para>
/// </summary>
public static class FileSnapshotIngestor
{
    /// <summary>The scan-position column synthesized by <see cref="FileSnapshotIngestorOptions.IncludeFileRowNumber"/>.</summary>
    public const string FileRowNumberColumn = "hawta$file_row_number";

    // The whitespace class .NET's Trim() strips in practice for these feeds (space, tab,
    // CR, LF, VT, FF) — DuckDB's bare trim() strips spaces only, which would let a
    // tab-padded key mint a different identity than the C#-trimming ingestors/incumbent.
    private const string TrimChars = "concat(' ', chr(9), chr(13), chr(10), chr(11), chr(12))";

    /// <summary>
    /// The whitespace-class trim this ingestor applies when deriving <c>_PrimaryKey</c>,
    /// as a DuckDB expression: <c>KeyTrim("x")</c> → <c>trim(x, &lt;class&gt;)</c>. A
    /// projection that guards on blank keys must use THIS, not bare <c>trim()</c> (spaces
    /// only) — otherwise a tab-only key passes the projection's guard and then becomes a
    /// NULL <c>_PrimaryKey</c>, failing the entire feed instead of skipping one messy row.
    /// </summary>
    public static string KeyTrim(string expression) => $"trim({expression}, {TrimChars})";

    public static SnapshotMergeResult Ingest(SnapshotStore store, FileSnapshotIngestorOptions options)
    {
        var format = ResolveFormat(options);

        if (options.SelectSql is not null && !options.SelectSql.Contains("{source}"))
            throw new ArgumentException("SelectSql must contain the {source} placeholder as its FROM target.", nameof(options));

        if (!File.Exists(options.FilePath))
        {
            // Absence is a first-class outcome (the incumbent's mass-delete hazard: a renamed
            // CSV must read as "source offline", never as "everything was deleted").
            var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
            var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceAbsent, 0, 0, 0, 0);
            SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow, skipped,
                $"Source file not found: {options.FilePath}");
            return skipped;
        }

        // A 0-byte file is the just-created-not-yet-written half of the mid-upload window
        // (the header-only half is caught by the zero-staged check below). DuckDB can't
        // even bind columns on it, so catch it here as the same graceful skip.
        if (options.MergeOptions is { DeletesEnabled: true, ForceDeletes: false }
            && new FileInfo(options.FilePath).Length == 0)
        {
            var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
            var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceEmpty, 0, 0, 0, 0);
            SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow, skipped,
                $"Source file is 0 bytes (mid-upload?): {options.FilePath}. Nothing merged.");
            return skipped;
        }

        var rawReader = format == FileSourceFormat.Parquet
            ? $"read_parquet('{GlobEscape(SqlLiteral(options.FilePath))}')"
            : CsvReader(options.FilePath, options.Csv);

        // Scan order IS file order (DuckDB preserves insertion order for file scans), so a
        // bare row_number gives the stable physical position — uniformly for CSV and parquet.
        // The $-named alias can't collide with snapshot columns (identifier rules forbid $).
        var reader = options.IncludeFileRowNumber
            ? $"(SELECT *, row_number() OVER () AS \"{FileRowNumberColumn}\" FROM {rawReader})"
            : rawReader;

        var projection = options.SelectSql
            ?.Replace("{source}", reader)
            .Replace("{sourcePath}", $"'{GlobEscape(SqlLiteral(options.FilePath))}'")
            ?? $"SELECT * FROM {reader}";

        var staging = store.CreateStagingTable(options.Table);

        try
        {
            // A source file that itself carries the synthesized column name would silently
            // shadow the scan index (DuckDB renames the alias to *_1 instead of erroring,
            // and dedup order would follow file DATA) — reject that one case loudly.
            // (Schema probe only — LIMIT 0 reads header/metadata, no rows.)
            if (options.IncludeFileRowNumber
                && SourceColumnNames(store, rawReader).Contains(FileRowNumberColumn, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"The source file already has a '{FileRowNumberColumn}' column — it would shadow the synthesized scan index.", nameof(options));

            // Selecting each table column BY NAME from the projection is the contract
            // tripwire: a renamed or missing file column fails here, loudly, before any row
            // lands. The insert cast into the typed staging columns is where all-varchar CSV
            // text becomes typed values (and where garbage in a numeric column fails the run).
            //
            // Blank-field policy, per column: the read preserves a present-but-empty field as
            // the EMPTY STRING (nullstr is a control char no feed contains), so raw-text
            // capture is faithful — .NET CSV readers give consumers "" too, and a pump
            // reproducing another pipeline's documents must not turn "" into null. But an
            // empty string is not a number or a date: for TYPED target columns, empty means
            // absent, so it becomes NULL rather than failing the whole run. Genuinely bad
            // values (e.g. "abc" in a numeric column) still fail loudly — that contract is
            // unchanged.
            var textualSources = TextualColumns(store, projection);
            var sourceColumns = string.Join(", ", options.Table.Columns.Select(c =>
                textualSources.Contains(c.Name) && !IsTextualType(c.DuckDbType)
                    ? $"nullif(s.\"{c.Name}\", '')"
                    : $"s.\"{c.Name}\""));
            // _SourceModified is a TIMESTAMP target too — same blank-field rule, or a blank
            // save-date field would CAST('' AS TIMESTAMP) and fail the whole run.
            var sourceModified = options.SourceModifiedColumn switch
            {
                null => "NULL",
                var column when textualSources.Contains(column) => $"nullif(s.\"{column}\", '')",
                var column => $"s.\"{column}\"",
            };

            store.Execute(
                $"""
                INSERT INTO {staging.QualifiedName}
                    ({options.Table.QuotedColumnList}, "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.RowHash}", "_SourceModified")
                SELECT {sourceColumns},
                       nullif(trim(CAST(s."{options.PrimaryKeyColumn}" AS VARCHAR), {TrimChars}), ''),
                       NULL,
                       {sourceModified}
                FROM ({projection}) AS s
                """);

            store.Execute(
                $"""
                UPDATE {staging.QualifiedName}
                SET "{BookkeepingColumns.RowHash}" = {RowHash.Expression(options.Table.Columns.Select(c => c.Name))}
                """);

            // A full-universe feed that reads as ZERO rows (0-byte or header-only file —
            // the normal mid-upload window on an SMB share) is presumed torn, never a purge:
            // merging it would tombstone the whole family, and on a small family the
            // guardrail's absolute floor wouldn't catch it (the 29-row NonJPM case).
            // ForceDeletes remains the intentional-wipe path.
            if (options.MergeOptions is { DeletesEnabled: true, ForceDeletes: false }
                && Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {staging.QualifiedName}")) == 0)
            {
                var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
                var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceEmpty, 0, 0, 0, 0);
                SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow, skipped,
                    $"Source file produced zero rows (empty or header-only — mid-upload?): {options.FilePath}. " +
                    "Nothing merged; re-run with ForceDeletes for an intentional purge.");
                return skipped;
            }
        }
        catch (Exception exception)
        {
            // The run record IS the alarm surface — a feed that stopped being readable
            // (share offline, torn upload, schema drift) must be visible in meta.SyncRuns.
            var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
            try
            {
                SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow,
                    new SnapshotMergeResult(runId, SnapshotMergeStatus.Failed, 0, 0, 0, 0),
                    exception.Message);
            }
            catch { /* recording must never mask the original failure */ }

            throw;
        }

        return SnapshotMerge.Execute(store, options.Table, staging, options.MergeOptions);
    }

    private static FileSourceFormat ResolveFormat(FileSnapshotIngestorOptions options)
    {
        if (options.Format != FileSourceFormat.Auto)
            return options.Format;

        return Path.GetExtension(options.FilePath).ToLowerInvariant() switch
        {
            ".parquet" => FileSourceFormat.Parquet,
            ".csv" => FileSourceFormat.Csv,
            var ext => throw new ArgumentException(
                $"Cannot infer the file format from '{ext}' — set Format explicitly.", nameof(options)),
        };
    }

    /// <summary>
    /// The exact <c>read_csv(…)</c> relation this ingestor would use for a feed — exposed so
    /// tooling (format-flip drills, ad-hoc inspection) reads a file with the SAME semantics
    /// the agent does. A hand-copied read config silently stops testing the real thing the
    /// moment a feed's options change.
    /// </summary>
    public static string CsvReaderSql(string filePath, CsvReadOptions csv) => CsvReader(filePath, csv);

    private static string CsvReader(string filePath, CsvReadOptions csv)
    {
        var arguments = new List<string>
        {
            $"'{GlobEscape(SqlLiteral(filePath))}'",
            $"header={(csv.HasHeader ? "true" : "false")}",
            $"delim='{SqlLiteral(csv.Delimiter)}'",
            // No sniffing: every column VARCHAR, dialect fully pinned by this config.
            "all_varchar=true",
            // A present-but-empty field is the EMPTY STRING, not NULL (nullstr = a control
            // char no feed contains). This is raw-text capture being honest — and it is what
            // .NET-side CSV readers (FileHelpers) give consumers, so blank-vs-null never
            // becomes a phantom document difference against an incumbent pipeline.
            "nullstr=chr(1)",
        };

        if (csv.ColumnNames is { Count: > 0 } names)
            arguments.Add($"names=[{string.Join(", ", names.Select(n => $"'{SqlLiteral(n)}'"))}]");

        if (csv.NullPadding)
            arguments.Add("null_padding=true");

        return $"read_csv({string.Join(", ", arguments)})";
    }

    private static string SqlLiteral(string value) => value.Replace("'", "''");

    /// <summary>
    /// DuckDB's file readers treat paths as GLOB patterns while <c>File.Exists</c> tests the
    /// literal — unescaped, a bracketed filename (legal on Windows) passes the existence
    /// check yet reads a DIFFERENT matching file. Escaping pins both to the same file.
    /// (<c>[</c> starts a character class; <c>*</c>/<c>?</c> can't appear in valid Windows
    /// paths but are escaped anyway.)
    /// </summary>
    private static string GlobEscape(string path) =>
        path.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]");

    /// <summary>Probes the reader's schema (LIMIT 0 — header/metadata only, no rows scanned).</summary>
    private static IReadOnlyList<string> SourceColumnNames(SnapshotStore store, string reader)
    {
        using var command = store.Connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {reader} LIMIT 0";
        using var schemaReader = command.ExecuteReader();
        return [.. Enumerable.Range(0, schemaReader.FieldCount).Select(schemaReader.GetName)];
    }

    /// <summary>
    /// The projection's VARCHAR-typed columns (metadata-only probe). Only these can carry
    /// the empty string that the blank-field policy converts to NULL for typed targets — a
    /// parquet source's real INTEGER column is never rewritten.
    /// </summary>
    private static HashSet<string> TextualColumns(SnapshotStore store, string projection)
    {
        var textual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var command = store.Connection.CreateCommand();
        command.CommandText = $"DESCRIBE SELECT * FROM ({projection}) AS s";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (IsTextualType(reader.GetString(1)))
                textual.Add(reader.GetString(0));
        }

        return textual;
    }

    private static bool IsTextualType(string duckDbType) =>
        duckDbType.Trim().ToUpperInvariant() is "VARCHAR" or "TEXT" or "STRING" or "CHAR" or "BPCHAR";
}

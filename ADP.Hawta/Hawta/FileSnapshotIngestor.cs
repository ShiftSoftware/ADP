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

public enum FileKeyNormalization
{
    Trim,
    TrimUpperInvariant,
}

public enum FileValueNormalization
{
    None,
    Trim,
    TrimUpperInvariant,
}

/// <summary>
/// Declarative binding from an external file header to one typed snapshot property. The
/// target is normally obtained through <see cref="SnapshotTableDefinition{TRow}.Column{TValue}"/>,
/// so renaming the model is a compile-time change while the external header remains an
/// explicit adapter-boundary string.
/// </summary>
public sealed record FileColumnBinding(
    string TargetColumn,
    string SourceColumn,
    FileValueNormalization Normalization = FileValueNormalization.None);

/// <summary>One typed projection column contributing to a generated logical file-row identity.</summary>
public sealed record FileLogicalKeyPart(string Column, FileKeyNormalization Normalization = FileKeyNormalization.Trim);

/// <summary>
/// Declarative logical identity for the common file path. Components are trimmed, blank
/// components fail the staging contract, and composites are joined only after rejecting the
/// separator inside a component so two different tuples can never alias one key.
/// </summary>
public sealed class FileLogicalKey
{
    public FileLogicalKey(params FileLogicalKeyPart[] parts)
    {
        if (parts.Length == 0)
            throw new ArgumentException("A logical key needs at least one component.", nameof(parts));
        if (parts.Any(part => !SnapshotTableDefinition.IsValidIdentifier(part.Column)))
            throw new ArgumentException("Logical-key columns must be plain snapshot identifiers.", nameof(parts));

        Parts = parts;
    }

    public IReadOnlyList<FileLogicalKeyPart> Parts { get; }

    /// <summary>Composite separator. Components containing it fail loudly instead of creating an ambiguous key.</summary>
    public string Separator { get; init; } = "|";

    public static FileLogicalKey Single(string column, FileKeyNormalization normalization = FileKeyNormalization.Trim) =>
        new(new FileLogicalKeyPart(column, normalization));
}

/// <summary>
/// Source-row identity for a file that legitimately contains repeated logical keys. Every
/// physical row is retained. Hawta assigns a one-based occurrence ordinal within the typed,
/// normalized group key and persists it in <see cref="OrdinalColumn"/>; the row key is the
/// unambiguous group tuple plus that ordinal.
/// </summary>
public sealed class FileOccurrenceRowIdentity
{
    public FileOccurrenceRowIdentity(string ordinalColumn, params FileLogicalKeyPart[] groupParts)
    {
        if (!SnapshotTableDefinition.IsValidIdentifier(ordinalColumn))
            throw new ArgumentException("The occurrence ordinal must be a plain snapshot identifier.", nameof(ordinalColumn));
        if (groupParts.Length == 0)
            throw new ArgumentException("Occurrence identity needs at least one group-key component.", nameof(groupParts));
        if (groupParts.Any(part => !SnapshotTableDefinition.IsValidIdentifier(part.Column)))
            throw new ArgumentException("Occurrence group-key columns must be plain snapshot identifiers.", nameof(groupParts));

        OrdinalColumn = ordinalColumn;
        GroupParts = groupParts;
    }

    public string OrdinalColumn { get; }
    public IReadOnlyList<FileLogicalKeyPart> GroupParts { get; }
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
    /// Optional source-specific reshaping over the raw file relation. Must contain the
    /// <c>{source}</c> placeholder. Null is the normal typed path: the table model binds
    /// directly by column name. Use SQL only for a real transform such as renamed headers,
    /// aggregation, joins, filtering, or unusual deduplication.
    /// </summary>
    public string? SelectSql { get; init; }

    /// <summary>
    /// Optional declarative external-header aliases and value normalization for the common
    /// typed path. Unlisted model properties bind to same-named source columns. This cannot
    /// be combined with <see cref="SelectSql"/>: aliases describe binding, SQL describes a
    /// custom relational transform.
    /// </summary>
    public IReadOnlyList<FileColumnBinding>? ColumnBindings { get; init; }

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

    /// <summary>
    /// Model-driven logical identity. Preferred for the common typed path; Hawta generates
    /// the key expression and validates every component.
    /// </summary>
    public FileLogicalKey? LogicalKey { get; init; }

    /// <summary>
    /// Identity for repeated logical keys: retains every source row and derives a stable
    /// occurrence number within each normalized group. Exactly one of this,
    /// <see cref="LogicalKey"/>, or <see cref="PrimaryKeyColumn"/> must be configured.
    /// </summary>
    public FileOccurrenceRowIdentity? OccurrenceRowIdentity { get; init; }

    /// <summary>
    /// Legacy single-result-column key escape hatch. New file sources should use
    /// <see cref="LogicalKey"/> even for a one-column identity.
    /// </summary>
    public string? PrimaryKeyColumn { get; init; }

    /// <summary>
    /// Explicitly capture each CSV row's verbatim source line into the table model's
    /// <see cref="SnapshotRawSourceAttribute"/> property. Default false. This is audit-only,
    /// unsupported for parquet, and never participates in replication change detection.
    /// </summary>
    public bool CaptureRawSource { get; init; }

    /// <summary>Optional result column carrying the row's own save date (becomes <c>_SourceModified</c>).</summary>
    public string? SourceModifiedColumn { get; init; }

    /// <summary>
    /// Skip the read entirely when the file is unchanged since the last successful merge.
    /// <b>Null keeps the incumbent behaviour</b> — read, hash and merge every cycle — so wiring the
    /// gate is an explicit, per-host decision rather than something a package bump turns on.
    /// See <see cref="SourceChangeGate"/> for what "unchanged" is allowed to mean.
    /// </summary>
    public SourceChangeGate? ChangeGate { get; init; }

    /// <summary>
    /// Per-source override of <see cref="SourceChangeGate.ReingestAfter"/>: re-read this feed
    /// unconditionally once its stamp is older than this. Null — the default — inherits the gate's
    /// setting, which is itself normally "never".
    ///
    /// <para>The knob belongs per source because the cost is entirely a property of the feed. A
    /// 51 KB stock file can afford an hourly blind re-read; a 391 MiB catalogue that changes twice a
    /// year cannot, and forcing one on it would read hundreds of gigabytes a year to notice
    /// nothing. Set this only where the file is cheap AND its producer might rewrite content while
    /// preserving the timestamp.</para>
    /// </summary>
    public TimeSpan? ReingestAfter { get; init; }

    /// <summary>
    /// Operator-supplied version folded into the change gate's fingerprint. Changing it forces one
    /// full re-read. It is the only lever that does so <b>without a deploy</b> — which is what you
    /// need when the reason to distrust the stamps cannot be expressed as a configuration change.
    /// Ignored when <see cref="ChangeGate"/> is null.
    /// </summary>
    public string? IngestVersion { get; init; }

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

    /// <param name="fileMetadata">
    /// The cycle's metadata probe, when the caller has one. Null disables
    /// <see cref="FileSnapshotIngestorOptions.ChangeGate"/> for this call — a caller with no probe
    /// reads, which is the safe direction. The agent supplies one per cycle via
    /// <see cref="SnapshotSourceContext.FileMetadata"/>.
    /// </param>
    public static SnapshotMergeResult Ingest(
        SnapshotStore store, FileSnapshotIngestorOptions options, FileMetadataProbe? fileMetadata = null)
    {
        var format = ResolveFormat(options);

        if (options.SelectSql is not null && !options.SelectSql.Contains("{source}"))
            throw new ArgumentException("SelectSql must contain the {source} placeholder as its FROM target.", nameof(options));
        if (options.SelectSql is not null && options.ColumnBindings is { Count: > 0 })
            throw new ArgumentException("ColumnBindings cannot be combined with SelectSql.", nameof(options));
        if (options.SelectSql is not null && options.OccurrenceRowIdentity is not null)
            throw new ArgumentException("OccurrenceRowIdentity belongs to the common typed path and cannot be combined with SelectSql.", nameof(options));

        var identityCount = (options.LogicalKey is null ? 0 : 1)
                            + (options.OccurrenceRowIdentity is null ? 0 : 1)
                            + (string.IsNullOrWhiteSpace(options.PrimaryKeyColumn) ? 0 : 1);
        if (identityCount != 1)
            throw new ArgumentException(
                "Configure exactly one of LogicalKey, OccurrenceRowIdentity, or PrimaryKeyColumn.", nameof(options));

        var storedColumns = options.Table.Columns.Select(column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var identityColumns = options.LogicalKey?.Parts.Select(part => part.Column)
            ?? options.OccurrenceRowIdentity?.GroupParts.Select(part => part.Column)
            ?? [];
        var unknownIdentityColumns = identityColumns
            .Where(column => !storedColumns.Contains(column))
            .ToList();
        if (unknownIdentityColumns.Count > 0)
        {
            throw new ArgumentException(
                $"Identity column(s) are not part of the typed table: {string.Join(", ", unknownIdentityColumns)}.",
                nameof(options));
        }
        var occurrence = options.OccurrenceRowIdentity;
        if (occurrence is not null)
        {
            var ordinal = options.Table.Columns.SingleOrDefault(column =>
                column.Name.Equals(occurrence.OrdinalColumn, StringComparison.OrdinalIgnoreCase));
            if (ordinal is null)
                throw new ArgumentException(
                    $"Occurrence ordinal column '{occurrence.OrdinalColumn}' is not part of the typed table.", nameof(options));
            if (ordinal.DuckDbType is not ("INTEGER" or "BIGINT"))
                throw new ArgumentException(
                    $"Occurrence ordinal column '{occurrence.OrdinalColumn}' must be INTEGER or BIGINT.", nameof(options));
        }

        if (options.ColumnBindings is { Count: > 0 } bindings)
        {
            var duplicateTargets = bindings
                .GroupBy(binding => binding.TargetColumn, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateTargets.Count > 0)
                throw new ArgumentException(
                    $"Column binding target(s) are declared more than once: {string.Join(", ", duplicateTargets)}.",
                    nameof(options));
            var unknownTargets = bindings
                .Where(binding => !storedColumns.Contains(binding.TargetColumn))
                .Select(binding => binding.TargetColumn)
                .ToList();
            if (unknownTargets.Count > 0)
                throw new ArgumentException(
                    $"Column binding target(s) are not part of the typed table: {string.Join(", ", unknownTargets)}.",
                    nameof(options));
            if (bindings.Any(binding => string.IsNullOrWhiteSpace(binding.SourceColumn)))
                throw new ArgumentException("Column binding source names must be non-blank.", nameof(options));
            if (occurrence is not null
                && bindings.Any(binding => binding.TargetColumn.Equals(
                    occurrence.OrdinalColumn, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    "The occurrence ordinal is generated by Hawta and cannot have a source-column binding.", nameof(options));
            }
        }
        if (options.CaptureRawSource && format != FileSourceFormat.Csv)
            throw new ArgumentException("Raw-source capture is available only for CSV files.", nameof(options));
        if (options.CaptureRawSource && options.Table.RawSourceColumn is null)
            throw new ArgumentException(
                "CaptureRawSource requires one [SnapshotRawSource] string property on the typed table model.", nameof(options));

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

        // ---- Source change gate ------------------------------------------------------------
        // Placed AFTER the absence guard (absence is its own outcome and must stay one) and
        // BEFORE everything expensive. Skips on one condition only: metadata established, file
        // identical, configuration identical, stamp still inside its trust window. Every other
        // answer — including every failure — falls through to the ordinary read below.
        SourceChangeDecision? gateDecision = null;
        if (options.ChangeGate is { } changeGate && fileMetadata is not null)
        {
            gateDecision = changeGate.Evaluate(
                store, fileMetadata, StampKey(options.MergeOptions), options.FilePath,
                SourceConfigFingerprint.Compute(options, options.IngestVersion),
                options.ReingestAfter);

            if (gateDecision.ShouldSkip)
            {
                var runId = options.MergeOptions.RunId ?? Guid.NewGuid().ToString("N");
                var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceUnchanged, 0, 0, 0, 0);
                SnapshotMerge.InsertRunRecord(store, options.Table, options.MergeOptions, runId, DateTime.UtcNow,
                    skipped, gateDecision.Describe(options.FilePath));
                return skipped;
            }
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

        if (options.CaptureRawSource)
        {
            if (SourceColumnNames(store, rawReader).Contains(options.Table.RawSourceColumn!, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"The CSV already contains the audit target column '{options.Table.RawSourceColumn}'.", nameof(options));
            rawReader = RawCapturedCsvReader(options.FilePath, options.Csv, rawReader, options.Table.RawSourceColumn!);
        }

        // Scan order IS file order (DuckDB preserves insertion order for file scans), so a
        // bare row_number gives the stable physical position — uniformly for CSV and parquet.
        // The $-named alias can't collide with snapshot columns (identifier rules forbid $).
        var includeFileRowNumber = options.IncludeFileRowNumber || options.OccurrenceRowIdentity is not null;
        var reader = includeFileRowNumber
            ? $"(SELECT *, row_number() OVER () AS \"{FileRowNumberColumn}\" FROM {rawReader})"
            : rawReader;

        var projection = options.SelectSql
            ?.Replace("{source}", reader)
            ?? BuildTypedProjection(options, reader);

        var staging = store.CreateStagingTable(options.Table);

        try
        {
            // A source file that itself carries the synthesized column name would silently
            // shadow the scan index (DuckDB renames the alias to *_1 instead of erroring,
            // and dedup order would follow file DATA) — reject that one case loudly.
            // (Schema probe only — LIMIT 0 reads header/metadata, no rows.)
            if (includeFileRowNumber
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
                c.Name.Equals(options.Table.RawSourceColumn, StringComparison.OrdinalIgnoreCase)
                    && !options.CaptureRawSource
                    ? "NULL"
                    : textualSources.Contains(c.Name) && !IsTextualType(c.DuckDbType)
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
                    ({options.Table.QuotedColumnList}, "{BookkeepingColumns.PrimaryKey}", "{BookkeepingColumns.RowHash}", "{BookkeepingColumns.ReplicationHash}", "_SourceModified")
                SELECT {sourceColumns},
                       {PrimaryKeyExpression(options)},
                       NULL,
                       NULL,
                       {sourceModified}
                FROM ({projection}) AS s
                """);

            store.Execute(
                $"""
                UPDATE {staging.QualifiedName}
                SET "{BookkeepingColumns.RowHash}" = {RowHash.Expression(options.Table.Columns.Select(c => c.Name))},
                    "{BookkeepingColumns.ReplicationHash}" = {RowHash.Expression(options.Table.ReplicationColumns.Select(c => c.Name))}
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

        var merge = SnapshotMerge.Execute(store, options.Table, staging, options.MergeOptions);

        // Stamp ONLY on success. A failed, aborted or guardrail-tripped run must leave the previous
        // stamp alone so the next cycle reads again — stamping any other outcome would let the gate
        // skip a file that was never actually ingested.
        //
        // And stamp the metadata read BEFORE the ingest, never a fresh probe: it describes the file
        // whose bytes are now in the table. Re-probing here would record a file rewritten DURING the
        // read as already ingested — the one shape of miss this gate must not be able to produce.
        if (options.ChangeGate is not null
            && merge.Status == SnapshotMergeStatus.Succeeded
            && gateDecision is { Verdict: not SourceChangeVerdict.MetadataUnavailable })
        {
            store.WriteSourceFileStamp(new SourceFileStamp(
                StampKey(options.MergeOptions),
                options.FilePath,
                gateDecision.Metadata.Length,
                gateDecision.Metadata.LastWriteUtc,
                SourceConfigFingerprint.Compute(options, options.IngestVersion),
                options.ChangeGate.TimeProvider.GetUtcNow().UtcDateTime));
        }

        return merge;
    }

    /// <summary>
    /// The stamp's identity. <see cref="SnapshotMergeOptions.Source"/> alone is normally unique, but
    /// the per-dealer pattern (many sources, one shared table) distinguishes itself by scope — so
    /// the scope is folded in and two dealers can never overwrite each other's stamp.
    /// </summary>
    /// <remarks>
    /// Length-prefixed rather than delimiter-joined, matching the encoding
    /// <see cref="PrimaryKeyExpression"/> already uses for composite keys. Source keys legitimately
    /// contain <c>/</c> (<c>dms-order-lines/AAD</c>), so a plain join could alias
    /// (<c>a/b</c> + <c>c</c>) onto (<c>a</c> + <c>b/c</c>) and let two sources share one stamp.
    /// </remarks>
    internal static string StampKey(SnapshotMergeOptions mergeOptions)
    {
        if (string.IsNullOrWhiteSpace(mergeOptions.SourceScope))
            return mergeOptions.Source;

        var source = mergeOptions.Source;
        var scope = mergeOptions.SourceScope;
        return $"V{source.Length}:{source};V{scope.Length}:{scope};";
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

    private static string BuildTypedProjection(FileSnapshotIngestorOptions options, string reader)
    {
        var bindings = (options.ColumnBindings ?? [])
            .ToDictionary(binding => binding.TargetColumn, StringComparer.OrdinalIgnoreCase);
        var occurrence = options.OccurrenceRowIdentity;
        var externalSourceModified = options.SourceModifiedColumn is { } sourceModified
                                     && !options.Table.Columns.Any(column => column.Name.Equals(
                                         sourceModified, StringComparison.OrdinalIgnoreCase))
            ? sourceModified
            : null;

        string BoundColumn(SnapshotColumn column)
        {
            if (column.Name.Equals(options.Table.RawSourceColumn, StringComparison.OrdinalIgnoreCase)
                && !options.CaptureRawSource)
            {
                return $"NULL AS {QuoteIdentifier(column.Name)}";
            }

            var binding = bindings.GetValueOrDefault(column.Name)
                          ?? new FileColumnBinding(column.Name, column.Name);
            var source = $"s.{QuoteIdentifier(binding.SourceColumn)}";
            var expression = binding.Normalization switch
            {
                FileValueNormalization.None => source,
                FileValueNormalization.Trim => KeyTrim($"CAST({source} AS VARCHAR)"),
                FileValueNormalization.TrimUpperInvariant =>
                    $"upper({KeyTrim($"CAST({source} AS VARCHAR)")})",
                _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown file value normalization."),
            };
            return $"{expression} AS {QuoteIdentifier(column.Name)}";
        }

        if (occurrence is null)
        {
            var columns = options.Table.Columns.Select(BoundColumn).ToList();
            if (externalSourceModified is not null)
            {
                columns.Add(
                    $"s.{QuoteIdentifier(externalSourceModified)} AS {QuoteIdentifier(externalSourceModified)}");
            }
            return $"SELECT {string.Join(", ", columns)} FROM {reader} AS s";
        }

        var sourceColumns = options.Table.Columns
            .Where(column => !column.Name.Equals(occurrence.OrdinalColumn, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var boundColumns = string.Join(", ", sourceColumns.Select(BoundColumn));
        var projectedColumns = string.Join(", ", sourceColumns.Select(column => $"s.{QuoteIdentifier(column.Name)}"));
        if (externalSourceModified is not null)
        {
            boundColumns +=
                $", s.{QuoteIdentifier(externalSourceModified)} AS {QuoteIdentifier(externalSourceModified)}";
            projectedColumns += $", s.{QuoteIdentifier(externalSourceModified)}";
        }
        var partition = string.Join(", ", occurrence.GroupParts.Select(part =>
            NormalizeKeyPart($"s.{QuoteIdentifier(part.Column)}", part.Normalization)));

        return
            $"WITH \"hawta$bound\" AS (" +
            $"SELECT {boundColumns}, s.{QuoteIdentifier(FileRowNumberColumn)} AS {QuoteIdentifier(FileRowNumberColumn)} " +
            $"FROM {reader} AS s) " +
            $"SELECT {projectedColumns}, " +
            $"row_number() OVER (PARTITION BY {partition} ORDER BY s.{QuoteIdentifier(FileRowNumberColumn)}) " +
            $"AS {QuoteIdentifier(occurrence.OrdinalColumn)} FROM \"hawta$bound\" AS s";
    }

    private static string PrimaryKeyExpression(FileSnapshotIngestorOptions options)
    {
        if (options.OccurrenceRowIdentity is { } occurrence)
        {
            var encodedGroup = occurrence.GroupParts.Select(part =>
            {
                var value = NormalizeKeyPart(
                    $"CAST(s.{QuoteIdentifier(part.Column)} AS VARCHAR)", part.Normalization);
                return $"CASE WHEN {value} IS NULL THEN 'N;' " +
                       $"ELSE concat('V', length({value}), ':', {value}, ';') END";
            });
            var ordinal = $"s.{QuoteIdentifier(occurrence.OrdinalColumn)}";
            return $"CASE WHEN {ordinal} IS NULL OR {ordinal} <= 0 THEN NULL ELSE " +
                   $"concat({string.Join(", ", encodedGroup)}, 'O', " +
                   $"lpad(CAST({ordinal} AS VARCHAR), 20, '0')) END";
        }

        if (options.LogicalKey is null)
            return $"nullif({KeyTrim($"CAST(s.\"{options.PrimaryKeyColumn}\" AS VARCHAR)")}, '')";

        if (string.IsNullOrEmpty(options.LogicalKey.Separator))
            throw new ArgumentException("A composite logical-key separator cannot be empty.", nameof(options));

        var parts = options.LogicalKey.Parts.Select(part =>
            $"nullif({NormalizeKeyPart($"CAST(s.{QuoteIdentifier(part.Column)} AS VARCHAR)", part.Normalization)}, '')")
            .ToList();

        if (parts.Count == 1)
            return parts[0];

        var separator = SqlLiteral(options.LogicalKey.Separator);
        var blankGuard = string.Join(" OR ", parts.Select(part => $"{part} IS NULL"));
        var ambiguityGuard = string.Join(" OR ", parts.Select(part => $"contains({part}, '{separator}')"));
        return $"CASE WHEN {blankGuard} THEN NULL " +
               $"WHEN {ambiguityGuard} THEN error('Logical-key component contains reserved separator {separator}') " +
               $"ELSE concat_ws('{separator}', {string.Join(", ", parts)}) END";
    }

    private static string NormalizeKeyPart(string expression, FileKeyNormalization normalization) =>
        normalization switch
        {
            FileKeyNormalization.Trim => KeyTrim(expression),
            FileKeyNormalization.TrimUpperInvariant => $"upper({KeyTrim(expression)})",
            _ => throw new ArgumentOutOfRangeException(nameof(normalization)),
        };

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

    private static string RawCapturedCsvReader(
        string filePath,
        CsvReadOptions csv,
        string parsedReader,
        string targetColumn)
    {
        var path = GlobEscape(SqlLiteral(filePath));
        var headerOffset = csv.HasHeader ? 1 : 0;
        return
            $$"""
            (WITH parsed AS (
                SELECT *, row_number() OVER () AS "hawta$parsed_row" FROM {{parsedReader}}
            ), raw AS (
                SELECT rawline, row_number() OVER () AS "hawta$raw_row"
                FROM (
                    SELECT rawline
                    FROM read_csv('{{path}}', columns = {'rawline': 'VARCHAR'},
                                  delim = chr(30), header = false, quote = '', escape = '',
                                  all_varchar = true, nullstr = chr(1))
                    WHERE rawline IS NOT NULL AND rawline <> ''
                )
            ), alignment AS (
                SELECT CASE WHEN (SELECT count(*) FROM raw) <> (SELECT count(*) FROM parsed) + {{headerOffset}}
                    THEN error('Raw-source audit capture could not align physical CSV lines with parsed rows')
                    ELSE 1 END AS ok
            )
            SELECT parsed.* EXCLUDE ("hawta$parsed_row"), raw.rawline AS "{{targetColumn}}"
            FROM parsed
            JOIN raw ON raw."hawta$raw_row" = parsed."hawta$parsed_row" + {{headerOffset}}
            JOIN alignment ON alignment.ok = 1)
            """;
    }

    private static string SqlLiteral(string value) => value.Replace("'", "''");

    private static string QuoteIdentifier(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

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

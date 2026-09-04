using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A source whose input is the snapshot itself: DuckDB SQL over other snapshot tables, staged,
/// hashed and merged exactly like a file or view source. This is how a SERVING table — a table
/// shaped like the canonical model a consumer reads (a Cosmos family, a lookup, a report) — is
/// derived from the source-shaped tables without a second interpreter in C#.
/// </summary>
public sealed class ProjectionSnapshotIngestorOptions
{
    /// <summary>The table this projection fills. Typed from the model it serves, so column drift fails at staging.</summary>
    public required SnapshotTableDefinition Table { get; init; }

    /// <summary>
    /// The projection, as DuckDB SQL. Every input table is referenced by placeholder —
    /// <c>{DmsOrderLine}</c>, <c>{VehiclesDbVehicle}</c> — and each placeholder resolves to that
    /// table's LIVE rows (tombstones excluded), read from the write database when the table is
    /// Resident and straight from its published parquet when it is Deferred. The SELECT must
    /// return every column of <see cref="Table"/> by name; a missing or misnamed column fails
    /// the run at staging, before any row lands.
    /// </summary>
    public required string SelectSql { get; init; }

    /// <summary>
    /// The snapshot tables the SQL reads. Every one must appear as a placeholder in
    /// <see cref="SelectSql"/>, and every placeholder must name one of these — the list is what the
    /// change gate watches, so an input the gate does not know about would be a silent staleness.
    /// </summary>
    public required IReadOnlyList<SnapshotTableDefinition> Inputs { get; init; }

    /// <summary>Projected column whose trimmed text becomes <c>_PrimaryKey</c>. Exactly one of this or <see cref="LogicalKey"/>.</summary>
    public string? PrimaryKeyColumn { get; init; }

    /// <summary>Composite identity over projected columns, same contract as a file source's logical key.</summary>
    public FileLogicalKey? LogicalKey { get; init; }

    /// <summary>
    /// Optional projected column carrying the row's own change time (becomes <c>_SourceModified</c>).
    /// May be a column the SELECT emits but the table does not store — a projection normally
    /// hands through its inputs' <c>_LastModified</c> here, so the serving row's freshness is the
    /// source's, not the run clock's.
    /// </summary>
    public string? SourceModifiedColumn { get; init; }

    /// <summary>
    /// Operator lever folded into the gate fingerprint: change it to force one re-projection with
    /// no deploy. Same role as the file gate's ingest version.
    /// </summary>
    public string? IngestVersion { get; init; }

    /// <summary>
    /// Skip the run when no input's change sequence has moved since this projection's last
    /// successful merge. On by default — it is what makes running a projection every cycle free.
    /// </summary>
    public bool GateOnInputChanges { get; init; } = true;

    public required SnapshotMergeOptions MergeOptions { get; init; }
}

/// <summary>What the gate remembers about a projection's last successful merge.</summary>
public sealed record ProjectionStamp(
    string SourceKey,
    long InputWatermark,
    string ConfigFingerprint,
    DateTime StampedAtUtc);

public static class ProjectionSnapshotIngestor
{
    private static readonly Regex PlaceholderPattern = new(@"\{([A-Za-z][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    public static SnapshotMergeResult Ingest(SnapshotStore store, ProjectionSnapshotIngestorOptions options)
    {
        Validate(options);

        var mergeOptions = options.MergeOptions;
        var stampKey = FileSnapshotIngestor.StampKey(mergeOptions);
        var fingerprint = Fingerprint(options);

        // ---- Change gate: the inputs' change sequences are the whole question --------------
        // A merge that inserts, updates, resurrects, adopts or tombstones a row advances the
        // store-wide sequence; nothing else does. So "has any input changed since I last ran" is
        // one max() per input, and a projection can sit on every cadence tick for the price of
        // reading a handful of column statistics.
        var watermark = options.Inputs.Max(input => store.ReadChangeSequenceWatermark(input));
        if (options.GateOnInputChanges)
        {
            var stamp = store.ReadProjectionStamp(stampKey);
            if (stamp is not null
                && string.Equals(stamp.ConfigFingerprint, fingerprint, StringComparison.Ordinal)
                && stamp.InputWatermark == watermark)
            {
                var runId = mergeOptions.RunId ?? Guid.NewGuid().ToString("N");
                var skipped = new SnapshotMergeResult(runId, SnapshotMergeStatus.SkippedSourceUnchanged, 0, 0, 0, 0);
                SnapshotMerge.InsertRunRecord(store, options.Table, mergeOptions, runId, DateTime.UtcNow, skipped,
                    $"No input has changed since the last successful projection (input change sequence {watermark}, " +
                    $"stamped {stamp.StampedAtUtc:O}). Nothing projected.");
                return skipped;
            }
        }

        var projection = ResolveSql(store, options);
        var staging = store.CreateStagingTable(options.Table);

        try
        {
            // Selecting each table column BY NAME from the projection is the contract tripwire:
            // a column the SQL forgot, misnamed or mistyped fails here, loudly, before any row lands.
            var sourceColumns = string.Join(", ", options.Table.Columns.Select(c => $"s.\"{c.Name}\""));
            var sourceModified = options.SourceModifiedColumn is { } modified ? $"s.\"{modified}\"" : "NULL";

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
        }
        catch (Exception exception)
        {
            // The run record IS the alarm surface: a projection whose SQL no longer binds against
            // its inputs (a renamed source column, a dropped input) must be visible in meta.SyncRuns.
            var runId = mergeOptions.RunId ?? Guid.NewGuid().ToString("N");
            try
            {
                SnapshotMerge.InsertRunRecord(store, options.Table, mergeOptions, runId, DateTime.UtcNow,
                    new SnapshotMergeResult(runId, SnapshotMergeStatus.Failed, 0, 0, 0, 0),
                    exception.Message);
            }
            catch { /* recording must never mask the original failure */ }

            throw;
        }

        // No zero-row "presumed torn" skip here, deliberately. A projection reads LOCAL tables
        // whose own ingest already stood behind the torn-source guards; zero projected rows is
        // the truth of those inputs (no closed orders yet, say). The merge's wipes-entire-scope
        // guardrail still refuses to tombstone a whole populated serving table in one step, and
        // ForceDeletes remains the intentional path — so an emptied input never silently empties
        // its consumers either.
        var merge = SnapshotMerge.Execute(store, options.Table, staging, mergeOptions);

        // Stamp ONLY on success, with the watermark read BEFORE the projection ran — an input
        // merged mid-run is above it and re-projects next cycle, never below it and lost.
        if (merge.Status == SnapshotMergeStatus.Succeeded)
            store.WriteProjectionStamp(new ProjectionStamp(stampKey, watermark, fingerprint, DateTime.UtcNow));

        return merge;
    }

    /// <summary>
    /// The SQL with every placeholder resolved to its input's live relation — exposed so a harness
    /// can print, explain or run exactly what the ingestor runs.
    /// </summary>
    public static string ResolveSql(SnapshotStore store, ProjectionSnapshotIngestorOptions options)
    {
        var inputs = options.Inputs.ToDictionary(input => input.Name, StringComparer.OrdinalIgnoreCase);
        return PlaceholderPattern.Replace(options.SelectSql, match =>
        {
            var name = match.Groups[1].Value;
            if (!inputs.TryGetValue(name, out var input))
                throw new ArgumentException(
                    $"Projection '{options.MergeOptions.Source}' references {{{name}}}, which is not one of its declared inputs " +
                    $"({string.Join(", ", options.Inputs.Select(i => i.Name))}).");
            return store.LiveRowsRelation(input);
        });
    }

    /// <summary>
    /// What a stamp remembers about the configuration: the SQL text, the target and input shapes,
    /// the identity recipe, Hawta's own version and the manual lever. Any change re-projects once.
    /// </summary>
    public static string Fingerprint(ProjectionSnapshotIngestorOptions options)
    {
        var builder = new StringBuilder();

        void Add(string label, object? value)
        {
            builder.Append(label).Append('=');
            builder.Append(value switch
            {
                null => "\0",
                bool flag => flag ? "1" : "0",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString(),
            });
            builder.Append('');
        }

        Add("code", HawtaVersion);
        Add("manual", string.IsNullOrWhiteSpace(options.IngestVersion) ? null : options.IngestVersion.Trim());
        Add("table", options.Table.Name);
        foreach (var column in options.Table.Columns)
            Add("col", $"{column.Name}:{column.DuckDbType}");
        foreach (var column in options.Table.ReplicationColumns)
            Add("replCol", column.Name);
        foreach (var input in options.Inputs)
        {
            Add("input", input.Name);
            foreach (var column in input.Columns)
                Add("inputCol", $"{input.Name}.{column.Name}:{column.DuckDbType}");
        }
        Add("selectSql", options.SelectSql);
        Add("primaryKeyColumn", options.PrimaryKeyColumn);
        if (options.LogicalKey is { } logicalKey)
        {
            Add("keySeparator", logicalKey.Separator);
            foreach (var part in logicalKey.Parts)
                Add("keyPart", $"{part.Column}:{part.Normalization}");
        }
        Add("sourceModifiedColumn", options.SourceModifiedColumn);
        Add("sourceScope", options.MergeOptions.SourceScope);
        Add("recordIdentityKind", options.MergeOptions.RecordIdentityKind);
        Add("deletesEnabled", options.MergeOptions.DeletesEnabled);
        Add("forceDeletes", options.MergeOptions.ForceDeletes);
        Add("forceAdoptions", options.MergeOptions.ForceAdoptions);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static readonly string HawtaVersion =
        typeof(ProjectionSnapshotIngestor).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ProjectionSnapshotIngestor).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    private static void Validate(ProjectionSnapshotIngestorOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SelectSql);
        if (options.Inputs.Count == 0)
            throw new ArgumentException("A projection needs at least one input table.", nameof(options));

        var identityCount = (options.LogicalKey is null ? 0 : 1) + (string.IsNullOrWhiteSpace(options.PrimaryKeyColumn) ? 0 : 1);
        if (identityCount != 1)
            throw new ArgumentException("Configure exactly one of PrimaryKeyColumn or LogicalKey.", nameof(options));

        var referenced = PlaceholderPattern.Matches(options.SelectSql)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var declared = options.Inputs.Select(input => input.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var undeclared = referenced.Where(name => !declared.Contains(name)).Order(StringComparer.OrdinalIgnoreCase).ToList();
        if (undeclared.Count > 0)
        {
            throw new ArgumentException(
                $"Projection '{options.MergeOptions.Source}' references {string.Join(", ", undeclared.Select(name => $"{{{name}}}"))}, " +
                $"which is not among its declared inputs ({string.Join(", ", options.Inputs.Select(i => i.Name))}). " +
                "Every table the SQL reads must be declared, because the declared inputs are what the change gate watches.",
                nameof(options));
        }

        var unreferenced = options.Inputs
            .Where(input => !referenced.Contains(input.Name))
            .Select(input => input.Name)
            .ToList();
        if (unreferenced.Count > 0)
        {
            throw new ArgumentException(
                $"Projection '{options.MergeOptions.Source}' declares input(s) its SQL never references: " +
                $"{string.Join(", ", unreferenced)}. Reference them as {{Name}} or drop them — a declared input " +
                "the SQL does not read would gate the projection on a table that cannot change its output.",
                nameof(options));
        }

        var duplicates = options.Inputs
            .GroupBy(input => input.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new ArgumentException($"Duplicate input table(s): {string.Join(", ", duplicates)}.", nameof(options));

        if (options.Inputs.Any(input => input.Name.Equals(options.Table.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("A projection cannot read the table it fills.", nameof(options));
    }

    private static string PrimaryKeyExpression(ProjectionSnapshotIngestorOptions options)
    {
        if (options.LogicalKey is null)
            return $"nullif({FileSnapshotIngestor.KeyTrim($"CAST(s.\"{options.PrimaryKeyColumn}\" AS VARCHAR)")}, '')";

        if (string.IsNullOrEmpty(options.LogicalKey.Separator))
            throw new ArgumentException("A composite logical-key separator cannot be empty.", nameof(options));

        var parts = options.LogicalKey.Parts.Select(part =>
            {
                var value = $"CAST(s.\"{part.Column}\" AS VARCHAR)";
                var normalized = part.Normalization switch
                {
                    FileKeyNormalization.Trim => FileSnapshotIngestor.KeyTrim(value),
                    FileKeyNormalization.TrimUpperInvariant => $"upper({FileSnapshotIngestor.KeyTrim(value)})",
                    _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown key normalization."),
                };
                return $"nullif({normalized}, '')";
            })
            .ToList();

        if (parts.Count == 1)
            return parts[0];

        var separator = options.LogicalKey.Separator.Replace("'", "''");
        var blankGuard = string.Join(" OR ", parts.Select(part => $"{part} IS NULL"));
        var ambiguityGuard = string.Join(" OR ", parts.Select(part => $"contains({part}, '{separator}')"));
        return $"CASE WHEN {blankGuard} THEN NULL " +
               $"WHEN {ambiguityGuard} THEN error('Logical-key component contains reserved separator {separator}') " +
               $"ELSE concat_ws('{separator}', {string.Join(", ", parts)}) END";
    }
}

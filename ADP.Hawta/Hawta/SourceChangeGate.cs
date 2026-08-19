using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>What a file source looked like at its last SUCCESSFUL merge.</summary>
public sealed record SourceFileStamp(
    string SourceKey,
    string FilePath,
    long Length,
    DateTime LastWriteUtc,
    string ConfigFingerprint,
    DateTime StampedAtUtc);

/// <summary>Why the gate let a source through, for the run record and for operators.</summary>
public enum SourceChangeVerdict
{
    /// <summary>File and configuration both match the stamp — skip the read entirely.</summary>
    Unchanged,

    /// <summary>No stamp: never ingested, or the write DB was rebuilt.</summary>
    NoStamp,

    /// <summary>Length or last-write time differs from the stamp.</summary>
    FileChanged,

    /// <summary>The source's declared path is not the one that was stamped.</summary>
    PathChanged,

    /// <summary>Configuration, Hawta's own version, or the manual ingest version changed.</summary>
    ConfigurationChanged,

    /// <summary>The stamp is older than <see cref="SourceChangeGate.ReingestAfter"/>.</summary>
    TrustExpired,

    /// <summary>Metadata could not be established. Never a skip — the read decides.</summary>
    MetadataUnavailable,
}

/// <param name="StampAge">
/// How long since this source was last ACTUALLY read, or null when there is no stamp. With no
/// re-ingest bound configured — the default — this is the only thing that makes a long-unread feed
/// visible, so it is carried into the run record rather than left implicit.
/// </param>
public sealed record SourceChangeDecision(
    SourceChangeVerdict Verdict, FileMetadata Metadata, TimeSpan? StampAge = null)
{
    public bool ShouldSkip => Verdict is SourceChangeVerdict.Unchanged;

    /// <summary>Operator-facing sentence for the run record.</summary>
    public string Describe(string filePath) => Verdict switch
    {
        SourceChangeVerdict.Unchanged =>
            $"Source unchanged since the last successful merge ({Metadata.Length} bytes, " +
            $"modified {Metadata.LastWriteUtc:O}): {filePath}. Nothing read; " +
            $"last actually read {Describe(StampAge)} ago.",
        SourceChangeVerdict.NoStamp => "No previous successful merge recorded for this source.",
        SourceChangeVerdict.FileChanged => "Source file's length or modification time changed.",
        SourceChangeVerdict.PathChanged => "Source file path changed since the last successful merge.",
        SourceChangeVerdict.ConfigurationChanged =>
            "Ingest configuration, Hawta version, or the manual ingest version changed.",
        SourceChangeVerdict.TrustExpired =>
            $"Stamp is {Describe(StampAge)} old, past this source's re-ingest interval.",
        SourceChangeVerdict.MetadataUnavailable => "Source metadata could not be established.",
        _ => "Unrecognised verdict.",
    };

    private static string Describe(TimeSpan? age) => age switch
    {
        null => "an unknown time",
        { TotalDays: >= 1 } span => $"{span.TotalDays:F1} day(s)",
        { TotalHours: >= 1 } span => $"{span.TotalHours:F1} hour(s)",
        var span => $"{span.Value.TotalMinutes:F0} minute(s)",
    };
}

/// <summary>
/// Decides whether a file source can skip its read this cycle.
///
/// <para><b>Why this exists.</b> Every cadence tick otherwise re-reads the whole file, re-hashes
/// every row and re-runs the merge, even when the file is byte-identical. That was measured as
/// acceptable for ONE large file at a LOW cadence (the 409 MB JPM feed, ~7 s idle). It is not the
/// same question at TIQ's file count run at Hawta's cadence — and raising the cadence is the point
/// of Hawta, so this gate is what makes the higher frequency affordable rather than an optimization
/// of what runs today.</para>
///
/// <para><b>It skips on exactly one condition</b> — metadata <see cref="FileProbeStatus.Found"/>,
/// identical path, length and last-write time, an identical fingerprint, and a stamp inside
/// <see cref="ReingestAfter"/>. Every other answer, including every failure, falls through to the
/// ordinary read. No condition here can turn an outage into a silent skip.</para>
///
/// <para><b>Size and time are not a content identity.</b> A producer that preserves timestamps
/// (<c>robocopy /COPY:T</c>, <c>rsync --times</c>, archive extraction) can change content while both
/// hold — and this estate has already observed the safe inverse, an mtime refresh with length and
/// SHA-256 unchanged. Hashing the content would cost the very read being avoided, so the mitigation
/// is <see cref="ReingestAfter"/>: a bounded window after which the file is re-read regardless.
/// "Invisible forever" becomes "invisible for at most N hours".</para>
/// </summary>
public sealed class SourceChangeGate
{
    /// <remarks>
    /// This carries <b>policy only</b>, deliberately. The probe is a per-cycle object — it caches so
    /// one cycle sees one consistent picture of the share — while the policy is configured once and
    /// lives on the source definition. Holding the probe here would force the whole source registry
    /// to be rebuilt every cycle just to refresh a cache.
    /// </remarks>
    public SourceChangeGate(TimeSpan? reingestAfter = null, TimeProvider? timeProvider = null)
    {
        if (reingestAfter is { } bound && bound <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(reingestAfter),
                "A re-ingest interval must be positive. Omit it entirely to never expire a stamp.");

        ReingestAfter = reingestAfter;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Re-read unconditionally once a stamp is older than this. <b>Null — the default — never
    /// expires a stamp.</b>
    ///
    /// <para><b>Why off by default.</b> A periodic re-read is a blind full read, not a detector: it
    /// costs the entire file every interval and reports nothing when nothing changed. JPM is 391 MiB
    /// and changes once or twice a year, so a six-hour bound reads roughly 570 GB annually to notice
    /// two changes. That is not a trade worth making by default, and it scales the wrong way with
    /// exactly the feeds that motivated the gate.</para>
    ///
    /// <para><b>What it guards, precisely.</b> A content change that leaves BOTH length and
    /// last-write time identical. On NTFS's 100 ns resolution that cannot happen by coincidence — it
    /// needs a deliberate timestamp-preserving actor: <c>robocopy /COPY:T</c>, <c>rsync -t</c>,
    /// <c>cp -p</c>, a restore-over-file, or a producer stamping data-as-of rather than write time.
    /// Such an actor behaves that way consistently, so the symptom is "this feed stopped updating",
    /// which is noticed rather than silently absorbed.</para>
    ///
    /// <para><b>Know what does NOT cover you.</b> <c>SnapshotRecon</c> compares the snapshot against
    /// COSMOS. A missed source read leaves both tiers stale together, so recon reports agreement.
    /// Nothing automatic detects this; the fix is
    /// <see cref="FileSnapshotIngestorOptions.IngestVersion"/>, which forces a re-read with no
    /// deploy. Set a bound per source where a feed is small enough that the read costs nothing and
    /// its producer is not trusted.</para>
    /// </summary>
    public TimeSpan? ReingestAfter { get; }

    public TimeProvider TimeProvider { get; }

    /// <param name="reingestAfter">
    /// Per-source override of <see cref="ReingestAfter"/>. Null uses the gate's own setting — which
    /// is itself normally null, i.e. never expire. A per-source bound is the right shape for this
    /// knob: the cost of the periodic read is entirely a property of the individual feed.
    /// </param>
    public SourceChangeDecision Evaluate(
        SnapshotStore store, FileMetadataProbe probe, string sourceKey, string filePath, string fingerprint,
        TimeSpan? reingestAfter = null)
    {
        var probed = probe.Read(filePath);
        if (probed.Status is not FileProbeStatus.Found)
        {
            // Absent is handled by the ingestor's own guard (it must record SourceAbsent, not
            // tombstone the family); Unavailable must never read as "nothing changed".
            return new SourceChangeDecision(SourceChangeVerdict.MetadataUnavailable, default);
        }

        var stamp = store.ReadSourceFileStamp(sourceKey);
        if (stamp is null)
            return new SourceChangeDecision(SourceChangeVerdict.NoStamp, probed.Metadata);

        if (!string.Equals(stamp.ConfigFingerprint, fingerprint, StringComparison.Ordinal))
            return new SourceChangeDecision(SourceChangeVerdict.ConfigurationChanged, probed.Metadata);

        if (!FileMetadataProbe.PathComparer.Equals(stamp.FilePath, filePath))
            return new SourceChangeDecision(SourceChangeVerdict.PathChanged, probed.Metadata);

        var stampAge = TimeProvider.GetUtcNow().UtcDateTime - stamp.StampedAtUtc;
        if ((reingestAfter ?? ReingestAfter) is { } bound && stampAge >= bound)
            return new SourceChangeDecision(SourceChangeVerdict.TrustExpired, probed.Metadata, stampAge);

        var unchanged = stamp.Length == probed.Metadata.Length
                        && stamp.LastWriteUtc == probed.Metadata.LastWriteUtc;

        return new SourceChangeDecision(
            unchanged ? SourceChangeVerdict.Unchanged : SourceChangeVerdict.FileChanged,
            probed.Metadata,
            stampAge);
    }
}

/// <summary>
/// The fingerprint that makes the gate notice <b>our own</b> changes.
///
/// <para>Size and time answer "did the file change". They cannot answer "would we produce a
/// different result from the same file", and a gate that only asks the first question silently
/// defers every deploy that changes the second — a new column binding, a changed projection, a
/// flipped delete flag — until that feed happens to be rewritten. Which may be days.</para>
///
/// <para>Three components, doing three different jobs:</para>
/// <list type="number">
///   <item><b>Configuration</b> — every declarative option that changes what the merge produces.
///     Automatic, and the common case.</item>
///   <item><b>Code</b> — Hawta's own assembly version. Configuration is byte-identical when someone
///     fixes a parsing bug in the ingestor or changes how row hashes are computed, so no
///     configuration hash can see it. Costs one extra full read per package bump, which App Service
///     already pays anyway: a slot swap hands the new instance an empty local disk, so the stamp
///     table starts empty regardless.</item>
///   <item><b>Manual</b> — an operator-supplied version string. The only lever that forces a re-read
///     <i>without a deploy</i>, which is exactly what you want when the fix is a configuration
///     change you cannot express, or when you simply do not trust the stamps.</item>
/// </list>
///
/// <para><b>Deliberately excluded:</b> the mass-delete and mass-adoption guardrail thresholds. Those
/// change whether a merge ABORTS, not what it produces on success — and an aborted merge writes no
/// stamp, so the next cycle re-reads on its own. Including them would only cause spurious re-reads.</para>
/// </summary>
public static class SourceConfigFingerprint
{
    /// <summary>Unit separator — no configuration value can contain it, so fields cannot alias.</summary>
    private const char Separator = '';

    private static readonly string HawtaVersion =
        typeof(SourceConfigFingerprint).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(SourceConfigFingerprint).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    /// <param name="ingestVersion">
    /// Operator-supplied. Any change forces every source carrying it to re-read once. Null or blank
    /// means "not in use" and contributes nothing.
    /// </param>
    public static string Compute(FileSnapshotIngestorOptions options, string? ingestVersion = null)
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
            builder.Append(Separator);
        }

        Add("code", HawtaVersion);
        Add("manual", string.IsNullOrWhiteSpace(ingestVersion) ? null : ingestVersion.Trim());

        // The table shape: a renamed, retyped, added or removed column changes both the projection
        // and the row hash, so the model IS part of the contract.
        Add("table", options.Table.Name);
        foreach (var column in options.Table.Columns)
            Add("col", $"{column.Name}:{column.DuckDbType}");
        Add("rawSourceColumn", options.Table.RawSourceColumn);
        foreach (var column in options.Table.ReplicationColumns)
            Add("replCol", column.Name);

        Add("path", options.FilePath);
        Add("format", options.Format);
        Add("delimiter", options.Csv.Delimiter);
        Add("hasHeader", options.Csv.HasHeader);
        Add("nullPadding", options.Csv.NullPadding);
        foreach (var name in options.Csv.ColumnNames ?? [])
            Add("csvName", name);

        Add("selectSql", options.SelectSql);
        foreach (var binding in options.ColumnBindings ?? [])
            Add("binding", $"{binding.TargetColumn}<-{binding.SourceColumn}:{binding.Normalization}");

        Add("fileRowNumber", options.IncludeFileRowNumber);
        Add("captureRawSource", options.CaptureRawSource);
        Add("sourceModifiedColumn", options.SourceModifiedColumn);
        Add("primaryKeyColumn", options.PrimaryKeyColumn);

        if (options.LogicalKey is { } logicalKey)
        {
            Add("keySeparator", logicalKey.Separator);
            foreach (var part in logicalKey.Parts)
                Add("keyPart", $"{part.Column}:{part.Normalization}");
        }

        if (options.OccurrenceRowIdentity is { } occurrence)
        {
            Add("ordinalColumn", occurrence.OrdinalColumn);
            foreach (var part in occurrence.GroupParts)
                Add("groupPart", $"{part.Column}:{part.Normalization}");
        }

        // Merge behaviour that changes the OUTPUT. Guardrail thresholds are excluded on purpose —
        // see the type remarks.
        Add("sourceScope", options.MergeOptions.SourceScope);
        Add("recordIdentityKind", options.MergeOptions.RecordIdentityKind);
        Add("deletesEnabled", options.MergeOptions.DeletesEnabled);
        Add("forceDeletes", options.MergeOptions.ForceDeletes);
        Add("forceAdoptions", options.MergeOptions.ForceAdoptions);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}

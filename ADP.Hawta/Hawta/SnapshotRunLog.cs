using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Settings for the run-log exporter. Null on <see cref="SnapshotAgentOptions.RunLog"/> keeps the
/// run log off, which is the incumbent behaviour.
/// </summary>
public sealed class SnapshotRunLogOptions
{
    /// <summary>
    /// Where the run log is written: a local directory, or a blob container of its own. It must
    /// never be the published set's location. The published set has consumers, a manifest,
    /// retention and a cold start, and the run log must not gain any of those by accident.
    ///
    /// <para>The store is used for the reachability check, folder creation and the local staging
    /// promote. The parquet itself is written by DuckDB through <c>COPY … TO</c>, exactly as the
    /// publisher writes its tables.</para>
    /// </summary>
    public required PublishStore Store { get; init; }

    /// <summary>
    /// The credential DuckDB uses for the run-log location when that location is a container
    /// reached with a DIFFERENT credential than the publish tier's. Null means the run log is
    /// reached with the store's own credential (<see cref="SnapshotAgentOptions.AzureConnectionString"/>),
    /// which is the normal shape: the same storage account, a container of its own.
    ///
    /// <para>When set, the loop registers it as a second DuckDB secret scoped to
    /// <see cref="PublishStore.Root"/>, so parquet written under that root uses this credential and
    /// everything else keeps using the publish one. Never logged: it carries the account key or SAS.</para>
    /// </summary>
    public string? AzureConnectionString { get; init; }

    /// <summary>
    /// How often the loop flushes. Never per cycle: a flush rewrites the day's file, so the cadence
    /// bounds both the write amplification and the tail lost on a hard kill.
    /// </summary>
    public TimeSpan Cadence { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The longest <c>Error</c> text written, in characters. The explanation of a skip or a failure
    /// is the point of the run log; the cap only bounds a runaway stack trace.
    /// </summary>
    public int ErrorTextLimit { get; init; } = 4_000;

    /// <summary>
    /// Which process wrote the file, supplied by the host: an instance id on a hosting platform,
    /// null on a developer machine. Written as a column, never as a path segment.
    /// </summary>
    public string? HostInstance { get; init; }

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(Store);
        if (Cadence <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Cadence), Cadence, "The run-log cadence must be positive.");
        if (ErrorTextLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(ErrorTextLimit), ErrorTextLimit, "The error text limit must be at least 1 character.");
    }
}

/// <param name="Skipped">True when no run table held a row newer than the previous flush. Nothing was written and the store was not touched.</param>
/// <param name="FilesWritten">The locations rewritten by this flush: one per run table per UTC day that gained rows.</param>
/// <param name="RowsWritten">Rows across every file written. A rewritten day file holds the whole day, so this counts the day's rows, not only the new ones.</param>
/// <param name="FlushedThrough">The watermark to pass as <c>lastFlushedThrough</c> next time: this flush's start time when something was written, the previous watermark when the flush was skipped.</param>
/// <param name="Elapsed">Wall-clock time the flush took.</param>
public sealed record SnapshotRunLogFlushResult(
    bool Skipped,
    IReadOnlyList<string> FilesWritten,
    long RowsWritten,
    DateTime? FlushedThrough,
    TimeSpan Elapsed);

/// <param name="Before">Rows recorded on UTC days before this one were eligible.</param>
/// <param name="RowsDeleted">How many rows each run table lost, by qualified table name. Zero for a table that had nothing eligible.</param>
/// <param name="Elapsed">Wall-clock time the prune took.</param>
public sealed record SnapshotRunLogPruneResult(
    DateOnly Before,
    IReadOnlyDictionary<string, long> RowsDeleted,
    TimeSpan Elapsed)
{
    /// <summary>Rows deleted across every run table.</summary>
    public long Total => RowsDeleted.Values.Sum();
}

/// <summary>
/// Copies the engine's run history out of the write DB as parquet, so it can be read outside the
/// process: <c>meta.SyncRuns</c> (every source run, including every skip and its reason),
/// <c>meta.PublishRuns</c> (every publish attempt), <c>meta.CycleRuns</c> (every loop cycle) and
/// <c>meta.PumpRuns</c> (every table's pump drain within a cycle).
///
/// <para><b>Why it exists.</b> The write DB lives on instance-local disk and a host may delete it at
/// every start, so the run history never left the process and was lost at every deploy. The
/// manifest carries only the newest run per source, and only when a publish commits. This exporter
/// is the fix, and it changes nothing about the published set: the run log is its own artifact in
/// its own location.</para>
///
/// <para><b>Facts only.</b> Every column is a recorded fact: the run tables as they are, plus
/// provenance (which estate, which process lifetime, which host instance, which package, when the
/// file was last rewritten). No threshold, no verdict, no health word. Judgement belongs to whatever
/// reads the log later.</para>
///
/// <para><b>Layout.</b> One file per run table per UTC day per process boot, under a hive-style day
/// folder: <c>sync-runs/date=2026-09-19/20260919061500123-3fa9c1e2.parquet</c>. The file name is
/// the boot id. Each flush rewrites every day file that gained rows with ALL of that day's rows
/// recorded by this boot, ordered by start time. The write DB is the day's buffer, so no watermark
/// state lives in the destination. A restart starts a new file, and two overlapping processes during
/// a deploy swap write two files: no gate, no rename, no conflict. The boot id never matches the
/// publisher's own file shape, so retention could not mistake a run-log file for a published one
/// even if the two shared a location, which by default they do not.</para>
///
/// <para><b>What it reads and what it skips.</b> Rows with <c>StartedAt</c> at or after the boot,
/// because a warm-reopened local estate may hold rows an earlier boot already wrote to that boot's
/// file. A flush is skipped when no table holds a row newer than the previous flush: the tables are
/// insert-only and stamped with the system clock by the merge and the publisher, so "newer" is
/// <c>coalesce(FinishedAt, StartedAt)</c> after the previous flush's start time. On a live estate
/// there is almost always something new; the guard exists so a paused or dark agent does not rewrite
/// identical files forever.</para>
///
/// <para><b>Runs inside the agent loop, on its thread.</b> The store is single-connection and the
/// loop is one caller at a time, so anything that reads the run tables runs in the cycle. This type
/// is a static function the loop calls; it opens nothing and creates no container. The location's
/// container is provisioned by an operator, like every other container the engine writes into.</para>
///
/// <para><b>The write DB is pruned; the destination never is.</b> Retention on the destination is
/// the operator's lifecycle rule; nothing here deletes a file. The run tables in the write DB, on
/// the other hand, only ever grow, so <see cref="Prune"/> deletes rows from UTC days before today
/// once they have been flushed, keeping the newest row per source, which the manifest reads. See
/// the method for the exact rule.</para>
/// </summary>
public static class SnapshotRunLog
{
    /// <summary>Folder for <c>meta.SyncRuns</c> under the run-log root.</summary>
    public const string SyncRunsFolder = "sync-runs";

    /// <summary>Folder for <c>meta.PublishRuns</c> under the run-log root.</summary>
    public const string PublishRunsFolder = "publish-runs";

    /// <summary>Folder for <c>meta.CycleRuns</c> under the run-log root.</summary>
    public const string CycleRunsFolder = "cycle-runs";

    /// <summary>Folder for <c>meta.PumpRuns</c> under the run-log root.</summary>
    public const string PumpRunsFolder = "pump-runs";

    /// <summary>The day folder's prefix. Hive style, so <c>read_parquet(…, hive_partitioning = true)</c> yields a <c>date</c> column and prunes on it.</summary>
    public const string PartitionPrefix = "date=";

    /// <summary>The DuckDB secret name for a run-log credential of its own. See <see cref="SnapshotRunLogOptions.AzureConnectionString"/>.</summary>
    internal const string SecretName = "hawta_run_log";

    private const string TimestampLiteral = "yyyy-MM-dd HH:mm:ss.ffffff";
    private const string DayLiteral = "yyyy-MM-dd";

    private static readonly string PackageVersion =
        typeof(SnapshotRunLog).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>
    /// The run tables the exporter copies: where each lands, the order its rows are written in, and
    /// whether it carries an <c>Error</c> column to shorten. The pump table has none: a drain that
    /// throws fails the cycle, and the cycle row carries that error.
    /// </summary>
    private static readonly RunTable[] Tables =
    [
        new("meta.SyncRuns", SyncRunsFolder, ["StartedAt", "RunId"]),
        new("meta.PublishRuns", PublishRunsFolder, ["StartedAt", "PublishId"]),
        new("meta.CycleRuns", CycleRunsFolder, ["StartedAt", "CycleId"]),
        new("meta.PumpRuns", PumpRunsFolder, ["StartedAt", "CycleId", "Table"], HasErrorColumn: false),
    ];

    private sealed record RunTable(string QualifiedName, string Folder, string[] OrderBy, bool HasErrorColumn = true);

    /// <summary>
    /// A new boot id: the boot time in the publisher's timestamp form, then eight random hex
    /// characters. The time part keeps files in chronological name order; the random part keeps two
    /// processes that boot in the same millisecond apart. It can never match the publisher's
    /// published-file shape, which is seventeen digits alone.
    /// </summary>
    public static string NewBootId(DateTime bootStartedAt) =>
        $"{bootStartedAt.ToString(SnapshotPublisher.TimestampFormat, CultureInfo.InvariantCulture)}-" +
        RandomNumberGenerator.GetHexString(8, lowercase: true);

    /// <summary>
    /// Registers the run log's own DuckDB credential, scoped to the run-log root, when the options
    /// carry one. Without it the run log is reached with the store's own credential. Applied by the
    /// loop right after the store opens, so the first flush finds it in place.
    /// </summary>
    internal static void ApplyCredential(SnapshotStore store, SnapshotRunLogOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AzureConnectionString) || !PublishPath.IsRemote(options.Store.Root))
            return;

        // Never interpolated into a log or an exception message: the value carries the account key
        // or SAS. DuckDB has no parameter binding for CREATE SECRET, so it is escaped instead. The
        // scope makes this secret win over the unscoped publish secret for paths under the run-log
        // root only; DuckDB picks the secret with the longest matching scope.
        store.Execute(
            $"CREATE OR REPLACE SECRET {SecretName} (TYPE azure, " +
            $"CONNECTION_STRING '{Sql(SnapshotStore.ExpandDevelopmentStorage(options.AzureConnectionString))}', " +
            $"SCOPE '{Sql(options.Store.Root)}')");
    }

    /// <summary>
    /// Flushes the run tables: rewrites every day file that gained rows since the previous flush.
    /// Returns what it wrote, or a skipped result when nothing was new. Throws when the destination
    /// cannot be written; the loop turns that into a warning, never into a failed cycle.
    /// </summary>
    /// <param name="store">The open write DB. Read on the caller's thread, which must be the loop's.</param>
    /// <param name="options">Where and how to write.</param>
    /// <param name="snapshotName">Which estate wrote the rows. A column, so several estates can share one reader.</param>
    /// <param name="bootId">This process lifetime's id (<see cref="NewBootId"/>). Also the file name.</param>
    /// <param name="bootStartedAt">System-clock UTC time the process booted. Rows older than this belong to an earlier boot's file and are not copied again.</param>
    /// <param name="lastFlushedThrough">The previous flush's <see cref="SnapshotRunLogFlushResult.FlushedThrough"/>, or null on the first flush of this boot.</param>
    public static SnapshotRunLogFlushResult Flush(
        SnapshotStore store,
        SnapshotRunLogOptions options,
        string snapshotName,
        string bootId,
        DateTime bootStartedAt,
        DateTime? lastFlushedThrough)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bootId);
        options.Validate();

        var stopwatch = Stopwatch.StartNew();

        // The system clock, not an injectable one, on purpose: the run tables are stamped with
        // DateTime.UtcNow by the merge and the publisher, and the "newer than" comparison below has
        // to speak the same clock. Cadence arithmetic in the loop uses its TimeProvider as usual.
        //
        // Cut to whole microseconds, which is the precision DuckDB stores. The watermark this
        // flush hands back is then exactly the value the file carries as FlushedAt, and exactly
        // the literal the next flush compares the run tables against.
        var flushedAt = TruncateToMicroseconds(DateTime.UtcNow);

        // First, which day files need rewriting. This reads only the write DB, so a flush with
        // nothing new costs no round trip to the destination at all.
        var work = new List<(RunTable Table, DateOnly Day)>();
        foreach (var table in Tables)
        {
            foreach (var day in DaysWithNewRows(store, table, bootStartedAt, lastFlushedThrough))
                work.Add((table, day));
        }

        if (work.Count == 0)
            return new SnapshotRunLogFlushResult(true, [], 0, lastFlushedThrough, stopwatch.Elapsed);

        // Loud before writing. A listing over an unreachable or missing container reports it as
        // empty; this is the one call that turns "not there" into a throw with the container's name
        // in it, which is what an operator needs to read.
        options.Store.EnsureReady();

        var files = new List<string>();
        var rows = 0L;
        foreach (var (table, day) in work)
        {
            var location = options.Store.Resolve(
                $"{table.Folder}/{PartitionPrefix}{day.ToString(DayLiteral, CultureInfo.InvariantCulture)}/{bootId}.parquet");
            rows += WriteDayFile(store, options, table, day, location, snapshotName, bootId, bootStartedAt, flushedAt);
            files.Add(location);
        }

        return new SnapshotRunLogFlushResult(false, files, rows, flushedAt, stopwatch.Elapsed);
    }

    /// <summary>The UTC days (by <c>StartedAt</c>) on which this table holds a row newer than the previous flush, since boot.</summary>
    private static IReadOnlyList<DateOnly> DaysWithNewRows(
        SnapshotStore store, RunTable table, DateTime bootStartedAt, DateTime? lastFlushedThrough)
    {
        // The day is read back as text rather than as a DATE value, so the result does not depend
        // on how the client library maps DuckDB dates.
        var newer = lastFlushedThrough is { } previous
            ? $" AND coalesce(\"FinishedAt\", \"StartedAt\") > {Timestamp(previous)}"
            : string.Empty;

        using var command = store.Connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT DISTINCT CAST(CAST("StartedAt" AS DATE) AS VARCHAR)
            FROM {table.QualifiedName}
            WHERE "StartedAt" >= {Timestamp(bootStartedAt)}{newer}
            ORDER BY 1
            """;

        var days = new List<DateOnly>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            days.Add(DateOnly.ParseExact(reader.GetString(0), DayLiteral, CultureInfo.InvariantCulture));
        return days;
    }

    /// <summary>
    /// Rewrites one day file with the whole day's rows recorded by this boot, and returns how many
    /// rows it holds. On a filesystem the bulk write lands on a staging name and is renamed, so a
    /// reader never sees a half-written file. On blob it targets the final name directly: the
    /// service makes the content appear all at once, and a reader that hits the zero-byte window
    /// fails loudly and retries, exactly as it does for the published set.
    /// </summary>
    private static long WriteDayFile(
        SnapshotStore store, SnapshotRunLogOptions options, RunTable table, DateOnly day, string location,
        string snapshotName, string bootId, DateTime bootStartedAt, DateTime flushedAt)
    {
        var orderBy = string.Join(", ", table.OrderBy.Select(column => $"\"{column}\""));
        var hostInstance = options.HostInstance is null ? "NULL::VARCHAR" : $"'{Sql(options.HostInstance)}'";
        var predicate =
            $"\"StartedAt\" >= {Timestamp(bootStartedAt)} " +
            $"AND CAST(\"StartedAt\" AS DATE) = DATE '{day.ToString(DayLiteral, CultureInfo.InvariantCulture)}'";

        options.Store.EnsureFolderFor(location);
        var destination = options.Store.BulkWriteNeedsStaging ? location + SnapshotPublisher.StagingSuffix : location;

        // Every column of the run table as it is, with only Error shortened in place where the
        // table has one, then the provenance columns. The values are written as escaped literals
        // rather than bound parameters because COPY statements do not take parameters. None of
        // them is a credential.
        var columns = table.HasErrorColumn
            ? $"* REPLACE (left(\"Error\", {options.ErrorTextLimit}) AS \"Error\")"
            : "*";
        store.Execute(
            $"""
            COPY (
                SELECT {columns},
                       '{Sql(snapshotName)}' AS "SnapshotName",
                       '{Sql(bootId)}' AS "BootId",
                       {hostInstance} AS "HostInstance",
                       '{Sql(PackageVersion)}' AS "PackageVersion",
                       {Timestamp(flushedAt)} AS "FlushedAt"
                FROM {table.QualifiedName}
                WHERE {predicate}
                ORDER BY {orderBy}
            ) TO '{Sql(destination)}' (FORMAT parquet, COMPRESSION zstd)
            """);

        if (options.Store.BulkWriteNeedsStaging)
            options.Store.PromoteStaged(destination, location);

        return Convert.ToInt64(store.ExecuteScalar($"SELECT count(*) FROM {table.QualifiedName} WHERE {predicate}"));
    }

    /// <summary>
    /// Deletes run rows the write DB no longer needs: rows recorded on a UTC day before
    /// <paramref name="today"/> that a flush has already copied out, except the newest row per
    /// source in <c>meta.SyncRuns</c>, which the publisher reads into the manifest. Nothing in the
    /// destination is touched; retention there is the operator's lifecycle rule.
    ///
    /// <para><b>Flushed</b> means what the exporter means by it: <c>coalesce(FinishedAt, StartedAt)</c>
    /// at or before <paramref name="flushedThrough"/>, the watermark of the last flush that wrote.
    /// A row newer than that is left for the next flush to copy first. A row from before this boot
    /// is older than any watermark, so it is eligible too: this process could not export it (the
    /// exporter copies rows since boot only), and the process that recorded it copied it at its own
    /// shutdown, or lost it then. Keeping it would leave a persistent write DB growing forever.</para>
    ///
    /// <para>A day file is rewritten only when its day gains a row, and a day before today cannot
    /// gain one, so the rows deleted here are never read by the exporter again. The loop calls this
    /// once per UTC day, after a cadence flush, on its own thread; a host that wipes its write DB at
    /// every start never has anything for it to delete.</para>
    /// </summary>
    /// <param name="store">The open write DB, on the loop's thread.</param>
    /// <param name="flushedThrough">The last watermark a flush handed back. Rows newer than it are kept.</param>
    /// <param name="today">The current UTC day. Rows recorded on this day are kept whatever their state.</param>
    public static SnapshotRunLogPruneResult Prune(SnapshotStore store, DateTime flushedThrough, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(store);
        var stopwatch = Stopwatch.StartNew();

        var eligible =
            $"CAST(\"StartedAt\" AS DATE) < DATE '{today.ToString(DayLiteral, CultureInfo.InvariantCulture)}' " +
            $"AND coalesce(\"FinishedAt\", \"StartedAt\") <= {Timestamp(flushedThrough)}";

        // The same partition and order the publisher uses to pick the newest run per source, so
        // what it reads after the prune is exactly what it read before.
        const string newestPerSource =
            """
            SELECT "RunId" FROM meta.SyncRuns
            QUALIFY row_number() OVER (PARTITION BY "Source" ORDER BY "StartedAt" DESC, "RunId" DESC) = 1
            """;

        var deleted = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in Tables)
        {
            var keep = table.QualifiedName == "meta.SyncRuns" ? $" AND \"RunId\" NOT IN ({newestPerSource})" : string.Empty;
            deleted[table.QualifiedName] = store.Execute($"DELETE FROM {table.QualifiedName} WHERE {eligible}{keep}");
        }

        return new SnapshotRunLogPruneResult(today, deleted, stopwatch.Elapsed);
    }

    private static string Timestamp(DateTime value) =>
        $"TIMESTAMP '{value.ToString(TimestampLiteral, CultureInfo.InvariantCulture)}'";

    /// <summary>A .NET time has 100-nanosecond ticks; a DuckDB TIMESTAMP has microseconds. Drop the extra digit.</summary>
    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);

    /// <summary>A value going into a SQL literal. DuckDB escapes a quote by doubling it.</summary>
    private static string Sql(string value) => value.Replace("'", "''");
}

using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// Deletes published sets beyond the retention depth, and any parquet no surviving manifest
/// references. Extracted from <see cref="SnapshotPublisher"/> so it can also run as a
/// standalone task on its own schedule — cleanup that only happens when a publish happens
/// stops happening entirely the moment publishing stalls, which is exactly when orphans
/// accumulate.
///
/// <para><b>Running this independently is only safe because of
/// <see cref="SnapshotRetentionOptions.MinimumAge"/>, so do not remove it.</b> A publish
/// commits its manifest LAST, so while one is in flight its parquet is on disk and no manifest
/// references it yet. To a sweeper that only asks "is this file referenced?", a publish in
/// progress is indistinguishable from an orphan — it would delete the set being written. The
/// age floor is what closes that window: nothing is deleted until it has been unreferenced
/// long enough that no publish could still be writing it.</para>
///
/// <para>The same floor is what replaces the held-open protection when the publish tier moves
/// to blob. On a filesystem, deleting a file a consumer has open throws and retention retries
/// next pass, so a reader mid-query is safe by accident. <b>Blobs cannot be held open</b> —
/// every delete succeeds immediately — so that protection does not weaken on blob, it vanishes
/// silently. Age is the portable replacement: keep an unreferenced file for longer than the
/// longest plausible consumer read.</para>
/// </summary>
public static class SnapshotRetention
{
    /// <summary>
    /// Sweeps one publish directory. Idempotent, and safe to run concurrently with a publish
    /// (that is the whole point of the age floor) — but it is still a writer, so callers that
    /// gate their publisher should gate this too rather than racing two deleters.
    /// </summary>
    public static SnapshotRetentionResult Sweep(SnapshotRetentionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var now = options.TimeProvider.GetUtcNow().UtcDateTime;
        var store = options.Store ?? new LocalPublishStore(options.PublishDirectory);

        // Loud before anything else. Every listing below reports an unreachable store as an EMPTY
        // one, and an empty store looks exactly like "nothing is published, so nothing to protect" —
        // which is the state in which a sweeper is most dangerous, not least.
        store.EnsureReady();

        // ONE listing for the whole sweep. It carries Length and LastWriteUtc per entry, so the age
        // floor costs no extra call — locally that is a minor saving, but on blob a per-file
        // GetProperties would be one remote round trip per candidate, per pass, forever.
        var inventory = store.List();
        var ageByLocation = inventory.ToDictionary(
            entry => entry.Location, entry => entry.LastWriteUtc, StringComparer.OrdinalIgnoreCase);

        var manifests = PublishedSnapshot.ListManifests(store, options.SnapshotName);

        var manifestsDeleted = 0;
        var skipped = 0;
        var heldByAge = 0;

        foreach (var old in manifests.Skip(options.KeepPublishes))
        {
            if (IsYoungerThanFloor(old, now, options, ageByLocation)) { heldByAge++; continue; }
            if (TryDelete(old, store, options.Delete)) manifestsDeleted++;
            else skipped++;
        }

        // Parquet cleanup assumes exclusive directory ownership. A manifest-shaped file under
        // another snapshot name means a second publisher shares this directory — never guess
        // at its references; skip the sweep and surface it.
        //
        // TOP LEVEL ONLY, deliberately. The local version got that from EnumerateFiles defaulting to
        // TopDirectoryOnly; blob listing is flat-and-recursive, so without the filter a nested
        // .json would read as a foreign manifest and disable parquet cleanup permanently — a
        // failure that presents as "retention silently stopped reclaiming space".
        var ownPattern = PublishedSnapshot.ManifestPattern(options.SnapshotName);
        var foreignManifestPresent = inventory
            .Where(entry => !entry.RelativePath.Contains('/'))
            .Select(entry => PublishPath.FileName(entry.Location))
            .Any(name => AnyManifestShape.IsMatch(name) && !ownPattern.IsMatch(name));
        if (foreignManifestPresent)
            return new SnapshotRetentionResult(manifestsDeleted, 0, skipped, heldByAge, CleanupSkipped: true);

        // The referenced set comes from EVERY manifest still on disk — kept ones, those whose
        // delete was skipped because a consumer holds them open, and those still inside the age
        // floor. A manifest that survives for any reason keeps its parquet alive; the set shrinks
        // by itself once the manifest can actually be removed. Only delete when every manifest is
        // readable and sane; guessing risks breaking a consumer mid-query.
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifestPath in PublishedSnapshot.ListManifests(store, options.SnapshotName))
        {
            try
            {
                foreach (var entry in PublishedSnapshot.Read(store, manifestPath).Tables)
                {
                    foreach (var file in entry.Location.Paths)
                    {
                        referenced.Add(file);
                        // Only folders a manifest actually names are swept. A table that dropped
                        // out of the configuration keeps its files rather than being guessed at.
                        var folder = file.Contains('/') ? file[..file.LastIndexOf('/')] : string.Empty;
                        if (folder.Length > 0)
                            folders.Add(folder);
                    }
                }
            }
            catch
            {
                return new SnapshotRetentionResult(manifestsDeleted, 0, skipped, heldByAge, CleanupSkipped: true);
            }
        }

        var parquetDeleted = 0;

        // Served from the single inventory taken up front rather than one listing per folder.
        // Entries exactly one level deep, in a folder a surviving manifest actually names: a
        // deeper path is not something this publisher writes, and guessing at it is how a sweep
        // starts deleting another tool's files.
        foreach (var candidate in inventory)
        {
            var separator = candidate.RelativePath.LastIndexOf('/');
            if (separator < 0)
                continue;

            var folder = candidate.RelativePath[..separator];
            if (folder.Contains('/') || !folders.Contains(folder))
                continue;

            var name = candidate.RelativePath[(separator + 1)..];
            if (!SnapshotPublisher.PublishedParquetShape.IsMatch(name))
                continue;

            // Referenced-set membership is by the manifest's own forward-slashed relative
            // path, so a file is only ever protected by the exact entry that names it.
            if (referenced.Contains(candidate.RelativePath))
                continue;

            if (IsYoungerThanFloor(candidate.Location, now, options, ageByLocation)) { heldByAge++; continue; }
            if (TryDelete(candidate.Location, store, options.Delete)) parquetDeleted++;
            else skipped++;
        }

        return new SnapshotRetentionResult(manifestsDeleted, parquetDeleted, skipped, heldByAge, CleanupSkipped: false);
    }

    private static readonly Regex AnyManifestShape =
        new($@"^.+-[0-9]{{17}}\{PublishedSnapshot.Extension}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Age is taken from the store's last-write time, not from the timestamp in the name. The name
    /// records when the publish STARTED; a large export finishes minutes later, and it is the finish
    /// that matters for "could something still be writing this".
    ///
    /// <para>The time comes from the inventory taken at the start of the sweep, so it is the same
    /// listing that found the file. When a location is absent from it — it appeared mid-sweep, or
    /// the listing could not report it — the answer is <b>young</b>: an age that cannot be
    /// established is never grounds to delete.</para>
    /// </summary>
    private static bool IsYoungerThanFloor(
        string location, DateTime now, SnapshotRetentionOptions options,
        IReadOnlyDictionary<string, DateTime> ageByLocation)
    {
        if (options.MinimumAge <= TimeSpan.Zero)
            return false;

        if (!ageByLocation.TryGetValue(location, out var lastWrite))
            return true; // cannot establish age — never delete on a guess

        return now - lastWrite < options.MinimumAge;
    }

    /// <summary>
    /// Deletes through the STORE, never File.Delete directly. Routing this at the filesystem was a
    /// real defect on blob: the listing and the age floor had already moved to the store, so the
    /// sweep correctly identified an unreferenced blob and then failed to remove it — silently,
    /// because File.Delete on an 'az://…' path raises an IOException that this method swallows as
    /// "in use by a consumer, retry next sweep". Retention would have reported success forever
    /// while reclaiming nothing.
    ///
    /// <para>The <see cref="SnapshotRetentionOptions.Delete"/> override still wins where set — it
    /// is how the drills observe a sweep without performing one.</para>
    /// </summary>
    private static bool TryDelete(string path, PublishStore store, Func<string, bool>? delete)
    {
        try
        {
            return delete is not null ? delete(path) : store.Delete(path);
        }
        catch (IOException)
        {
            return false; // in use by a consumer; the next sweep retries
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed class SnapshotRetentionOptions
{
    /// <summary>The published set's location — the same directory the publisher writes to.</summary>
    public required string PublishDirectory { get; init; }

    /// <summary>
    /// Where the sweep reads and deletes. Null means a plain local directory at
    /// <see cref="PublishDirectory"/> — the incumbent behaviour, and what every local caller gets
    /// without changing a line. Set it to a <see cref="BlobPublishStore"/> to sweep a container.
    ///
    /// <para>When this is set, <see cref="PublishDirectory"/> is unused for IO and only identifies
    /// the estate in messages. Both exist so a deployment can move the tier without a flag day.</para>
    /// </summary>
    public PublishStore? Store { get; init; }

    /// <summary>Base name of the manifests, e.g. <c>company-data-read</c>.</summary>
    public required string SnapshotName { get; init; }

    /// <summary>
    /// Published sets kept; parquet referenced by any kept manifest survives.
    ///
    /// <para><b>A recovery parameter, not a storage-cost knob.</b> Cold start walks published
    /// sets newest-first and skips any that are torn, so "keep 3" also means "survive 2 bad
    /// publishes on startup". It is likewise what lets a consumer mid-refresh against the
    /// previous manifest still find its files.</para>
    /// </summary>
    public int KeepPublishes { get; init; } = 3;

    /// <summary>
    /// Nothing is deleted until it has gone this long without being modified.
    ///
    /// <para>This is the safety floor that makes sweeping on an independent schedule
    /// legitimate. A publish writes its parquet first and commits its manifest LAST, so
    /// mid-publish there are real files that nothing references yet; without an age floor a
    /// sweeper cannot tell them from orphans and would delete the publish in progress. It is
    /// also the portable replacement for filesystem held-open protection, which blob storage
    /// cannot provide at all.</para>
    ///
    /// <para>Set it longer than the longest plausible publish AND the longest plausible
    /// consumer read. The 45-minute default clears both comfortably — a full JPM-scale export is
    /// tens of seconds and a large BI refresh a few minutes — with margin for a slow one, rather
    /// than being cut fine against them.</para>
    ///
    /// <para><b>Any floor longer than a few publish cadences makes the floor — not
    /// <see cref="KeepPublishes"/> — the thing that bounds retention</b>, and that is intended.
    /// Every manifest still on disk keeps its parquet alive, so at a 60-second cadence a
    /// 45-minute floor means roughly "keep the last 45 minutes of publishes" rather than "keep 3",
    /// and a frequently-changing table will hold more than three versions. That is accepted. The
    /// cost stays small because unchanged tables are not re-exported: a table that does not change
    /// produces no new file however often the publisher runs, so growth tracks the CHANGE rate,
    /// not the publish cadence. Redo that arithmetic before lengthening this much further — for a
    /// table that genuinely changes every cycle the cost is cadence-driven and grows fast.</para>
    ///
    /// <para><see cref="TimeSpan.Zero"/> disables the floor. That is for tests and for the
    /// publisher's own in-process sweep, where the just-written set is protected by being
    /// referenced; a standalone sweeper must never run with it.</para>
    /// </summary>
    public TimeSpan MinimumAge { get; init; } = TimeSpan.FromMinutes(45);

    /// <summary>Injectable clock for tests.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    /// <summary>
    /// Fault-injection hook for tests and recovery drills only. When supplied, it performs the
    /// requested delete and returns whether retention should treat it as successful.
    /// </summary>
    public Func<string, bool>? Delete { get; init; }

    internal void Validate()
    {
        // The floor of 2 is a recovery window: cold start must be able to skip a torn newest
        // set and still find a whole one behind it.
        if (KeepPublishes < 2)
            throw new ArgumentOutOfRangeException(nameof(KeepPublishes), KeepPublishes,
                "Retention must keep at least 2 published sets — cold start needs a whole set to fall back to " +
                "when the newest is torn, and consumers mid-refresh need the previous set's files to survive.");
        if (MinimumAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MinimumAge), MinimumAge, "Minimum age cannot be negative.");
    }
}

/// <param name="ManifestsDeleted">Manifests removed beyond the retention depth.</param>
/// <param name="ParquetFilesDeleted">Parquet removed because no surviving manifest referenced them.</param>
/// <param name="Skipped">Deletes refused by the filesystem — held open by a consumer; retried next sweep.</param>
/// <param name="HeldByAge">Files that were otherwise eligible but are inside
/// <see cref="SnapshotRetentionOptions.MinimumAge"/>. A non-zero count is normal and <b>not</b> a
/// fault signal — it is simply the publishes made within the floor that are beyond the retention
/// depth. It is only worth reading against the floor: a count that keeps growing across sweeps
/// spanning more than the floor means files are being rewritten that should have settled.</param>
/// <param name="CleanupSkipped">Parquet cleanup was skipped: a foreign manifest shares the directory,
/// or a manifest would not parse. Never a silent partial sweep.</param>
public sealed record SnapshotRetentionResult(
    int ManifestsDeleted,
    int ParquetFilesDeleted,
    int Skipped,
    int HeldByAge,
    bool CleanupSkipped)
{
    public bool DeletedAnything => ManifestsDeleted > 0 || ParquetFilesDeleted > 0;
}

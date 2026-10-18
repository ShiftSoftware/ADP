namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// A serving host's copy of the match configuration its registry recorded (the
/// <see cref="MatchSettings.ArtifactType"/> artifact): read once per resolve and reused until a newer
/// resolve has finished, so the per-request cost is one cheap "latest finished run" probe and the
/// payload is parsed once per configuration.
///
/// <para>Storage-agnostic: the host supplies the two reads (the newest finished resolve run, and the
/// recorded row's content hash and payload), so the same cache serves an EF host, a raw SQL reader or
/// a test. Only runs that FINISHED count: a resolve writes its artifacts and stamps its run finished in
/// one transaction, so a finished run's configuration is always the committed one — a run still in
/// progress never pairs its number with the previous run's configuration.</para>
///
/// <para>Thread-safe. Readers never block each other on the fast path; a reload is serialized so one
/// change of run is read once, however many requests notice it together. A run number that moves in
/// either direction (a registry restored from a backup) triggers a reload.</para>
/// </summary>
public sealed class RecordedMatchSettings
{
    private sealed record Loaded(int RunId, string? Hash, MatchSettings? Settings);

    private Loaded? loaded;
    private readonly SemaphoreSlim reload = new(1, 1);

    /// <summary>The run whose configuration is currently held; null before the first read.</summary>
    public int? RunId => Volatile.Read(ref loaded)?.RunId;

    /// <summary>
    /// The configuration of the registry's latest finished resolve, or null when the registry recorded
    /// none (no row, a tombstone, or a registry from before configurations were recorded) — then the
    /// host scores with its own process configuration, exactly as it did before.
    /// </summary>
    /// <param name="latestFinishedRun">The highest run id with a finish time; 0 when there is none.</param>
    /// <param name="readRecorded">The recorded row's content hash and payload; (null, null) when there is no row.</param>
    /// <exception cref="FormatException">The recorded payload is not a configuration.</exception>
    /// <exception cref="NotSupportedException">The configuration was recorded by a newer engine.</exception>
    public async Task<MatchSettings?> GetAsync(
        Func<CancellationToken, Task<int>> latestFinishedRun,
        Func<CancellationToken, Task<(string? ContentHash, string? Payload)>> readRecorded,
        CancellationToken ct = default)
    {
        int run = await latestFinishedRun(ct).ConfigureAwait(false);
        var current = Volatile.Read(ref loaded);
        if (current is not null && current.RunId == run) return current.Settings;

        await reload.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            current = Volatile.Read(ref loaded);
            if (current is not null && current.RunId == run) return current.Settings;

            var (hash, payload) = await readRecorded(ct).ConfigureAwait(false);
            string? h = hash?.TrimEnd();
            // A newer run on an unchanged configuration (the usual zero-delta re-run) keeps the parsed copy.
            var settings = current is not null && h is not null && current.Hash == h
                ? current.Settings
                : MatchSettings.FromStored(h, payload);
            Volatile.Write(ref loaded, new Loaded(run, h, settings));
            return settings;
        }
        finally
        {
            reload.Release();
        }
    }
}

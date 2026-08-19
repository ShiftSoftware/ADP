namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// How far a Cosmos-reading source has already ingested: the change feed's own continuation
/// token, plus the container it addresses and the operator ingest version it was taken under.
///
/// <para><b>This is the upstream cursor, and it is not <c>_ChangeSequence</c>.</b> The change
/// sequence answers "what changed in the snapshot since I last read?" for downstream consumers.
/// This answers "what have I already ingested from the container?" for the source's own ingest
/// loop. They travel in opposite directions and must never be confused.</para>
///
/// <para><b>Why a token and not a watermark.</b> A <c>LookupDate</c>-style creation stamp never
/// moves when a document is mutated, so a watermark on it silently misses every later write; a
/// <c>_ts</c> watermark does move but has one-second granularity and no cross-partition
/// monotonicity, so <c>&gt; watermark</c> can skip a straggler permanently. The continuation
/// token is not a heuristic: at-least-once, per-partition ordered, resumable.</para>
///
/// <para><see cref="Database"/>/<see cref="Container"/> are the addressing check — the analogue of
/// <see cref="SourceFileStamp.FilePath"/>. A token is only meaningful against the container that
/// issued it, so a repointed source must never resume on the old one's position.</para>
/// </summary>
/// <param name="IngestVersion">
/// The operator lever, carried so a change can be detected: any difference from the source's
/// configured value discards the cursor and re-reads the container from the beginning. It is
/// deliberately the ONLY thing that does so automatically — see
/// <see cref="CosmosChangeFeedIngestorOptions.IngestVersion"/>.
/// </param>
public sealed record SourceCosmosCursor(
    string SourceKey,
    string Database,
    string Container,
    string ContinuationToken,
    string? IngestVersion,
    DateTime StampedAtUtc);

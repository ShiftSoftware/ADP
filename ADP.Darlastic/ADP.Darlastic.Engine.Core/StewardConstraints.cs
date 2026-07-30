using ShiftSoftware.ADP.Darlastic.Shared;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// The steward verdicts the engine honors on a resolve — loaded from <c>Darlastic.StewardDecision</c>
/// (Active rows only) and applied inside <see cref="Merge.ClusterFromBlocks"/>.
///
/// <para>This is the piece that makes the steward queue a LOOP rather than a logbook. Without it a
/// steward's verdict lands in <c>AuditEntry</c> and nothing else: the next resolve re-derives the
/// identical pair from the same sources and re-queues it, forever, because
/// <c>WriteStewardQueue</c> replaces the queue wholesale every run. A queue that re-asks a question
/// the steward already answered is precisely the automation-complacency failure the queue-hygiene
/// principle warns about — so replay is a correctness requirement of the steward surface, not a
/// later refinement of it.</para>
///
/// <para><b>Semantics, stated honestly (v0).</b>
/// <list type="bullet">
/// <item><c>merge</c> — a hard edge. The pair is unioned regardless of score, so a steward can
/// unify what the engine scored below the line.</item>
/// <item><c>separate</c> — a DIRECT-edge veto plus permanent queue suppression. The pair is never
/// queued again, and if a later run scores it at or above the auto-merge line (sources changed,
/// rules changed) the steward's explicit verdict wins and the edge is dropped. What it does
/// <b>not</b> do is prevent the two records landing in one identity TRANSITIVELY through a third
/// record — that needs constrained clustering, and the honest remedy for a bad transitive merge is
/// the split action (a later steward slice), not a pairwise veto. A veto that silently failed to
/// hold under transitivity would be worse than one with a documented boundary.</item>
/// </list></para>
///
/// <para>Decisions are keyed by the canonical pair key — the SAME string
/// <see cref="Merge.PairKey(RealRecord, RealRecord)"/> builds for the persisted queue and the case
/// browser's audit rows. Sharing one key builder is deliberate: a key format that drifted between
/// the writer and the replayer would turn every constraint into a silent no-op, which is the one
/// failure mode of this feature that no test output would show.</para>
/// </summary>
public sealed class StewardConstraints
{
    /// <summary>The empty set — a resolve with no steward history behaves exactly as before.</summary>
    public static readonly StewardConstraints None = new();

    private readonly HashSet<string> _merge = new(StringComparer.Ordinal);
    private readonly HashSet<string> _separate = new(StringComparer.Ordinal);

    public int MergeCount => _merge.Count;
    public int SeparateCount => _separate.Count;
    public bool IsEmpty => _merge.Count == 0 && _separate.Count == 0;

    /// <summary>Every decided pair key, whichever way it was decided — the queue-suppression set.</summary>
    public IEnumerable<string> DecidedPairKeys => _merge.Concat(_separate);

    /// <summary>
    /// Add one decision. A pair decided both ways (a steward reversing an earlier verdict whose row
    /// was left Active by a buggy writer) resolves to <c>separate</c>: between two contradictory
    /// instructions, the engine takes the one that does not fabricate an identity, because an
    /// over-merge is the expensive error to undo and a missed merge re-surfaces on its own.
    /// </summary>
    public void Add(string kind, string pairKey)
    {
        if (string.IsNullOrWhiteSpace(pairKey)) return;
        switch (kind)
        {
            case StewardVerdict.Merge:
                if (!_separate.Contains(pairKey)) _merge.Add(pairKey);
                break;
            case StewardVerdict.Separate:
                _merge.Remove(pairKey);
                _separate.Add(pairKey);
                break;
        }
    }

    public bool ForcesMerge(string pairKey) => _merge.Contains(pairKey);
    public bool ForcesSeparate(string pairKey) => _separate.Contains(pairKey);
}

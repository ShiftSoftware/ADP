namespace ShiftSoftware.ADP.Darlastic.Shared;

/// <summary>
/// The steward verdict vocabulary — stored in <c>StewardDecision.Kind</c> and
/// <c>AuditEntry.Action</c>.
///
/// <para>It lives in Shared because three layers must agree on these strings and none of them can
/// see each other: the Engine replays them during a resolve, the API captures them from a steward,
/// and the Web surface offers them as actions. (The API deliberately does not reference the Engine —
/// that would pull the Cosmos client into a package that only needs to write two rows.) A verdict
/// string that drifted between capture and replay would silently turn every steward decision into a
/// no-op, so there is exactly one definition.</para>
/// </summary>
public static class StewardVerdict
{
    /// <summary>"These are the same person" — forces the edge on every subsequent resolve.</summary>
    public const string Merge = "merge";

    /// <summary>"These are different people" — vetoes the direct edge and suppresses the queue entry.</summary>
    public const string Separate = "separate";

    /// <summary>
    /// "I can't decide yet" — audited, but deliberately NOT a <c>StewardDecision</c> row: it
    /// constrains nothing, so writing one would put a row the engine must skip into the table whose
    /// whole contract is "Active rows are hard constraints". Deferral is a queue-presentation
    /// concern, applied by reading the audit trail.
    /// </summary>
    public const string Defer = "defer";

    /// <summary>
    /// "Undo my verdict" — deactivates whatever constraint this pair carried and writes no
    /// replacement, returning the pair to the engine's own judgement and to the queue. Phase 5's
    /// exit criteria require every steward action to be reversible; re-deciding the other way is
    /// not the same thing, because it leaves a human constraint in force where the steward wanted
    /// none.
    /// </summary>
    public const string Release = "release";

    /// <summary>Verdicts that write an Active <c>StewardDecision</c> the engine replays.</summary>
    public static bool IsConstraint(string kind) => kind is Merge or Separate;

    public static bool IsKnown(string kind) => kind is Merge or Separate or Defer or Release;
}

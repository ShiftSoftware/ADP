using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared.Workflow;

/// <summary>
/// The declarative, pure, DB-free case-workflow engine (skr-dtr D4). Construct one per case type
/// with that domain's transition table; invoke <see cref="Apply"/> from the repository's
/// <c>UpsertAsync</c> (or a batch endpoint) after the consumer has performed its own authorization.
/// </summary>
/// <remarks>
/// Unlike the legacy <see cref="Services.SharedClaimService"/> (moved verbatim, mutate-then-throw),
/// this engine is VALIDATE-FIRST: an illegal transition throws before any mutation. Timestamps come
/// from an injected <see cref="TimeProvider"/> so tests are deterministic (skr-dtr D15).
/// </remarks>
/// <typeparam name="TStatus">The consumer's case-status enum.</typeparam>
/// <typeparam name="TTrigger">The consumer's trigger/action type (typically an enum).</typeparam>
public class CaseWorkflow<TStatus, TTrigger>
    where TStatus : struct, Enum
    where TTrigger : notnull
{
    private readonly IReadOnlyList<CaseTransition<TStatus, TTrigger>> transitions;
    private readonly TimeProvider timeProvider;

    public CaseWorkflow(IEnumerable<CaseTransition<TStatus, TTrigger>> transitions, TimeProvider? timeProvider = null)
    {
        this.transitions = transitions.ToList();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>The full transition table (read-only) — e.g. for rendering available actions in UI.</summary>
    public IReadOnlyList<CaseTransition<TStatus, TTrigger>> Transitions => transitions;

    /// <summary>Finds the transition for the given source status + trigger, or null when illegal.</summary>
    public CaseTransition<TStatus, TTrigger>? Find(TStatus? from, TTrigger trigger) =>
        from is null
            ? null
            : transitions.FirstOrDefault(t =>
                EqualityComparer<TStatus>.Default.Equals(t.From, from.Value) &&
                EqualityComparer<TTrigger>.Default.Equals(t.Trigger, trigger));

    /// <summary>True when the trigger is legal from the given status.</summary>
    public bool CanApply(TStatus? from, TTrigger trigger) => Find(from, trigger) is not null;

    /// <summary>All triggers legal from the given status (for building action menus).</summary>
    public IEnumerable<CaseTransition<TStatus, TTrigger>> AvailableFrom(TStatus? from) =>
        from is null
            ? []
            : transitions.Where(t => EqualityComparer<TStatus>.Default.Equals(t.From, from.Value));

    /// <summary>
    /// Applies <paramref name="trigger"/> to the case: validates legality FIRST, then mutates
    /// <see cref="ICase{TStatus}.Status"/> and returns the history entry the consumer must persist.
    /// </summary>
    /// <exception cref="ShiftEntityException">
    /// Illegal transition — thrown BEFORE any mutation, carrying the same "Error"-titled aggregated
    /// message shape the claim engine uses, so consumer UIs render one consistent dialog.
    /// </exception>
    public CaseStatusHistoryEntry<TStatus> Apply(ICase<TStatus> @case, TTrigger trigger, string? actorUserId = null, string? comment = null)
    {
        var transition = Find(@case.Status, trigger);

        if (transition is null)
        {
            var statusText = @case.Status?.Describe() ?? "none";

            throw new ShiftEntityException(
                new Message(
                    "Error",
                    "",
                    [new Message($"Case #{@case.GetCaseIdentifier()}", $"has [{statusText}] status — [{DescribeTrigger(trigger)}] is not allowed.")]
                ));
        }

        var fromStatus = @case.Status;
        @case.Status = transition.To;

        return new CaseStatusHistoryEntry<TStatus>(fromStatus, transition.To, timeProvider.GetUtcNow(), actorUserId, comment);
    }

    /// <summary>
    /// Records a note WITHOUT a status change (skr-dtr §4.2.7: e.g. org staff adds a comment) — returns a
    /// history entry whose FromStatus equals ToStatus. The case must already have a status.
    /// </summary>
    public CaseStatusHistoryEntry<TStatus> AppendNote(ICase<TStatus> @case, string? actorUserId = null, string? comment = null)
    {
        if (@case.Status is null)
            throw new InvalidOperationException("Cannot append a note to a case that has no status yet.");

        return new CaseStatusHistoryEntry<TStatus>(@case.Status, @case.Status.Value, timeProvider.GetUtcNow(), actorUserId, comment);
    }

    private static string DescribeTrigger(TTrigger trigger) =>
        trigger is Enum e ? e.Describe() : trigger.ToString() ?? string.Empty;
}

using ShiftSoftware.ADP.Cases.Shared.Validation;
using ShiftSoftware.ADP.Cases.Shared.Workflow;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared.Flow;

/// <summary>
/// Everything the shared case code has to know about one case type: its transition table, and the few facts the table
/// cannot say. The repository, the endpoints, the buttons and the history all read it, so two case types differ only in
/// what they declare here.
/// </summary>
public sealed class CaseDefinition<TStatus, TTrigger>
    where TStatus : struct, Enum
    where TTrigger : struct, Enum
{
    /// <summary>Every legal move. The role on a row is whose move it is (<see cref="CaseRoles"/>).</summary>
    public required IReadOnlyList<CaseTransition<TStatus, TTrigger>> Transitions { get; init; }

    /// <summary>Where a case starts when it is sent straight away.</summary>
    public required TStatus Initial { get; init; }

    /// <summary>Where a case starts when the dealer keeps it as a draft. Only its dealership sees it.</summary>
    public required TStatus Draft { get; init; }

    /// <summary>
    /// The trigger that sends a draft in. It arrives with a save, which checks the required fields, so it is never a
    /// button.
    /// </summary>
    public required TTrigger Submit { get; init; }

    /// <summary>
    /// The statuses a dealer may no longer edit or comment on. Not readable off the table: a dealer has no move out of
    /// the initial status either, and may still edit there.
    /// </summary>
    public required IReadOnlySet<TStatus> ClosedToDealer { get; init; }

    /// <summary>What a case is called in a sentence, for example "report".</summary>
    public required string Noun { get; init; }

    /// <summary>Builds the engine over <see cref="Transitions"/>.</summary>
    /// <param name="timeProvider">The clock history lines are stamped with; the system's when null.</param>
    public CaseWorkflow<TStatus, TTrigger> CreateWorkflow(TimeProvider? timeProvider = null) => new(Transitions, timeProvider);

    // ----- Actions: the buttons -----

    /// <summary>The actions <paramref name="role"/> can take on a case in <paramref name="status"/>, in table order.</summary>
    /// <remarks>Read off the table, so a button can never offer a move the engine would refuse.</remarks>
    public IReadOnlyList<TTrigger> ActionsFrom(TStatus? status, string? role) =>
        status is null || role is null
            ? []
            : Transitions
                .Where(t => t.RequiredRole == role && Same(t.From, status.Value) && !IsSubmit(t.Trigger))
                .Select(t => t.Trigger)
                .Distinct()
                .ToList();

    /// <summary>All of <paramref name="role"/>'s actions, whatever status they apply from.</summary>
    public IReadOnlyList<TTrigger> ActionsOf(string? role) =>
        Transitions
            .Where(t => role is not null && t.RequiredRole == role && !IsSubmit(t.Trigger))
            .Select(t => t.Trigger)
            .Distinct()
            .ToList();

    /// <summary>Whether <paramref name="trigger"/> is one of <paramref name="role"/>'s actions at all.</summary>
    public bool IsActionOf(TTrigger trigger, string? role) => ActionsOf(role).Contains(trigger);

    /// <summary>
    /// Whether <paramref name="role"/> may add a note, a history line that moves nothing, to a case in
    /// <paramref name="status"/>: nobody on a draft, the distributor on anything else, a dealer until the case is closed
    /// to them.
    /// </summary>
    public bool CanAddNote(TStatus? status, string? role) =>
        status is not null
        && !Same(status.Value, Draft)
        && (role == CaseRoles.Distributor
            || (role == CaseRoles.Dealer && !ClosedToDealer.Contains(status.Value)));

    // ----- Saving -----

    /// <summary>Whether a new case asking for <paramref name="requested"/> opens as a draft.</summary>
    public bool OpensAsDraft(TStatus? requested) => requested is not null && Same(requested.Value, Draft);

    /// <summary>
    /// Works out what one save means: refuse it, save it without moving it, or save it and send the draft in. Every
    /// other move is an action, so a save that asks for one is refused. Legality is the engine's to check.
    /// </summary>
    /// <param name="current">The stored status, or null for a case being created.</param>
    /// <param name="requested">
    /// The status the form sent. The draft box is the only thing that changes it; otherwise the form sends the stored
    /// one back.
    /// </param>
    /// <param name="actor">Who is saving. <c>IsElevated</c> means the distributor.</param>
    public CaseSaveResolution<TTrigger> ResolveSave(TStatus? current, TStatus? requested, CaseActorContext actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        // A draft is the dealer's. One from the distributor would skip the required fields and then be hidden from the
        // person who raised it.
        if (current is null)
            return actor.IsElevated && OpensAsDraft(requested)
                ? Refuse($"Only a dealer can save a {Noun} as a draft.")
                : CaseSaveResolution<TTrigger>.NoStatusChange;

        if (!actor.IsElevated && ClosedToDealer.Contains(current.Value))
            return Refuse($"This {Noun} is [{current.Value.Describe()}] and can no longer be edited by the dealer.");

        if (requested is null || Same(requested.Value, current.Value))
            return CaseSaveResolution<TTrigger>.NoStatusChange;

        if (!actor.IsElevated && Same(current.Value, Draft) && Same(requested.Value, Initial))
            return CaseSaveResolution<TTrigger>.Apply(Submit);

        // Going back would take the case out of the distributor's queue with nothing recording that it was ever sent.
        if (!actor.IsElevated && Same(requested.Value, Draft))
            return Refuse($"This {Noun} has already been submitted and cannot be put back into draft. You can still edit it.");

        return Refuse($"The status of a {Noun} is changed with the buttons next to its title.");
    }

    private static CaseSaveResolution<TTrigger> Refuse(string message) => CaseSaveResolution<TTrigger>.Reject(message);

    private bool IsSubmit(TTrigger trigger) => EqualityComparer<TTrigger>.Default.Equals(trigger, Submit);

    private static bool Same(TStatus a, TStatus b) => EqualityComparer<TStatus>.Default.Equals(a, b);
}

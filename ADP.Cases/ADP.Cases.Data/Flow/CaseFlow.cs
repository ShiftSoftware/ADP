using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Flow;
using ShiftSoftware.ADP.Cases.Shared.Workflow;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;

namespace ShiftSoftware.ADP.Cases.Data.Flow;

/// <summary>
/// Moves a case and writes its history: the one place either happens, for every case type. Pure: it changes the case it
/// is given and saves nothing.
/// </summary>
public sealed class CaseFlow<TCase, THistory, TStatus, TTrigger>
    where TCase : class, ICase<TStatus>, ICaseWithHistory<THistory>
    where THistory : CaseStatusHistoryBase<THistory, TStatus>, new()
    where TStatus : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly CaseDefinition<TStatus, TTrigger> definition;
    private readonly CaseWorkflow<TStatus, TTrigger> workflow;

    // One clock, so the line written when a case is raised and the engine's lines are stamped the same way.
    private readonly TimeProvider timeProvider;

    public CaseFlow(CaseDefinition<TStatus, TTrigger> definition, TimeProvider? timeProvider = null)
    {
        this.definition = definition;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        workflow = definition.CreateWorkflow(this.timeProvider);
    }

    // ----- Saving -----

    /// <summary>What saving <paramref name="stored"/> means. Throws when the save is refused.</summary>
    public CaseSaveResolution<TTrigger> Resolve(TCase stored, TStatus? requested, CaseActor actor)
    {
        var resolution = definition.ResolveSave(stored.Status, requested, actor.Context);

        return resolution.IsRejected ? throw new ShiftEntityException(resolution.Rejection!) : resolution;
    }

    /// <summary>
    /// Gives a new case its first status and opens its history. The engine has no transition into the first status, so
    /// the opening line is written here, with a null "from".
    /// </summary>
    public void Raise(TCase raised, TStatus? requested, CaseActor actor)
    {
        var opening = definition.OpensAsDraft(requested) ? definition.Draft : definition.Initial;

        raised.Status = opening;

        Append(raised, new(null, opening, timeProvider.GetUtcNow(), actor.UserID?.ToString(), null), actor);
    }

    /// <summary>Applies what <see cref="Resolve"/> decided, which is a move only when a draft is sent in.</summary>
    public void Save(TCase saved, CaseSaveResolution<TTrigger> resolution, CaseActor actor)
    {
        if (resolution.Trigger is { } trigger)
            Append(saved, workflow.Apply(saved, trigger, actor.UserID?.ToString()), actor);
    }

    // ----- Actions and notes -----

    /// <summary>Refuses an action that is not one of the caller's side's.</summary>
    public void EnsureActionOf(CaseActor actor, TTrigger trigger)
    {
        if (!definition.IsActionOf(trigger, actor.Role))
            throw NotAllowed($"[{trigger.Describe()}] is not an action you can take on a {definition.Noun}.");
    }

    /// <summary>Applies an action. The engine refuses a move the table does not have, before anything changes.</summary>
    public void Act(TCase @case, TTrigger trigger, string? comment, CaseActor actor)
    {
        EnsureActionOf(actor, trigger);

        Append(@case, workflow.Apply(@case, trigger, actor.UserID?.ToString(), Trimmed(comment)), actor);
    }

    /// <summary>
    /// Applies an action to several cases, all or none: every one that cannot take it is named in one message, and
    /// nothing is changed.
    /// </summary>
    public void ActOnAll(IReadOnlyCollection<TCase> cases, TTrigger trigger, string? comment, CaseActor actor)
    {
        EnsureActionOf(actor, trigger);

        if (cases.Count == 0)
            throw new ShiftEntityException(new Message("Nothing Selected", $"None of the selected {definition.Noun}s can be found."));

        CaseMessageAggregator.ThrowIfAny(
            cases
                .Where(x => !workflow.CanApply(x.Status, trigger))
                .Select(x => new Message(
                    $"#{x.GetCaseIdentifier()}",
                    $"is [{(x.Status is { } status ? status.Describe() : "none")}], so [{trigger.Describe()}] cannot be applied."))
                .ToList(),
            $"{definition.Noun}s");

        foreach (var x in cases)
            Act(x, trigger, comment, actor);
    }

    /// <summary>The note itself, or a refusal when nothing was written.</summary>
    public static string RequireNote(string? comment) =>
        Trimmed(comment) ?? throw new ShiftEntityException(new Message("Note Required", "Write the note before adding it."));

    /// <summary>Adds a note: a history line that moves nothing.</summary>
    public void AddNote(TCase @case, string? comment, CaseActor actor)
    {
        var note = RequireNote(comment);

        if (!definition.CanAddNote(@case.Status, actor.Role))
            throw NotAllowed($"You cannot add a note to a {definition.Noun} that is [{(@case.Status is { } status ? status.Describe() : "none")}].");

        Append(@case, workflow.AppendNote(@case, actor.UserID?.ToString(), note), actor);
    }

    // ----- The history -----

    private static void Append(TCase @case, CaseStatusHistoryEntry<TStatus> entry, CaseActor actor)
    {
        var line = new THistory();

        line.Record(entry, actor.UserID, actor.UserName, actor.Role);

        @case.StatusHistory.Add(line);
    }

    private static string? Trimmed(string? comment) => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static ShiftEntityException NotAllowed(string body) =>
        new(new Message("Not Allowed", body), (int)HttpStatusCode.Forbidden);
}

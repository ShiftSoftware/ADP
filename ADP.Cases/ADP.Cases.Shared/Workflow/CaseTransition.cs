namespace ShiftSoftware.ADP.Cases.Shared.Workflow;

/// <summary>
/// One row of a declarative case-workflow transition table: "from <see cref="From"/>, trigger
/// <see cref="Trigger"/> moves the case to <see cref="To"/>".
/// </summary>
/// <remarks>
/// <see cref="RequiredRole"/> is carried as DATA only — the engine never evaluates authorization.
/// The consumer maps the role string to its own TypeAuth action (e.g. <c>Skr.ChangeStatus</c>) and
/// checks it in the API layer BEFORE calling <see cref="CaseWorkflow{TStatus, TTrigger}.Apply"/>.
/// Action trees are per-app (decision D9); the engine stays authorization-agnostic.
/// </remarks>
/// <typeparam name="TStatus">The consumer's case-status enum.</typeparam>
/// <typeparam name="TTrigger">The consumer's trigger/action type (typically an enum).</typeparam>
public sealed record CaseTransition<TStatus, TTrigger>(
    TStatus From,
    TTrigger Trigger,
    TStatus To,
    string? RequiredRole = null)
    where TStatus : struct, Enum
    where TTrigger : notnull;

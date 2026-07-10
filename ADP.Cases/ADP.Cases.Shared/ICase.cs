namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>
/// The general case contract for workflow-driven request/case entities (SKR, DTR and future
/// case types). Generic over the consumer's own status enum — each case domain defines its OWN
/// <c>[Description]</c>-annotated status enum (per skr-dtr decision D3; the engine deliberately
/// does NOT impose ADP's <c>ClaimStatus</c>).
/// </summary>
/// <remarks>
/// Claim-type cases keep implementing the frozen <see cref="IClaim"/> sibling contract for now —
/// see the remarks there for why the two are not yet unified.
/// </remarks>
/// <typeparam name="TStatus">The consumer's case-status enum.</typeparam>
public interface ICase<TStatus> where TStatus : struct, Enum
{
    /// <summary>Current workflow status. Null only before the consumer assigns the initial state.</summary>
    TStatus? Status { get; set; }

    /// <summary>Human-facing identifier used in aggregated error messages (e.g. case/claim number).</summary>
    string GetCaseIdentifier();
}

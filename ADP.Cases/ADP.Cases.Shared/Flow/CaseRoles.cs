namespace ShiftSoftware.ADP.Cases.Shared.Flow;

/// <summary>
/// The two sides of every case, carried as data on the workflow's transitions, the actor and every history line. The
/// engine never evaluates them; <see cref="CaseDefinition{TStatus, TTrigger}"/> checks that a move is the caller's side's.
/// </summary>
public static class CaseRoles
{
    public const string Distributor = "Distributor";

    /// <summary>Anyone who is not the distributor.</summary>
    public const string Dealer = "Dealer";
}

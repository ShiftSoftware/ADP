using ShiftSoftware.ADP.Cases.Shared.Validation;

namespace ShiftSoftware.ADP.Cases.Shared.Flow;

/// <summary>
/// Who is doing something to a case, and on which side, as its history records it.
/// </summary>
/// <param name="UserID">The acting user, or null for anything the system does on its own.</param>
/// <param name="UserName">The name that user goes by now, copied onto the line.</param>
/// <param name="IsDistributor">Whether they belong to the distributor. Everyone else is a dealer.</param>
public sealed record CaseActor(long? UserID, string? UserName, bool IsDistributor)
{
    /// <summary>One of the <see cref="CaseRoles"/> constants.</summary>
    public string Role => IsDistributor ? CaseRoles.Distributor : CaseRoles.Dealer;

    /// <summary>The same actor as the pure rules take it.</summary>
    public CaseActorContext Context => new(UserID?.ToString(), IsDistributor, Role);
}

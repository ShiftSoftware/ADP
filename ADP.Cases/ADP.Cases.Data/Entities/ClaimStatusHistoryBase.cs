using ShiftSoftware.ADP.Cases.Data.Flow;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.Cases.Data.Entities;

/// <summary>One row of a claim's history. <see cref="ClaimHistory{TClaim, THistory}"/> writes it.</summary>
public abstract class ClaimStatusHistoryBase<TSelf, TClaim> : CaseStatusHistoryBase<TSelf, ClaimStatus>
    where TSelf : ShiftEntity<TSelf>
    where TClaim : class
{
    public long ClaimID { get; set; }
    public virtual TClaim Claim { get; set; } = default!;

    /// <summary>About the manufacturer's side, so a dealer is never shown it.</summary>
    public bool DistributorOnly { get; set; }
}

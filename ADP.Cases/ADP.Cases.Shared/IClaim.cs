using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>
/// The shared claim contract implemented by claim-type cases (the host's WarrantyClaim and the
/// ClaimableItems module's ItemClaim). Moved VERBATIM from the original host application's <c>Services.Data.Entities.IClaim</c>
/// (Phase 2, decision D14/D16) — member names and types are frozen: implementers in production
/// depend on them, and <see cref="Services.SharedClaimService"/> mutates them.
/// </summary>
/// <remarks>
/// This is deliberately a SIBLING contract to <see cref="ICase{TStatus}"/>, not a sub-interface:
/// unifying them (e.g. <c>IClaim : ICase&lt;ClaimStatus&gt;</c>) would force renames/additions on the
/// existing implementers, breaking the "minimal retarget, zero behavior change" mandate. Unification
/// happens when the warranty and item claims fully adopt the declarative engine in a later phase.
/// </remarks>
public interface IClaim
{
    public ClaimStatus? ClaimStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }
    public DateTime? ProcessDate { get; set; }
    string GetClaimIdentifier();
    public string? InvoiceNo { get; set; }
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
}

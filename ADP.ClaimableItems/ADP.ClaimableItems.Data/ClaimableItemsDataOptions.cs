using ShiftSoftware.TypeAuth.Core.Actions;

namespace ShiftSoftware.ADP.ClaimableItems.Data;

/// <summary>
/// Consumer options the Data-layer repositories read (Phase 3 Slice 3.6). Set via
/// <c>ClaimableItemsApiOptions</c>; the API extension captures the values into this object and
/// registers it as a singleton so the Data layer can read them without referencing the API options
/// type (same capture-at-registration pattern as <c>ClaimableItemsReportOverrides</c>).
/// </summary>
public class ClaimableItemsDataOptions
{
    /// <summary>
    /// The TypeAuth boolean node that allows editing the otherwise-immutable claim fields of a Draft
    /// item claim (consumer-supplied, per D9 — the original host application passes its existing
    /// <c>PostClaimModification</c> node). Null (default) → never allowed, unless a derived
    /// repository overrides <c>ItemClaimRepository.CanModifyPostClaim</c>.
    /// </summary>
    public BooleanAction? PostClaimModificationAction { get; set; }

    /// <summary>
    /// Km of tolerance when a service contract leaves out periodic services the vehicle has already
    /// passed: a periodic menu due at or below <c>mileage + margin</c> is not offered. Default 0.
    /// </summary>
    public long ContractPeriodicMileageMargin { get; set; }

    /// <summary>
    /// The TypeAuth boolean node a user needs to issue a service contract (consumer-supplied, per D9).
    /// Null (default) → anyone who may write contracts may issue them.
    /// </summary>
    public BooleanAction? IssueContractAction { get; set; }

    /// <summary>
    /// The TypeAuth boolean node a user needs to cancel a service contract or one of its lines
    /// (consumer-supplied, per D9). Null (default) → anyone who may write contracts may cancel them.
    /// </summary>
    public BooleanAction? CancelContractAction { get; set; }

    /// <summary>The fewest services a contract can be issued with. Default 1.</summary>
    public int ContractMinimumLines { get; set; } = 1;

    /// <summary>
    /// The price increments a contract may use, as fractions (0.05 is 5%). Each must be within the
    /// validator's 0–10%. Default 0%, 1%, …, 10%.
    /// </summary>
    public IReadOnlyList<decimal> ContractPriceIncrements { get; set; } = DefaultContractPriceIncrements;

    /// <summary>
    /// The time zone that decides what "today" is for contracts: a contract cannot be issued to start before
    /// it, and a line expiring before it no longer holds its menu. Null (default) → the server's local zone.
    /// </summary>
    public TimeZoneInfo? ContractTimeZone { get; set; }

    public static readonly IReadOnlyList<decimal> DefaultContractPriceIncrements =
        Enumerable.Range(0, 11).Select(x => x / 100m).ToArray();
}

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
}

using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Entities;

/// <summary>
/// Empty placeholder entity for a future claimable-item contracts feature. Moved from the original host application
/// Services.Data.Entities (Phase 2 Slice 5) because ItemClaim FKs it AND its ID participates in
/// both the claim UniqueHash and the Cosmos doc-id formats (see D18). Prod table
/// dbo.ClaimableItemContract (+History) — schema unchanged.
/// </summary>
[TemporalShiftEntity]
public class ClaimableItemContract : ShiftEntity<ClaimableItemContract>
{

}

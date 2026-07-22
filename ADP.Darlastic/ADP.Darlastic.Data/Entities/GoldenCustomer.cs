namespace ShiftSoftware.ADP.Darlastic.Data.Entities;

/// <summary>
/// Read-only row over the <c>[schema].[GoldenCustomer]</c> SQL view — one row per live golden
/// identity, extracted from the canonical payload JSON the resolve engine stages in
/// <see cref="ProjectionState"/> (ArtifactType 'golden'). The view mirrors what the Cosmos drain
/// serves as <c>GoldenCustomerModel</c>, but straight from the host's own database: the drain
/// never clears <see cref="ProjectionState.Payload"/>, so a golden list/search surface costs no
/// Cosmos RU and is exactly as fresh as the last resolve.
///
/// Mapped with <c>ToView</c> (host migrations create the view itself — see
/// <see cref="DarlasticViews"/>). Not a ShiftEntity: nothing here is editable — goldens are
/// minted by the engine and corrected through stewardship; consumers only read.
/// </summary>
public class GoldenCustomer
{
    /// <summary>The golden identity ID — the same value consumers see as GoldenCustomerID in
    /// Cosmos and landing-doc stamps. Deliberately NOT hash-encoded anywhere: it is a
    /// cross-system identifier, not a local entity key.</summary>
    public long ID { get; set; }
    public string? FullName { get; set; }
    /// <summary>The survived phone (identical pick to the Cosmos doc: the engine stages attrs
    /// sorted by (type, value) and the drain last-wins per type, which is MAX(value) — the view
    /// uses the same MAX).</summary>
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? IDNumber { get; set; }
    public string? Email { get; set; }
    /// <summary>How many source records (DMS / services / tickets rows) this identity unifies.</summary>
    public int SourceCount { get; set; }
}

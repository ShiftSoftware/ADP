using System.Collections.Generic;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// PROVENANCE for one golden identity: which source records it unifies, its lifecycle, and the
/// identities merged into it — read from the registry's own ledger tables.
///
/// Deliberately NOT an explanation of the matcher's reasoning. Attribute survivorship reasons
/// (<c>Merge.GoldenAttr.WonBy</c>), pair scores, match flags and the name-survival trace are all
/// computed during a resolve and never persisted — the case browser recomputes them from the full
/// corpus in memory. So a consumer must present this as "what this identity is made of", never as
/// "why the engine decided this". Surfacing the latter needs a new staged artifact family from the
/// engine, not a richer read here.
///
/// Carries no display name: the only by-ID path to one would filter the GoldenCustomer view on
/// <c>CAST(ArtifactKey AS bigint)</c>, which is non-SARGable and would OPENJSON the whole golden
/// slice for a single row. Every caller opens this from a context that already has the name.
/// </summary>
public class GoldenCustomerSourcesDTO
{
    /// <summary>The golden identity ID — the cross-system GoldenCustomerID, not a local key.</summary>
    public string? ID { get; set; }

    public IdentityStatus Status { get; set; }

    /// <summary>The resolve run that minted this identity, and the last one that changed it.</summary>
    public int CreatedRunID { get; set; }
    public int LastChangedRunID { get; set; }

    /// <summary>The source records this identity unifies, ordered by system then record id.</summary>
    public List<GoldenCustomerSourceDTO> Sources { get; set; } = new();

    /// <summary>
    /// Identities merged INTO this one. Readers holding those IDs resolve here in one hop —
    /// a merge redirects, it never deletes.
    /// </summary>
    public List<string> AbsorbedIdentityIDs { get; set; } = new();
}

/// <summary>One row of the engine's assignment ledger — a single source record's membership.</summary>
public class GoldenCustomerSourceDTO
{
    /// <summary>Source-system slug as the tenant's feeds emit it (e.g. "activation", "dms-aad").
    /// Rendered verbatim: this package is tenant-agnostic, so any friendly naming is a host concern.</summary>
    public string SourceSystem { get; set; } = default!;

    public string SourceRecordId { get; set; } = default!;

    /// <summary>
    /// The record vanished from its source and is being HELD, not deleted — so if a transient
    /// source glitch made it disappear, its return revives this identity instead of minting a new one.
    /// </summary>
    public bool Removed { get; set; }

    public int FirstRunID { get; set; }
    public int LastChangedRunID { get; set; }
}

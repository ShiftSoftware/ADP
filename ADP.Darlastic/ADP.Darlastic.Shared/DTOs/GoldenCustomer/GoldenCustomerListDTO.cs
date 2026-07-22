using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// List row for golden-customer grids, served from the host database's
/// <c>[schema].[GoldenCustomer]</c> view (the registry's golden artifacts) — not from Cosmos.
/// Read-only: goldens are engine-owned; there is no upsert DTO and no form.
/// </summary>
[ShiftEntityKeyAndName(nameof(ID), nameof(FullName))]
public class GoldenCustomerListDTO : ShiftEntityListDTO
{
    // Deliberately NOT hash-encoded: this is the cross-system GoldenCustomerID (the same value
    // stamped on landing docs and served by GoldenCustomerLookupService), not a local entity key.
    public override string? ID { get; set; }

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? IDNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>How many source records (DMS / services / tickets rows) this identity unifies.</summary>
    public int SourceCount { get; set; }
}

using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// List row for golden-customer grids, served from the host database's
/// <c>[schema].[GoldenCustomer]</c> view (the registry's golden artifacts) — not from Cosmos.
/// Read-only: goldens are engine-owned; there is no upsert DTO and no form.
///
/// <para>Name, phone, email and national ID are protected representations. Every list response carries them
/// masked, for every caller, whatever their permissions. The raw value of one field on one
/// identity comes only from the reveal route, <c>POST GoldenCustomer/{id}/pii/{field}/reveal</c>.
/// Filter on <c>FullName/Display</c>, <c>Phone/Display</c>, <c>Email/Display</c> or
/// <c>IDNumber/Display</c>; the server matches against the raw value. Sorting on these members is
/// rejected.</para>
/// </summary>
[ShiftEntityKeyAndName(nameof(ID), nameof(FullName))]
public class GoldenCustomerListDTO : ShiftEntityListDTO
{
    // Deliberately NOT hash-encoded: this is the cross-system GoldenCustomerID (the same value
    // stamped on landing docs and served by GoldenCustomerLookupService), not a local entity key.
    public override string? ID { get; set; }

    /// <summary>Protected survived name. Masked in every list response; raw only through reveal.</summary>
    [Pii(PiiKind.Name)]
    public PiiFieldDTO? FullName { get; set; }

    /// <summary>Protected survived phone. Masked in every list response; raw only through reveal.</summary>
    [Pii(PiiKind.Phone)]
    public PiiFieldDTO? Phone { get; set; }

    public string? City { get; set; }

    /// <summary>Protected survived national ID. Masked in every list response; raw only through reveal.</summary>
    [Pii(PiiKind.Identifier)]
    public PiiFieldDTO? IDNumber { get; set; }

    /// <summary>Protected survived email. Masked in every list response; raw only through reveal.</summary>
    [Pii(PiiKind.Email)]
    public PiiFieldDTO? Email { get; set; }

    /// <summary>How many source records (DMS / services / tickets rows) this identity unifies.</summary>
    public int SourceCount { get; set; }
}

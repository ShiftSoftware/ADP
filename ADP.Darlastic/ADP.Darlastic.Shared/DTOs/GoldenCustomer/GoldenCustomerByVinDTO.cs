using System;
using System.Collections.Generic;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// The golden customers linked to one vehicle — read from the <c>vinowner</c> artifact the resolve
/// stages per VIN, so it is as fresh as the last resolve and agrees field for field with each
/// golden's own vehicle list (<see cref="GoldenCustomerVehiclesDTO"/>).
///
/// <para>Ordering is the resolve's: owners first in timeline order (earliest ownership first, an
/// unknown start first of all), then service contacts.</para>
///
/// <para>One entry per surviving identity. When goldens the resolve linked to the VIN were merged into
/// one survivor since, that survivor is listed once, by its strongest link: an owner link over a
/// service-contact link, then the longer period (an open period, still the owner, is the longest; an
/// unknown start the shortest), then the earlier staged link. The dropped links' counts and sources are
/// not added to the kept one, so every fact on an entry comes from one staged link; the next resolve
/// restages the VIN with the survivor's combined evidence.</para>
///
/// <para>Name and phone are protected representations: every response carries them masked, for
/// every caller. The raw value of one field comes only from the reveal route,
/// <c>POST GoldenCustomer/{id}/pii/{field}/reveal</c>, with the link's <see cref="GoldenCustomerVinLinkDTO.ID"/>.</para>
/// </summary>
public class GoldenCustomerByVinDTO
{
    /// <summary>The canonical VIN the links are keyed by (trimmed, upper case).</summary>
    public string VIN { get; set; } = default!;

    public List<GoldenCustomerVinLinkDTO> Goldens { get; set; } = new();
}

/// <summary>One golden customer's link to the vehicle, with the period the resolve computed for it.</summary>
public class GoldenCustomerVinLinkDTO
{
    /// <summary>The LIVE identity ID, after redirect-chasing — the ID to open or reveal.</summary>
    public string ID { get; set; } = default!;

    /// <summary>The identity the last resolve linked; differs from <see cref="ID"/> when that identity
    /// was merged away since. When several linked identities share one survivor, this is the one whose
    /// link was kept.</summary>
    public string? LinkedID { get; set; }

    public bool WasRedirected { get; set; }

    /// <summary><c>owner</c> (sale-grade evidence opened an ownership period) or
    /// <c>service-contact</c> (only service evidence: an observation window, never an ownership claim).</summary>
    public string Role { get; set; } = default!;

    /// <summary>Start of the ownership period (owner) or first service seen (service contact).</summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>End of the period: the next owner's sale for an owner, exclusive (null = still the
    /// owner); the last service seen for a service contact.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public int SaleCount { get; set; }

    public int ServiceCount { get; set; }

    public DateOnly? FirstSale { get; set; }

    public DateOnly? LastSale { get; set; }

    public DateOnly? FirstService { get; set; }

    public DateOnly? LastService { get; set; }

    /// <summary>The source profiles that evidence this link, as <c>sourceSystem|sourceRecordId</c>.</summary>
    public List<string> Sources { get; set; } = new();

    /// <summary>True when the identity has no staged golden yet (minted since the last resolve), so
    /// it has no survived name or phone to show.</summary>
    public bool AwaitingResolve { get; set; }

    /// <summary>Protected survived name. Masked in every response; raw only through reveal.</summary>
    [Pii(PiiKind.Name)]
    public PiiFieldDTO? FullName { get; set; }

    /// <summary>Protected survived phone. Masked in every response; raw only through reveal.</summary>
    [Pii(PiiKind.Phone)]
    public PiiFieldDTO? Phone { get; set; }
}

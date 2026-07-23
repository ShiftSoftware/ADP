using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Customer;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Customer;

/// <summary>
/// One golden-customer match from a customer lookup: the unified identity plus the vehicles the
/// identity-resolution engine links to it. A lookup by phone can return SEVERAL matches — phone
/// numbers are legitimately shared (family members, a company and its contact person, dealer
/// front-desk numbers) — so consumers must disambiguate by name/role, never assume one hit.
/// </summary>
[Docable]
public class GoldenCustomerLookupDTO
{
    /// <summary>The unified golden-customer identity (survived name, phones, city, e-mail, source backlinks).</summary>
    public GoldenCustomerModel Customer { get; set; }

    /// <summary>
    /// The vehicles linked to this identity, with role (<c>owner</c> on sale-grade evidence,
    /// <c>service-contact</c> for service-only observation), effective period, and per-source
    /// counts. Empty when the identity has no vehicle links.
    /// </summary>
    public List<GoldenVehicleLinkModel> Vehicles { get; set; } = new List<GoldenVehicleLinkModel>();
}

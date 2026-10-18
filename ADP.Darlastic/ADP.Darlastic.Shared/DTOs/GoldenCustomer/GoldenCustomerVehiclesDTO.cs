using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// The vehicles one golden customer is linked to, by golden ID — read from the <c>vinlinks</c>
/// artifact the resolve stages per golden. Keyed by identity rather than by phone on purpose: a
/// shared phone (family, fleet, a dealer front desk) would put other people's vehicles on the card.
///
/// <para>Ordering: owner links before service contacts; within each, the most recent activity
/// first (latest of last sale, last service and period start); then VIN.</para>
/// </summary>
public class GoldenCustomerVehiclesDTO
{
    /// <summary>The LIVE identity ID, after redirect-chasing.</summary>
    public string? ID { get; set; }

    public string? RequestedID { get; set; }

    public bool WasRedirected { get; set; }

    public List<GoldenCustomerVehicleDTO> Vehicles { get; set; } = new();
}

/// <summary>One VIN link of a golden, with the ownership period the resolve computed for it.</summary>
public class GoldenCustomerVehicleDTO
{
    public string VIN { get; set; } = default!;

    /// <summary><c>owner</c> (sale-grade evidence opened an ownership period) or
    /// <c>service-contact</c> (only service evidence: an observation window, never an ownership claim).</summary>
    public string Role { get; set; } = default!;

    /// <summary>Start of the ownership period (owner) or first service seen (service contact).</summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>End of the period: the next owner's sale for an owner (null = still the owner), the
    /// last service seen for a service contact.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public int SaleCount { get; set; }

    public int ServiceCount { get; set; }

    public DateOnly? FirstSale { get; set; }

    public DateOnly? LastSale { get; set; }

    public DateOnly? FirstService { get; set; }

    public DateOnly? LastService { get; set; }

    /// <summary>The source profiles that evidence this link, as <c>sourceSystem|sourceRecordId</c>.</summary>
    public List<string> Sources { get; set; } = new();
}

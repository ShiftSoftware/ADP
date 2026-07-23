using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Customer;

/// <summary>
/// One resolved vehicle↔customer ownership link: a <see cref="GoldenCustomerModel">Golden Customer</see>
/// holding one role on one VIN over one time period, with the evidence that supports it.
/// The element type shared by both projections of the ownership data:
/// <see cref="GoldenCustomerVehicleLinksModel"/> (by customer) and
/// <see cref="Vehicle.VehicleGoldenOwnershipModel"/> (by VIN).
/// </summary>
[Docable]
public class GoldenVehicleLinkModel
{
    /// <summary>
    /// The vehicle's VIN.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// The <see cref="GoldenCustomerModel">Golden Customer</see> this link belongs to.
    /// </summary>
    public string GoldenCustomerID { get; set; }

    /// <summary>
    /// The golden customer's survived full name — denormalized for display (service-bay forms,
    /// ownership-history views) without a second read.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// The role the customer holds on the vehicle. Extensible vocabulary; currently
    /// "owner" (sale-grade evidence binds the VIN to this customer) or
    /// "service-contact" (the customer is only observed bringing the vehicle in for service —
    /// possibly a driver, relative, or later owner; never treated as ownership).
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// When the link becomes effective. For "owner" links: the first sale-grade evidence date.
    /// For "service-contact" links: the first observed service visit. Null when the evidence
    /// carries no usable date.
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// When the link ends. For "owner" links: the next owner's <see cref="EffectiveFrom"/> when an
    /// ownership transfer is detected (a later sale of the same VIN to a different golden customer);
    /// null while the link is current. For "service-contact" links: the last observed service visit
    /// (an observation window, not a tenure claim).
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Number of sale-grade evidence records (vehicle sales / warranty activations) behind this link.
    /// </summary>
    public int SaleCount { get; set; }

    /// <summary>
    /// Number of service-visit evidence records behind this link.
    /// </summary>
    public int ServiceCount { get; set; }

    /// <summary>
    /// Date range covered by the sale-grade evidence, when dated.
    /// </summary>
    public DateTime? FirstSale { get; set; }

    /// <summary>
    /// See <see cref="FirstSale"/>.
    /// </summary>
    public DateTime? LastSale { get; set; }

    /// <summary>
    /// Date range covered by the service-visit evidence, when dated.
    /// </summary>
    public DateTime? FirstService { get; set; }

    /// <summary>
    /// See <see cref="FirstService"/>.
    /// </summary>
    public DateTime? LastService { get; set; }

    /// <summary>
    /// The source records that contributed evidence to this link, as "sourceSystem|sourceRecordId"
    /// keys (the same key form as <see cref="GoldenCustomerModel.SourceProfiles"/>).
    /// </summary>
    public IEnumerable<string> Sources { get; set; } = [];
}

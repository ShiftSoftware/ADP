using System.Collections.Generic;
using ShiftSoftware.ADP.Models.Customer;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// The by-VIN projection of vehicle ownership: every <see cref="GoldenCustomerModel"/> linked to
/// one VIN, ordered as an ownership timeline — current-owner lookup and ownership history for a
/// vehicle. One document per VIN that has any customer links.
/// The current owner is the "owner"-role link with a null <see cref="GoldenVehicleLinkModel.EffectiveTo"/>.
/// Note: golden ownership deliberately spans companies, so this document carries no CompanyID —
/// it lives at the container's undefined CompanyID partition level.
/// </summary>
[Docable]
public class VehicleGoldenOwnershipModel :
    IPartitionedItem
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The vehicle's VIN. Same value as <see cref="id"/>.
    /// </summary>
    public string VIN { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.VehicleGoldenOwnership;

    /// <summary>
    /// The vehicle's customer links: "owner"-role links first in ownership-timeline order
    /// (each closed by the next owner's <see cref="GoldenVehicleLinkModel.EffectiveFrom"/>),
    /// then "service-contact" links.
    /// </summary>
    public IEnumerable<GoldenVehicleLinkModel> Owners { get; set; } = [];
}

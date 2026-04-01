using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a claim made against a <see cref="ServiceItemModel">Service Item</see> for a specific vehicle.
/// Tracks the claim details including cost, associated inspection, and job/invoice references.
/// </summary>
[Docable]
public class ItemClaimModel : IPartitionedItem, ICompanyProps, IBranchProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this claim is for.
    /// </summary>
    public string VIN { get; set; } = default!;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    [DocIgnore]
    public long? BranchID { get; set; }

    /// <summary>
    /// The Branch Hash ID from the Identity System.
    /// </summary>
    public string BranchHashID { get; set; }

    /// <summary>
    /// The date when this service item was claimed.
    /// </summary>
    public DateTimeOffset ClaimDate { get; set; }

    /// <summary>
    /// Indicates whether this claim has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The ID of the <see cref="ServiceItemModel">Service Item</see> being claimed.
    /// </summary>
    public string ServiceItemID { get; set; }

    /// <summary>
    /// The ID of the <see cref="VehicleInspectionModel">Vehicle Inspection</see> associated with this claim, if any.
    /// </summary>
    public string VehicleInspectionID { get; set; }

    /// <summary>
    /// The cost of this claim.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// The package code grouping this claim with related service items.
    /// </summary>
    public string PackageCode { get; set; }

    /// <summary>
    /// The job number from the dealer's service system.
    /// </summary>
    public string JobNumber { get; set; }

    /// <summary>
    /// The invoice number associated with this claim.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// A QR code identifier for this claim.
    /// </summary>
    public string QRCode { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.ItemClaim;
}
using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a warranty claim submitted for a vehicle repair.
/// Tracks the claim lifecycle from submission through distributor and manufacturer processing, including repair details and labor operations.
/// </summary>
[Docable]
public class WarrantyClaimModel : IPartitionedItem, IBrandProps, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) this warranty claim is for.
    /// </summary>
    public string VIN { get; set; }

    /// <summary>
    /// Indicates whether this warranty claim has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The date the repair was performed.
    /// </summary>
    public DateTime? RepairDate { get; set; }

    /// <summary>
    /// The current status of this warranty claim in the processing lifecycle.
    /// </summary>
    public ClaimStatus ClaimStatus { get; set; }

    /// <summary>
    /// The manufacturer's status for this warranty claim.
    /// </summary>
    public WarrantyManufacturerClaimStatus ManufacturerStatus { get; set; }

    /// <summary>
    /// Comments added by the distributor during claim processing.
    /// </summary>
    public string DistributorComment { get; set; }

    /// <summary>
    /// The main labor operation number for this claim.
    /// </summary>
    public string LaborOperationNumberMain { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.WarrantyClaim;

    /// <summary>
    /// The unique claim number assigned to this warranty claim.
    /// </summary>
    public string ClaimNumber { get; set; }

    /// <summary>
    /// The invoice number associated with this warranty claim.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// The dealer's own claim number for internal tracking.
    /// </summary>
    public string DealerClaimNumber { get; set; }

    /// <summary>
    /// The date the claim was received by the distributor.
    /// </summary>
    public DateTime? DateOfReceipt { get; set; }

    /// <summary>
    /// The type of warranty coverage (e.g., Standard, Extended).
    /// </summary>
    public string WarrantyType { get; set; }

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }

    /// <summary>
    /// The vehicle delivery date to the customer.
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// The date the repair was completed.
    /// </summary>
    public DateTime? RepairCompletionDate { get; set; }

    /// <summary>
    /// The vehicle's odometer reading at the time of the warranty repair.
    /// </summary>
    public int? Odometer { get; set; }

    /// <summary>
    /// The repair order number from the dealer's service system.
    /// </summary>
    public string RepairOrderNumber { get; set; } = default!;

    /// <summary>
    /// The date this claim was processed by the manufacturer.
    /// </summary>
    public DateTime? ProcessDate { get; set; }

    /// <summary>
    /// The date this claim was processed by the distributor.
    /// </summary>
    public DateTime? DistributorProcessDate { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// The <see cref="WarrantyClaimLaborLineModel">labor lines</see> associated with this warranty claim.
    /// </summary>
    public IEnumerable<WarrantyClaimLaborLineModel> LaborLines { get; set; }
}
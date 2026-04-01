namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents an accessory or aftermarket part installed on a vehicle.
/// Tracks the part details, installation job/invoice references, and an optional image.
/// </summary>
[Docable]
public class VehicleAccessoryModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Vehicle Identification Number (VIN) this accessory is installed on.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The part number of the installed accessory.
    /// </summary>
    public string PartNumber { get; set; } = default!;

    /// <summary>
    /// A description of the accessory part.
    /// </summary>
    public string PartDescription { get; set; } = default!;

    /// <summary>
    /// The job number from the dealer's service system for the accessory installation.
    /// </summary>
    public int JobNumber { get; set; } = default!;

    /// <summary>
    /// The invoice number for the accessory installation.
    /// </summary>
    public int InvoiceNumber { get; set; }

    /// <summary>
    /// URL of an image of the installed accessory.
    /// </summary>
    public string Image { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.VehicleAccessory;
}
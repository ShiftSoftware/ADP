namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a labor line item within a <see cref="WarrantyClaimModel">Warranty Claim</see>.
/// Each line captures a specific labor operation performed during the warranty repair.
/// </summary>
[Docable]
public class WarrantyClaimLaborLineModel
{
    /// <summary>
    /// The unique identifier for this labor line.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// The pay code indicating the payment type for this labor operation.
    /// </summary>
    public string PayCode { get; set; }

    /// <summary>
    /// Indicates whether this is the main (primary) operation on the warranty claim.
    /// </summary>
    public bool MainOperation { get; set; }

    /// <summary>
    /// The labor operation code identifying the type of work performed.
    /// </summary>
    public string LaborCode { get; set; }

    /// <summary>
    /// The number of labor hours claimed by the dealer for this operation.
    /// </summary>
    public decimal Hour { get; set; }

    /// <summary>
    /// The number of labor hours approved by the distributor. May differ from the dealer-claimed hours.
    /// </summary>
    public decimal? DistributorHour { get; set; }
}
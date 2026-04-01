using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a Special Service Campaign (SSC) / Safety Recall record for a specific vehicle.
/// Each record links a VIN to a recall campaign, including the labor operations and parts required for the repair.
/// </summary>
[Docable]
public class SSCAffectedVINModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The Vehicle Identification Number (VIN) affected by this recall campaign.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The unique code identifying the recall/SSC campaign.
    /// </summary>
    public string CampaignCode { get; set; }

    /// <summary>
    /// A description of the recall campaign and the issue being addressed.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The first labor operation code required for the recall repair.
    /// </summary>
    public string LaborCode1 { get; set; }

    /// <summary>
    /// The estimated hours for the first labor operation.
    /// </summary>
    public double? LaborHour1 { get; set; }

    /// <summary>
    /// The second labor operation code, if applicable.
    /// </summary>
    public string LaborCode2 { get; set; }

    /// <summary>
    /// The estimated hours for the second labor operation.
    /// </summary>
    public double? LaborHour2 { get; set; }

    /// <summary>
    /// The third labor operation code, if applicable.
    /// </summary>
    public string LaborCode3 { get; set; }

    /// <summary>
    /// The estimated hours for the third labor operation.
    /// </summary>
    public double? LaborHour3 { get; set; }

    /// <summary>
    /// The first part number required for the recall repair.
    /// </summary>
    public string PartNumber1 { get; set; }

    /// <summary>
    /// The second part number required for the recall repair, if applicable.
    /// </summary>
    public string PartNumber2 { get; set; }

    /// <summary>
    /// The third part number required for the recall repair, if applicable.
    /// </summary>
    public string PartNumber3 { get; set; }

    /// <summary>
    /// The date the recall repair was completed. Null if the recall has not been repaired yet.
    /// </summary>
    public DateTime? RepairDate { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.SSCAffectedVIN;

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// The labor operations required for the recall repair. This is the preferred, unbounded representation and
    /// supersedes the legacy numbered <see cref="LaborCode1"/>…<see cref="LaborCode3"/> /
    /// <see cref="LaborHour1"/>…<see cref="LaborHour3"/> fields when populated.
    /// </summary>
    public List<SSCLaborLineModel> Labors { get; set; }

    /// <summary>
    /// The part numbers required for the recall repair. This is the preferred, unbounded representation and
    /// supersedes the legacy numbered <see cref="PartNumber1"/>…<see cref="PartNumber3"/> fields when populated.
    /// </summary>
    public List<string> PartNumbers { get; set; }

    // --- Legacy numbered fields -------------------------------------------------------------------------------
    // Retained so SSC records produced before the move to the Labors/PartNumbers arrays still deserialize.
    // Readers must go through EffectiveLabors / EffectivePartNumbers (never these directly) so both the old and
    // new shapes resolve. Remove once every producer (Toyota Central Asia Hub + tiq-sync-agent) emits the array
    // shape and pre-migration data has aged out. See .shift/repos/adp/ssc-multi-part-labor/.

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
    /// The effective labor operations for this campaign — <see cref="Labors"/> when populated, otherwise the
    /// legacy numbered fields. Always read this instead of either source directly.
    /// </summary>
    [DocIgnore]
    public IEnumerable<SSCLaborLineModel> EffectiveLabors =>
        Labors != null && Labors.Count > 0
            ? Labors
            : new[]
            {
                new SSCLaborLineModel { LaborCode = LaborCode1, LaborHour = LaborHour1 },
                new SSCLaborLineModel { LaborCode = LaborCode2, LaborHour = LaborHour2 },
                new SSCLaborLineModel { LaborCode = LaborCode3, LaborHour = LaborHour3 },
            }.Where(l => !string.IsNullOrWhiteSpace(l.LaborCode));

    /// <summary>
    /// The effective part numbers for this campaign — <see cref="PartNumbers"/> when populated, otherwise the
    /// legacy numbered fields. Always read this instead of either source directly.
    /// </summary>
    [DocIgnore]
    public IEnumerable<string> EffectivePartNumbers =>
        PartNumbers != null && PartNumbers.Count > 0
            ? PartNumbers
            : new[] { PartNumber1, PartNumber2, PartNumber3 }.Where(p => !string.IsNullOrWhiteSpace(p));

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
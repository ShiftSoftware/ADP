using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Service;

/// <summary>
/// Represents a flat-rate labor time entry used to look up standard repair times.
/// Each entry maps a labor code and vehicle variant (identified by WMI and VDS from the VIN) to a set of time allowances.
/// </summary>
[Docable]
public class FlatRateModel : IBrandProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The labor operation code identifying the type of work.
    /// </summary>
    public string LaborCode { get; set; } = default!;

    /// <summary>
    /// The Vehicle Descriptor Section (VDS) from the VIN, identifying the vehicle model/variant.
    /// </summary>
    public string VDS { get; set; } = default!;

    /// <summary>
    /// A dictionary of time allowances keyed by time category. Values are in hours.
    /// </summary>
    public Dictionary<string, decimal?> Times { get; set; }

    /// <summary>
    /// The World Manufacturer Identifier (WMI) from the VIN, identifying the vehicle manufacturer.
    /// </summary>
    public string WMI { get; set; } = default!;

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }
}
using ShiftSoftware.ADP.Models;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Service;

/// <summary>
/// Represents a flat-rate labor time lookup result, as returned by the service lookup.
/// </summary>
[Docable]
public class FlatRateDTO
{
    [DocIgnore]
    public new string id { get; set; }
    /// <summary>The labor operation code.</summary>
    public string LaborCode { get; set; }
    /// <summary>The Vehicle Descriptor Section (VDS) from the VIN.</summary>
    public string VDS { get; set; }
    /// <summary>Time allowances keyed by category, in hours.</summary>
    public Dictionary<string, decimal?> Times { get; set; }
    /// <summary>The World Manufacturer Identifier (WMI) from the VIN.</summary>
    public string WMI { get; set; }
    /// <summary>The Brand Hash ID from the Identity System.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string BrandID { get; set; }
}
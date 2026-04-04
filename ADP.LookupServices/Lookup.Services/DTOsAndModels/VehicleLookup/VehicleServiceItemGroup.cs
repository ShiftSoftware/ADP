using ShiftSoftware.ADP.Models;
namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Defines a grouping for service items, used to organize them into tabs in the UI.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceItemGroup
{
    /// <summary>The display name of the group/tab.</summary>
    public string Name { get; set; }
    /// <summary>The display order of this tab in the UI.</summary>
    public int TabOrder { get; set; }
    /// <summary>Whether this is the default (initially selected) tab.</summary>
    public bool IsDefault { get; set; }
    /// <summary>Whether the service items in this group follow sequential validity (one after another).</summary>
    public bool IsSequential { get; set; }
}
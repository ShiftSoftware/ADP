using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// A single panel reading on a <see cref="PaintThicknessCertificateModel">Paint Thickness Certificate</see>.
/// One row in the certificate's readings table; carries the resolved panel-image URLs for the gallery.
/// </summary>
[TypeScriptModel]
[Docable]
public class PaintThicknessCertificateReadingModel
{
    /// <summary>The type of vehicle panel (e.g., Door, Fender, Roof, Hood, Tail Gate).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelType PanelType { get; set; }

    /// <summary>The side of the vehicle this panel is on (Left, Center, Right), if specified.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelSide? PanelSide { get; set; }

    /// <summary>The position of the panel on the vehicle (Front, Middle, Rear), if specified.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehiclePanelPosition? PanelPosition { get; set; }

    /// <summary>The measured paint thickness on this panel, in microns.</summary>
    public decimal MeasuredThickness { get; set; }

    /// <summary>
    /// A human-readable label for this panel, e.g. "Door (Front Left)" — derived from the panel's
    /// type/position/side <c>[Description]</c> attributes. Convenience for the print template and landing page.
    /// </summary>
    public string PanelLabel { get; set; }

    /// <summary>Resolved photo URLs of this panel taken during the inspection.</summary>
    public List<string> Images { get; set; } = [];
}

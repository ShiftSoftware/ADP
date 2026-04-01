using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a paint thickness inspection performed on a vehicle.
/// Paint thickness measurements across multiple <see cref="PaintThicknessInspectionPanelModel">panels</see> help detect prior bodywork, repainting, or accident damage.
/// </summary>
[Docable]
public class PaintThicknessInspectionModel : IPartitionedItem, ICompanyProps
{
    [DocIgnore]
    public string id { get; set; } = default!;

    /// <summary>
    /// The model year of the vehicle at the time of inspection.
    /// </summary>
    public string ModelYear { get; set; }

    /// <summary>
    /// A description of the vehicle model at the time of inspection.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// The exterior color code of the vehicle at the time of inspection.
    /// </summary>
    public string ColorCode { get; set; }

    /// <summary>
    /// The date the paint thickness inspection was performed.
    /// </summary>
    public DateTime? InspectionDate { get; set; }

    /// <summary>
    /// The Vehicle Identification Number (VIN) of the inspected vehicle.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The individual <see cref="PaintThicknessInspectionPanelModel">panel measurements</see> taken during the inspection.
    /// </summary>
    public IEnumerable<PaintThicknessInspectionPanelModel> Panels { get; set; }

    [DocIgnore]
    public long? CompanyID { get; set; }

    /// <summary>
    /// The Company Hash ID from the Identity System.
    /// </summary>
    public string CompanyHashID { get; set; }

    /// <summary>
    /// The source system or process that performed this inspection.
    /// </summary>
    public string Source { get; set; }

    [DocIgnore]
    public string ItemType => ModelTypes.PaintThicknessInspection;
}
using ShiftSoftware.ADP.Models;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One service that has to have happened before an item unlocks.
/// <para>
/// Carries no catalog name, and cannot: prerequisites are services the customer pays for outside the
/// claimable-item catalog, so there is no entry to read a name from. <see cref="Label"/> is the
/// mileage rendered, and it is the only name there is.
/// </para>
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceItemPrerequisiteDTO
{
    /// <summary>The scheduled service, in kilometres.</summary>
    public long Mileage { get; set; }

    /// <summary>
    /// The mileage written the way the milestone itself is written — "45K" for 45,000. A plain
    /// rendering of <see cref="Mileage"/>, not a name.
    /// </summary>
    public string Label { get; set; }

    /// <summary>Whether the vehicle's service history records this service.</summary>
    public bool Satisfied { get; set; }

    /// <summary>
    /// When it was first recorded, or null when it has not been. The earliest invoice date, so a
    /// service performed twice reports when the prerequisite was met rather than when it was repeated.
    /// </summary>
    public DateTime? SatisfiedOn { get; set; }
}

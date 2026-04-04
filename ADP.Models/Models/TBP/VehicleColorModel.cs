namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents a vehicle color in the broker system, with a code and description.
/// Used for both exterior and interior colors on <see cref="BrokerVehicleModel">broker vehicles</see>.
/// </summary>
[Docable]
public class VehicleColorModel
{
    /// <summary>
    /// The color code.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// A human-readable description of the color.
    /// </summary>
    public string Description { get; set; }
}

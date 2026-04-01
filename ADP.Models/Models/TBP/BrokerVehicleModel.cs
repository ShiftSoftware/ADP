namespace ShiftSoftware.ADP.Models.TBP;

/// <summary>
/// Represents vehicle details as stored in the broker stock context.
/// Contains the vehicle specifications and color information relevant to broker inventory.
/// </summary>
[Docable]
public class BrokerVehicleModel
{
    /// <summary>
    /// The Vehicle Identification Number (VIN).
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The vehicle model description.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// The model year of the vehicle.
    /// </summary>
    public string ModelYear { get; set; }

    /// <summary>
    /// The engine description.
    /// </summary>
    public string Engine { get; set; }

    /// <summary>
    /// A description of the vehicle variant.
    /// </summary>
    public string VariantDescription { get; set; }

    /// <summary>
    /// The variant code within the model range.
    /// </summary>
    public string VariantCode { get; set; }

    /// <summary>
    /// The Katashiki (manufacturer-specific model identifier).
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The number of cylinders in the engine.
    /// </summary>
    public string Cylinders { get; set; }

    /// <summary>
    /// The region ID where this vehicle is stocked.
    /// </summary>
    public long? StockRegionID { get; set; }

    /// <summary>
    /// The <see cref="VehicleColorModel">exterior color</see> of the vehicle.
    /// </summary>
    public VehicleColorModel ExteriorColor { get; set; } = new();

    /// <summary>
    /// The <see cref="VehicleColorModel">interior color</see> of the vehicle.
    /// </summary>
    public VehicleColorModel InteriorColor { get; set; } = new();
}
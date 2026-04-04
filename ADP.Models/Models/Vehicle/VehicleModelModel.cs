namespace ShiftSoftware.ADP.Models.Vehicle;

/// <summary>
/// Represents a vehicle model variant with its full technical specifications.
/// Each record defines a specific combination of model, variant, body type, engine, transmission, and other attributes for a brand.
/// </summary>
[Docable]
public class VehicleModelModel : IBrandProps
{
    [DocIgnore]
    public string id { get; set; }

    [DocIgnore]
    public long? BrandID { get; set; }

    /// <summary>
    /// The Brand Hash ID from the Identity System.
    /// </summary>
    public string BrandHashID { get; set; }

    /// <summary>
    /// The basic model code that identifies the vehicle model line.
    /// </summary>
    public string BasicModelCode { get; set; }

    /// <summary>
    /// The suffix code differentiating trim or equipment levels within a model.
    /// </summary>
    public string SFX { get; set; }

    /// <summary>
    /// The full model code identifying this vehicle model.
    /// </summary>
    public string ModelCode { get; set; }

    /// <summary>
    /// A human-readable description of the vehicle model.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// The variant code within the model range.
    /// </summary>
    public string VariantCode { get; set; }

    /// <summary>
    /// A human-readable description of the variant.
    /// </summary>
    public string VariantDescription { get; set; }

    /// <summary>
    /// The Katashiki (manufacturer-specific model identifier).
    /// </summary>
    public string Katashiki { get; set; }

    /// <summary>
    /// The vehicle class (e.g., Sedan, SUV, Truck).
    /// </summary>
    public string Class { get; set; }

    /// <summary>
    /// The body type of the vehicle (e.g., Hatchback, Coupe, Wagon).
    /// </summary>
    public string BodyType { get; set; }

    /// <summary>
    /// The engine description (e.g., displacement, configuration).
    /// </summary>
    public string Engine { get; set; }

    /// <summary>
    /// The number of cylinders in the engine.
    /// </summary>
    public string Cylinders { get; set; }

    /// <summary>
    /// Whether this is a light or heavy vehicle type.
    /// </summary>
    public string LightHeavyType { get; set; }

    /// <summary>
    /// The number of doors on the vehicle.
    /// </summary>
    public string Doors { get; set; }

    /// <summary>
    /// The fuel type (e.g., Gasoline, Diesel, Hybrid, Electric).
    /// </summary>
    public string Fuel { get; set; }

    /// <summary>
    /// The transmission type (e.g., Automatic, Manual, CVT).
    /// </summary>
    public string Transmission { get; set; }

    /// <summary>
    /// The steering/driving side (e.g., Left-Hand Drive, Right-Hand Drive).
    /// </summary>
    public string Side { get; set; }

    /// <summary>
    /// The engine type classification.
    /// </summary>
    public string EngineType { get; set; }

    /// <summary>
    /// The fuel tank capacity.
    /// </summary>
    public string TankCap { get; set; }

    /// <summary>
    /// The style or design trim of the vehicle.
    /// </summary>
    public string Style { get; set; }
}
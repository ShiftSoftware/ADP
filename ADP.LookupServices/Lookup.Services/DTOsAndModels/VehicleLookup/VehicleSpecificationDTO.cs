using ShiftSoftware.ADP.Models;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;


/// <summary>
/// Contains the technical specifications for a vehicle — model details, body, engine, transmission, fuel, and colors.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleSpecificationDTO
{
    /// <summary>The model code identifying the vehicle model.</summary>
    public string ModelCode { get; set; }
    /// <summary>The model year of the vehicle.</summary>
    public int? ModelYear { get; set; }
    /// <summary>The date the vehicle was produced.</summary>
    public DateTime? ProductionDate { get; set; }
    /// <summary>A human-readable description of the vehicle model.</summary>
    public string ModelDescription { get; set; }
    /// <summary>A human-readable description of the vehicle variant.</summary>
    public string VariantDescription { get; set; }
    /// <summary>The vehicle class (e.g., Sedan, SUV, Truck).</summary>
    public string Class { get; set; }
    /// <summary>The body type (e.g., Hatchback, Coupe, Wagon).</summary>
    public string BodyType { get; set; }
    /// <summary>The engine description (e.g., displacement, configuration).</summary>
    public string Engine { get; set; }
    /// <summary>The number of cylinders in the engine.</summary>
    public string Cylinders { get; set; }
    /// <summary>Whether this is a light or heavy vehicle type.</summary>
    public string LightHeavyType { get; set; }
    /// <summary>The number of doors.</summary>
    public string Doors { get; set; }
    /// <summary>The fuel type (e.g., Gasoline, Diesel, Hybrid, Electric).</summary>
    public string Fuel { get; set; }
    /// <summary>The transmission type (e.g., Automatic, Manual, CVT).</summary>
    public string Transmission { get; set; }
    /// <summary>The steering/driving side (e.g., Left-Hand Drive, Right-Hand Drive).</summary>
    public string Side { get; set; }
    /// <summary>The engine type classification.</summary>
    public string EngineType { get; set; }
    /// <summary>The fuel tank capacity.</summary>
    public string TankCap { get; set; }
    /// <summary>The style or design trim of the vehicle.</summary>
    public string Style { get; set; }
    /// <summary>The fuel capacity in liters.</summary>
    public int? FuelLiter { get; set; }
    /// <summary>The resolved exterior color description.</summary>
    public string ExteriorColor { get; set; }
    /// <summary>The resolved interior color description.</summary>
    public string InteriorColor { get; set; }
}
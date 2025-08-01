﻿using ShiftSoftware.ADP.Models;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;


[TypeScriptModel]
public class VehicleSpecificationDTO
{
    public string ModelCode { get; set; }
    public int? ModelYear { get; set; }
    public DateTime? ProductionDate { get; set; }
    public string ModelDescription { get; set; }
    public string VariantDescription { get; set; }
    public string Class { get; set; }
    public string BodyType { get; set; }
    public string Engine { get; set; }
    public string Cylinders { get; set; }
    public string LightHeavyType { get; set; }
    public string Doors { get; set; }
    public string Fuel { get; set; }
    public string Transmission { get; set; }
    public string Side { get; set; }
    public string EngineType { get; set; }
    public string TankCap { get; set; }
    public string Style { get; set; }
    public int? FuelLiter { get; set; }
    public string ExteriorColor { get; set; }
    public string InteriorColor { get; set; }
}
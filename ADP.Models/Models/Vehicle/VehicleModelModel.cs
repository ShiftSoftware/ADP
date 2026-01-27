using ShiftSoftware.ADP.Models.Content;
using ShiftSoftware.ShiftEntity.Model.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleModelModel: IBrandProps, IContentProps
{
    public string id { get; set; }
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
    public string BasicModelCode { get; set; }
    public string SFX { get; set; }
    public string ModelCode { get; set; }
    public string ModelDescription { get; set; }
    public string VariantCode { get; set; }
    public string VariantDescription { get; set; }
    public string Katashiki { get; set; }
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
    public string DisplayName { get; set; }
    public IEnumerable<PublishTarget> PublishTargets { get; set; } = new HashSet<PublishTarget>();
    public long ContentID { get; set; }
}
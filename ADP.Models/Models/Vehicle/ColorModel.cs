using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ColorModel: IBrandProps
{
    public string id { get; set; }
    public Brands Brand { get; set; }
    public string BrandID { get; set; }
    public string BrandHashID { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
}
namespace ShiftSoftware.ADP.Models.Vehicle;

public class ColorModel: IBrandProps
{
    public string id { get; set; }
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
}
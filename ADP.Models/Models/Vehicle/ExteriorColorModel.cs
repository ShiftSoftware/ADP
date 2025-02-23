using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ExteriorColorModel
{
    public string id { get; set; }
    public Brands Brand { get; set; }
    public string BrandIntegrationID { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
}
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models;

internal interface IBrandProps
{
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
    public Brands Brand { get; set; }
}
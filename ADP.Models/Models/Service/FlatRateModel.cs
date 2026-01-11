using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Service;

public class FlatRateModel : IBrandProps
{
    public string id { get; set; } = default!;
    public string LaborCode { get; set; } = default!;
    public string VDS { get; set; } = default!;
    public Dictionary<string, decimal?> Times { get; set; }
    public string WMI { get; set; } = default!;
    public long? BrandID { get; set; }
    public string BrandHashID { get; set; }
}
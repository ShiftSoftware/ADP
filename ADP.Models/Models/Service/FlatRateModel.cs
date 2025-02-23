using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Service;

public class FlatRateModel
{
    public string id { get; set; } = default!;
    public string LaborCode { get; set; } = default!;
    public string VDS { get; set; } = default!;
    public Dictionary<string, decimal?> Times { get; set; }
    public string WMI { get; set; } = default!;
    public Brands? Brand { get; set; }
}
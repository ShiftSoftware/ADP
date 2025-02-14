using ShiftSoftware.ADP.Models.Enums;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Service.CosmosModels;

public class FlatRateCosmosModel : CacheableCSV
{
    public new string id { get; set; } = default!;
    public string LaborCode { get; set; } = default!;
    public string VDS { get; set; } = default!;
    public Dictionary<string, decimal?> Times { get; set; }
    public string? WMI { get; set; } = default!;
    public Franchises? Brand { get; set; }
}
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Service;

public class FlatRateDTO
{
    public new string id { get; set; }

    public string LaborCode { get; set; }

    public string VDS { get; set; }

    public Dictionary<string, decimal?> Times { get; set; }

    public string WMI { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string BrandID { get; set; }
}
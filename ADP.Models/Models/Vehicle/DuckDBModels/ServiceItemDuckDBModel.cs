using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class ServiceItemDuckDBModel : ServiceItemModel
{
    public new long id { get; set; }
    public DateTime? LastSaveDate { get; set; }
}

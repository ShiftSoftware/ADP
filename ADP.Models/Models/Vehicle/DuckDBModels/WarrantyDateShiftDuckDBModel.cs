using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class WarrantyDateShiftDuckDBModel : WarrantyDateShiftModel
{
    public new long id { get; set; }
    public DateTimeOffset? LastSaveDate { get; set; }
}

using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class ExtendedWarrantyDuckDBModel : ExtendedWarrantyModel
{
    public new long id { get; set; }
    public DateTime? LastSaveDate { get; set; }
}

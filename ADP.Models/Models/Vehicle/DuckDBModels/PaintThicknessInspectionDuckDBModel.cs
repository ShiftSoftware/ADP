using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class PaintThicknessInspectionDuckDBModel : PaintThicknessInspectionModel
{
    public new long id { get; set; }
    public DateTime? LastSaveDate { get; set; }
}

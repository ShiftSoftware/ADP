using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaintThicknessInspectionModel : IPartitionedItem, ICompanyProps
{
    public string id { get; set; } = default!;
    public string ModelYear { get; set; }
    public string ModelDescription { get; set; }
    public string ColorCode { get; set; }
    public DateTime? InspectionDate { get; set; }
    public string VIN { get; set; } = default!;
    public IEnumerable<PaintThicknessInspectionPanelModel> Panels { get; set; }
    public long? CompanyID { get; set; }
    public string CompanyHashID { get; set; }
    public string Source { get; set; }
    public string ItemType => ModelTypes.PaintThicknessInspection;
}
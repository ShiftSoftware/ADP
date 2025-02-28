using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class PaintThicknessInspectionModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public long ID { get; set; }
    public string Katashiki { get; set; }
    public string ModelYear { get; set; }
    public string ModelDescription { get; set; }
    public string ColorCode { get; set; }
    public DateTime? CreateDate { get; set; }
    public IEnumerable<string> Images { get; set; }
    public string VIN { get; set; } = default!;
    public IEnumerable<PaintThicknessPartModel> Parts { get; set; }
    public string ItemType => ModelTypes.PaintThicknessInspection;
}
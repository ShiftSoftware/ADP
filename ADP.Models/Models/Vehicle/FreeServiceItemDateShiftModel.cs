using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class FreeServiceItemDateShiftModel : IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime NewDate { get; set; }
    public PartitionedItemType ItemType => ModelTypes.FreeServiceItemDateShift;
}
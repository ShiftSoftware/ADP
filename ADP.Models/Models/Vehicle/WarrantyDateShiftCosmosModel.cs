using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyDateShiftCosmosModel : IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime NewDate { get; set; }
    public string ItemType => ModelTypes.WarrantyDateShift;
}
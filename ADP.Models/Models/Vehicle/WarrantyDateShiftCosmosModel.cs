using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class WarrantyDateShiftCosmosModel : IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime NewInvoiceDate { get; set; }
    public PartitionedItemType ItemType => ModelTypes.WarrantyDateShift;
}
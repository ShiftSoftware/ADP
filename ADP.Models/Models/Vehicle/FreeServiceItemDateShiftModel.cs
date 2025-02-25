using System;

namespace ShiftSoftware.ADP.Models.LookupCosmosModels;

public class FreeServiceItemDateShiftModel: IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime NewInvoiceDate { get; set; }
    public int ShiftDays { get; set; }
    public PartitionedItemType ItemType => ModelTypes.FreeServiceItemDateShift;
}
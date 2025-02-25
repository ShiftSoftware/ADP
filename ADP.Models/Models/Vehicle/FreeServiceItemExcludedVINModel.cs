namespace ShiftSoftware.ADP.Models.LookupCosmosModels;

public class FreeServiceItemExcludedVINModel: IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public PartitionedItemType ItemType => ModelTypes.FreeServiceItemExcludedVIN;
}
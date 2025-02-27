namespace ShiftSoftware.ADP.Models.Vehicle;

public class FreeServiceItemExcludedVINModel : IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => ModelTypes.FreeServiceItemExcludedVIN;
}
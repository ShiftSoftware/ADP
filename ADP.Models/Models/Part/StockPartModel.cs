namespace ShiftSoftware.ADP.Models.Part;

public class StockPartModel: IPartitionedItem
{
    public string id { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string PartNumber { get; set; }
    public string PartDescription { get; set; }
    public string SupersededTo { get; set; }
    public string SupersededFrom { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? FOB { get; set; }
    public string Group { get; set; }
    public string InventoryType { get; set; }
    public PartitionedItemType ItemType => ModelTypes.StockPart;
}
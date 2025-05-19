namespace ShiftSoftware.ADP.Models.Part;

[Docable]
public class StockPartModel: IPartitionedItem
{
    public string id { get; set; } = default!;
    public string Location { get; set; } = default!;
    public string PartNumber { get; set; }
    public decimal Quantity { get; set; }
    public string ItemType => ModelTypes.StockPart;
}
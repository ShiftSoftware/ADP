namespace ShiftSoftware.ADP.Models.Vehicle;

public class AccessoryModel
{
    public string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string PartNumber { get; set; } = default!;
    public string PartDescription { get; set; } = default!;
    public int JobNumber { get; set; } = default!;
    public int InvoiceNumber { get; set; }
    public string Image { get; set; }
    public string ItemType => "Accessory";
}
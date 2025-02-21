using System;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class AccessoryCosmosModel
{
    public string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string PartNumber { get; set; } = default!;
    public string PartDescription { get; set; } = default!;
    public int WIP { get; set; } = default!;
    public int InvoiceNumber { get; set; }
    public DateTime DateEdited { get; set; }
    public string City { get; set; } = default!;
    public string Image { get; set; }
    public string ItemType => "Accessory";
}
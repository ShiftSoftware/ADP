using System;

namespace ShiftSoftware.ADP.Models.LookupCosmosModels;

public class WarrantyDateShiftCosmosModel
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => "WarrantyDateShift";
    public DateTime NewInvoiceDate { get; set; }
}

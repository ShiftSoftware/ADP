using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.LookupCosmosModels;

public class VehicleFreeServiceInvoiceDateShiftVINsCosmosModel
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => "VehicleFreeServiceInvoiceDateShiftVINs";
    public DateTime NewInvoiceDate { get; set; }
    public int ShiftDays { get; set; }
}

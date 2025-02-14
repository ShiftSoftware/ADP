using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.LookupCosmosModels;

public class VehicleFreeServiceExcludedVINsCosmosModel
{
    public string id { get; set; }
    public string VIN { get; set; }
    public string ItemType => "VehicleFreeServiceExcludedVINs";
}

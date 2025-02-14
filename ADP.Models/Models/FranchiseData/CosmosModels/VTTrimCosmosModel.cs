using System;
using System.Collections.Generic;
using System.Text;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.FranchiseData.CosmosModels;

public class VTTrimCosmosModel : VTTrimCSV
{
    public new string id { get; set; }
    public Franchises Brand { get; set; }
    public string BrandIntegrationID { get; set; }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class CPUCosmosModel : CPUCSV
{
    public new string id { get; set; } = default!;
    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.DealerData;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.FranchiseData;
using ShiftSoftware.ADP.Models.FranchiseData.CosmosModels;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class VSDataCosmosModel : VSDataCSV
{
    public new string id { get; set; } = default!;
    public VTModelRecordsCosmosModel VTModel { get; set; }
    public VTColorCosmosModel VTColor { get; set; }
    public VTTrimCosmosModel VTTrim { get; set; }

    public Franchises Brand { get; set; }
    public string BrandIntegrationID { get; set; }

    public string DealerIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }

    public string RegionIntegrationId { get; set; }
}

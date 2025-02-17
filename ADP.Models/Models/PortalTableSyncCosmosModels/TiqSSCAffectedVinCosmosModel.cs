using ShiftSoftware.ADP.Models.DealerData;
using System;

namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class TiqSSCAffectedVinCosmosModel : IDealerDataCSV
{
    public string id { get; set; } = default!;

    public long TiqsscaffectedVinid { get; set; }

    public string VIN { get; set; } = default!;

    public string SscDescription { get; set; }

    public string CampaignCode { get; set; }

    public string Status { get; set; }

    public string OpCode { get; set; }
    public string OpCode2 { get; set; }
    public string OpCode3 { get; set; }
    public DateTime? RepairDate { get; set; }

    public string OriginalFormatPartNumber1 { get; set; }
    public string PartNumber1 { get; set; }

    public string OriginalFormatPartNumber2 { get; set; }
    public string PartNumber2 { get; set; }

    public string OriginalFormatPartNumber3 { get; set; }
    public string PartNumber3 { get; set; }

    public string ItemType => "SSCAffectedVin";
}

﻿using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class SSCAffectedVINModel : IPartitionedItem
{
    public string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string CampaignCode { get; set; }
    public string Description { get; set; }
    public string LaborCode1 { get; set; }
    public double? LaborHour1 { get; set; }
    public string LaborCode2 { get; set; }
    public double? LaborHour2 { get; set; }
    public string LaborCode3 { get; set; }
    public double? LaborHour3 { get; set; }
    public string PartNumber1 { get; set; }
    public string PartNumber2 { get; set; }
    public string PartNumber3 { get; set; }
    public string OriginalFormatPartNumber1 { get; set; }
    public string OriginalFormatPartNumber2 { get; set; }
    public string OriginalFormatPartNumber3 { get; set; }
    public DateTime? RepairDate { get; set; }
    public string ItemType => ModelTypes.SSCAffectedVIN;
}
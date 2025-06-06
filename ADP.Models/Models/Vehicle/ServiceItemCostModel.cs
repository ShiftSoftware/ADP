﻿namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceItemCostModel
{
    public long ID { get; set; }
    public long? ServiceItemID { get; set; }
    public string Variant { get; set; }
    public string Katashiki { get; set; }
    public string PackageCode { get; set; }
    public decimal? Cost { get; set; }
}
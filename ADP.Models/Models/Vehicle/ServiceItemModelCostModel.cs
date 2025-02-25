namespace ShiftSoftware.ADP.Models.PortalTableSyncCosmosModels;

public class ServiceItemModelCostModel
{
    public long Id { get; set; }
    public long? ServiceItemID { get; set; }
    public string Variant { get; set; }
    public string Katashiki { get; set; }
    public string MenuCode { get; set; }
    public decimal Cost { get; set; }
}
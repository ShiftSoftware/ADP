namespace ShiftSoftware.ADP.Models.Stock.CosmosModels;

public class TIQStockCosmosModel : TIQStockCSV
{
    public new string id { get; set; } = default!;
    public string RegionIntegrationID { get; set; } = default!;
}

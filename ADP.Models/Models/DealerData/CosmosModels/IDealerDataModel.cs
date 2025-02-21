namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels
{
    public interface IDealerDataModel
    {
        string VIN { get; set; }
        string ItemType { get; }
    }
}
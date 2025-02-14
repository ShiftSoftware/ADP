using ShiftSoftware.ADP.Models.DealerData;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels;

public class AccessoryCosmosModel : AccessoryCSV
{
    public new string id { get; set; } = default!;

    public string Image { get; set; }

    public string ItemType => "Accessory";
}

using ShiftSoftware.ADP.Menus.Shared.DTOs.ReplcamentItem;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;

public class ReplacementItemMenuUsageRequestDTO
{
    public List<string> ReplacementItemIDs { get; set; } = new();
}

public class ReplacementItemMenuUsageDTO
{
    [ReplacementItemHashId]
    public string ReplacementItemID { get; set; } = default!;

    public string ReplacementItemName { get; set; } = default!;

    public List<string> MenuLabels { get; set; } = new();
}

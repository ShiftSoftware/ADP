using ShiftSoftware.ADP.Menus.Shared.Enums;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;

public class ServiceIntervalIDSelectorDTO
{
    [ServiceIntervalHashId]
    public string ID { get; set; }

    /// <summary>The variant's menu types this interval is visible under. Ignored when the variant has one type; empty means all.</summary>
    public List<MenuType> MenuTypes { get; set; } = [];
}

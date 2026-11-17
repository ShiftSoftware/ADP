using ShiftSoftware.ADP.Menus.Shared.Enums;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;

public class ServiceIntervalListSelectableUI : ServiceIntervalListDTO
{
    public bool IsSelected { get; set; }

    /// <summary>The variant's menu types the interval is visible under; read only when the variant has several.</summary>
    public List<MenuType> MenuTypes { get; set; } = [];
}

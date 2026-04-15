using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuItemUI
{
    public MenuReplacementItemDTO ReplacementItem { get; set; }

    [Required]
    public List<MenuItemPartDTO> Parts { get; set; } = new();

    [Required]
    public decimal? StandaloneAllowedTime { get; set; }
}

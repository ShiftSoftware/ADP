using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class MenuItemPartDTO
{
    [Required]
    public string PartNumber { get => field; set => field = value?.Trim() ?? string.Empty; }

    public decimal? PeriodicQuantity { get; set; }

    public decimal? StandaloneQuantity { get; set; }

    public decimal? PartPrice { get; set; }

    public decimal? PartPriceMarginPercentage { get; set; }

    [Required]
    public decimal? PartFinalPrice { get; set; }

    public List<PartPriceByCountryDTO> CountryPrices { get; set; } = new();
}

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class PartPriceByCountryDTO
{
    public long? CountryID { get; set; }
    public decimal? PartPrice { get; set; }
    public decimal? PartPriceMarginPercentage { get; set; }
    public decimal? PartFinalPrice { get; set; }

    /// <summary>The unit currently selected to drive <see cref="PartPrice"/> (transient; populated on refresh).</summary>
    public string? SelectedUnitName { get; set; }

    /// <summary>The available retail unit prices for this part/country (transient; populated on refresh).</summary>
    public List<PartUnitPriceDTO> UnitPrices { get; set; } = new();
}

public class PartUnitPriceDTO
{
    public string? UnitName { get; set; }
    public decimal? Price { get; set; }
    public bool IsDefault { get; set; }
}

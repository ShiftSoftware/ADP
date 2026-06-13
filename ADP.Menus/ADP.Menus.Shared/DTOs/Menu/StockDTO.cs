namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class StockDTO
{
    public string PartNumber { get; set; }
    public decimal? Price { get; set; }
    public List<StockPriceByCountryDTO> CountryPrices { get; set; } = new();
}

public class StockPriceByCountryDTO
{
    public long CountryID { get; set; }
    public decimal? Price { get; set; }

    /// <summary>The retail price broken down by selling unit (e.g. each, box).</summary>
    public List<StockUnitPriceDTO> UnitPrices { get; set; } = new();
}

public class StockUnitPriceDTO
{
    public string UnitName { get; set; }
    public decimal? Price { get; set; }
    public bool IsDefault { get; set; }
}

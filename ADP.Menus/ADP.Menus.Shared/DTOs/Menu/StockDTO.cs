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
}

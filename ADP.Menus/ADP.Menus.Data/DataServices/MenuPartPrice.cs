namespace ShiftSoftware.ADP.Menus.Data.DataServices;

public class MenuPartPrice
{
    public string PartNumber { get; set; } = string.Empty;
    public List<MenuPartCountryPrice> CountryPrices { get; set; } = [];
}

public class MenuPartCountryPrice
{
    public long? CountryID { get; set; }
    public decimal? Price { get; set; }
}

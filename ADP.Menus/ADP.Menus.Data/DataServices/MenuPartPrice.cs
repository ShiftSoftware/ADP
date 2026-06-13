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

    /// <summary>The retail price broken down by selling unit (e.g. each, box).</summary>
    public List<MenuPartUnitPrice> UnitPrices { get; set; } = [];
}

public class MenuPartUnitPrice
{
    public string UnitName { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public bool IsDefault { get; set; }
}

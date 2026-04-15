namespace ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

public class PartPriceByCountryDTO
{
    public long? CountryID { get; set; }
    public decimal? PartPrice { get; set; }
    public decimal? PartPriceMarginPercentage { get; set; }
    public decimal? PartFinalPrice { get; set; }
}

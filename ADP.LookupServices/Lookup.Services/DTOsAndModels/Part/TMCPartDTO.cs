namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;

public class TMCPartDTO
{
    public string PartDescription { get; set; }
    public string Group { get; set; }
    public decimal? WarrantyPrice { get; set; }
    public string? PNC { get; set; }
    public string? PNCLocalName { get; set; }
    public string? BinType { get; set; }
    public decimal? dimension1 { get; set; }
    public decimal? dimension2 { get; set; }
    public decimal? dimension3 { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? CubicMeasure { get; set; }
    public string? HSCode { get; set; }
    public string? UZHSCode { get; set; }
    public string? Origin { get; set; }
}
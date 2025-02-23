using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VTTrimCosmosModel
{
    public string id { get; set; }
    public Brands Brand { get; set; }
    public string BrandIntegrationID { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Trim_Code { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Trim_Desc { get; set; }

    [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
    public string Franchise { get; set; }
}

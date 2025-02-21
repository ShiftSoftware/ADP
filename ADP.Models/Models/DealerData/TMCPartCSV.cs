using FileHelpers;

namespace ShiftSoftware.ADP.Models.DealerData;

[DelimitedRecord(",")]
[IgnoreFirst]
public class TMCPartCSV : CacheableCSV
{
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string PartNumber { get; set; } = default!;

    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string PartName { get; set; } = default!;

    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string ProductGroup { get; set; } = default!;

    public decimal FOBAndDealerWarrantyPrice { get; set; }
    public decimal TIQWarrantyPrice { get; set; }

    //[FieldConverter(typeof(TMCPartDecimalConverter))]
    public decimal? DealerSellingPrice { get; set; }
}

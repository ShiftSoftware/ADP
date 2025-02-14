
namespace ShiftSoftware.ADP.Models.Stock
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class TIQStockCSV : CacheableCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string PartNumber { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string PartDescription { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SupersededTo { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SupersededFrom { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal Qty { get; set; }

        //This will be converted to TLP if an item is scanned.
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal Price { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal FOB { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Group { get; set; }
    }
}

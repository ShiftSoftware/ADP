namespace ShiftSoftware.ADP.Models.FranchiseData
{
    [FileHelpers.DelimitedRecord(",")]
    public class VTColorCSV : CacheableCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Color_Code { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Color_Desc { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Franchise { get; set; }
    }
}

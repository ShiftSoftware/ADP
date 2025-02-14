namespace ShiftSoftware.ADP.Models.FranchiseData
{
    [FileHelpers.DelimitedRecord(",")]
    public class VTTrimCSV : CacheableCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Trim_Code { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Trim_Desc { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Franchise { get; set; }
    }
}

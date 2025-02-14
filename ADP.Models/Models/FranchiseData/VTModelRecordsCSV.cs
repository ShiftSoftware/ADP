namespace ShiftSoftware.ADP.Models.FranchiseData
{
    [FileHelpers.DelimitedRecord(",")]
    public class VTModelRecordsCSV : CacheableCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Model_Code { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Model_Desc { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Variant_Code { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Variant_Desc { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Katashiki { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Class { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Body_Type { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Engine { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Cylinders { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Light_Heavy { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Doors { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Fuel { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Transmission { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Side { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Engine_Type { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Tank_Cap { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Style { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Franchise { get; set; }

        [FileHelpers.FieldOptional]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int? Fuelliter { get; set; }
    }
}

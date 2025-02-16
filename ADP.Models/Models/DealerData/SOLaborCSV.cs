using System;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class SOLaborCSV : CacheableCSV, IDealerDataCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string VIN { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string RTSCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string InvoiceState { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string LoadStatus { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldConverter(FileHelpers.ConverterKind.Date, "yyyy-MM-dd")]
        public DateTime DateEdited { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int SOLabordata_DealerId { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int Comp { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int WIP { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldNullValue(0)]
        public int InvoiceNumber { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SalesType { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Account { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string MenuCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal? ExtendedPrice { get; set; }

        public int LineNumber { get; set; }
        [FileHelpers.FieldNullValue(0)]
        public int OriginalInvoiceNumber { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Customer_Magic { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Department { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Description { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string ServiceCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? DateCreated { get; set; }


        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? DateInserted { get; set; }


        [FileHelpers.FieldHidden]
        public string ItemType { get { return "Labor"; } }
    }
}
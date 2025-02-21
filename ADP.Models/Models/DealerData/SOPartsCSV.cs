using System;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class SOPartsCSV : CacheableCSV, IDealerDataCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string InvoiceStatus { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string OrderStatus { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldConverter(FileHelpers.ConverterKind.Date, "yyyy-MM-dd")]
        public DateTime DateLastEditted { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int SOPartsData_DealerId { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int COMP { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int WIPNumber { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldNullValue(0)]
        public int InvoiceNumber { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal? OrderQuantity { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SaleType { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AccountNo { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string MenuCode { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal? ExtendedPrice { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string PartNumber { get; set; }

        public int LineNumber { get; set; }
        [FileHelpers.FieldNullValue(0)]
        public int OriginalInvoiceNumber { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Customer_Magic { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Department { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string VIN { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? DateCreated { get; set; }


        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? DateInserted { get; set; }


        [FileHelpers.FieldHidden]
        public string ItemType { get { return "Part"; } }
    }
}
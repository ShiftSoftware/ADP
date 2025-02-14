using System;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class VSDataCSV : CacheableCSV, IDealerDataCSV
    {
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int VSData_DealerId { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string VIN { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        //[FileHelpers.FieldConverter(FileHelpers.ConverterKind.Date, "yyyy-MM-dd")]
        [FileHelpers.FieldConverter(typeof(QlikDateConverter))]
        public DateTime? InvoiceDate { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SellingBranch { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string SaleType { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public int? SalesInvoiceNumber { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string ModelCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Katashiki { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string VariantCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public decimal? InvoiceTotal { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string ProgressCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string InvoiceAccount { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string ACSStatus { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string CustomerAccount { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Color { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Trim { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string LocationCode { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldOptional]
        public string Franchise { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldOptional]
        public string Region { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        [FileHelpers.FieldOptional]
        public string CustomerMagic { get; set; }

        [FileHelpers.FieldHidden]
        public string ItemType { get { return "VS"; } }
    }
}
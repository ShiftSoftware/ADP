using System;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class CPUCSV : CacheableCSV, IDealerDataCSV
    {
        public int? DealerId { get; set; }
        public string VIN { get; set; }

        [FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? InvoiceDate { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string ServiceDetails { get; set; }
        public int? Mileage { get; set; }
        public int? WIPNo { get; set; }
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Account { get; set; }
        public int? Invoice { get; set; }
        public int? Branch { get; set; }

        [FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? next_service { get; set; }

        [FileHelpers.FieldHidden]
        public string ItemType { get { return "CPU"; } }
    }
}
using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [FileHelpers.DelimitedRecord(","), FileHelpers.IgnoreFirst]
    public class CustomerDataCSV : CacheableCSV
    {
        public long DealerId { get; set; }

        public long MagicNumber { get; set; }

        [FileHelpers.FieldCaption("Customer_Name")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string CustomerName { get; set; }

        [FileHelpers.FieldCaption("Phone_No")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        //[FieldConverter(typeof(AutolinePhoneConverter))]
        public List<string> Phones { get; set; }

        [FileHelpers.FieldCaption("Address001")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AddressLine1 { get; set; }

        [FileHelpers.FieldCaption("Address002")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AddressLine2 { get; set; }

        [FileHelpers.FieldCaption("Address003")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AddressLine3 { get; set; }

        [FileHelpers.FieldCaption("Address004")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AddressLine4 { get; set; }

        [FileHelpers.FieldCaption("Address005")]
        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string AddressLine5 { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string JobTitle { get; set; }

        [FileHelpers.FieldCaption("LanguageCodeC")]
        public int? LanguageCode { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string Gender { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        //[FileHelpers.FieldConverter(typeof(CSVDateConverter))]
        public DateTime? DateOfBirth { get; set; }

        [FileHelpers.FieldQuoted(FileHelpers.QuoteMode.OptionalForBoth)]
        public string WorkName { get; set; }
    }

}
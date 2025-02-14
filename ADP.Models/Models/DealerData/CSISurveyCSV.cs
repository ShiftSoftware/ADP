using FileHelpers;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ShiftSoftware.ADP.Models.DealerData
{
    [DelimitedRecord(",", null)]
    [IgnoreFirst]
    public class CSISurveyCSV
    {
        [FieldQuoted(QuoteMode.OptionalForBoth)]
        [FieldConverter(typeof(AutolinePhoneConverter))]
        public List<string> Phones { get; set; }

        public int? LanguageCode { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        public string CustomerName { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        public string Dealer_Branch { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        public string VIN { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        [FieldConverter(typeof(QlikDateConverter))]
        public DateTime? Date { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        public string WIP { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        public string JobType { get; set; }


        [FieldQuoted(QuoteMode.OptionalForBoth)]
        [FieldOptional]
        public string Franchise { get; set; }

        [FieldQuoted(QuoteMode.OptionalForBoth)]
        [FieldOptional]
        public string Gender { get; set; }


        [FieldHidden]
        public string DealerId { get { return Dealer_Branch.Split('_').FirstOrDefault(); } }

        [FieldHidden]
        public string BranchId { get { return Dealer_Branch.Split('_').ElementAtOrDefault(1); } }
    }
}
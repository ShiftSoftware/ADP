using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels
{
    public class CustomerDataCosmosModel
    {
        public new string id { get; set; } = default!;
        public Genders GenderEnum { get; set; }
        public string DealerIntegrationID { get; set; }
        public Languages Language { get; set; }

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

        //public IEnumerable<string> Phones { get; set; }

        public static Genders GetGender(string gender)
        {
            if (gender.ToLower() == "m")
                return Genders.Male;

            if (gender.ToLower() == "f")
                return Genders.Female;

            return Genders.NotSpecified;
        }

        public static Languages GetLanguage(int? language)
        {
            if (language == 1)
                return Languages.English;

            if (language == 2)
                return Languages.Arabic;

            if (language == 3)
                return Languages.Kurdish;

            return Languages.Unspecified;
        }

        public static IEnumerable<string> GetPhones(string phone)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(phone))
                return new List<string>();

            var phones = phone?.Split('/').Select(x => x.Trim()) ?? new List<string>();

            foreach (var item in phones)
            {
                var formattedPhone = GetFormattedPhone(item);

                if (formattedPhone is not null)
                    result.Add(formattedPhone);
            }

            return result;
        }

        private static string? GetFormattedPhone(string phone)
        {
            var phoneNumberUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();

            try
            {
                var phoneNumber = phoneNumberUtil.Parse(phone, "IQ");

                if (!phoneNumberUtil.IsValidNumber(phoneNumber))
                    return null;

                return phoneNumberUtil.Format(phoneNumber, PhoneNumbers.PhoneNumberFormat.INTERNATIONAL);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

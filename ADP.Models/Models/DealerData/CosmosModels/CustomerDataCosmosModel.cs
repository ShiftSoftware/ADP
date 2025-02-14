using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.Models.DealerData.CosmosModels
{
    public class CustomerDataCosmosModel : CustomerDataCSV
    {
        public new string id { get; set; } = default!;
        public Genders GenderEnum { get; set; }
        public string DealerIntegrationID { get; set; }
        public Languages Language { get; set; }
        public IEnumerable<string> Phones { get; set; }

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

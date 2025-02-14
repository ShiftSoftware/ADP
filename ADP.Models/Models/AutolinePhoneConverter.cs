using PhoneNumbers;
using System.Collections.Generic;
using System.Linq;

namespace ShiftSoftware.ADP.Models
{
    public class AutolinePhoneConverter : FileHelpers.ConverterBase
    {
        protected override bool CustomNullHandling => true;
        public override object StringToField(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
                return new List<string>();

            var phoneNumberUtil = PhoneNumberUtil.GetInstance();

            var phoneArray = from
                .Split('/')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                {
                    try
                    {
                        return phoneNumberUtil.Parse(x, "IQ");
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .Where(x => phoneNumberUtil.IsValidNumber(x))
                .Select(x => phoneNumberUtil.Format(x, PhoneNumbers.PhoneNumberFormat.INTERNATIONAL));

            return phoneArray.ToList();
            //return Newtonsoft.Json.JsonConvert.SerializeObject(phoneArray);
        }

        public override string FieldToString(object fieldValue)
        {
            if (fieldValue == null)
                return null;

            return
                string.Join(
                    "/",
                    Newtonsoft.Json.Linq.JArray.Parse(fieldValue.ToString())
                    .Select(x => x.ToObject<string>())
            );
        }
    }
}

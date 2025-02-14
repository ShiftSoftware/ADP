
using PhoneNumbers;

namespace ShiftSoftware.ADP.Models
{
    public static class PhoneConverter
    {
        /// <summary>
        /// Gets the formatted phone in international format. Throws and exception if the phone number is invalid.
        /// </summary>
        /// <param name="phone"></param>
        /// <returns>International Formatted Phone Number</returns>
        /// <exception cref="System.Exception"></exception>
        public static string GetFormattedPhone(string phone)
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();

            var phoneNumber = phoneNumberUtil.Parse(phone, "IQ");

            if (!phoneNumberUtil.IsValidNumber(phoneNumber))
                throw new Exceptions.InvalidPhoneNumberException(phone);

            return phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.INTERNATIONAL);
        }
    }
}
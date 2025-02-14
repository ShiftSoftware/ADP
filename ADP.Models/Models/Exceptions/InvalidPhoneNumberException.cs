using System;

namespace ShiftSoftware.ADP.Models.Exceptions
{
    public class InvalidPhoneNumberException : Exception
    {
        public InvalidPhoneNumberException(string phoneNumber): base($"{phoneNumber} Is Not a Valid Phone Number") { }
    }
}

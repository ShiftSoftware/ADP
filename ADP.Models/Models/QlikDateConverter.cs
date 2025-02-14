using System;
using System.Globalization;
using System.Linq;

namespace ShiftSoftware.ADP.Models
{
    public class QlikDateConverter : FileHelpers.ConverterBase
    {
        public override object StringToField(string from)
        {
            if (string.IsNullOrWhiteSpace(from))
                return null;

            if (from.Contains(' '))
                from = from.Split(' ').First();

            if (from.Contains("/"))
            {
                var splitted = from.Split('/');
                return new DateTime(int.Parse(splitted[2]), int.Parse(splitted[0]), int.Parse(splitted[1]));
            }

            return DateTime.ParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public override string FieldToString(object fieldValue)
        {
            if (fieldValue is null)
                return "";

            return ((DateTime)fieldValue).ToString("O");
        }
    }
}
using System;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.JsonConverters
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonCustomDateTimeAttribute : JsonConverterAttribute
    {
        private readonly string _format;

        public JsonCustomDateTimeAttribute(string format)
        {
            _format = format;
        }

        public override JsonConverter CreateConverter(Type typeToConvert)
        {
            if (typeToConvert == typeof(DateTime))
                return new CustomDateTimeConverter(_format);
            else if (typeToConvert == typeof(DateTime?))
                return new CustomDateTimeNullableConverter(_format);
            else
                return null;
        }
    }
}

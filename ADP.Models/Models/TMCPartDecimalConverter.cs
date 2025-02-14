namespace ShiftSoftware.ADP.Models;

internal class TMCPartDecimalConverter : FileHelpers.ConverterBase
{
    public override object StringToField(string from)
    {
        decimal? value = null;

        if (decimal.TryParse(from, out decimal result))
            value = result;

        return value!;
    }
}

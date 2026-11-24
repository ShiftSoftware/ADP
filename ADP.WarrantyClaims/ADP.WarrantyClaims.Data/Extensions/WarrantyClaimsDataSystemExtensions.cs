namespace System;

/// <summary>
/// JPY currency formatting the manufacturer-invoice printout renders, and USD currency formatting
/// the financial-report printout renders. The module Web project has its own copy for the claim form,
/// which the Data-side print methods must not reach into; keep the behavior of the two identical.
/// </summary>
internal static class WarrantyClaimsDataSystemExtensions
{
    public static string ToCurrencyFormat(this decimal value)
    {
        return value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-us"));
    }

    public static string? ToCurrencyFormat(this decimal? value)
    {
        if (value is null)
            return null;

        return value.Value.ToCurrencyFormat();
    }

    public static string ToJPYCurrencyFormat(this decimal value)
    {
        return value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("ja-JP"));
    }

    public static string? ToJPYCurrencyFormat(this decimal? value)
    {
        if (value is null)
            return null;

        return value.Value.ToJPYCurrencyFormat();
    }
}

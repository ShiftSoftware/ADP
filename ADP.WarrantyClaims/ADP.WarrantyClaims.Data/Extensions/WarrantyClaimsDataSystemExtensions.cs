namespace System;

/// <summary>
/// JPY currency formatting the manufacturer-invoice printout renders, and USD currency formatting
/// the financial-report printout renders. The original host defined these as <c>System</c>-namespace
/// extensions (Services.Shared/Extensions/SystemExtentsions.cs) and the module Web project carries
/// its own copy for the claim form — the Data-side print methods must not reach into either, so an
/// internal copy travels with them. Kept byte-identical in behavior.
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

namespace System;

/// <summary>
/// Currency-formatting helpers the WarrantyClaimForm renders (USD via en-US, JPY via ja-JP). These were
/// <c>System</c>-namespace extensions in the original host application (Services.Shared/Extensions/SystemExtentsions.cs);
/// the moved form must not reach back into the consumer, so a module-owned copy travels with the page.
/// Kept byte-identical to the original host behavior.
/// </summary>
public static class WarrantyClaimsSystemExtensions
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

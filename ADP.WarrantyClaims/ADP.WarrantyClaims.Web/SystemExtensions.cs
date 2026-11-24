namespace System;

/// <summary>
/// Currency-formatting helpers the WarrantyClaimForm renders (USD via en-US, JPY via ja-JP). The Data
/// project has its own copy for the printouts; keep the behavior of the two identical.
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

    /// <summary>
    /// An amount in a claim's currency: USD exactly as <see cref="ToCurrencyFormat(decimal)"/>, any other
    /// currency with its own decimals and its ISO code.
    /// </summary>
    public static string ToCurrencyFormat(this decimal value, string? currency)
    {
        if (ShiftSoftware.ADP.Cases.Shared.DocumentCurrency.IsUSD(currency))
            return value.ToCurrencyFormat();

        var decimals = ShiftSoftware.ADP.Cases.Shared.DocumentCurrency.Decimals(currency);

        return $"{value.ToString("N" + decimals, System.Globalization.CultureInfo.GetCultureInfo("en-us"))} {currency!.Trim().ToUpperInvariant()}";
    }

    public static string? ToCurrencyFormat(this decimal? value, string? currency)
    {
        if (value is null)
            return null;

        return value.Value.ToCurrencyFormat(currency);
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

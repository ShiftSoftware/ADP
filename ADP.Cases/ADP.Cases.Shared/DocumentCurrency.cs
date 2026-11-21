namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>
/// Conversion between a document's currency and USD. A rate is always units of the currency per 1 USD.
/// </summary>
public static class DocumentCurrency
{
    public const string USD = "USD";

    private static readonly HashSet<string> zeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "UYI", "VND", "VUV", "XAF", "XOF", "XPF",
    };

    private static readonly HashSet<string> threeDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND",
    };

    /// <summary>A document without a currency is a USD document.</summary>
    public static bool IsUSD(string? currency) =>
        string.IsNullOrWhiteSpace(currency) || currency.Trim().Equals(USD, StringComparison.OrdinalIgnoreCase);

    /// <summary>The ISO 4217 minor units of the currency.</summary>
    public static int Decimals(string? currency)
    {
        if (IsUSD(currency))
            return 2;

        var code = currency!.Trim();

        return zeroDecimalCurrencies.Contains(code) ? 0 : threeDecimalCurrencies.Contains(code) ? 3 : 2;
    }

    public static decimal Round(decimal amount, string? currency) =>
        Math.Round(amount, Decimals(currency), MidpointRounding.AwayFromZero);

    /// <summary>
    /// The USD equivalent of an amount in the document's currency. A USD amount is returned as it is, not rounded.
    /// Without a usable rate the result is 0.
    /// </summary>
    public static decimal ToUSD(decimal amount, string? currency, decimal rate)
    {
        if (IsUSD(currency))
            return amount;

        return rate > 0 ? Round(amount / rate, USD) : 0;
    }

    public static decimal? ToUSD(decimal? amount, string? currency, decimal rate) =>
        amount is null ? null : ToUSD(amount.Value, currency, rate);

    /// <summary>A USD amount in the document's currency, rounded to that currency's minor units.</summary>
    public static decimal FromUSD(decimal amount, string? currency, decimal rate) =>
        IsUSD(currency) ? amount : Round(amount * rate, currency);
}

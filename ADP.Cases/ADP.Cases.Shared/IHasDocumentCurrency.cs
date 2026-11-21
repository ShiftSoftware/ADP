namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>
/// A document (a claim, a contract) whose amounts are all in one currency, with the rate to USD stored on it.
/// </summary>
public interface IHasDocumentCurrency
{
    /// <summary>ISO 4217 code of the currency every amount on the document is in.</summary>
    string Currency { get; set; }

    /// <summary>Units of <see cref="Currency"/> per 1 USD. Always 1 for a USD document.</summary>
    decimal CurrencyRate { get; set; }
}

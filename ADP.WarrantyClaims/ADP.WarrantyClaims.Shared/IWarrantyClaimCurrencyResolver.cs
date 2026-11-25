using ShiftSoftware.ADP.Cases.Shared;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam that decides the currency a branch's claims are made in and the labor rate of a claim.
/// The module registers <see cref="DefaultWarrantyClaimCurrencyResolver"/> (USD, labor rate 25) with TryAdd,
/// so a host that registers nothing keeps USD claims.
/// </summary>
public interface IWarrantyClaimCurrencyResolver
{
    /// <summary>
    /// The ISO 4217 code of the currency for claims of this branch. Asked once, when the claim is created.
    /// </summary>
    ValueTask<string> GetCurrencyAsync(long? companyBranchID);

    /// <summary>
    /// The labor rate per hour in the claim's currency, for a currency that has no row on the Labor Rates page. A row
    /// wins over this. Asked when the claim is created and again when its currency rate changes. A host with a USD
    /// figure converts it with <see cref="DocumentCurrency.FromUSD"/> and the context's rate. Null means the host has
    /// no rate, so a new claim can not be saved until a row is added. The default is
    /// <see cref="DefaultWarrantyClaimCurrencyResolver.LaborRate"/> for USD and none for another currency.
    /// </summary>
    ValueTask<decimal?> GetLaborRateAsync(WarrantyClaimLaborRateContext context) =>
        new(DocumentCurrency.IsUSD(context.Currency) ? DefaultWarrantyClaimCurrencyResolver.LaborRate : null);
}

public class WarrantyClaimLaborRateContext
{
    public long? CompanyBranchID { get; set; }
    public string? Franchise { get; set; }

    /// <summary>The claim's currency, as returned by <see cref="IWarrantyClaimCurrencyResolver.GetCurrencyAsync"/>.</summary>
    public string Currency { get; set; } = DocumentCurrency.USD;

    /// <summary>Units of <see cref="Currency"/> per 1 USD.</summary>
    public decimal CurrencyRate { get; set; } = 1;
}

public class DefaultWarrantyClaimCurrencyResolver : IWarrantyClaimCurrencyResolver
{
    public const decimal LaborRate = 25;

    public ValueTask<string> GetCurrencyAsync(long? companyBranchID) => new(DocumentCurrency.USD);
}

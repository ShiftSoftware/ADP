namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

/// <summary>The currency a claim is made in, as the claim form needs it.</summary>
public class WarrantyClaimCurrencyDTO
{
    public string Currency { get; set; } = default!;

    /// <summary>Units of <see cref="Currency"/> per 1 USD. Null when no rate is set for the currency yet.</summary>
    public decimal? CurrencyRate { get; set; }

    /// <summary>Labor rate per hour in <see cref="Currency"/>, at <see cref="CurrencyRate"/>.</summary>
    public decimal LaborRate { get; set; }

    /// <summary>
    /// The distributor's labor rate per hour in USD, null when it is the dealer's labor rate in USD.
    /// Distributor-only: a dealer is never sent it.
    /// </summary>
    public decimal? LaborRateDistributor { get; set; }

    /// <summary>Whether the current user may change the rate of this claim.</summary>
    public bool CanEditRate { get; set; }
}

/// <summary>The price of a part for a claim, from <c>IPartLookupProvider.LookupForClaimAsync</c>.</summary>
public class WarrantyClaimPartPriceDTO
{
    public string? PartDescription { get; set; }

    /// <summary>Unit warranty price in the claim's currency.</summary>
    public decimal? WarrantyPrice { get; set; }

    /// <summary>The distributor's purchase price, in USD.</summary>
    public decimal? DistributorPurchasePrice { get; set; }
}

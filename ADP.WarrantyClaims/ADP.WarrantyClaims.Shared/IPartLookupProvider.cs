using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.Part;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam behind the module's <c>part-lookup/{partNumber}</c> endpoint (Phase 3 Slice 3.3).
/// Keeps the module free of the lookup-service registration/storage requirements: the original host
/// wraps <c>PartLookupService.LookupAsync(partNumber, skipLogging: true)</c> (price visibility driven
/// by its own authorization nodes) and serves JSON fixtures in development. Resolved lazily from the
/// request services — consumers that never call the endpoint need no registration.
/// </summary>
public interface IPartLookupProvider
{
    Task<PartLookupDTO> LookupAsync(string partNumber);

    /// <summary>
    /// The price of a part for a claim, behind the <c>part-price/{partNumber}</c> endpoint the claim form
    /// prices its part lines from. A host whose claims are not all in USD overrides it and returns the
    /// warranty price in the claim's currency. The default is the first price of the plain lookup, as it is.
    /// </summary>
    async Task<WarrantyClaimPartPriceDTO> LookupForClaimAsync(string partNumber, WarrantyClaimPartLookupContext context)
    {
        var part = await this.LookupAsync(partNumber);

        return new WarrantyClaimPartPriceDTO
        {
            PartDescription = part.PartDescription,
            WarrantyPrice = part.Prices?.FirstOrDefault()?.WarrantyPrice?.Value,
            DistributorPurchasePrice = part.DistributorPurchasePrice,
        };
    }
}

public class WarrantyClaimPartLookupContext
{
    /// <summary>The branch the claim belongs to.</summary>
    public long? CompanyBranchID { get; set; }

    public string Currency { get; set; } = DocumentCurrency.USD;

    /// <summary>Units of <see cref="Currency"/> per 1 USD.</summary>
    public decimal CurrencyRate { get; set; } = 1;
}

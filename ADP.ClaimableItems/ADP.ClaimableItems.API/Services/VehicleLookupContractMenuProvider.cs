using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.ServiceMenu;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Models.Enums;

namespace ShiftSoftware.ADP.ClaimableItems.API.Services;

/// <summary>
/// The default <see cref="IClaimableItemContractMenuProvider"/>: the vehicle and its contract-type menu from
/// the vehicle lookup, the countries from the host's <see cref="IMenuCountryProvider"/>. Both are resolved
/// when first used, not at construction, so a host that never opens a contract needs neither.
/// </summary>
public class VehicleLookupContractMenuProvider : IClaimableItemContractMenuProvider
{
    private readonly IServiceProvider services;

    public VehicleLookupContractMenuProvider(IServiceProvider services)
    {
        this.services = services;
    }

    public async ValueTask<IReadOnlyList<ClaimableItemContractCountry>> GetSupportedCountriesAsync()
    {
        // No country provider means no countries: the lookup's default country prices the menu.
        if (services.GetService<IMenuCountryProvider>() is not { } countryProvider)
            return [];

        var countries = await countryProvider.GetSupportedCountriesAsync() ?? [];

        return countries.Select(x => new ClaimableItemContractCountry { ID = x.Id, Name = x.Name }).ToList();
    }

    public async Task<ClaimableItemContractVehicleMenu> GetVehicleMenuAsync(string vin, long? countryId, decimal transferRate, string language)
    {
        var lookupService = services.GetService<VehicleLookupService>()
            ?? throw new InvalidOperationException(
                "Service contracts read the vehicle and its menu through the vehicle lookup. Register it in the host " +
                "with services.AddLookupService(...), or register your own IClaimableItemContractMenuProvider.");

        var lookup = await lookupService.LookupAsync(vin, new VehicleLookupRequestOptions
        {
            LanguageCode = language,
            ServiceMenuOptions = new VehicleServiceMenuRequestOptions
            {
                Include = true,
                CountryID = countryId,
                TransferRate = transferRate,
                MenuTypes = [MenuType.Contract],
            },
        });

        var menu = lookup.ServiceMenu;

        return new ClaimableItemContractVehicleMenu
        {
            IsOfficial = lookup.IsAuthorized,
            Katashiki = lookup.Identifiers?.Katashiki,
            BasicModelCode = lookup.BasicModelCode,
            Variant = lookup.Identifiers?.Variant,
            ModelCode = lookup.VehicleSpecification?.ModelCode,
            ModelDescription = lookup.VehicleSpecification?.ModelDescription,
            BrandID = long.TryParse(lookup.Identifiers?.BrandID, out var brandId) ? brandId : null,
            MenuStatus = menu?.Status switch
            {
                VehicleServiceMenuStatus.Found => ClaimableItemContractVehicleMenuStatus.Found,
                VehicleServiceMenuStatus.NoBasicModelCode or VehicleServiceMenuStatus.NotFound => ClaimableItemContractVehicleMenuStatus.NotFound,
                _ => ClaimableItemContractVehicleMenuStatus.Unavailable,
            },
            Lines = menu?.Services.Select(ToMenuLine).ToList() ?? [],
        };
    }

    private static ClaimableItemContractMenuLineDTO ToMenuLine(VehicleServiceMenuLineDTO line) => new()
    {
        MenuLineKey = line.LineKey,
        MenuCode = line.Code,
        LabourCode = line.LabourCode,
        VariantID = line.VariantID,
        VariantName = line.VariantName,
        LineType = line.LineType switch
        {
            ServiceMenuLineType.Periodic => ClaimableItemContractLineType.Periodic,
            ServiceMenuLineType.StandaloneUngrouped => ClaimableItemContractLineType.Standalone,
            ServiceMenuLineType.StandaloneGrouped => ClaimableItemContractLineType.StandaloneGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(line), line.LineType, "Unknown menu line type."),
        },
        ServiceIntervalCode = line.ServiceIntervalCode,
        ServiceIntervalValueInMeter = line.ServiceIntervalValueInMeter,
        Description = line.Description,
        LabourTotalPrice = line.LabourTotalPrice,
        PartsTotalPrice = line.PartsTotalPrice,
        DiscountAmount = line.DiscountAmount,
        Price = line.TotalPrice,
        HasUnpricedParts = line.HasUnpricedParts,
    };
}

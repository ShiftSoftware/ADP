using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Services;

/// <summary>
/// The contract rules applied to a vehicle's contract-type menu: which country it is priced for, and which
/// lines a contract may take. The form's VIN lookup and the repository's save both go through here, so a
/// line the form offers is a line the save accepts.
/// </summary>
public class ClaimableItemContractMenuService
{
    public const string DefaultLanguage = "en";

    private readonly ShiftDbContext db;
    private readonly IClaimableItemContractMenuProvider provider;
    private readonly ClaimableItemsDataOptions options;
    private readonly TimeProvider timeProvider;

    public ClaimableItemContractMenuService(
        ShiftDbContext db,
        IClaimableItemContractMenuProvider provider,
        ClaimableItemsDataOptions options,
        TimeProvider timeProvider)
    {
        this.db = db;
        this.provider = provider;
        this.options = options;
        this.timeProvider = timeProvider;
    }

    public ClaimableItemsDataOptions Options => options;

    /// <summary>Today in the contracts' time zone (<see cref="ClaimableItemsDataOptions.ContractTimeZone"/>).</summary>
    public DateTime Today =>
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), options.ContractTimeZone ?? TimeZoneInfo.Local).Date;

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Every contract-type line of the vehicle's menu, each with the reason it cannot be put on a contract
    /// (null when it can).
    /// </summary>
    /// <param name="countryId">The user's choice. Ignored when one country or none is configured.</param>
    /// <param name="transferRate">The user's entry. Ignored (fixed at 1) when one country or none is configured.</param>
    /// <param name="excludeContractId">The contract being edited, so its own lines do not count as taken.</param>
    /// <param name="today">Lines that expired before this date no longer hold their menu.</param>
    public async Task<ClaimableItemContractVehicleLookupDTO> LookupAsync(
        string vin,
        long? mileage,
        long? countryId,
        decimal? transferRate,
        string? language,
        long? excludeContractId,
        DateTime today)
    {
        vin = NormalizeVin(vin);

        var result = new ClaimableItemContractVehicleLookupDTO
        {
            VIN = vin,
            MenuLanguage = string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language,
            PriceIncrements = options.ContractPriceIncrements.ToList(),
            MinimumLines = options.ContractMinimumLines,
        };

        // The same rule as the menus module: with one country (or none) the server picks it and the rate is 1;
        // with several the user picks the country and enters the rate.
        var countries = await provider.GetSupportedCountriesAsync();
        result.Countries = countries.Select(x => new ShiftEntitySelectDTO { Value = x.ID.ToString(), Text = x.Name }).ToList();

        decimal rate;
        if (countries.Count <= 1)
        {
            result.CountryIsFixed = true;
            countryId = countries.Count == 1 ? countries[0].ID : null;
            rate = 1m;
        }
        else
        {
            if (countryId is null || !countries.Any(x => x.ID == countryId))
            {
                result.Status = ClaimableItemContractLookupStatus.CountryRequired;
                return result;
            }

            rate = transferRate ?? 1m;
        }

        result.Country = countryId is null ? null : result.Countries.Single(x => x.Value == countryId.Value.ToString());
        result.TransferRate = rate;

        var menu = await provider.GetVehicleMenuAsync(vin, countryId, rate, result.MenuLanguage);

        result.Katashiki = menu.Katashiki;
        result.BasicModelCode = menu.BasicModelCode;
        result.Variant = menu.Variant;
        result.ModelCode = menu.ModelCode;
        result.ModelDescription = menu.ModelDescription;
        result.Brand = menu.BrandID is null ? null : new ShiftEntitySelectDTO { Value = menu.BrandID.Value.ToString() };

        if (!menu.IsOfficial)
        {
            result.Status = ClaimableItemContractLookupStatus.NotOfficial;
            return result;
        }

        if (menu.MenuStatus != ClaimableItemContractVehicleMenuStatus.Found)
        {
            result.Status = menu.MenuStatus == ClaimableItemContractVehicleMenuStatus.NotFound
                ? ClaimableItemContractLookupStatus.MenuNotFound
                : ClaimableItemContractLookupStatus.MenuUnavailable;
            return result;
        }

        var taken = await GetTakenPeriodicMenuLineKeysAsync(vin, excludeContractId, today);

        foreach (var line in menu.Lines)
            line.UnavailableReason = GetUnavailableReason(line, mileage, taken);

        result.Lines = menu.Lines;
        result.Status = ClaimableItemContractLookupStatus.Found;

        return result;
    }

    /// <summary>
    /// Leaves out what the form must not offer at all. A periodic line on another contract stays, with its
    /// reason, so the user sees why it cannot be picked.
    /// </summary>
    public static ClaimableItemContractVehicleLookupDTO ForForm(ClaimableItemContractVehicleLookupDTO lookup)
    {
        lookup.Lines = lookup.Lines
            .Where(x => x.UnavailableReason is null or ClaimableItemContractMenuLineUnavailableReason.OnAnotherContract)
            .ToList();

        return lookup;
    }

    /// <summary>
    /// The periodic menus a VIN already holds: on an issued contract that is not cancelled, on a line that is
    /// neither cancelled nor expired. Standalone menus are never held.
    /// </summary>
    public async Task<HashSet<string>> GetTakenPeriodicMenuLineKeysAsync(string vin, long? excludeContractId, DateTime today)
    {
        vin = NormalizeVin(vin);

        var keys = await db.Set<ClaimableItemContractLine>()
            .Where(x => x.ClaimableItemContract.VIN == vin)
            .Where(x => !x.ClaimableItemContract.IsDeleted && !x.ClaimableItemContract.IsDraft && !x.ClaimableItemContract.IsCancelled)
            .Where(x => x.ClaimableItemContractID != excludeContractId)
            .Where(x => !x.IsDeleted && !x.IsCancelled)
            .Where(x => x.LineType == ClaimableItemContractLineType.Periodic)
            .Where(x => x.ExpireDate == null || x.ExpireDate >= today)
            .Select(x => x.MenuLineKey)
            .Distinct()
            .ToListAsync();

        return keys.ToHashSet();
    }

    public static string NormalizeVin(string vin) => vin.Trim().ToUpperInvariant();

    private ClaimableItemContractMenuLineUnavailableReason? GetUnavailableReason(ClaimableItemContractMenuLineDTO line, long? mileage, HashSet<string> taken)
    {
        if (line.HasUnpricedParts)
            return ClaimableItemContractMenuLineUnavailableReason.UnpricedParts;

        if (line.LineType != ClaimableItemContractLineType.Periodic)
            return null;

        if (mileage is not null && line.ServiceIntervalValueInMeter is not null &&
            line.ServiceIntervalValueInMeter <= mileage + options.ContractPeriodicMileageMargin)
            return ClaimableItemContractMenuLineUnavailableReason.AtOrBelowMileage;

        if (taken.Contains(line.MenuLineKey))
            return ClaimableItemContractMenuLineUnavailableReason.OnAnotherContract;

        return null;
    }
}

using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Brand;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class MenuVariantRepository : ShiftRepository<ShiftDbContext, MenuVariant, MenuVariantListDTO, MenuVariantDTO>
{
    private readonly IMenuCountryProvider countryProvider;

    public MenuVariantRepository(ShiftDbContext db, IMenuCountryProvider countryProvider) : base(db, i =>
    {
        i.IncludeRelatedEntitiesWithFindAsync(
            x => x.Include(a => a.Items).ThenInclude(a => a.ReplacementItemVehicleModel).ThenInclude(a => a.ReplacementItem).ThenInclude(a => a.StandaloneReplacementItemGroup),
            x => x.Include(a => a.Items).ThenInclude(a => a.Parts).ThenInclude(a => a.CountryPrices),
            x => x.Include(a => a.LabourDetails),
            x => x.Include(a => a.LabourRates),
            x => x.Include(a => a.PeriodicAvailabilities),
            x => x.Include(a => a.Menu).ThenInclude(a => a.VehicleModel)
        );

        i.FilterByTypeAuthValues(x => (x.ReadableTypeAuthValues != null && x.Entity.Menu.BrandID != null
            && x.ReadableTypeAuthValues.Contains(x.Entity.Menu.BrandID!.ToString()!))
            || x.WildCardRead)
        .ValueProvider<BrandDTO>(ShiftIdentityActions.DataLevelAccess.Brands);
    })
    {
        this.countryProvider = countryProvider;
    }

    public override async ValueTask<MenuVariant> UpsertAsync(MenuVariant entity, MenuVariantDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var menuID = dto.MenuID.ToLong();
        var menu = await db.Set<MenuEntity>()
            .Where(x => !x.IsDeleted && x.ID == menuID)
            .Include(x => x.VehicleModel)
            .FirstOrDefaultAsync();

        if (menu is null)
            throw new ShiftEntityException(new("NotFound", "Menu group not found"), 404);

        var vehicleModelItemIds = dto.Items.Select(x => x.ReplacementItemVehicleModelID).Where(x => x.HasValue).Select(x => x!.Value);
        if (await db.Set<ReplacementItemVehicleModel>().Where(x => vehicleModelItemIds.Contains(x.ID)).AnyAsync(x => x.VehicleModelID != menu.VehicleModelID))
            throw new ShiftEntityException(new("Conflict", "Menu group vehicle model and menu items should belong to the same vehicle model"));

        var replacementItemRules = await db.Set<ReplacementItemVehicleModel>()
            .Where(x => vehicleModelItemIds.Contains(x.ID))
            .Select(x => new
            {
                x.ID,
                x.ReplacementItem.AllowMultiplePartNumbers
            })
            .ToDictionaryAsync(x => x.ID, x => x.AllowMultiplePartNumbers);

        foreach (var item in dto.Items)
        {
            if (!item.ReplacementItemVehicleModelID.HasValue)
                continue;

            var partsCount = item.Parts?.Count ?? 0;
            if (partsCount == 0)
                throw new ShiftEntityException(new("Conflict", "Each menu item must contain at least one part"));

            var allowMultiple = replacementItemRules.GetValueOrDefault(item.ReplacementItemVehicleModelID.Value);
            if (!allowMultiple && partsCount != 1)
                throw new ShiftEntityException(new("Conflict", "This replacement item allows exactly one part number"));
        }

        if (await db.Set<MenuVariant>().Where(x => !x.IsDeleted && x.ID != entity.ID && x.MenuID == menuID)
            .AnyAsync(x => x.MenuPrefix == dto.MenuPrefix && x.MenuPostfix == dto.MenuPostfix))
            throw new ShiftEntityException(new("Conflict", "The combination of menu prefix and menu postfix should be unique within group"));

        if (await db.Set<MenuVariant>().Where(x => !x.IsDeleted && x.ID != entity.ID && x.MenuID == menuID)
            .AnyAsync(x => x.Name == dto.Name))
            throw new ShiftEntityException(new("Conflict", "Menu variant name should be unique within group"));

        var countries = await countryProvider.GetSupportedCountriesAsync();
        ValidateLabourRates(dto, countries);
        ValidatePartCountryPrices(dto, countries);

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    private static void ValidateLabourRates(MenuVariantDTO dto, IReadOnlyList<CountryInfo> countries)
    {
        // 0/1-country mode: per-country labour rates are not used; primary rate on the variant is authoritative.
        if (countries.Count <= 1)
            return;

        if (dto.LabourRates is null || dto.LabourRates.Count == 0)
            throw new ShiftEntityException(new Message("Conflict", "Menu variant labour rates are required."));

        var duplicateCountries = dto.LabourRates
            .GroupBy(x => x.CountryID)
            .Where(x => x.Key.HasValue && x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateCountries.Any())
            throw new ShiftEntityException(new Message("Conflict", "Duplicate country labour rates are not allowed."));

        var supportedCountryIds = countries.Select(c => c.Id).ToHashSet();
        var countrySet = dto.LabourRates
            .Where(x => x.CountryID.HasValue)
            .Select(x => x.CountryID!.Value)
            .ToHashSet();
        if (!supportedCountryIds.All(countrySet.Contains))
            throw new ShiftEntityException(new Message("Conflict", "Menu variant labour rates must include all required countries."));
    }

    private static void ValidatePartCountryPrices(MenuVariantDTO dto, IReadOnlyList<CountryInfo> countries)
    {
        foreach (var item in dto.Items ?? [])
        {
            foreach (var part in item.Parts ?? [])
            {
                if (part.CountryPrices is null || part.CountryPrices.Count == 0)
                    throw new ShiftEntityException(new Message("Conflict", "Part country prices are required."));

                var duplicateCountries = part.CountryPrices
                    .GroupBy(x => x.CountryID)
                    .Where(x => x.Key.HasValue && x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();
                if (duplicateCountries.Any())
                    throw new ShiftEntityException(new Message("Conflict", "Duplicate part country prices are not allowed."));

                var supportedCountryIds = countries.Select(c => c.Id).ToHashSet();
                var countrySet = part.CountryPrices
                    .Where(x => x.CountryID.HasValue)
                    .Select(x => x.CountryID!.Value)
                    .ToHashSet();
                if (!supportedCountryIds.All(countrySet.Contains))
                    throw new ShiftEntityException(new Message("Conflict", "Part country prices must include all required countries."));
            }
        }
    }
}


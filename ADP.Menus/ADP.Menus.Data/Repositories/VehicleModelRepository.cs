using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

public class VehicleModelRepository : ShiftRepository<ShiftDbContext, VehicleModel, VehicleModelListDTO, VehicleModelDTO>
{
    private readonly IMenuCountryProvider countryProvider;

    public VehicleModelRepository(ShiftDbContext db, IMenuCountryProvider countryProvider) : base(db,
        x => x.IncludeRelatedEntitiesWithFindAsync(
            i => i.Include(s => s.ReplacementItemVehicleModels!).ThenInclude(r => r.ReplacementItem).ThenInclude(r => r.StandaloneReplacementItemGroup),
            i => i.Include(s => s.ReplacementItemVehicleModels!).ThenInclude(r => r.DefaultParts),
            i => i.Include(s => s.LabourDetails),
            i => i.Include(s => s.LabourRates)))
    {
        this.countryProvider = countryProvider;
    }

    public override async ValueTask<VehicleModel> UpsertAsync(VehicleModel entity, VehicleModelDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        // check uniqueness for name
        if (await db.Set<VehicleModel>().Where(x => !x.IsDeleted).AnyAsync(x => x.ID != entity.ID && x.Name == dto.Name.Trim()))
            throw new ShiftEntityException(new Message("Conflict", $"Vehicle Model with Name: {dto.Name}, already exists"));

        // Trim the name
        dto.Name = dto.Name.Trim();

        var countries = await countryProvider.GetSupportedCountriesAsync();
        ValidateLabourRates(dto, countries);

        // When the user unticks a replacement item (RIVM) the AutoMapper profile marks
        // the RIVM IsDeleted. Cascade that soft-delete to the MenuItem rows that still
        // reference it (plus their parts and per-country prices) so the reports and
        // menu UIs don't keep surfacing an item that no longer belongs to the vehicle.
        if (actionType == ActionTypes.Update && entity.ReplacementItemVehicleModels is not null)
        {
            var incomingReplacementItemIds = dto.ReplacementItems?
                .Where(r => !string.IsNullOrEmpty(r.ReplacementItemID))
                .Select(r => r.ReplacementItemID.ToLong())
                .ToHashSet() ?? [];

            var rivmIdsToRemove = entity.ReplacementItemVehicleModels
                .Where(r => !r.IsDeleted && !incomingReplacementItemIds.Contains(r.ReplacementItemID))
                .Select(r => r.ID)
                .ToList();

            if (rivmIdsToRemove.Count > 0)
            {
                var menuItemsToRemove = await db.Set<MenuItem>()
                    .Where(mi => !mi.IsDeleted
                        && mi.ReplacementItemVehicleModelID.HasValue
                        && rivmIdsToRemove.Contains(mi.ReplacementItemVehicleModelID.Value))
                    .Include(mi => mi.Parts).ThenInclude(p => p.CountryPrices)
                    .ToListAsync();

                foreach (var mi in menuItemsToRemove)
                {
                    mi.IsDeleted = true;
                    foreach (var part in mi.Parts.Where(p => !p.IsDeleted))
                    {
                        part.IsDeleted = true;
                        foreach (var cp in part.CountryPrices.Where(c => !c.IsDeleted))
                            cp.IsDeleted = true;
                    }
                }
            }
        }

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    public async Task<List<(long ReplacementItemID, string ReplacementItemName, List<string> MenuLabels)>> GetReplacementItemMenuUsageAsync(long vehicleModelId, IReadOnlyCollection<long> replacementItemIds)
    {
        if (replacementItemIds is null || replacementItemIds.Count == 0)
            return [];

        var rows = await db.Set<MenuItem>()
            .AsNoTracking()
            .Where(mi => !mi.IsDeleted
                && mi.ReplacementItemVehicleModel != null
                && mi.ReplacementItemVehicleModel.VehicleModelID == vehicleModelId
                && replacementItemIds.Contains(mi.ReplacementItemVehicleModel.ReplacementItemID))
            .Select(mi => new
            {
                ReplacementItemID = mi.ReplacementItemVehicleModel!.ReplacementItemID,
                ReplacementItemName = mi.ReplacementItemVehicleModel.ReplacementItem.Name,
                MenuLabel = mi.MenuVariant.Menu.BasicModelCode + " / " + mi.MenuVariant.Name
            })
            .Distinct()
            .ToListAsync();

        return rows
            .GroupBy(x => new { x.ReplacementItemID, x.ReplacementItemName })
            .Select(g => (
                ReplacementItemID: g.Key.ReplacementItemID,
                ReplacementItemName: g.Key.ReplacementItemName,
                MenuLabels: g.Select(x => x.MenuLabel).OrderBy(x => x).ToList()))
            .ToList();
    }

    private static void ValidateLabourRates(VehicleModelDTO dto, IReadOnlyList<CountryInfo> countries)
    {
        // 0/1-country mode: per-country labour rates are not used; primary rate on the model is authoritative.
        if (countries.Count <= 1)
            return;

        if (dto.LabourRates is null || dto.LabourRates.Count == 0)
            throw new ShiftEntityException(new Message("Conflict", "Vehicle model labour rates are required."));

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
            throw new ShiftEntityException(new Message("Conflict", "Vehicle model labour rates must include all required countries."));
    }
}

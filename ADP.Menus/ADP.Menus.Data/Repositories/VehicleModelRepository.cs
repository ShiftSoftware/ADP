using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.VehicleModel;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

// Two generator diagnostics are suppressed here, both verified against the mapping profile this
// replaced:
//   SHENGEN004/007 — LabourDetailsDTO.Name has no source on VehicleModelLabourDetails. It was never
//     populated by the profile either (it is filled client-side), so it stays null, deliberately.
//   SHENGEN008 — ReplacementItems is reported as "never written back" because the check pairs members
//     by name and this one is written to a differently named entity collection
//     (ReplacementItemVehicleModels) from the AfterEntity hook, which the generator cannot see into.
#pragma warning disable SHENGEN004, SHENGEN007, SHENGEN008
public class VehicleModelRepository : ShiftRepository<ShiftDbContext, VehicleModel, VehicleModelListDTO, VehicleModelDTO>
{
    private readonly IMenuCountryProvider countryProvider;
    private readonly IHashIdService hashIdService;

    public VehicleModelRepository(ShiftDbContext db, IMenuCountryProvider countryProvider, IHashIdService hashIdService) : base(db, x =>
    {
        x.IncludeRelatedEntitiesWithFindAsync(
            i => i.Include(s => s.ReplacementItemVehicleModels!).ThenInclude(r => r.ReplacementItem).ThenInclude(r => r.StandaloneReplacementItemGroup),
            i => i.Include(s => s.ReplacementItemVehicleModels!).ThenInclude(r => r.DefaultParts),
            i => i.Include(s => s.LabourDetails),
            i => i.Include(s => s.LabourRates));

        x.UseGeneratedMapper(map => map

            // ── VIEW ──────────────────────────────────────────────────────────────────────────────
            // The form's replacement-item rows are a flattening of RIVM + its ReplacementItem + its
            // default parts, with soft-deleted rows and parts excluded and the parts kept in SortOrder.
            // Nothing about that is convention-derivable, so it is spelled out.
            // Brand needs no configuration: VehicleModel has no Brand navigation, so the convention's
            // selector carries Value only — exactly what the profile built by hand.
            .ForView(d => d.ReplacementItems, e => e.ReplacementItemVehicleModels == null
                ? new List<VehicleModelDTOReplacementItem>()
                : e.ReplacementItemVehicleModels
                    .Where(s => !s.IsDeleted)
                    .Select(s => new VehicleModelDTOReplacementItem(s.ReplacementItemID.ToString())
                    {
                        Name = s.ReplacementItem != null ? s.ReplacementItem.Name : string.Empty,
                        Type = s.ReplacementItem != null ? s.ReplacementItem.Type : default,
                        AllowMultiplePartNumbers = s.ReplacementItem != null && s.ReplacementItem.AllowMultiplePartNumbers,
                        StandaloneAllowedTime = s.StandaloneAllowedTime,
                        DefaultPartPriceMarginPercentage = s.DefaultPartPriceMarginPercentage,
                        HasPendingPropagation = s.HasPendingPropagation,
                        PendingSince = s.PendingSince,
                        DefaultParts = (s.DefaultParts ?? new List<ReplacementItemVehicleModelPart>())
                            .Where(p => !p.IsDeleted)
                            .OrderBy(p => p.SortOrder)
                            .Select(p => new ReplacementItemDefaultPartDTO
                            {
                                ID = p.ID,
                                PartNumber = p.PartNumber,
                                DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                                DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                            }).ToList(),
                        StandaloneReplacementItemGroup = s.ReplacementItem != null && s.ReplacementItem.StandaloneReplacementItemGroup != null
                            ? new ShiftEntitySelectDTO
                            {
                                Value = s.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                Text = s.ReplacementItem.StandaloneReplacementItemGroup.Name
                            }
                            : null
                    })
                    .ToList())

            // ── ENTITY ────────────────────────────────────────────────────────────────────────────
            // Both child collections are tracked rows with required foreign keys, so the automatic
            // replace-with-new deep write is wrong for them (SHENGEN010): it would build fresh entities
            // for every incoming row and sever the existing ones. They are reconciled in the hook below
            // instead, which also keeps the profile's asymmetry — labour details and rates are HARD
            // removed, replacement items are soft-deleted.
            .IgnoreEntity(e => e.LabourDetails)
            .IgnoreEntity(e => e.LabourRates)

            .AfterEntity((dto, entity, ctx) =>
            {
                // Handle ReplacementItemVehicleModels
                entity.ReplacementItemVehicleModels ??= [];

                // 1. Soft-delete items that are not in the source. Physically removing them
                //    would sever a required non-nullable FK (ShiftEntity forces Restrict).
                //    Soft-deleted RIVMs are filtered out of the forward map.
                var itemsToRemove = entity.ReplacementItemVehicleModels
                    .Where(existing => !existing.IsDeleted && !dto.ReplacementItems.Any(r => r.ReplacementItemID == existing.ReplacementItemID.ToString()))
                    .ToList();
                foreach (var item in itemsToRemove)
                {
                    item.IsDeleted = true;
                    foreach (var part in item.DefaultParts?.Where(p => !p.IsDeleted) ?? [])
                        part.IsDeleted = true;
                }

                // 2. Update existing items or add new items
                foreach (var item in dto.ReplacementItems)
                {
                    var existingItem = entity.ReplacementItemVehicleModels
                        .FirstOrDefault(r => !r.IsDeleted && r.ReplacementItemID.ToString() == item.ReplacementItemID);
                    if (existingItem != null)
                    {
                        existingItem.StandaloneAllowedTime = item.StandaloneAllowedTime ?? existingItem.StandaloneAllowedTime;
                        existingItem.DefaultPartPriceMarginPercentage = item.DefaultPartPriceMarginPercentage ?? existingItem.DefaultPartPriceMarginPercentage;
                        existingItem.DefaultParts ??= [];

                        var incomingPartIds = item.DefaultParts
                            .Where(p => p.ID.HasValue)
                            .Select(p => p.ID!.Value)
                            .ToHashSet();

                        // Soft-delete parts missing from the incoming list. Physically removing
                        // them would sever a required non-nullable FK (ShiftEntity forces Restrict).
                        foreach (var existingPart in existingItem.DefaultParts.Where(p => !p.IsDeleted && !incomingPartIds.Contains(p.ID)).ToList())
                            existingPart.IsDeleted = true;

                        for (int i = 0; i < item.DefaultParts.Count; i++)
                        {
                            var sourcePart = item.DefaultParts[i];
                            var existingPart = sourcePart.ID.HasValue
                                ? existingItem.DefaultParts.FirstOrDefault(p => !p.IsDeleted && p.ID == sourcePart.ID.Value)
                                : null;

                            if (existingPart != null)
                            {
                                existingPart.SortOrder = i;
                                existingPart.PartNumber = sourcePart.PartNumber;
                                existingPart.DefaultPeriodicQuantity = sourcePart.DefaultPeriodicQuantity;
                                existingPart.DefaultStandaloneQuantity = sourcePart.DefaultStandaloneQuantity;
                            }
                            else
                            {
                                existingItem.DefaultParts.Add(new ReplacementItemVehicleModelPart
                                {
                                    SortOrder = i,
                                    PartNumber = sourcePart.PartNumber,
                                    DefaultPeriodicQuantity = sourcePart.DefaultPeriodicQuantity,
                                    DefaultStandaloneQuantity = sourcePart.DefaultStandaloneQuantity
                                });
                            }
                        }
                    }
                    else
                    {
                        // The profile mapped this through a dedicated DTO→entity pair whose DefaultParts,
                        // HasPendingPropagation and PendingSince were all ignored, then rebuilt the parts
                        // in an AfterMap. Written out here so the pending flags stay repository-owned.
                        entity.ReplacementItemVehicleModels.Add(new ReplacementItemVehicleModel
                        {
                            ReplacementItemID = item.ReplacementItemID.ToLong(),
                            StandaloneAllowedTime = item.StandaloneAllowedTime.GetValueOrDefault(),
                            DefaultPartPriceMarginPercentage = item.DefaultPartPriceMarginPercentage,
                            DefaultParts = item.DefaultParts
                                .Select((p, index) => new ReplacementItemVehicleModelPart
                                {
                                    SortOrder = index,
                                    PartNumber = p.PartNumber,
                                    DefaultPeriodicQuantity = p.DefaultPeriodicQuantity,
                                    DefaultStandaloneQuantity = p.DefaultStandaloneQuantity
                                }).ToList()
                        });
                    }
                }

                // Handle LabourDetails. These are hard-removed, not soft-deleted — unlike the RIVM rows
                // above. Kept as-is: it is what the profile did.
                entity.LabourDetails ??= [];

                var laborDetailItemsToRemove = entity.LabourDetails
                    .Where(existing => !dto.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();
                foreach (var item in laborDetailItemsToRemove)
                    entity.LabourDetails.Remove(item);

                foreach (var item in dto.LabourDetails)
                {
                    var existingItem = entity.LabourDetails
                        .FirstOrDefault(r => r.ServiceIntervalGroupID.ToString() == item.ServiceIntervalGroupID);
                    if (existingItem != null)
                    {
                        existingItem.AllowedTime = item.AllowedTime.GetValueOrDefault();
                        existingItem.Consumable = item.Consumable.GetValueOrDefault();
                    }
                    else
                        entity.LabourDetails.Add(new VehicleModelLabourDetails
                        {
                            ServiceIntervalGroupID = item.ServiceIntervalGroupID.ToLong(),
                            AllowedTime = item.AllowedTime.GetValueOrDefault(),
                            Consumable = item.Consumable.GetValueOrDefault()
                        });
                }

                // Handle LabourRates
                entity.LabourRates ??= [];
                var labourRatesToRemove = entity.LabourRates
                    .Where(existing => !dto.LabourRates.Any(r => r.CountryID == existing.CountryID))
                    .ToList();
                foreach (var item in labourRatesToRemove)
                    entity.LabourRates.Remove(item);

                foreach (var item in dto.LabourRates)
                {
                    var existingItem = entity.LabourRates
                        .FirstOrDefault(r => r.CountryID == item.CountryID);
                    if (existingItem is not null)
                        existingItem.LabourRate = item.LabourRate.GetValueOrDefault();
                    else
                        entity.LabourRates.Add(new VehicleModelLabourRate
                        {
                            CountryID = item.CountryID.GetValueOrDefault(),
                            LabourRate = item.LabourRate.GetValueOrDefault()
                        });
                }
            }));
    })
    {
        this.countryProvider = countryProvider;
        this.hashIdService = hashIdService;
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

        // When the user unticks a replacement item (RIVM) the mapper's AfterEntity hook marks
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

        // Diff replacement-item defaults BEFORE base.UpsertAsync triggers the mapper's
        // mutation. If any default values changed and at least one MenuItem still references
        // the RIVM, flag it pending so users see a propagation reminder on reopen.
        if (actionType == ActionTypes.Update && entity.ReplacementItemVehicleModels is not null && dto.ReplacementItems is not null)
        {
            var changedRivmIds = new List<long>();
            foreach (var existing in entity.ReplacementItemVehicleModels.Where(r => !r.IsDeleted))
            {
                var incoming = dto.ReplacementItems
                    .FirstOrDefault(r => r.ReplacementItemID == existing.ReplacementItemID.ToString());
                if (incoming is null)
                    continue;

                if (HasReplacementItemDefaultsChanged(existing, incoming))
                    changedRivmIds.Add(existing.ID);
            }

            if (changedRivmIds.Count > 0)
            {
                var rivmIdsWithMenuRefs = await db.Set<MenuItem>()
                    .AsNoTracking()
                    .Where(mi => !mi.IsDeleted
                        && mi.ReplacementItemVehicleModelID.HasValue
                        && changedRivmIds.Contains(mi.ReplacementItemVehicleModelID.Value))
                    .Select(mi => mi.ReplacementItemVehicleModelID!.Value)
                    .Distinct()
                    .ToListAsync();

                if (rivmIdsWithMenuRefs.Count > 0)
                {
                    var now = DateTimeOffset.UtcNow;
                    var refSet = rivmIdsWithMenuRefs.ToHashSet();
                    foreach (var rivm in entity.ReplacementItemVehicleModels.Where(r => !r.IsDeleted && refSet.Contains(r.ID)))
                    {
                        rivm.HasPendingPropagation = true;
                        rivm.PendingSince = now;
                    }
                }
            }
        }

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    private static bool HasReplacementItemDefaultsChanged(ReplacementItemVehicleModel existing, VehicleModelDTOReplacementItem incoming)
    {
        // Mirrors the mapper's AfterEntity "incoming null = no change" semantics so we
        // only flag pending when the user actually altered a value.
        if (incoming.StandaloneAllowedTime.HasValue && incoming.StandaloneAllowedTime.Value != existing.StandaloneAllowedTime)
            return true;

        if (incoming.DefaultPartPriceMarginPercentage.HasValue
            && incoming.DefaultPartPriceMarginPercentage.Value != existing.DefaultPartPriceMarginPercentage)
            return true;

        var existingParts = (existing.DefaultParts ?? [])
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.SortOrder)
            .ToList();

        var incomingParts = (incoming.DefaultParts ?? new()).ToList();

        if (existingParts.Count != incomingParts.Count)
            return true;

        for (int i = 0; i < existingParts.Count; i++)
        {
            var ep = existingParts[i];
            var ip = incomingParts[i];

            if (ep.PartNumber != ip.PartNumber) return true;
            if (ep.DefaultPeriodicQuantity != ip.DefaultPeriodicQuantity) return true;
            if (ep.DefaultStandaloneQuantity != ip.DefaultStandaloneQuantity) return true;
        }

        return false;
    }

    public async Task<List<MenuVariantReplacementItemUsageDTO>> GetReplacementItemUsageAsync(long vehicleModelId, long replacementItemId)
    {
        // Resolve the RIVM ID; if it doesn't exist for this pair, the dialog has nothing to show.
        var rivm = await db.Set<ReplacementItemVehicleModel>()
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.VehicleModelID == vehicleModelId && r.ReplacementItemID == replacementItemId)
            .Select(r => new { r.ID, r.HasPendingPropagation, r.PendingSince })
            .FirstOrDefaultAsync();

        if (rivm is null)
            return [];

        var rivmId = rivm.ID;

        var rows = await db.Set<MenuVariant>()
            .AsNoTracking()
            .Where(v => !v.IsDeleted && !v.Menu.IsDeleted && v.Menu.VehicleModelID == vehicleModelId)
            .Select(v => new
            {
                MenuID = v.MenuID,
                MenuLabel = v.Menu.BasicModelCode,
                VariantID = v.ID,
                VariantName = v.Name,
                v.MenuPrefix,
                v.MenuPostfix,
                MenuItem = v.Items
                    .Where(i => !i.IsDeleted && i.ReplacementItemVehicleModelID == rivmId)
                    .Select(i => new
                    {
                        i.ID,
                        i.StandaloneAllowedTime,
                        i.LastPropagatedAt,
                        Parts = i.Parts.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder).Select(p => new
                        {
                            p.ID,
                            p.SortOrder,
                            p.PartNumber,
                            p.PeriodicQuantity,
                            p.StandaloneQuantity,
                            CountryPrices = p.CountryPrices.Where(cp => !cp.IsDeleted).Select(cp => new
                            {
                                cp.CountryID,
                                cp.PartPrice,
                                cp.PartPriceMarginPercentage,
                                cp.PartFinalPrice
                            }).ToList()
                        }).ToList()
                    })
                    .FirstOrDefault()
            })
            .OrderBy(x => x.MenuLabel).ThenBy(x => x.VariantName)
            .ToListAsync();

        return rows.Select(r => new MenuVariantReplacementItemUsageDTO
        {
            MenuID = r.MenuID.ToString(),
            MenuLabel = r.MenuLabel,
            VariantID = r.VariantID.ToString(),
            VariantName = r.VariantName,
            MenuPrefix = r.MenuPrefix,
            MenuPostfix = r.MenuPostfix,
            MenuItemID = r.MenuItem?.ID,
            StandaloneAllowedTime = r.MenuItem?.StandaloneAllowedTime,
            IsPropagated = IsMenuItemPropagated(rivm.HasPendingPropagation, rivm.PendingSince, r.MenuItem?.LastPropagatedAt, hasMenuItem: r.MenuItem is not null),
            Parts = r.MenuItem == null ? new() : r.MenuItem.Parts.Select(p => new MenuItemPartUsageDTO
            {
                ID = p.ID,
                SortOrder = p.SortOrder,
                PartNumber = p.PartNumber,
                PeriodicQuantity = p.PeriodicQuantity,
                StandaloneQuantity = p.StandaloneQuantity,
                CountryPrices = p.CountryPrices.Select(cp => new PartPriceByCountryDTO
                {
                    CountryID = cp.CountryID,
                    PartPrice = cp.PartPrice,
                    PartPriceMarginPercentage = cp.PartPriceMarginPercentage,
                    PartFinalPrice = cp.PartFinalPrice
                }).ToList()
            }).ToList()
        }).ToList();
    }

    private static bool IsMenuItemPropagated(bool rivmPending, DateTimeOffset? pendingSince, DateTimeOffset? lastPropagatedAt, bool hasMenuItem)
    {
        // Variants without the item are by definition not propagated — they must be added.
        if (!hasMenuItem) return false;
        // No pending change → every existing item is trivially up-to-date.
        if (!rivmPending || !pendingSince.HasValue) return true;
        // Item was last propagated against (or after) the current pending change.
        return lastPropagatedAt.HasValue && lastPropagatedAt.Value >= pendingSince.Value;
    }

    public async Task<bool> PropagateReplacementItemAsync(long vehicleModelId, long replacementItemId, PropagateReplacementItemRequestDTO request)
    {
        var rivm = await db.Set<ReplacementItemVehicleModel>()
            .Where(r => !r.IsDeleted && r.VehicleModelID == vehicleModelId && r.ReplacementItemID == replacementItemId)
            .FirstOrDefaultAsync()
            ?? throw new ShiftEntityException(new Message("NotFound", "Replacement item is not assigned to this vehicle model."));

        var decodedVariants = (request.Variants ?? [])
            .Where(v => !string.IsNullOrEmpty(v.VariantID))
            .Select(v => new
            {
                VariantID = hashIdService.Decode<MenuVariantDTO>(v.VariantID),
                v.MenuItemID,
                v.StandaloneAllowedTime,
                Parts = v.Parts ?? new List<PropagateReplacementItemPartDTO>()
            })
            .ToList();

        if (decodedVariants.Count == 0)
            return rivm.HasPendingPropagation == false;

        // Validate all referenced variants belong to this vehicle model — no cross-vehicle leakage.
        var requestedVariantIds = decodedVariants.Select(v => v.VariantID).ToList();
        var validVariantIds = await db.Set<MenuVariant>()
            .AsNoTracking()
            .Where(v => !v.IsDeleted && !v.Menu.IsDeleted
                && v.Menu.VehicleModelID == vehicleModelId
                && requestedVariantIds.Contains(v.ID))
            .Select(v => v.ID)
            .ToListAsync();

        var validSet = validVariantIds.ToHashSet();
        var menuItemIdsToLoad = decodedVariants
            .Where(v => v.MenuItemID.HasValue && validSet.Contains(v.VariantID))
            .Select(v => v.MenuItemID!.Value)
            .ToList();

        var existingMenuItems = menuItemIdsToLoad.Count == 0
            ? new List<MenuItem>()
            : await db.Set<MenuItem>()
                .Where(mi => !mi.IsDeleted && menuItemIdsToLoad.Contains(mi.ID))
                .Include(mi => mi.Parts).ThenInclude(p => p.CountryPrices)
                .ToListAsync();

        var propagatedAt = DateTimeOffset.UtcNow;

        foreach (var vu in decodedVariants)
        {
            if (!validSet.Contains(vu.VariantID))
                continue;

            if (vu.MenuItemID.HasValue)
            {
                var menuItem = existingMenuItems.FirstOrDefault(mi => mi.ID == vu.MenuItemID.Value);
                if (menuItem is null)
                    continue;

                if (vu.StandaloneAllowedTime.HasValue)
                    menuItem.StandaloneAllowedTime = vu.StandaloneAllowedTime.Value;

                ApplyPartsToMenuItem(menuItem, vu.Parts);
                menuItem.LastPropagatedAt = propagatedAt;
            }
            else
            {
                var newItem = new MenuItem
                {
                    MenuVariantID = vu.VariantID,
                    ReplacementItemVehicleModelID = rivm.ID,
                    StandaloneAllowedTime = vu.StandaloneAllowedTime ?? rivm.StandaloneAllowedTime,
                    LastPropagatedAt = propagatedAt
                };
                ApplyPartsToMenuItem(newItem, vu.Parts);
                db.Set<MenuItem>().Add(newItem);
            }
        }

        await db.SaveChangesAsync();

        // Clear pending when there are no stale variants left. A variant is stale if it
        // either lacks a MenuItem for this RIVM, or its MenuItem.LastPropagatedAt is older
        // than RIVM.PendingSince. This handles the partial-save flow: the user can save
        // just some rows now, come back later to finish the rest, and pending clears as
        // soon as every row's propagation timestamp catches up to PendingSince.
        if (rivm.HasPendingPropagation && rivm.PendingSince.HasValue)
        {
            var pendingSince = rivm.PendingSince.Value;

            var staleVariantCount = await db.Set<MenuVariant>()
                .AsNoTracking()
                .Where(v => !v.IsDeleted && !v.Menu.IsDeleted && v.Menu.VehicleModelID == vehicleModelId)
                .Where(v => !v.Items.Any(i => !i.IsDeleted
                    && i.ReplacementItemVehicleModelID == rivm.ID
                    && i.LastPropagatedAt.HasValue
                    && i.LastPropagatedAt.Value >= pendingSince))
                .CountAsync();

            if (staleVariantCount == 0)
            {
                rivm.HasPendingPropagation = false;
                rivm.PendingSince = null;
                await db.SaveChangesAsync();
            }
        }

        return !rivm.HasPendingPropagation;
    }

    private static void ApplyPartsToMenuItem(MenuItem menuItem, List<PropagateReplacementItemPartDTO> incomingParts)
    {
        menuItem.Parts ??= new HashSet<MenuItemPart>();

        var incomingIds = incomingParts
            .Where(p => p.MenuItemPartID.HasValue)
            .Select(p => p.MenuItemPartID!.Value)
            .ToHashSet();

        // Soft-delete parts dropped from the incoming list (and their per-country prices).
        // Cannot remove physically — MenuItemPart.MenuItemID is non-nullable with Restrict.
        foreach (var existing in menuItem.Parts.Where(p => !p.IsDeleted && !incomingIds.Contains(p.ID)).ToList())
        {
            existing.IsDeleted = true;
            foreach (var cp in existing.CountryPrices?.Where(c => !c.IsDeleted) ?? [])
                cp.IsDeleted = true;
        }

        for (int i = 0; i < incomingParts.Count; i++)
        {
            var ip = incomingParts[i];
            var existing = ip.MenuItemPartID.HasValue
                ? menuItem.Parts.FirstOrDefault(p => !p.IsDeleted && p.ID == ip.MenuItemPartID.Value)
                : null;

            if (existing != null)
            {
                existing.SortOrder = i;
                existing.PartNumber = ip.PartNumber ?? string.Empty;
                existing.PeriodicQuantity = ip.PeriodicQuantity;
                existing.StandaloneQuantity = ip.StandaloneQuantity;
                ApplyCountryPricesToPart(existing, ip.CountryPrices);
            }
            else
            {
                var newPart = new MenuItemPart
                {
                    SortOrder = i,
                    PartNumber = ip.PartNumber ?? string.Empty,
                    PeriodicQuantity = ip.PeriodicQuantity,
                    StandaloneQuantity = ip.StandaloneQuantity
                };
                ApplyCountryPricesToPart(newPart, ip.CountryPrices);
                menuItem.Parts.Add(newPart);
            }
        }
    }

    private static void ApplyCountryPricesToPart(MenuItemPart part, List<PartPriceByCountryDTO> incoming)
    {
        part.CountryPrices ??= new HashSet<MenuItemPartCountryPrice>();

        var incomingCountryIds = incoming
            .Where(cp => cp.CountryID.HasValue)
            .Select(cp => cp.CountryID!.Value)
            .ToHashSet();

        // Soft-delete country prices that aren't in the incoming list. Same Restrict-FK
        // constraint as MenuItemPart — we can't physically remove.
        foreach (var existing in part.CountryPrices.Where(c => !c.IsDeleted && !incomingCountryIds.Contains(c.CountryID)).ToList())
            existing.IsDeleted = true;

        foreach (var src in incoming)
        {
            if (!src.CountryID.HasValue) continue;

            var existing = part.CountryPrices.FirstOrDefault(c => !c.IsDeleted && c.CountryID == src.CountryID.Value);
            if (existing != null)
            {
                existing.PartPrice = src.PartPrice;
                existing.PartPriceMarginPercentage = src.PartPriceMarginPercentage;
                existing.PartFinalPrice = src.PartFinalPrice.GetValueOrDefault();
            }
            else
            {
                part.CountryPrices.Add(new MenuItemPartCountryPrice
                {
                    CountryID = src.CountryID.Value,
                    PartPrice = src.PartPrice,
                    PartPriceMarginPercentage = src.PartPriceMarginPercentage,
                    PartFinalPrice = src.PartFinalPrice.GetValueOrDefault()
                });
            }
        }
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
#pragma warning restore SHENGEN004, SHENGEN007, SHENGEN008

using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;
using ShiftSoftware.ADP.Menus.Shared.DTOs.MenuVariant;
using ShiftSoftware.ADP.Menus.Shared.DTOs.Menu;
using ShiftSoftware.ADP.Menus.Shared.DTOs.ServiceInterval;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Brand;
using System.Text.Json;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Data.Repositories;

// The generated pair mappers below are reported as incomplete, and every one of these members was
// equally unmapped by the mapping profile this replaced — they stay null/false, deliberately:
// MenuItemDTO.NotStoredInMenuItemsTable, PartPriceByCountryDTO.UnitPrices and
// MenuItemPartDTO.PartPrice/PartPriceMarginPercentage/PartFinalPrice are transient, filled by the
// pricing refresh rather than read from the entity; LabourDetailsDTO.Name has no entity source.
// The list-direction (SHENGEN007) variants of those are moot as well — MenuVariantListDTO carries no
// Items collection, so no pair list projection is ever used.
#pragma warning disable SHENGEN004, SHENGEN007
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

        // Two of the four child collections need help; LabourRates and LabourDetails are left entirely to
        // the generator's automatic deep composition, which gets them right.
        //
        //   PeriodicAvailabilities — taken over completely. The pair mapper matches
        //     ServiceIntervalIDSelectorDTO.ID against the LINK ROW's own primary key and emits
        //     `dto.ID = source.ID.ToString()`. That is a real id of the wrong entity: the form would
        //     round-trip MenuPeriodicAvailability ids as if they were ServiceInterval ids.
        //
        //   Items — the pair is kept and only corrected. It already maps ReplacementItemVehicleModelID,
        //     StandaloneAllowedTime and the whole part/country-price graph correctly; what it cannot
        //     derive is the flattened ReplacementItem block, BackingItemHasPendingPropagation (a
        //     comparison across two entities), and the soft-delete filtering / SortOrder ordering.
        i.UseGeneratedMapper(map => map

            // ── VIEW ──────────────────────────────────────────────────────────────────────────────────
            .ForView(d => d.PeriodicAvailabilities, e => e.PeriodicAvailabilities
                .Select(s => new ServiceIntervalIDSelectorDTO { ID = s.ServiceIntervalID.ToString() })
                .ToList())

            // Soft-deleted items are excluded, and so are items whose backing RIVM has been unticked on
            // the vehicle model — those rows still exist but no longer belong to the menu. Everything the
            // pair mappers already get right (ReplacementItemVehicleModelID, StandaloneAllowedTime, and
            // the whole part/country-price graph including the part ID the save-time diff matches on) is
            // left to them; only the three things they cannot derive are supplied here.
            .ForViewChildren(d => d.Items,
                e => e.Items.Where(mi => !mi.IsDeleted
                    && (mi.ReplacementItemVehicleModel == null || !mi.ReplacementItemVehicleModel.IsDeleted)),
                item => item

                    // Flattened across RIVM → ReplacementItem → its standalone group.
                    .For(d => d.ReplacementItem, mi => mi.ReplacementItemVehicleModel == null || mi.ReplacementItemVehicleModel.ReplacementItem == null
                        ? null!
                        : new MenuItemReplacementItemDTO
                        {
                            ID = mi.ReplacementItemVehicleModel.ReplacementItem.ID.ToString(),
                            Name = mi.ReplacementItemVehicleModel.ReplacementItem.Name,
                            Type = mi.ReplacementItemVehicleModel.ReplacementItem.Type,
                            AllowMultiplePartNumbers = mi.ReplacementItemVehicleModel.ReplacementItem.AllowMultiplePartNumbers,
                            StandaloneAllowedTime = mi.ReplacementItemVehicleModel.StandaloneAllowedTime,
                            DefaultPartPriceMarginPercentage = mi.ReplacementItemVehicleModel.DefaultPartPriceMarginPercentage,
                            StandaloneReplacementItemGroup = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup == null
                                ? null
                                : new ShiftEntitySelectDTO
                                {
                                    Value = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.ID.ToString(),
                                    Text = mi.ReplacementItemVehicleModel.ReplacementItem.StandaloneReplacementItemGroup.Name
                                }
                        })

                    // Per-MenuItem staleness: only highlight when the RIVM is pending AND this
                    // particular MenuItem's LastPropagatedAt is older than RIVM.PendingSince.
                    // After bulk-form propagation or manual MenuForm save, LastPropagatedAt is
                    // bumped → highlight clears for that row even if other variants are still stale.
                    .For(d => d.BackingItemHasPendingPropagation, mi => mi.ReplacementItemVehicleModel != null
                        && mi.ReplacementItemVehicleModel.HasPendingPropagation
                        && mi.ReplacementItemVehicleModel.PendingSince.HasValue
                        && (!mi.LastPropagatedAt.HasValue
                            || mi.LastPropagatedAt.Value < mi.ReplacementItemVehicleModel.PendingSince.Value))

                    // Soft-deleted parts excluded and the rest kept in SortOrder. Country prices are NOT
                    // filtered by IsDeleted — the profile did not filter them either.
                    .ForChildren(d => d.Parts, mi => mi.Parts.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder)))

            // ── ENTITY ────────────────────────────────────────────────────────────────────────────────
            // MenuID needs nothing: the convention converts the hash-id string to the long foreign key
            // (MappingHelpers.ToLong), which is what the profile did by hand.
            //
            // All four child collections are tracked rows with required foreign keys back to the variant,
            // so the automatic replace-with-new deep write is wrong for every one of them (SHENGEN010).
            .IgnoreEntity(e => e.PeriodicAvailabilities)
            .IgnoreEntity(e => e.LabourDetails)
            .IgnoreEntity(e => e.Items)
            .IgnoreEntity(e => e.LabourRates)

            .AfterEntity((dto, entity, ctx) =>
            {
                entity.LabourDetails ??= [];
                var itemsToRemove = entity.LabourDetails
                    .Where(existing => !dto.LabourDetails.Any(r => r.ServiceIntervalGroupID == existing.ServiceIntervalGroupID.ToString()))
                    .ToList();
                foreach (var item in itemsToRemove)
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
                        entity.LabourDetails.Add(new MenuLabourDetails
                        {
                            ServiceIntervalGroupID = item.ServiceIntervalGroupID.ToLong(),
                            AllowedTime = item.AllowedTime.GetValueOrDefault(),
                            Consumable = item.Consumable.GetValueOrDefault()
                        });
                }

                entity.PeriodicAvailabilities ??= [];
                var serviceIntervalItemsToRemove = entity.PeriodicAvailabilities
                    .Where(existing => !dto.PeriodicAvailabilities.Any(r => r.ID == existing.ServiceIntervalID.ToString()))
                    .ToList();
                foreach (var item in serviceIntervalItemsToRemove)
                    entity.PeriodicAvailabilities.Remove(item);

                foreach (var item in dto.PeriodicAvailabilities)
                {
                    var existingItem = entity.PeriodicAvailabilities
                        .FirstOrDefault(r => r.ServiceIntervalID.ToString() == item.ID);
                    if (existingItem == null)
                        entity.PeriodicAvailabilities.Add(new MenuPeriodicAvailability { ServiceIntervalID = item.ID.ToLong() });
                }

                entity.Items ??= [];
                // Skip already-soft-deleted items: they won't be in the DTO (they're filtered out of
                // the forward map) and we must not touch them here — removing them would sever
                // a required non-nullable FK (MenuItem.MenuVariantID) and throw.
                foreach (var item in dto.Items)
                {
                    var existingItem = entity.Items
                        .FirstOrDefault(r => !r.IsDeleted && r.ReplacementItemVehicleModelID == item.ReplacementItemVehicleModelID);
                    if (existingItem != null)
                        ApplyMenuItem(item, existingItem);
                    else
                    {
                        var created = new MenuItem();
                        ApplyMenuItem(item, created);
                        entity.Items.Add(created);
                    }
                }

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
                        entity.LabourRates.Add(new MenuVariantLabourRate
                        {
                            CountryID = item.CountryID.GetValueOrDefault(),
                            LabourRate = item.LabourRate.GetValueOrDefault()
                        });
                }
            }));
    })
    {
        this.countryProvider = countryProvider;
    }

    /// <summary>
    /// Writes one incoming <see cref="MenuItemDTO"/> onto a menu item — a fresh one when the variant has
    /// no row for that replacement item yet, otherwise the tracked one. Reproduces the mapping profile's
    /// MenuItemDTO→MenuItem reverse map together with the AfterMap that owned the parts diff.
    /// </summary>
    private static void ApplyMenuItem(MenuItemDTO src, MenuItem dest)
    {
        dest.ReplacementItemVehicleModelID = src.ReplacementItemVehicleModelID;
        dest.StandaloneAllowedTime = src.StandaloneAllowedTime.GetValueOrDefault();

        // Stamp LastPropagatedAt on every MenuItem save (MenuForm path). Saving
        // counts as the user confirming the values against current vehicle-model
        // defaults — same effect as propagating via the bulk dialog. Clears the
        // per-MenuItem pending highlight for this row.
        dest.LastPropagatedAt = DateTimeOffset.UtcNow;

        // Don't Clear()/Remove() on tracked Parts — MenuItemPart.MenuItemID is
        // non-nullable and ShiftEntity forces DeleteBehavior.Restrict, which turns
        // severed FKs into a HandleConceptualNulls throw. Diff by ID instead, and
        // soft-delete rows missing from the incoming DTO.
        dest.Parts ??= [];

        var incomingPartIds = src.Parts
            .Where(p => p.ID.HasValue)
            .Select(p => p.ID!.Value)
            .ToHashSet();

        foreach (var existingPart in dest.Parts.Where(p => !p.IsDeleted && !incomingPartIds.Contains(p.ID)).ToList())
        {
            existingPart.IsDeleted = true;
            foreach (var cp in existingPart.CountryPrices?.Where(c => !c.IsDeleted) ?? [])
                cp.IsDeleted = true;
        }

        for (int i = 0; i < src.Parts.Count; i++)
        {
            var sourcePart = src.Parts[i];
            var sourceCountryPrices = sourcePart.CountryPrices ?? [];

            var existingPart = sourcePart.ID.HasValue
                ? dest.Parts.FirstOrDefault(p => !p.IsDeleted && p.ID == sourcePart.ID.Value)
                : null;

            if (existingPart != null)
            {
                existingPart.SortOrder = i;
                existingPart.PartNumber = sourcePart.PartNumber;
                existingPart.PeriodicQuantity = sourcePart.PeriodicQuantity;
                existingPart.StandaloneQuantity = sourcePart.StandaloneQuantity;

                existingPart.CountryPrices ??= new HashSet<MenuItemPartCountryPrice>();

                var incomingCountryIds = sourceCountryPrices
                    .Where(cp => cp.CountryID.HasValue)
                    .Select(cp => cp.CountryID!.Value)
                    .ToHashSet();

                foreach (var cp in existingPart.CountryPrices.Where(c => !c.IsDeleted && !incomingCountryIds.Contains(c.CountryID)).ToList())
                    cp.IsDeleted = true;

                foreach (var sourceCp in sourceCountryPrices)
                {
                    var existingCp = sourceCp.CountryID.HasValue
                        ? existingPart.CountryPrices.FirstOrDefault(c => !c.IsDeleted && c.CountryID == sourceCp.CountryID.Value)
                        : null;

                    if (existingCp != null)
                    {
                        existingCp.PartPrice = sourceCp.PartPrice;
                        existingCp.PartPriceMarginPercentage = sourceCp.PartPriceMarginPercentage;
                        existingCp.PartFinalPrice = sourceCp.PartFinalPrice.GetValueOrDefault();
                        existingCp.SelectedUnitName = sourceCp.SelectedUnitName;
                    }
                    else
                    {
                        existingPart.CountryPrices.Add(new MenuItemPartCountryPrice
                        {
                            CountryID = sourceCp.CountryID.GetValueOrDefault(),
                            PartPrice = sourceCp.PartPrice,
                            PartPriceMarginPercentage = sourceCp.PartPriceMarginPercentage,
                            PartFinalPrice = sourceCp.PartFinalPrice.GetValueOrDefault(),
                            SelectedUnitName = sourceCp.SelectedUnitName
                        });
                    }
                }
            }
            else
            {
                dest.Parts.Add(new MenuItemPart
                {
                    SortOrder = i,
                    PartNumber = sourcePart.PartNumber,
                    PeriodicQuantity = sourcePart.PeriodicQuantity,
                    StandaloneQuantity = sourcePart.StandaloneQuantity,
                    CountryPrices = sourceCountryPrices
                        .Select(cp => new MenuItemPartCountryPrice
                        {
                            CountryID = cp.CountryID.GetValueOrDefault(),
                            PartPrice = cp.PartPrice,
                            PartPriceMarginPercentage = cp.PartPriceMarginPercentage,
                            PartFinalPrice = cp.PartFinalPrice.GetValueOrDefault(),
                            SelectedUnitName = cp.SelectedUnitName
                        })
                        .ToList()
                });
            }
        }
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
            .AnyAsync(x => x.Name == dto.Name))
            throw new ShiftEntityException(new("Conflict", "Menu variant name should be unique within group"));

        var siblingVariants = await db.Set<MenuVariant>()
            .Where(x => !x.IsDeleted && x.ID != entity.ID && x.MenuID == menuID)
            .Select(x => new
            {
                x.Name,
                x.MenuPrefix,
                x.MenuPostfix,
                x.HasStandaloneItems,
                x.StandaloneMenuPrefix,
                x.StandaloneMenuPostfix
            })
            .ToListAsync();

        foreach (var sibling in siblingVariants)
        {
            var conflict = FindPrefixPostfixConflict(
                sibling.MenuPrefix, sibling.MenuPostfix,
                dto.MenuPrefix, dto.MenuPostfix);

            if (conflict is not null)
            {
                throw new ShiftEntityException(new(
                    "Conflict",
                    $"Menu prefix/postfix of variant \"{dto.Name}\" conflicts with variant \"{sibling.Name}\" in {DescribeLanguage(conflict.Value.Language)}: prefix \"{conflict.Value.Prefix}\", postfix \"{conflict.Value.Postfix}\"."));
            }
        }

        if (dto.HasStandaloneItems)
        {
            foreach (var sibling in siblingVariants.Where(x => x.HasStandaloneItems))
            {
                var conflict = FindPrefixPostfixConflict(
                    sibling.StandaloneMenuPrefix, sibling.StandaloneMenuPostfix,
                    dto.StandaloneMenuPrefix, dto.StandaloneMenuPostfix);

                if (conflict is not null)
                {
                    throw new ShiftEntityException(new(
                        "Conflict",
                        $"Standalone menu prefix/postfix of variant \"{dto.Name}\" conflicts with variant \"{sibling.Name}\" in {DescribeLanguage(conflict.Value.Language)}: prefix \"{conflict.Value.Prefix}\", postfix \"{conflict.Value.Postfix}\"."));
                }
            }
        }

        var countries = await countryProvider.GetSupportedCountriesAsync();
        ValidateLabourRates(dto, countries);
        ValidatePartCountryPrices(dto, countries);

        DeleteChildrenDroppedByTheDto(entity, dto);

        return await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);
    }

    /// <summary>
    /// Deletes the child rows this save drops, before the mapper detaches them from the variant.
    ///
    /// The mapping profile syncs these collections by REMOVING entries the DTO no longer carries. Each
    /// of these children has a non-nullable <c>MenuVariantID</c>, so removing one severs a required
    /// relationship: EF records a "conceptual null" on the foreign key and plans to resolve it by
    /// cascade-deleting the orphan during <c>SaveChanges</c>. It never gets that far — ShiftEntity's
    /// repository calls <c>ChangeTracker.Entries()</c> first, to run the registered
    /// <c>IShiftEntitySaveValidator</c>s (ShiftIdentity's dashboard always registers one), and that
    /// call detects changes and throws <see cref="InvalidOperationException"/> on the unresolved
    /// conceptual null. Removing a service interval from a variant fails exactly this way, and so do
    /// labour details and country labour rates.
    ///
    /// Deleting the orphans up front settles the foreign key before anything can trip over it.
    /// <c>MenuItem</c> needs no entry here: the mapper never removes from that collection — see the
    /// comment on <c>Items</c> in the <c>AfterEntity</c> hook above, which describes this same trap.
    ///
    /// The delete is HARD rather than soft on purpose. These are join rows, and the unique index on
    /// (MenuVariantID, ServiceIntervalID) carries no <c>IsDeleted</c> filter, so a soft-deleted row
    /// would permanently block re-adding that interval to the variant. It also replicates correctly:
    /// a hard delete is the one case that REMOVES the Cosmos document rather than upserting it.
    /// </summary>
    private void DeleteChildrenDroppedByTheDto(MenuVariant entity, MenuVariantDTO dto)
    {
        DeleteDropped(entity.PeriodicAvailabilities, x => x.ServiceIntervalID,
            dto.PeriodicAvailabilities?.Select(x => x.ID.ToLong()));

        DeleteDropped(entity.LabourDetails, x => x.ServiceIntervalGroupID,
            dto.LabourDetails?.Select(x => x.ServiceIntervalGroupID.ToLong()));

        DeleteDropped(entity.LabourRates, x => x.CountryID,
            dto.LabourRates?.Select(x => x.CountryID.GetValueOrDefault()));
    }

    /// <param name="keptKeys">
    /// The keys the DTO still carries. Null is treated as "none kept", matching the mapper — it syncs
    /// against the DTO's collection whatever it holds.
    /// </param>
    private void DeleteDropped<TChild>(
        ICollection<TChild> children,
        Func<TChild, long> keySelector,
        IEnumerable<long>? keptKeys)
        where TChild : class
    {
        if (children is null || children.Count == 0)
            return;

        var kept = (keptKeys ?? []).ToHashSet();

        // Materialised before deleting: EF's fixup mutates the navigation as entries become Deleted.
        var dropped = children.Where(child => !kept.Contains(keySelector(child))).ToList();

        foreach (var child in dropped)
            db.Remove(child);
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

    // Returns the first language where the resolved (prefix, postfix) pair matches,
    // or null if no collision exists.
    private static PrefixPostfixConflict? FindPrefixPostfixConflict(string? aPrefix, string? aPostfix, string? bPrefix, string? bPostfix)
    {
        var aPrefixMap = AsLanguageMap(aPrefix);
        var aPostfixMap = AsLanguageMap(aPostfix);
        var bPrefixMap = AsLanguageMap(bPrefix);
        var bPostfixMap = AsLanguageMap(bPostfix);

        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in aPrefixMap.Keys) languages.Add(k);
        foreach (var k in aPostfixMap.Keys) languages.Add(k);
        foreach (var k in bPrefixMap.Keys) languages.Add(k);
        foreach (var k in bPostfixMap.Keys) languages.Add(k);

        // Null-null or plain-plain pairs still need a comparison pass.
        if (languages.Count == 0)
            languages.Add("");

        foreach (var lang in languages)
        {
            var aP = ResolveForLanguage(aPrefixMap, lang);
            var aS = ResolveForLanguage(aPostfixMap, lang);
            var bP = ResolveForLanguage(bPrefixMap, lang);
            var bS = ResolveForLanguage(bPostfixMap, lang);

            if (string.Equals(aP, bP, StringComparison.OrdinalIgnoreCase)
                && string.Equals(aS, bS, StringComparison.OrdinalIgnoreCase))
            {
                return new PrefixPostfixConflict(lang, aP, aS);
            }
        }

        return null;
    }

    private readonly record struct PrefixPostfixConflict(string Language, string Prefix, string Postfix);

    private static string DescribeLanguage(string lang) => lang switch
    {
        "" => "all languages (no language-specific values)",
        PlainKey => "all languages (plain text)",
        _ => $"language \"{lang}\""
    };

    // "" → empty map; "{...}" → parsed dict (case-insensitive keys); plain string → { "*": raw }.
    private static IReadOnlyDictionary<string, string> AsLanguageMap(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return EmptyMap;

        if (raw[0] == '{')
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
                if (parsed is not null && parsed.Count > 0)
                {
                    var dict = new Dictionary<string, string>(parsed.Count, StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in parsed)
                        dict[kv.Key] = kv.Value;
                    return dict;
                }
            }
            catch { /* fall through to plain-string handling */ }
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [PlainKey] = raw };
    }

    private static string ResolveForLanguage(IReadOnlyDictionary<string, string> map, string lang)
    {
        if (map.Count == 0) return string.Empty;
        if (map.TryGetValue(PlainKey, out var plain)) return plain;
        if (map.TryGetValue(lang, out var v)) return v;
        if (map.TryGetValue("en", out var en)) return en;
        return map.Values.First();
    }

    private const string PlainKey = "*"; // reserved bucket for non-JSON values
    private static readonly IReadOnlyDictionary<string, string> EmptyMap = new Dictionary<string, string>(0);

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
#pragma warning restore SHENGEN004, SHENGEN007

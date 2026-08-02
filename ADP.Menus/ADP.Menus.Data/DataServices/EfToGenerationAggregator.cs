using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Data.DataServices;

/// <summary>
/// The DMS export's adapter: EF menu graph → the source-agnostic
/// <see cref="MenuGenerationRequest"/>. One of two such adapters — the vehicle lookup has its own,
/// built from Cosmos documents — feeding the one shared <see cref="MenuCodeGenerator"/>. See
/// COSMOS_REPLICATION_PLAN.md §1.1.
///
/// <b>SOFT-DELETED ROWS ARE FILTERED OUT HERE</b>, on every table, so the generator receives live rows
/// only and never reasons about deletion. Its Cosmos twin does the identical filtering, and the two are
/// held in step by <c>ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted</c>, which
/// soft-deletes one row of each table in turn and asserts both paths still produce identical output.
/// <b>Add a table to the replication and you must add it to both adapters and to that test.</b>
///
/// This is also the point where the filtering can be pushed down into the caller's SQL query as a load
/// optimisation. It cannot be pushed down entirely — EF filtered includes only work on collection
/// navigations, and "keep the item but drop its deleted standalone group" is not expressible that way —
/// so this method stays the authority either way.
///
/// Beyond the delete filter it is a PURE PROJECTION: it copies, it does not decide, and it reorders
/// nothing, so every remaining inclusion rule and every "first match wins" tie-break stays in the
/// generator where both consumers share it.
///
/// Two consequences worth knowing:
///
/// • <b>Order is behaviour.</b> The periodic pass takes the FIRST matching labour detail and a grouped
///   standalone line takes its allowed time from the FIRST item in its group, so the order EF hands
///   collections over in decides which row wins (open item O8). Collections are copied in place.
///
/// • <b>A missing <c>Include</c> loses lines quietly.</b> Reference data is read from
///   <c>PeriodicAvailabilities.ServiceInterval</c>, <c>LabourDetails.ServiceIntervalGroup.ServiceIntervals</c>
///   and <c>Items.ReplacementItemVehicleModel.ReplacementItem.ReplacementItemServiceIntervalGroups.ServiceIntervalGroup.ServiceIntervals</c>.
///   An unloaded navigation either throws here or yields an empty interval group, and an empty group
///   means the generator emits no periodic line for those intervals — no error, just missing menu codes.
///   Keep the caller's query in step with this list.
/// </summary>
public static class EfToGenerationAggregator
{
    /// <param name="menuVariants">
    /// Loaded with the navigations listed on the class. Order is preserved into the request.
    /// </param>
    /// <param name="labourRateMappings">
    /// Keyed by (brand, primary labour rate). Rekeyed to <see cref="MenuGenerationLabourRateKey"/>,
    /// which preserves the source's decimal-VALUE equality so 12.5 and 12.50 stay the same key.
    /// </param>
    /// <param name="brandMappings">Brand → mapping. An unmapped brand is valid; the generator falls back to "Z".</param>
    public static MenuGenerationRequest Build(
        IEnumerable<MenuVariant> menuVariants,
        IReadOnlyDictionary<CompositeKey<long?, decimal>, LabourRateMapping> labourRateMappings,
        IReadOnlyDictionary<long?, BrandMapping> brandMappings)
    {
        ArgumentNullException.ThrowIfNull(menuVariants);
        ArgumentNullException.ThrowIfNull(labourRateMappings);
        ArgumentNullException.ThrowIfNull(brandMappings);

        // The two row-level filters the export's own query applies, repeated here so the rule holds for
        // any caller — including one that loads variants without them.
        var variants = menuVariants
            .Where(variant => !variant.IsDeleted && !variant.Menu.IsDeleted)
            .ToList();
        var intervals = new Dictionary<long, MenuGenerationServiceInterval>();
        var groups = new Dictionary<long, MenuGenerationServiceIntervalGroup>();

        // Reference data is collected from exactly the navigations the fold used to walk, and no others.
        // In particular a group's interval membership comes ONLY from ServiceIntervalGroup.ServiceIntervals
        // — never inferred from ServiceInterval.ServiceIntervalGroupID — because that navigation is the
        // only thing the original fold consulted. Inferring membership from the foreign key would let a
        // partially-loaded graph produce periodic lines the export never emitted.
        foreach (var variant in variants)
        {
            foreach (var period in LivePeriods(variant))
                AddInterval(period.ServiceInterval, intervals);

            foreach (var labour in LiveLabours(variant))
                AddGroup(labour.ServiceIntervalGroup, groups);

            foreach (var item in LiveItems(variant))
            {
                var replacementItem = item.ReplacementItemVehicleModel?.ReplacementItem;
                if (replacementItem is null)
                    continue;

                foreach (var membership in LiveIntervalGroupLinks(replacementItem))
                    AddGroup(membership.ServiceIntervalGroup, groups);
            }
        }

        return new MenuGenerationRequest
        {
            Variants = variants.Select(MapVariant).ToList(),
            Reference = new MenuGenerationReferenceData
            {
                Intervals = intervals,
                Groups = groups,
                LabourRateCodes = labourRateMappings.ToDictionary(
                    x => new MenuGenerationLabourRateKey(x.Key.KeyPart1, x.Key.KeyPart2),
                    x => x.Value.Code),
                BrandMappings = brandMappings.ToDictionary(
                    x => x.Key,
                    x => new MenuGenerationBrandMapping
                    {
                        Code = x.Value.Code,
                        Abbreviation = x.Value.BrandAbbreviation,
                    }),
            },
        };
    }

    private static MenuGenerationVariant MapVariant(MenuVariant source) => new()
    {
        VariantID = source.ID,
        BasicModelCode = source.Menu.BasicModelCode,
        BrandID = source.Menu.VehicleModel!.BrandID,
        Model = source.Menu.VehicleModel.Name,
        VariantName = source.Name,
        MenuPrefix = source.MenuPrefix,
        MenuPostfix = source.MenuPostfix,
        StandaloneMenuPrefix = source.StandaloneMenuPrefix,
        StandaloneMenuPostfix = source.StandaloneMenuPostfix,
        LabourRate = source.LabourRate,
        DiscountPercentage = source.DiscountPercentage,
        HasStandaloneItems = source.HasStandaloneItems,
        CountryLabourRates = source.LabourRates
            .Where(x => !x.IsDeleted)
            .Select(x => new MenuGenerationCountryLabourRate
            {
                CountryID = x.CountryID,
                LabourRate = x.LabourRate,
            }).ToList(),
        Periods = LivePeriods(source).Select(x => new MenuGenerationPeriod
        {
            ServiceIntervalID = x.ServiceIntervalID,
        }).ToList(),
        Labours = LiveLabours(source).Select(x => new MenuGenerationLabour
        {
            ServiceIntervalGroupID = x.ServiceIntervalGroupID,
            AllowedTime = x.AllowedTime,
            Consumable = x.Consumable,
        }).ToList(),
        Items = LiveItems(source).Select(MapItem).ToList(),
    };

    // ---- the delete filters ------------------------------------------------------------------------
    // One named method per table, so the Cosmos adapter's equivalents can be read against them side by
    // side. These are the whole of the export's soft-delete rule.

    /// <summary>A deleted availability, or one whose service interval is deleted, produces no line.</summary>
    private static IEnumerable<MenuPeriodicAvailability> LivePeriods(MenuVariant variant) =>
        variant.PeriodicAvailabilities.Where(x => !x.IsDeleted && !x.ServiceInterval.IsDeleted);

    /// <summary>A deleted labour detail — or one whose interval group is deleted — supplies nothing.</summary>
    private static IEnumerable<MenuLabourDetails> LiveLabours(MenuVariant variant) =>
        variant.LabourDetails.Where(x => !x.IsDeleted && !x.ServiceIntervalGroup.IsDeleted);

    /// <summary>
    /// An item participates only when the item row, its replacement-item LINK and the replacement item
    /// ITSELF are all live. An item with no link at all is excluded too — the source's null guard.
    /// </summary>
    private static IEnumerable<MenuItem> LiveItems(MenuVariant variant) =>
        variant.Items.Where(item =>
            !item.IsDeleted
            && item.ReplacementItemVehicleModel is not null
            && !item.ReplacementItemVehicleModel.IsDeleted
            && !(item.ReplacementItemVehicleModel.ReplacementItem?.IsDeleted ?? false));

    /// <summary>
    /// A replacement item's live interval-group links.
    ///
    /// This MUST be the same set the reference dictionary is built from. The generator looks a group up
    /// with an INDEXER, so an id left on the item without a matching dictionary entry throws instead of
    /// being skipped — one method used by both is what stops the two diverging.
    /// </summary>
    private static IEnumerable<ReplacementItemServiceIntervalGroup> LiveIntervalGroupLinks(ReplacementItem replacementItem) =>
        replacementItem.ReplacementItemServiceIntervalGroups
            .Where(x => !x.IsDeleted && !x.ServiceIntervalGroup.IsDeleted);

    private static MenuGenerationItem MapItem(MenuItem source)
    {
        var replacementVehicleModel = source.ReplacementItemVehicleModel;
        var replacementItem = replacementVehicleModel?.ReplacementItem;
        var standaloneGroup = replacementItem?.StandaloneReplacementItemGroup;

        return new MenuGenerationItem
        {
            MenuItemID = source.ID,
            StandaloneAllowedTime = source.StandaloneAllowedTime,
            ReplacementItemServiceIntervalGroupIDs = replacementItem is null
                ? []
                : LiveIntervalGroupLinks(replacementItem).Select(x => x.ServiceIntervalGroupID).ToList(),
            StandaloneOperationCode = replacementItem?.StandaloneOperationCode,
            StandaloneLabourCode = replacementItem?.StandaloneLabourCode,
            FriendlyName = replacementItem?.FriendlyName,
            // A deleted group counts as NO group, so the item falls back to an ungrouped standalone line
            // rather than disappearing: deleting a grouping withdraws the grouping, not the items.
            StandaloneGroup = standaloneGroup is null || standaloneGroup.IsDeleted
                ? null
                : new MenuGenerationStandaloneGroup
                {
                    ID = standaloneGroup.ID,
                    MenuCode = standaloneGroup.MenuCode,
                    LabourCode = standaloneGroup.LabourCode,
                    Name = standaloneGroup.Name,
                },
            Parts = source.Parts.Where(x => !x.IsDeleted).Select(x => new MenuGenerationPart
            {
                PartNumber = x.PartNumber,
                SortOrder = x.SortOrder,
                PeriodicQuantity = x.PeriodicQuantity,
                StandaloneQuantity = x.StandaloneQuantity,
                CountryPrices = (x.CountryPrices ?? [])
                    .Where(price => !price.IsDeleted)
                    .Select(price => new MenuGenerationPartPrice
                    {
                        CountryID = price.CountryID,
                        PartPrice = price.PartPrice,
                        PartFinalPrice = price.PartFinalPrice,
                    }).ToList(),
            }).ToList(),
        };
    }

    private static void AddGroup(
        ServiceIntervalGroup source,
        IDictionary<long, MenuGenerationServiceIntervalGroup> groups)
    {
        groups[source.ID] = new MenuGenerationServiceIntervalGroup
        {
            LabourCode = source.LabourCode,
            // Deleted intervals are not members. A period naming one is already gone, but the membership
            // is also what a labour detail matches on, so leaving it here would resurrect the line.
            ServiceIntervalIDs = (source.ServiceIntervals ?? []).Where(x => !x.IsDeleted).Select(x => x.ID).ToHashSet(),
        };
    }

    private static void AddInterval(
        ServiceInterval source,
        IDictionary<long, MenuGenerationServiceInterval> intervals)
    {
        intervals[source.ID] = new MenuGenerationServiceInterval
        {
            Code = source.Code,
            Description = source.Description,
            ValueInMeter = source.ValueInMeter,

            // Carried for consumers; the generator never uses it to decide group membership.
            GroupID = source.ServiceIntervalGroupID,
        };
    }
}

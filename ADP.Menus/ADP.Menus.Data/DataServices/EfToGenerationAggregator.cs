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
/// A PURE PROJECTION, and that is the whole point: it copies, it does not decide. In particular it
/// does NOT filter soft-deleted rows and does not reorder anything, so every inclusion rule and every
/// "first match wins" tie-break stays in the generator where both consumers share it. Adding a
/// <c>Where(x =&gt; !x.IsDeleted)</c> here would silently give the export different rules from the
/// lookup — exactly the drift this design exists to prevent.
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

        var variants = menuVariants.ToList();
        var intervals = new Dictionary<long, MenuGenerationServiceInterval>();
        var groups = new Dictionary<long, MenuGenerationServiceIntervalGroup>();

        // Reference data is collected from exactly the navigations the fold used to walk, and no others.
        // In particular a group's interval membership comes ONLY from ServiceIntervalGroup.ServiceIntervals
        // — never inferred from ServiceInterval.ServiceIntervalGroupID — because that navigation is the
        // only thing the original fold consulted. Inferring membership from the foreign key would let a
        // partially-loaded graph produce periodic lines the export never emitted.
        foreach (var variant in variants)
        {
            foreach (var period in variant.PeriodicAvailabilities)
                AddInterval(period.ServiceInterval, intervals);

            foreach (var labour in variant.LabourDetails)
                AddGroup(labour.ServiceIntervalGroup, groups);

            foreach (var item in variant.Items)
            {
                var replacementItem = item.ReplacementItemVehicleModel?.ReplacementItem;
                if (replacementItem is null)
                    continue;

                foreach (var membership in replacementItem.ReplacementItemServiceIntervalGroups)
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
        CountryLabourRates = source.LabourRates.Select(x => new MenuGenerationCountryLabourRate
        {
            CountryID = x.CountryID,
            LabourRate = x.LabourRate,
            IsDeleted = x.IsDeleted,
        }).ToList(),
        Periods = source.PeriodicAvailabilities.Select(x => new MenuGenerationPeriod
        {
            ServiceIntervalID = x.ServiceIntervalID,
        }).ToList(),
        Labours = source.LabourDetails.Select(x => new MenuGenerationLabour
        {
            ServiceIntervalGroupID = x.ServiceIntervalGroupID,
            AllowedTime = x.AllowedTime,
            Consumable = x.Consumable,
        }).ToList(),
        Items = source.Items.Select(MapItem).ToList(),
    };

    private static MenuGenerationItem MapItem(MenuItem source)
    {
        var replacementVehicleModel = source.ReplacementItemVehicleModel;
        var replacementItem = replacementVehicleModel?.ReplacementItem;
        var standaloneGroup = replacementItem?.StandaloneReplacementItemGroup;

        return new MenuGenerationItem
        {
            MenuItemID = source.ID,
            IsDeleted = source.IsDeleted,
            HasReplacementItem = replacementVehicleModel is not null,
            ReplacementItemDeleted = replacementVehicleModel?.IsDeleted ?? false,
            StandaloneAllowedTime = source.StandaloneAllowedTime,
            ReplacementItemServiceIntervalGroupIDs = replacementItem?
                .ReplacementItemServiceIntervalGroups
                .Select(x => x.ServiceIntervalGroupID)
                .ToList() ?? [],
            StandaloneOperationCode = replacementItem?.StandaloneOperationCode,
            StandaloneLabourCode = replacementItem?.StandaloneLabourCode,
            FriendlyName = replacementItem?.FriendlyName,
            StandaloneGroup = standaloneGroup is null
                ? null
                : new MenuGenerationStandaloneGroup
                {
                    ID = standaloneGroup.ID,
                    MenuCode = standaloneGroup.MenuCode,
                    LabourCode = standaloneGroup.LabourCode,
                    Name = standaloneGroup.Name,
                },
            Parts = source.Parts.Select(x => new MenuGenerationPart
            {
                PartNumber = x.PartNumber,
                IsDeleted = x.IsDeleted,
                SortOrder = x.SortOrder,
                PeriodicQuantity = x.PeriodicQuantity,
                StandaloneQuantity = x.StandaloneQuantity,
                CountryPrices = (x.CountryPrices ?? []).Select(price => new MenuGenerationPartPrice
                {
                    CountryID = price.CountryID,
                    IsDeleted = price.IsDeleted,
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
            ServiceIntervalIDs = (source.ServiceIntervals ?? []).Select(x => x.ID).ToHashSet(),
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

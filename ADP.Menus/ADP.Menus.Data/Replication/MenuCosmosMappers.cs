using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Data.Replication;

/// <summary>
/// Manual EF entity → Cosmos document projections. No AutoMapper, so every field is explicit and
/// reviewable.
///
/// These are LAYER 2 only — persistence. They never generate menu codes; that is
/// <c>MenuCodeGenerator</c>'s job at read time, from documents these produce.
///
/// The file has two halves, and the split matters:
///
///  • <c>Map</c> — one per replicated table, producing that table's own document. A master entity's
///    <c>Map</c> serves double duty: it produces the document for the entity's own container AND the
///    copy embedded into the menu documents, so a master row has exactly one projection and the two
///    places it lands cannot drift.
///
///  • <c>ApplyTo</c> — the <c>UpdateReference</c> fan-outs. Each takes an EXISTING menu document,
///    replaces the slice this master entity owns, and returns it. Everything else on the document is
///    left alone, because the master row is not the authority on it.
///
/// Rules that hold throughout:
///  • <c>id</c> is the source row's database id, so a soft-deleted-and-recreated row cannot collide and
///    the replication stamp has a stable key.
///  • <c>IsDeleted</c> is carried, because soft deleting a row upserts its document rather than removing
///    it — only a hard delete removes one. Readers filter on it, embedded copies included.
///  • Collections are carried UNFILTERED, and soft-deleted rows are REPLICATED rather than skipped.
///    These are pure projections, exactly like the export's <c>EfToGenerationAggregator</c>: they copy,
///    they do not decide. Whether a soft-deleted row contributes to a menu is decided once, by
///    <c>MenuCodeGenerator</c>, so the DMS export and the vehicle lookup cannot disagree.
///    Not replicating a deleted row would be worse than useless: the document already in Cosmos would
///    simply go stale, since only a HARD delete removes one. The two places the source itself filters —
///    the labour-rate and brand mapping catalogues — are filtered by the caller that resolves them,
///    matching the export.
///
/// Every navigation these methods touch must be loaded by the matching reload in
/// <see cref="MenuReplicationReload"/>; a missing include silently produces a document with holes.
/// </summary>
public static class MenuCosmosMappers
{
    // ---- master entities ---------------------------------------------------------------------------

    public static ServiceIntervalCosmosModel Map(ServiceInterval interval) => new()
    {
        id = interval.ID.ToString(),
        ServiceIntervalID = interval.ID,
        Code = interval.Code,
        Description = interval.Description,
        ValueInMeter = interval.ValueInMeter,
        ServiceIntervalGroupID = interval.ServiceIntervalGroupID,
        IsDeleted = interval.IsDeleted,
    };

    public static ServiceIntervalGroupCosmosModel Map(ServiceIntervalGroup group) => new()
    {
        id = group.ID.ToString(),
        ServiceIntervalGroupID = group.ID,
        LabourCode = group.LabourCode,

        // Membership is what lets generation match a periodic interval to this group. Copied whole,
        // soft-deleted intervals included, because that is what the export's unfiltered include yields
        // — and an interval missing from here silently drops the periodic line rather than erroring.
        ServiceIntervalIDs = (group.ServiceIntervals ?? [])
            .Select(interval => interval.ID)
            .ToList(),

        IsDeleted = group.IsDeleted,
    };

    public static ReplacementItemCosmosModel Map(ReplacementItem replacementItem) => new()
    {
        id = replacementItem.ID.ToString(),
        ReplacementItemID = replacementItem.ID,
        FriendlyName = replacementItem.FriendlyName,
        StandaloneOperationCode = replacementItem.StandaloneOperationCode,
        StandaloneLabourCode = replacementItem.StandaloneLabourCode,
        StandaloneReplacementItemGroupID = replacementItem.StandaloneReplacementItemGroupID,
        ServiceIntervalGroupIDs = IntervalGroupLinks(replacementItem)
            .Select(link => link.ServiceIntervalGroupID)
            .ToList(),
        IsDeleted = replacementItem.IsDeleted,
    };

    public static StandaloneReplacementItemGroupCosmosModel Map(StandaloneReplacementItemGroup group) => new()
    {
        id = group.ID.ToString(),
        StandaloneReplacementItemGroupID = group.ID,
        Name = group.Name,
        MenuCode = group.MenuCode,
        LabourCode = group.LabourCode,
        IsDeleted = group.IsDeleted,
    };

    public static LabourRateMappingCosmosModel Map(LabourRateMapping mapping) => new()
    {
        id = mapping.ID.ToString(),
        BrandID = mapping.BrandID,
        LabourRate = mapping.LabourRate,
        Code = mapping.Code,
        IsDeleted = mapping.IsDeleted,
    };

    public static BrandMappingCosmosModel Map(BrandMapping mapping) => new()
    {
        id = mapping.ID.ToString(),
        BrandID = mapping.BrandID,
        Code = mapping.Code,
        BrandAbbreviation = mapping.BrandAbbreviation,
        IsDeleted = mapping.IsDeleted,
    };

    // ---- the ServiceMenus container ----------------------------------------------------------------

    /// <param name="labourRateMapping">
    /// The live mapping for the variant's (brand, primary labour rate) pair, or null when there is
    /// none. Null is meaningful: the generator throws on a missing pair (open item O1), so the reader
    /// must leave the dictionary entry out rather than invent a code.
    /// </param>
    /// <param name="brandMapping">The live mapping for the variant's brand, or null when unmapped.</param>
    public static MenuVariantCosmosModel Map(
        MenuVariant variant,
        LabourRateMapping? labourRateMapping,
        BrandMapping? brandMapping) => new()
    {
        id = variant.ID.ToString(),
        BasicModelCode = variant.Menu?.BasicModelCode,
        VariantID = variant.ID,
        BrandID = variant.Menu?.VehicleModel?.BrandID,
        Model = variant.Menu?.VehicleModel?.Name,
        VariantName = variant.Name,
        MenuPrefix = variant.MenuPrefix,
        MenuPostfix = variant.MenuPostfix,
        StandaloneMenuPrefix = variant.StandaloneMenuPrefix,
        StandaloneMenuPostfix = variant.StandaloneMenuPostfix,
        LabourRate = variant.LabourRate,
        DiscountPercentage = variant.DiscountPercentage,
        IsFree = variant.IsFree,
        HasStandaloneItems = variant.HasStandaloneItems,

        // The export selects on BOTH flags, and deleting a menu does not cascade to its variants — so
        // without this copy a deleted menu keeps serving menu codes from the lookup. See the field's
        // remarks on MenuVariantCosmosModel.
        MenuIsDeleted = variant.Menu?.IsDeleted ?? false,

        // Owned by the variant and never queried alone. Carried with their delete flags so the
        // generator, not this projection, decides which country rate applies.
        CountryLabourRates = variant.LabourRates
            .Select(rate => new MenuCountryLabourRateCosmosModel
            {
                CountryID = rate.CountryID,
                LabourRate = rate.LabourRate,
                IsDeleted = rate.IsDeleted,
            })
            .ToList(),

        LabourRateMapping = labourRateMapping is null ? null : Map(labourRateMapping),
        BrandMapping = brandMapping is null ? null : Map(brandMapping),

        IsDeleted = variant.IsDeleted,
    };

    public static MenuPeriodCosmosModel Map(MenuPeriodicAvailability period) => new()
    {
        id = period.ID.ToString(),
        BasicModelCode = period.MenuVariant?.Menu?.BasicModelCode,
        VariantID = period.MenuVariantID,
        ServiceIntervalID = period.ServiceIntervalID,
        ServiceInterval = period.ServiceInterval is null ? null : Map(period.ServiceInterval),
        IsDeleted = period.IsDeleted,
    };

    public static MenuLabourCosmosModel Map(MenuLabourDetails labour) => new()
    {
        id = labour.ID.ToString(),
        BasicModelCode = labour.MenuVariant?.Menu?.BasicModelCode,
        VariantID = labour.MenuVariantID,
        ServiceIntervalGroupID = labour.ServiceIntervalGroupID,
        AllowedTime = labour.AllowedTime,
        Consumable = labour.Consumable,
        ServiceIntervalGroup = labour.ServiceIntervalGroup is null ? null : Map(labour.ServiceIntervalGroup),
        IsDeleted = labour.IsDeleted,
    };

    public static MenuItemCosmosModel Map(MenuItem item)
    {
        var link = item.ReplacementItemVehicleModel;
        var replacementItem = link?.ReplacementItem;

        return new MenuItemCosmosModel
        {
            id = item.ID.ToString(),
            BasicModelCode = item.MenuVariant?.Menu?.BasicModelCode,
            MenuItemID = item.ID,
            VariantID = item.MenuVariantID,
            StandaloneAllowedTime = item.StandaloneAllowedTime,

            // The generator's own guards, resolved here so it never has to reason about EF nulls.
            // These are about the LINK row, not the replacement item's own delete flag.
            HasReplacementItem = link is not null,
            ReplacementItemDeleted = link?.IsDeleted ?? false,

            ReplacementItem = replacementItem is null ? null : Map(replacementItem),
            ServiceIntervalGroups = MapIntervalGroups(replacementItem),
            StandaloneGroup = replacementItem?.StandaloneReplacementItemGroup is null
                ? null
                : Map(replacementItem.StandaloneReplacementItemGroup),

            // Parts and their prices are embedded: they are owned by the item and never read alone.
            Parts = item.Parts.Select(part => new MenuItemPartCosmosModel
            {
                PartNumber = part.PartNumber,
                SortOrder = part.SortOrder,
                PeriodicQuantity = part.PeriodicQuantity,
                StandaloneQuantity = part.StandaloneQuantity,
                IsDeleted = part.IsDeleted,
                CountryPrices = (part.CountryPrices ?? [])
                    .Select(price => new MenuPartCountryPriceCosmosModel
                    {
                        CountryID = price.CountryID,
                        PartPrice = price.PartPrice,
                        PartFinalPrice = price.PartFinalPrice,
                        IsDeleted = price.IsDeleted,
                    })
                    .ToList(),
            }).ToList(),

            IsDeleted = item.IsDeleted,
        };
    }

    // ---- UpdateReference fan-outs ------------------------------------------------------------------
    // Each replaces only the slice its master entity owns. They run on ChangeType.Modified ONLY, which
    // is correct for an edit and is the reason a freshly INSERTED master row reaches existing documents
    // only when those documents are themselves re-saved (or pushed by a backfill).

    /// <summary>Refreshes the interval embedded on a periodic-availability document.</summary>
    public static MenuPeriodCosmosModel ApplyTo(ServiceInterval interval, MenuPeriodCosmosModel document)
    {
        document.ServiceInterval = Map(interval);
        return document;
    }

    /// <summary>Refreshes the interval group embedded on a labour-detail document.</summary>
    public static MenuLabourCosmosModel ApplyTo(ServiceIntervalGroup group, MenuLabourCosmosModel document)
    {
        document.ServiceIntervalGroup = Map(group);
        return document;
    }

    /// <summary>
    /// Refreshes this group's entry in a menu item's denormalized group list, leaving the item's other
    /// groups untouched. The entry is matched by id and replaced in place so the list keeps its order;
    /// a document that somehow lacks the entry gains it, which is the self-healing outcome.
    /// </summary>
    public static MenuItemCosmosModel ApplyTo(ServiceIntervalGroup group, MenuItemCosmosModel document)
    {
        document.ServiceIntervalGroups ??= [];

        var replacement = Map(group);
        var index = document.ServiceIntervalGroups
            .FindIndex(existing => existing.ServiceIntervalGroupID == group.ID);

        if (index >= 0)
            document.ServiceIntervalGroups[index] = replacement;
        else
            document.ServiceIntervalGroups.Add(replacement);

        return document;
    }

    /// <summary>
    /// Refreshes a menu item's whole replacement-item slice: the item itself, the standalone group it
    /// points at, and the interval groups it serves.
    ///
    /// All three move together on purpose — a replacement item can be re-pointed at a different
    /// standalone group or re-linked to different interval groups, and refreshing only its scalar
    /// fields would leave the document describing the old ones.
    /// </summary>
    public static MenuItemCosmosModel ApplyTo(ReplacementItem replacementItem, MenuItemCosmosModel document)
    {
        document.ReplacementItem = Map(replacementItem);
        document.ServiceIntervalGroups = MapIntervalGroups(replacementItem);
        document.StandaloneGroup = replacementItem.StandaloneReplacementItemGroup is null
            ? null
            : Map(replacementItem.StandaloneReplacementItemGroup);

        return document;
    }

    /// <summary>Refreshes the standalone group embedded on a menu-item document.</summary>
    public static MenuItemCosmosModel ApplyTo(StandaloneReplacementItemGroup group, MenuItemCosmosModel document)
    {
        document.StandaloneGroup = Map(group);
        return document;
    }

    /// <summary>Refreshes the labour-rate mapping embedded on a variant document.</summary>
    public static MenuVariantCosmosModel ApplyTo(LabourRateMapping mapping, MenuVariantCosmosModel document)
    {
        document.LabourRateMapping = Map(mapping);
        return document;
    }

    /// <summary>Refreshes the brand mapping embedded on a variant document.</summary>
    public static MenuVariantCosmosModel ApplyTo(BrandMapping mapping, MenuVariantCosmosModel document)
    {
        document.BrandMapping = Map(mapping);
        return document;
    }

    // ---- shared -------------------------------------------------------------------------------------

    /// <summary>
    /// The interval groups a replacement item serves, each with its labour code and FULL interval
    /// membership. The membership has to travel with the item: generation asks "does group G contain
    /// interval I" for every group the item serves, including groups the variant has no labour detail
    /// for, so it cannot be recovered from the sibling labour documents.
    /// </summary>
    private static List<ServiceIntervalGroupCosmosModel> MapIntervalGroups(ReplacementItem? replacementItem) =>
        IntervalGroupLinks(replacementItem)
            .Where(link => link.ServiceIntervalGroup is not null)
            .Select(link => Map(link.ServiceIntervalGroup))
            .ToList();

    /// <summary>
    /// The replacement item's LIVE interval-group links.
    ///
    /// <para><b>The one place this file filters a soft delete, and it has to.</b> Everywhere else the
    /// projection carries the flag and the reader decides — but a link row has no document and no flag
    /// anywhere in the document shape: it contributes only its group id to a flat
    /// <c>List&lt;long&gt;</c>. So a deleted link that is projected is indistinguishable from a live one,
    /// and the lookup would keep pricing parts onto periodic lines the export has stopped pricing.</para>
    ///
    /// <para>Filtering here rather than widening the shape is deliberate: the flat id list is what makes
    /// the interval-group fan-out an <c>ARRAY_CONTAINS</c> instead of a scan (§17), and a deleted link's
    /// group genuinely should no longer find this document.</para>
    ///
    /// <para>Mirrors <c>EfToGenerationAggregator.LiveIntervalGroupLinks</c>, minus the group's own flag —
    /// that one stays a read-time decision, because a group can be deleted long after the item was last
    /// replicated. <b>Note the usual caveat (§17): editing a link does not re-replicate its parent
    /// replacement item, so this takes effect on the next save of that item or on a catch-up sweep.</b></para>
    /// </summary>
    private static IEnumerable<ReplacementItemServiceIntervalGroup> IntervalGroupLinks(
        ReplacementItem? replacementItem) =>
        (replacementItem?.ReplacementItemServiceIntervalGroups ?? []).Where(link => !link.IsDeleted);
}

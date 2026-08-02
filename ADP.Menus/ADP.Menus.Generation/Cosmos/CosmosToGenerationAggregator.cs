using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Generation;

/// <summary>
/// The vehicle lookup's adapter: replicated Cosmos documents → the source-agnostic
/// <see cref="MenuGenerationRequest"/>. The exact counterpart of the DMS export's
/// <c>EfToGenerationAggregator</c>, feeding the one shared <see cref="MenuCodeGenerator"/>. See
/// COSMOS_REPLICATION_PLAN.md §1.1 and §8.
///
/// <para><b>This is where "the lookup's menu codes are identical to the export's" is actually won or
/// lost.</b> The generator is shared, so it cannot disagree; the two adapters can. Every filtering and
/// ordering decision below exists to mirror one the export makes, and each is called out.</para>
///
/// <para><b>SOFT-DELETED ROWS ARE FILTERED OUT HERE</b>, on every table, so the generator receives live
/// rows only. <c>EfToGenerationAggregator</c> does the identical filtering with the same method names —
/// <c>LivePeriods</c>, <c>LiveLabours</c>, <c>LiveItems</c>, <c>LiveIntervalGroups</c> — so the two can
/// be read against each other. Nothing in the type system makes them agree;
/// <c>ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted</c> does, by soft-deleting one row of
/// each table in turn and asserting both paths still produce identical output. <b>Add a table to the
/// replication and you must add it to both adapters and to that test.</b></para>
///
/// <para>Two things need care here that the EF side does not:</para>
/// <list type="bullet">
/// <item><b>A master row is embedded in several documents</b>, so its copies can disagree after a
/// partially landed fan-out. Deletion is decided ONCE, across every copy, before anything is filtered —
/// see <c>DeletedMasterIds</c>.</item>
/// <item><b>A deleted interval group must be stripped from the item's id list as well</b> as from the
/// reference dictionary. The generator resolves those ids with an indexer, so leaving one behind throws
/// instead of skipping — which is the correct signal for a MISSING group, and the wrong one for a
/// deleted one.</item>
/// </list>
///
/// <para><b>Order is behaviour</b> (open item O8). The periodic pass takes the FIRST matching labour
/// detail and a grouped standalone line takes its allowed time from the FIRST item in its group, so
/// input order decides which row wins. A Cosmos partition query has no defined order at all, so this
/// adapter imposes one: SOURCE ROW ID ascending, everywhere. That is both deterministic (two identical
/// reads cannot disagree) and the closest available match to the order EF returns included collections
/// in. Do not "optimise" the sorts away — losing them makes generated codes depend on document layout.</para>
///
/// <para><b>A missing document type degrades to missing lines, not to an error</b> — the same trap the
/// export's adapter has with a missing <c>Include</c>. A partition with no <c>MenuLabour</c> documents
/// produces no periodic lines, quietly. A partition whose <c>MenuPeriod</c> documents lack their
/// embedded interval, or whose items reference an interval group that is not embedded anywhere, fails
/// loudly instead: the generator indexes its reference dictionaries, so it throws
/// <see cref="KeyNotFoundException"/>. Both behaviours match the export.</para>
/// </summary>
public static class CosmosToGenerationAggregator
{
    /// <summary>
    /// Builds the generation request for one partition's documents.
    /// </summary>
    /// <param name="documents">
    /// One basic model code's documents, as read from the <c>ServiceMenus</c> container. Never
    /// pre-filter them — the filtering rules that keep the lookup in step with the export live here.
    /// </param>
    public static MenuGenerationRequest Build(ServiceMenuDocuments documents)
    {
        if (documents is null) throw new ArgumentNullException(nameof(documents));

        return Build(documents.Variants, documents.Periods, documents.Labours, documents.Items);
    }

    /// <summary>
    /// Builds the generation request from the four document collections directly, for callers that
    /// hold them separately.
    /// </summary>
    public static MenuGenerationRequest Build(
        IEnumerable<MenuVariantCosmosModel> variants,
        IEnumerable<MenuPeriodCosmosModel> periods,
        IEnumerable<MenuLabourCosmosModel> labours,
        IEnumerable<MenuItemCosmosModel> items)
    {
        var allPeriods = Safe(periods).ToList();
        var allLabours = Safe(labours).ToList();
        var allItems = Safe(items).ToList();

        // Which master rows count as deleted, decided ONCE across every embedded copy — see
        // DeletedMasterIds. Everything below filters against these rather than against whichever copy it
        // happens to be holding.
        var deletedIntervals = DeletedMasterIds(
            allPeriods.Select(period => period.ServiceInterval),
            interval => interval.ServiceIntervalID);

        var deletedGroups = DeletedMasterIds(
            allLabours.Select(labour => labour.ServiceIntervalGroup)
                .Concat(allItems.SelectMany(item => item.ServiceIntervalGroups ?? [])),
            group => group.ServiceIntervalGroupID);

        // A deleted variant, or one whose parent menu is deleted, contributes nothing — the two filters
        // the export's own query applies.
        var liveVariants = Safe(variants)
            .Where(variant => !variant.IsDeleted && !variant.MenuIsDeleted)
            .OrderBy(variant => variant.VariantID)
            .ToList();

        var periodsByVariant = GroupByVariant(
            allPeriods.Where(period => !period.IsDeleted && !IsDeletedInterval(period.ServiceInterval, deletedIntervals)),
            period => period.VariantID, period => period.id);

        var laboursByVariant = GroupByVariant(
            allLabours.Where(labour => !labour.IsDeleted && !IsDeletedGroup(labour.ServiceIntervalGroup, deletedGroups)),
            labour => labour.VariantID, labour => labour.id);

        var itemsByVariant = GroupByVariant(
            allItems.Where(IsLiveItem),
            item => item.VariantID, item => item.id);

        var intervals = new Dictionary<long, MenuGenerationServiceInterval>();
        var groups = new Dictionary<long, MenuGenerationServiceIntervalGroup>();
        var labourRateCodes = new Dictionary<MenuGenerationLabourRateKey, string>();
        var brandMappings = new Dictionary<long?, MenuGenerationBrandMapping>();

        var generationVariants = new List<MenuGenerationVariant>(liveVariants.Count);

        foreach (var variant in liveVariants)
        {
            var variantPeriods = Children(periodsByVariant, variant.VariantID);
            var variantLabours = Children(laboursByVariant, variant.VariantID);
            var variantItems = Children(itemsByVariant, variant.VariantID);

            // Reference data is collected from exactly the documents the export's adapter collects it
            // from, in the same order — periods, then labour details, then items — because the
            // dictionaries are last-write-wins and the export's are built that way too.
            foreach (var period in variantPeriods)
                AddInterval(period.ServiceInterval, intervals);

            foreach (var labour in variantLabours)
                AddGroup(labour.ServiceIntervalGroup, groups, deletedIntervals);

            foreach (var item in variantItems)
                foreach (var group in LiveIntervalGroups(item, deletedGroups))
                    AddGroup(group, groups, deletedIntervals);

            AddLabourRateMapping(variant.LabourRateMapping, labourRateCodes);
            AddBrandMapping(variant.BrandMapping, brandMappings);

            generationVariants.Add(MapVariant(variant, variantPeriods, variantLabours, variantItems, deletedGroups));
        }

        return new MenuGenerationRequest
        {
            Variants = generationVariants,
            Reference = new MenuGenerationReferenceData
            {
                Intervals = intervals,
                Groups = groups,
                LabourRateCodes = labourRateCodes,
                BrandMappings = brandMappings,
            },
        };
    }

    // ---- variant + children ------------------------------------------------------------------------

    private static MenuGenerationVariant MapVariant(
        MenuVariantCosmosModel variant,
        List<MenuPeriodCosmosModel> periods,
        List<MenuLabourCosmosModel> labours,
        List<MenuItemCosmosModel> items,
        HashSet<long> deletedGroups) => new MenuGenerationVariant
        {
            VariantID = variant.VariantID,
            BasicModelCode = variant.BasicModelCode,
            BrandID = variant.BrandID,
            Model = variant.Model,
            VariantName = variant.VariantName,
            MenuPrefix = variant.MenuPrefix,
            MenuPostfix = variant.MenuPostfix,
            StandaloneMenuPrefix = variant.StandaloneMenuPrefix,
            StandaloneMenuPostfix = variant.StandaloneMenuPostfix,
            LabourRate = variant.LabourRate,
            DiscountPercentage = variant.DiscountPercentage,
            HasStandaloneItems = variant.HasStandaloneItems,

            CountryLabourRates = (variant.CountryLabourRates ?? [])
                .Where(rate => !rate.IsDeleted)
                .Select(rate => new MenuGenerationCountryLabourRate
                {
                    CountryID = rate.CountryID,
                    LabourRate = rate.LabourRate,
                })
                .ToList(),

            // Already filtered by the caller — a deleted availability, or one whose interval is deleted,
            // never reaches here.
            Periods = periods
                .Select(period => new MenuGenerationPeriod
                {
                    ServiceIntervalID = period.ServiceIntervalID,
                })
                .ToList(),

            Labours = labours
                .Select(labour => new MenuGenerationLabour
                {
                    ServiceIntervalGroupID = labour.ServiceIntervalGroupID,
                    AllowedTime = labour.AllowedTime,

                    // Unscaled — the transfer rate is the generator's business.
                    Consumable = labour.Consumable,
                })
                .ToList(),

            Items = items.Select(item => MapItem(item, deletedGroups)).ToList(),
        };

    private static MenuGenerationItem MapItem(MenuItemCosmosModel item, HashSet<long> deletedGroups)
    {
        var replacementItem = item.ReplacementItem;
        var standaloneGroup = item.StandaloneGroup;

        return new MenuGenerationItem
        {
            MenuItemID = item.MenuItemID,
            StandaloneAllowedTime = item.StandaloneAllowedTime,

            // Deleted groups are stripped, and MUST be: the generator resolves each id against
            // Reference.Groups with an INDEXER, so an id left here without a matching entry throws
            // instead of being skipped. An id that is neither deleted nor embedded stays, and that throw
            // is the intended signal — a reference-data fault is not a delete.
            ReplacementItemServiceIntervalGroupIDs = (replacementItem?.ServiceIntervalGroupIDs ?? [])
                .Where(groupId => !deletedGroups.Contains(groupId))
                .ToList(),

            StandaloneOperationCode = replacementItem?.StandaloneOperationCode,
            StandaloneLabourCode = replacementItem?.StandaloneLabourCode,
            FriendlyName = replacementItem?.FriendlyName,

            // A deleted group counts as NO group, so the item falls back to an ungrouped standalone line
            // rather than disappearing: deleting a grouping withdraws the grouping, not the items.
            StandaloneGroup = standaloneGroup is null || standaloneGroup.IsDeleted
                ? null
                : new MenuGenerationStandaloneGroup
                {
                    ID = standaloneGroup.StandaloneReplacementItemGroupID,
                    MenuCode = standaloneGroup.MenuCode,
                    LabourCode = standaloneGroup.LabourCode,
                    Name = standaloneGroup.Name,
                },

            Parts = (item.Parts ?? [])
                .Where(part => !part.IsDeleted)
                .Select(part => new MenuGenerationPart
                {
                    PartNumber = part.PartNumber,
                    SortOrder = part.SortOrder,
                    PeriodicQuantity = part.PeriodicQuantity,
                    StandaloneQuantity = part.StandaloneQuantity,
                    CountryPrices = (part.CountryPrices ?? [])
                        .Where(price => !price.IsDeleted)
                        .Select(price => new MenuGenerationPartPrice
                        {
                            CountryID = price.CountryID,
                            PartPrice = price.PartPrice,
                            PartFinalPrice = price.PartFinalPrice,
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }

    // ---- the delete filters ------------------------------------------------------------------------
    // The Cosmos twins of EfToGenerationAggregator's LivePeriods / LiveLabours / LiveItems /
    // LiveIntervalGroupLinks. Read them side by side; they must agree.

    /// <summary>
    /// An item participates only when the item row, its replacement-item LINK and the replacement item
    /// ITSELF are all live. An item with no link at all is excluded too — the source's null guard.
    /// </summary>
    private static bool IsLiveItem(MenuItemCosmosModel item) =>
        !item.IsDeleted
        && item.HasReplacementItem
        && !item.ReplacementItemDeleted
        && !(item.ReplacementItem?.IsDeleted ?? false);

    private static IEnumerable<ServiceIntervalGroupCosmosModel> LiveIntervalGroups(
        MenuItemCosmosModel item, HashSet<long> deletedGroups) =>
        (item.ServiceIntervalGroups ?? [])
            .Where(group => !deletedGroups.Contains(group.ServiceIntervalGroupID));

    /// <summary>
    /// Null means the document was written WITHOUT its embedded master — a replication fault, which must
    /// still reach the generator so the missing dictionary entry throws. Only a present-and-deleted copy
    /// filters the row out.
    /// </summary>
    private static bool IsDeletedInterval(ServiceIntervalCosmosModel interval, HashSet<long> deleted) =>
        interval is not null && deleted.Contains(interval.ServiceIntervalID);

    private static bool IsDeletedGroup(ServiceIntervalGroupCosmosModel group, HashSet<long> deleted) =>
        group is not null && deleted.Contains(group.ServiceIntervalGroupID);

    /// <summary>
    /// The ids of every master row that ANY embedded copy reports as deleted.
    ///
    /// <para>A master row is embedded in several documents — an interval group rides on every
    /// <c>MenuLabour</c> and every <c>MenuItem</c> that serves it — so a fan-out that lands only
    /// partially (§18 recorded a sweep silently writing 1060 of 1188 documents) leaves the copies
    /// disagreeing. Deciding per copy would then make the answer depend on which document a given code
    /// path happened to be holding.</para>
    ///
    /// <para>Deletion is therefore decided ONCE, and stickily: any copy saying deleted wins. A delete is
    /// monotonic — nothing un-deletes — so this cannot be wrong in the way "whichever copy we read
    /// first" can, and it errs toward withholding a menu code rather than issuing one for a withdrawn
    /// row. The EF adapter needs none of this: its graph has one instance per row.</para>
    /// </summary>
    private static HashSet<long> DeletedMasterIds<T>(IEnumerable<T> copies, Func<T, long> id) where T : class
    {
        var deleted = new HashSet<long>();

        foreach (var copy in copies)
            if (copy is not null && IsDeletedCopy(copy))
                deleted.Add(id(copy));

        return deleted;
    }

    private static bool IsDeletedCopy(object copy) => copy switch
    {
        ServiceIntervalCosmosModel interval => interval.IsDeleted,
        ServiceIntervalGroupCosmosModel group => group.IsDeleted,
        _ => false,
    };

    // ---- reference data ----------------------------------------------------------------------------

    /// <summary>
    /// A null embedded interval leaves the dictionary entry out, so the generator throws when the
    /// periodic line needs it. That is the intended outcome: a document written without its interval
    /// is a replication fault, and inventing a blank code would issue a wrong menu code instead. A
    /// DELETED interval is a different case — it is added, flagged, and skipped by the generator.
    /// </summary>
    private static void AddInterval(
        ServiceIntervalCosmosModel interval,
        IDictionary<long, MenuGenerationServiceInterval> intervals)
    {
        if (interval is null)
            return;

        intervals[interval.ServiceIntervalID] = new MenuGenerationServiceInterval
        {
            Code = interval.Code,

            // Verbatim, NOT language-resolved — matching the source (open item O10).
            Description = interval.Description,

            ValueInMeter = interval.ValueInMeter,

            // Carried for consumers; group membership never comes from it.
            GroupID = interval.ServiceIntervalGroupID,
        };
    }

    private static void AddGroup(
        ServiceIntervalGroupCosmosModel group,
        IDictionary<long, MenuGenerationServiceIntervalGroup> groups,
        HashSet<long> deletedIntervals)
    {
        if (group is null)
            return;

        groups[group.ServiceIntervalGroupID] = new MenuGenerationServiceIntervalGroup
        {
            LabourCode = group.LabourCode,

            // Deleted intervals are not members. A period naming one is already gone, but the membership
            // is also what a labour detail matches on, so leaving it here would resurrect the line.
            // ToHashSet() is .NET Standard 2.1+; this project is 2.0.
            ServiceIntervalIDs = new HashSet<long>(
                (group.ServiceIntervalIDs ?? []).Where(id => !deletedIntervals.Contains(id))),
        };
    }

    /// <summary>
    /// Keyed by the MAPPING's own (brand, rate) pair rather than the variant's, mirroring how the
    /// export builds its dictionary from the catalogue. Soft-deleted mappings are dropped: the export
    /// filters them out of the catalogue, so a variant whose mapping has since been deleted must fail
    /// the same way it would there (open item O1 — the generator throws) rather than resolve to a code
    /// that is no longer in service.
    /// </summary>
    private static void AddLabourRateMapping(
        LabourRateMappingCosmosModel mapping,
        IDictionary<MenuGenerationLabourRateKey, string> labourRateCodes)
    {
        if (mapping is null || mapping.IsDeleted)
            return;

        labourRateCodes[new MenuGenerationLabourRateKey(mapping.BrandID, mapping.LabourRate)] = mapping.Code;
    }

    /// <summary>
    /// Same live-only rule as the labour-rate catalogue. An absent entry is valid here, though: the
    /// generator falls back to the "Z" abbreviation and a null company code.
    /// </summary>
    private static void AddBrandMapping(
        BrandMappingCosmosModel mapping,
        IDictionary<long?, MenuGenerationBrandMapping> brandMappings)
    {
        if (mapping is null || mapping.IsDeleted)
            return;

        brandMappings[mapping.BrandID] = new MenuGenerationBrandMapping
        {
            Code = mapping.Code,
            Abbreviation = mapping.BrandAbbreviation,
        };
    }

    // ---- grouping + ordering -----------------------------------------------------------------------

    private static IEnumerable<T> Safe<T>(IEnumerable<T> source) where T : class =>
        (source ?? []).Where(x => x is not null);

    private static Dictionary<long, List<T>> GroupByVariant<T>(
        IEnumerable<T> documents,
        Func<T, long> variantId,
        Func<T, string> documentId) =>
        documents
            .GroupBy(variantId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(document => DocumentOrder(documentId(document)))
                    .ThenBy(documentId, StringComparer.Ordinal)
                    .ToList());

    private static List<T> Children<T>(Dictionary<long, List<T>> byVariant, long variantId) =>
        byVariant.TryGetValue(variantId, out var children) ? children : [];

    /// <summary>
    /// A document's source row id as a number, for ordering. The <c>id</c> IS the database row id, so
    /// this recovers the source's own ordering; parsing it beats sorting the string, under which "10"
    /// sorts before "9". Anything unparseable sorts last and is then broken ordinally, so the result
    /// stays total — an odd id is not a reason to refuse to serve a menu.
    /// </summary>
    private static long DocumentOrder(string id) =>
        long.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : long.MaxValue;
}

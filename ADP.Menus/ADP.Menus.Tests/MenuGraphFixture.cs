// CS8714: Dictionary<long?, BrandMapping> mirrors the production GenerateMenuLines signature, which
// uses a nullable BrandID as its key. The nullability of the key type is not ours to change here.
#pragma warning disable CS8714

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared;

using MenuEntity = ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Builds the synthetic menu graph the golden contract tests run against.
///
/// All values are synthetic placeholders — no real model codes, brands, part numbers or operation
/// codes. Every navigation collection is materialised as a <see cref="List{T}"/> (never the entities'
/// default <c>HashSet</c>) so the fixture itself contributes no ordering nondeterminism; any ordering
/// the golden pins is the generator's, not the fixture's.
///
/// Coverage baked into the graph:
///  • periodic lines (two intervals that resolve labour details, one that does not → skipped)
///  • standalone ungrouped line, and a standalone grouped line folded from two items
///  • multi-language (JSON blob) menu prefix / standalone prefix / operation code / group menu code
///  • a JSON-blob service-interval Description (which today is NOT language-resolved — see the golden)
///  • soft-deleted item, soft-deleted replacement-item link, soft-deleted part, soft-deleted country
///    price and soft-deleted country labour rate — all of which must be excluded
///  • a part with zero periodic quantity (excluded from periodic, present in standalone)
///  • two country prices per part plus a country with no price row at all (0-country case)
/// </summary>
internal static class MenuGraphFixture
{
    internal const long BrandId = 101;
    internal const long UnmappedBrandId = 999;
    internal const string BasicModelCode = "ABC12";

    /// <summary>The variant's primary labour rate — also the labour-rate-mapping lookup key.</summary>
    internal const decimal PrimaryLabourRate = 12.50m;

    internal sealed class Fixture
    {
        public required List<MenuVariant> Variants { get; init; }
        public required Dictionary<CompositeKey<long?, decimal>, LabourRateMapping> LabourRateMappings { get; init; }
        public required Dictionary<long?, BrandMapping> BrandMappings { get; init; }
    }

    /// <param name="brandId">Use <see cref="UnmappedBrandId"/> to exercise the "Z" brand-abbreviation fallback.</param>
    /// <param name="includeBrandMapping">False drops the brand-mapping row, exercising the same fallback.</param>
    /// <param name="includeLabourRateMapping">False drops the labour-rate-mapping row (today: throws).</param>
    internal static Fixture Build(
        long brandId = BrandId,
        bool includeBrandMapping = true,
        bool includeLabourRateMapping = true)
    {
        // ---- service interval groups + intervals -------------------------------------------------
        var group10 = new ServiceIntervalGroup(10) { Name = "Group 10", LabourCode = "GRPA", LabourDescription = "Group 10 labour" };
        var group20 = new ServiceIntervalGroup(20) { Name = "Group 20", LabourCode = "GRPB", LabourDescription = "Group 20 labour" };

        var interval501 = new ServiceInterval(501)
        {
            Code = "S01",
            FullName = "Interval 501",
            ValueInMeter = 10000,
            Description = "Plain description",
            ServiceIntervalGroupID = group10.ID,
            ServiceIntervalGroup = group10,
        };

        // NOTE: a JSON multi-language blob in Description. The periodic pass assigns
        // ServiceInterval.Description straight onto the line WITHOUT language resolution, so the raw
        // blob is expected to surface verbatim in the golden. Pinned deliberately.
        var interval502 = new ServiceInterval(502)
        {
            Code = "S02",
            FullName = "Interval 502",
            ValueInMeter = 20000,
            Description = """{"en":"Description EN","ar":"Description AR"}""",
            ServiceIntervalGroupID = group10.ID,
            ServiceIntervalGroup = group10,
        };

        // Belongs to group20, for which the variant has NO MenuLabourDetails → its periodic line is skipped.
        var interval503 = new ServiceInterval(503)
        {
            Code = "S03",
            FullName = "Interval 503",
            ValueInMeter = 30000,
            Description = "Third description",
            ServiceIntervalGroupID = group20.ID,
            ServiceIntervalGroup = group20,
        };

        group10.ServiceIntervals = new List<ServiceInterval> { interval501, interval502 };
        group20.ServiceIntervals = new List<ServiceInterval> { interval503 };

        // ---- vehicle model + menu ----------------------------------------------------------------
        var vehicleModel = new VehicleModel { Name = "Model One", BrandID = brandId, LabourRate = PrimaryLabourRate };
        vehicleModel.ID = 300;

        var menu = new MenuEntity { BasicModelCode = BasicModelCode, BrandID = brandId, VehicleModelID = vehicleModel.ID, VehicleModel = vehicleModel };
        menu.ID = 200;

        // ---- standalone replacement-item group (shared by items B and C) --------------------------
        var standaloneGroup = new StandaloneReplacementItemGroup
        {
            Name = "Standalone Group One",
            MenuCode = """{"en":"GMC","ar":"GMCA"}""",
            LabourCode = "GLC",
        };
        standaloneGroup.ID = 800;

        // ---- replacement items -------------------------------------------------------------------
        var itemAReplacement = new ReplacementItem(600)
        {
            Name = "Replacement A",
            FriendlyName = "Item A Friendly",
            StandaloneOperationCode = """{"en":"OPA","ar":"OPAA"}""",
            StandaloneLabourCode = "SLA",
            StandaloneReplacementItemGroup = null,
        };
        itemAReplacement.ReplacementItemServiceIntervalGroups = new List<ReplacementItemServiceIntervalGroup>
        {
            new(6001) { ReplacementItemID = itemAReplacement.ID, ReplacementItem = itemAReplacement, ServiceIntervalGroupID = group10.ID, ServiceIntervalGroup = group10 },
        };

        var itemBReplacement = new ReplacementItem(601)
        {
            Name = "Replacement B",
            FriendlyName = "Item B Friendly",
            StandaloneOperationCode = "OPB",
            StandaloneLabourCode = "SLB",
            StandaloneReplacementItemGroupID = standaloneGroup.ID,
            StandaloneReplacementItemGroup = standaloneGroup,
        };
        itemBReplacement.ReplacementItemServiceIntervalGroups = new List<ReplacementItemServiceIntervalGroup>
        {
            new(6011) { ReplacementItemID = itemBReplacement.ID, ReplacementItem = itemBReplacement, ServiceIntervalGroupID = group10.ID, ServiceIntervalGroup = group10 },
        };

        // Item C shares the standalone group with B, but its interval group is group20 — so it takes
        // part in the grouped standalone line while being absent from the group10 periodic lines.
        var itemCReplacement = new ReplacementItem(602)
        {
            Name = "Replacement C",
            FriendlyName = "Item C Friendly",
            StandaloneOperationCode = "OPC",
            StandaloneLabourCode = "SLC",
            StandaloneReplacementItemGroupID = standaloneGroup.ID,
            StandaloneReplacementItemGroup = standaloneGroup,
        };
        itemCReplacement.ReplacementItemServiceIntervalGroups = new List<ReplacementItemServiceIntervalGroup>
        {
            new(6021) { ReplacementItemID = itemCReplacement.ID, ReplacementItem = itemCReplacement, ServiceIntervalGroupID = group20.ID, ServiceIntervalGroup = group20 },
        };

        var deletedReplacement = new ReplacementItem(603)
        {
            Name = "Replacement D",
            FriendlyName = "Item D Friendly",
            StandaloneOperationCode = "OPD",
            StandaloneLabourCode = "SLD",
        };
        deletedReplacement.ReplacementItemServiceIntervalGroups = new List<ReplacementItemServiceIntervalGroup>
        {
            new(6031) { ReplacementItemID = deletedReplacement.ID, ReplacementItem = deletedReplacement, ServiceIntervalGroupID = group10.ID, ServiceIntervalGroup = group10 },
        };

        // ---- replacement-item ↔ vehicle-model links ----------------------------------------------
        var replacementLinkA = CreateReplacementItemVehicleModel(700, itemAReplacement, vehicleModel, 0.25m);
        var replacementLinkB = CreateReplacementItemVehicleModel(701, itemBReplacement, vehicleModel, 0.75m);
        var replacementLinkC = CreateReplacementItemVehicleModel(702, itemCReplacement, vehicleModel, 0.90m);
        var replacementLinkDeleted = CreateReplacementItemVehicleModel(704, deletedReplacement, vehicleModel, 0.15m);
        replacementLinkDeleted.IsDeleted = true;   // soft-deleted link → its menu item must be excluded

        // ---- menu items + parts ------------------------------------------------------------------
        var itemA = MenuItem(900, replacementLinkA, standaloneAllowedTime: 0.25m, parts:
        [
            Part(9001, "PN-0001", periodicQuantity: 2m, standaloneQuantity: 1m, sortOrder: 0, prices:
            [
                Price(90011, countryId: 2, partPrice: 5.500m, finalPrice: 7.250m),
                Price(90012, countryId: 3, partPrice: 6.000m, finalPrice: 8.000m),
                Price(90013, countryId: 2, partPrice: 999m, finalPrice: 999m, isDeleted: true),   // excluded
            ]),
            // Zero periodic quantity → excluded from the periodic line, present in the standalone one.
            Part(9002, "PN-0002", periodicQuantity: 0m, standaloneQuantity: 3m, sortOrder: 1, prices:
            [
                Price(90021, countryId: 2, partPrice: 2.000m, finalPrice: 3.000m),
            ]),
            // Soft-deleted part → excluded everywhere.
            Part(9003, "PN-0003", periodicQuantity: 5m, standaloneQuantity: 5m, isDeleted: true, sortOrder: 2, prices:
            [
                Price(90031, countryId: 2, partPrice: 50m, finalPrice: 60m),
            ]),
            // Null periodic quantity → excluded from the periodic line.
            Part(9004, "PN-0004", periodicQuantity: null, standaloneQuantity: 2m, sortOrder: 3, prices:
            [
                Price(90041, countryId: 2, partPrice: 1.250m, finalPrice: 1.750m),
            ]),
        ]);

        var itemB = MenuItem(901, replacementLinkB, standaloneAllowedTime: 0.75m, parts:
        [
            Part(9011, "PN-0011", periodicQuantity: 1m, standaloneQuantity: 2m, prices:
            [
                Price(90111, countryId: 2, partPrice: 10.000m, finalPrice: 12.500m),
            ]),
        ]);

        var itemC = MenuItem(902, replacementLinkC, standaloneAllowedTime: 0.90m, parts:
        [
            Part(9021, "PN-0021", periodicQuantity: 4m, standaloneQuantity: 1m, prices:
            [
                Price(90211, countryId: 2, partPrice: 1.000m, finalPrice: 2.000m),
            ]),
        ]);

        var itemDeletedRow = MenuItem(903, replacementLinkA, standaloneAllowedTime: 0.10m, parts:
        [
            Part(9031, "PN-0031", periodicQuantity: 9m, standaloneQuantity: 9m, prices:
            [
                Price(90311, countryId: 2, partPrice: 90m, finalPrice: 90m),
            ]),
        ]);
        itemDeletedRow.IsDeleted = true;    // soft-deleted menu item → excluded

        var itemDeletedLink = MenuItem(904, replacementLinkDeleted, standaloneAllowedTime: 0.10m, parts:
        [
            Part(9041, "PN-0041", periodicQuantity: 9m, standaloneQuantity: 9m, prices:
            [
                Price(90411, countryId: 2, partPrice: 91m, finalPrice: 91m),
            ]),
        ]);

        // A menu item with no replacement-item link at all → excluded by the null guard.
        var itemNoLink = MenuItem(905, null, standaloneAllowedTime: 0.10m, parts:
        [
            Part(9051, "PN-0051", periodicQuantity: 9m, standaloneQuantity: 9m, prices:
            [
                Price(90511, countryId: 2, partPrice: 92m, finalPrice: 92m),
            ]),
        ]);

        // ---- the variant -------------------------------------------------------------------------
        var variant = new MenuVariant
        {
            MenuID = menu.ID,
            Menu = menu,
            Name = "Variant A",
            MenuPrefix = """{"en":"MEN","ar":"MENA"}""",
            MenuPostfix = "PX",
            StandaloneMenuPrefix = """{"en":"STD","ar":"STDA"}""",
            StandaloneMenuPostfix = "SPX",
            LabourRate = PrimaryLabourRate,
            DiscountPercentage = 10m,
            HasStandaloneItems = true,
        };
        variant.ID = 4471;

        variant.LabourRates = new List<MenuVariantLabourRate>
        {
            LabourRate(44711, variant, countryId: 2, rate: 20.00m),
            LabourRate(44712, variant, countryId: 3, rate: 30.00m),
            LabourRate(44713, variant, countryId: 2, rate: 999.00m, isDeleted: true),   // excluded
        };

        // Only group10 has labour details, so interval503 (group20) yields no periodic line.
        variant.LabourDetails = new List<MenuLabourDetails>
        {
            new()
            {
                MenuVariantID = variant.ID,
                MenuVariant = variant,
                ServiceIntervalGroupID = group10.ID,
                ServiceIntervalGroup = group10,
                AllowedTime = 0.50m,
                Consumable = 4.00m,
            },
        };
        variant.LabourDetails.First().ID = 44721;

        variant.PeriodicAvailabilities = new List<MenuPeriodicAvailability>
        {
            Availability(44731, variant, interval501),
            Availability(44732, variant, interval502),
            Availability(44733, variant, interval503),
        };

        variant.Items = new List<MenuItem> { itemA, itemB, itemC, itemDeletedRow, itemDeletedLink, itemNoLink };
        foreach (var item in variant.Items)
        {
            item.MenuVariantID = variant.ID;
            item.MenuVariant = variant;
        }

        menu.Variants = new List<MenuVariant> { variant };

        // ---- mapping dictionaries ----------------------------------------------------------------
        var labourRateMappings = new Dictionary<CompositeKey<long?, decimal>, LabourRateMapping>();
        if (includeLabourRateMapping)
        {
            labourRateMappings[new CompositeKey<long?, decimal>(brandId, PrimaryLabourRate)] =
                new LabourRateMapping(1) { BrandID = brandId, LabourRate = PrimaryLabourRate, Code = "LR1" };
        }

        var brandMappings = new Dictionary<long?, BrandMapping>();
        if (includeBrandMapping)
        {
            brandMappings[brandId] = new BrandMapping(1) { BrandID = brandId, Code = "BC1", BrandAbbreviation = "A" };
        }

        return new Fixture
        {
            Variants = [variant],
            LabourRateMappings = labourRateMappings,
            BrandMappings = brandMappings,
        };
    }

    // ---- small construction helpers --------------------------------------------------------------

    private static ReplacementItemVehicleModel CreateReplacementItemVehicleModel(long id, ReplacementItem replacementItem, VehicleModel model, decimal standaloneAllowedTime)
    {
        var replacementLink = new ReplacementItemVehicleModel
        {
            ReplacementItemID = replacementItem.ID,
            ReplacementItem = replacementItem,
            VehicleModelID = model.ID,
            VehicleModel = model,
            StandaloneAllowedTime = standaloneAllowedTime,
        };
        replacementLink.ID = id;
        return replacementLink;
    }

    private static MenuItem MenuItem(long id, ReplacementItemVehicleModel? replacementLink, decimal standaloneAllowedTime, List<MenuItemPart> parts)
    {
        var item = new MenuItem
        {
            ReplacementItemVehicleModelID = replacementLink?.ID,
            ReplacementItemVehicleModel = replacementLink,
            StandaloneAllowedTime = standaloneAllowedTime,
            Parts = parts,
        };
        item.ID = id;
        foreach (var part in parts)
        {
            part.MenuItemID = id;
            part.MenuItem = item;
        }
        return item;
    }

    private static MenuItemPart Part(
        long id,
        string partNumber,
        decimal? periodicQuantity,
        decimal? standaloneQuantity,
        List<MenuItemPartCountryPrice> prices,
        bool isDeleted = false,
        int sortOrder = 0)
    {
        var part = new MenuItemPart
        {
            PartNumber = partNumber,
            SortOrder = sortOrder,
            PeriodicQuantity = periodicQuantity,
            StandaloneQuantity = standaloneQuantity,
            CountryPrices = prices,
        };
        part.ID = id;
        part.IsDeleted = isDeleted;
        foreach (var price in prices)
        {
            price.MenuItemPartID = id;
            price.MenuItemPart = part;
        }
        return part;
    }

    private static MenuItemPartCountryPrice Price(long id, long countryId, decimal partPrice, decimal finalPrice, bool isDeleted = false)
    {
        var price = new MenuItemPartCountryPrice
        {
            CountryID = countryId,
            PartPrice = partPrice,
            PartFinalPrice = finalPrice,
        };
        price.ID = id;
        price.IsDeleted = isDeleted;
        return price;
    }

    private static MenuVariantLabourRate LabourRate(long id, MenuVariant variant, long countryId, decimal rate, bool isDeleted = false)
    {
        var labourRate = new MenuVariantLabourRate
        {
            MenuVariantID = variant.ID,
            MenuVariant = variant,
            CountryID = countryId,
            LabourRate = rate,
        };
        labourRate.ID = id;
        labourRate.IsDeleted = isDeleted;
        return labourRate;
    }

    private static MenuPeriodicAvailability Availability(long id, MenuVariant variant, ServiceInterval interval)
    {
        var availability = new MenuPeriodicAvailability
        {
            MenuVariantID = variant.ID,
            MenuVariant = variant,
            ServiceIntervalID = interval.ID,
            ServiceInterval = interval,
        };
        availability.ID = id;
        return availability;
    }
}

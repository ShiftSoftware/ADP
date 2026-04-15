using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using System.Text.Json;

using MenuEntity = global::ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Sample.API.Data;

public static class SeedData
{
    // Multi-language seed helper: builds a JSON object keyed by 2-letter ISO codes
    // for the languages configured in the Example consumer (en, ru, uz, tg, tk).
    // For demo purposes each non-English value is the English code with a culture suffix.
    private static string Loc(string en) => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["en"] = en,
        ["ru"] = $"{en}-RU",
        ["uz"] = $"{en}-UZ",
        ["tg"] = $"{en}-TG",
        ["tk"] = $"{en}-TM",
    });

    // USD-to-local FX snapshot for seeded demo data (source timestamp: 2026-04-02 UTC).
    private const decimal UsdToUzsRate = 12191.552819m;
    private const decimal UsdToTmtRate = 3.501513m;
    private const decimal UsdToTjsRate = 9.557886m;

    private static IReadOnlyDictionary<long, decimal> BuildCountryRatesFromPrimary(decimal primaryRate)
    {
        return new Dictionary<long, decimal>
        {
            [LabourRateCountries.Uzbekistan] = Math.Round(primaryRate * UsdToUzsRate, 2),
            [LabourRateCountries.Turkmenistan] = Math.Round(primaryRate * UsdToTmtRate, 2),
            [LabourRateCountries.Tajikistan] = Math.Round(primaryRate * UsdToTjsRate, 2)
        };
    }

    private static IReadOnlyDictionary<long, decimal> BuildCountryPartPricesFromPrimary(decimal primaryPrice)
    {
        return new Dictionary<long, decimal>
        {
            [LabourRateCountries.Uzbekistan] = Math.Round(primaryPrice * UsdToUzsRate, 2),
            [LabourRateCountries.Turkmenistan] = Math.Round(primaryPrice * UsdToTmtRate, 2),
            [LabourRateCountries.Tajikistan] = Math.Round(primaryPrice * UsdToTjsRate, 2)
        };
    }

    private static IEnumerable<VehicleModelLabourRate> CreateVehicleModelLabourRates(long vehicleModelID, decimal primaryRate)
    {
        var countryRates = BuildCountryRatesFromPrimary(primaryRate);

        foreach (var countryRate in countryRates)
        {
            yield return new VehicleModelLabourRate
            {
                VehicleModelID = vehicleModelID,
                CountryID = countryRate.Key,
                LabourRate = countryRate.Value
            };
        }
    }

    private static IEnumerable<MenuVariantLabourRate> CreateMenuVariantLabourRates(long menuVariantID, decimal primaryRate)
    {
        var countryRates = BuildCountryRatesFromPrimary(primaryRate);

        foreach (var countryRate in countryRates)
        {
            yield return new MenuVariantLabourRate
            {
                MenuVariantID = menuVariantID,
                CountryID = countryRate.Key,
                LabourRate = countryRate.Value
            };
        }
    }

    private static List<MenuItemPartCountryPrice> CreateMenuItemPartCountryPrices(decimal primaryPrice)
    {
        var countryPrices = BuildCountryPartPricesFromPrimary(primaryPrice);
        var result = new List<MenuItemPartCountryPrice>();

        foreach (var countryPrice in countryPrices)
        {
            result.Add(new MenuItemPartCountryPrice
            {
                CountryID = countryPrice.Key,
                PartPrice = countryPrice.Value,
                PartPriceMarginPercentage = 0,
                PartFinalPrice = countryPrice.Value
            });
        }

        return result;
    }

    public static async Task SeedAsync(this DbContext db)
    {
        // The static *Data() methods assign explicit IDs, so on SQL Server we must
        // toggle IDENTITY_INSERT around each table's save. Save tables one at a time.

        if (!db.Set<ReplacementItem>().Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[ReplacementItem]", ReplacementItemData());

        if (!db.Set<ServiceIntervalGroup>().Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[ServiceIntervalGroup]", ServiceIntervalGroupData());

        if (!db.Set<ServiceInterval>().Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[ServiceInterval]", ServiceIntervalData());

        if (!db.Set<ReplacementItemServiceIntervalGroup>().Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[ReplacementItemServiceIntervalGroup]", GetReplacementItemServiceIntervalGroupData());

        var existingLabourRateKeys = await db.Set<LabourRateMapping>()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.BrandID, x.LabourRate })
            .ToListAsync();

        var existingLabourRateSet = existingLabourRateKeys
            .Select(x => (x.BrandID, x.LabourRate))
            .ToHashSet();
        var missingLabourRateMappings = LabourRateMappingData()
            .Where(x => !existingLabourRateSet.Contains((x.BrandID, x.LabourRate)))
            .ToList();

        if (missingLabourRateMappings.Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[LabourRateMapping]", missingLabourRateMappings);

        if (!db.Set<BrandMapping>().Any())
            await SaveWithIdentityInsertAsync(db, "[Menu].[BrandMapping]", BrandMappingData());

        await SeedDemoMenuDataAsync(db);
    }

    private static async Task SaveWithIdentityInsertAsync<T>(DbContext db, string tableName, IEnumerable<T> entities) where T : class
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");
        try
        {
            await db.Set<T>().AddRangeAsync(entities);
            await db.SaveChangesAsync();
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");
        }
        await tx.CommitAsync();
    }

    private static async Task SeedDemoMenuDataAsync(DbContext db)
    {
        if (await db.Set<MenuEntity>().AnyAsync(x => !x.IsDeleted))
            return;

        const long toyotaBrandID = 2;
        const long lexusBrandID = 3;

        var toyotaModel = new VehicleModel
        {
            Name = "Corolla Cross Hybrid 2.0L",
            BrandID = toyotaBrandID,
            LabourRate = 45
        };

        var lexusModel = new VehicleModel
        {
            Name = "Lexus ES 300h 2.5L",
            BrandID = lexusBrandID,
            LabourRate = 62
        };

        var toyotaModel2 = new VehicleModel
        {
            Name = "Camry Hybrid 2.5L",
            BrandID = toyotaBrandID,
            LabourRate = 47
        };

        var lexusModel2 = new VehicleModel
        {
            Name = "Lexus RX 350 2.4T",
            BrandID = lexusBrandID,
            LabourRate = 68
        };

        db.Set<VehicleModel>().AddRange(toyotaModel, lexusModel, toyotaModel2, lexusModel2);
        await db.SaveChangesAsync();

        db.Set<VehicleModelLabourRate>().AddRange(
            CreateVehicleModelLabourRates(toyotaModel.ID, toyotaModel.LabourRate)
                .Concat(CreateVehicleModelLabourRates(lexusModel.ID, lexusModel.LabourRate))
                .Concat(CreateVehicleModelLabourRates(toyotaModel2.ID, toyotaModel2.LabourRate))
                .Concat(CreateVehicleModelLabourRates(lexusModel2.ID, lexusModel2.LabourRate))
        );
        await db.SaveChangesAsync();

        var serviceGroupIDs = await db.Set<ServiceIntervalGroup>().Where(x => !x.IsDeleted).OrderBy(x => x.ID).Select(x => x.ID).ToListAsync();

        var seededModels = new[]
        {
            new { Model = toyotaModel, LightAllowedTime = 1.1m, HeavyAllowedTime = 1.6m, LightConsumable = 12m, HeavyConsumable = 16m },
            new { Model = lexusModel, LightAllowedTime = 1.3m, HeavyAllowedTime = 1.8m, LightConsumable = 15m, HeavyConsumable = 20m },
            new { Model = toyotaModel2, LightAllowedTime = 1.2m, HeavyAllowedTime = 1.7m, LightConsumable = 13m, HeavyConsumable = 17m },
            new { Model = lexusModel2, LightAllowedTime = 1.4m, HeavyAllowedTime = 1.9m, LightConsumable = 16m, HeavyConsumable = 22m },
        };

        foreach (var groupID in serviceGroupIDs)
        {
            foreach (var seededModel in seededModels)
            {
                db.Set<VehicleModelLabourDetails>().Add(new VehicleModelLabourDetails
                {
                    VehicleModelID = seededModel.Model.ID,
                    ServiceIntervalGroupID = groupID,
                    AllowedTime = groupID <= 3 ? seededModel.LightAllowedTime : seededModel.HeavyAllowedTime,
                    Consumable = groupID <= 3 ? seededModel.LightConsumable : seededModel.HeavyConsumable
                });
            }
        }

        // Toyota replacement-item mappings
        var tOil = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 1, StandaloneAllowedTime = 0.30m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0262986102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tPlug = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 2, StandaloneAllowedTime = 0.10m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0263086102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tFilter = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 3, StandaloneAllowedTime = 0.20m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "040000010C", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tAcFilter = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 5, StandaloneAllowedTime = 0.25m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "040000020C", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tBrakeFluid = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 12, StandaloneAllowedTime = 0.35m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002212", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tWasher = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 32, StandaloneAllowedTime = 0.10m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002230", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var tHvbFilter = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel.ID, ReplacementItemID = 39, StandaloneAllowedTime = 0.25m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002312", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };

        // Lexus replacement-item mappings
        var lOil = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 1, StandaloneAllowedTime = 0.35m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0262986102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lPlug = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 2, StandaloneAllowedTime = 0.12m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0263086102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lFilter = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 3, StandaloneAllowedTime = 0.22m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "040000010C", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lAcFilter = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 5, StandaloneAllowedTime = 0.30m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400003152", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lBrakeFluid = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 12, StandaloneAllowedTime = 0.40m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002212", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lWasher = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 32, StandaloneAllowedTime = 0.10m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002230", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var lHvbFilter = new ReplacementItemVehicleModel { VehicleModelID = lexusModel.ID, ReplacementItemID = 39, StandaloneAllowedTime = 0.28m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002312", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };

        // Toyota Camry replacement-item mappings
        var cOil = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel2.ID, ReplacementItemID = 1, StandaloneAllowedTime = 0.30m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0262986102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var cFilter = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel2.ID, ReplacementItemID = 3, StandaloneAllowedTime = 0.20m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "040000010C", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var cBrakeFluid = new ReplacementItemVehicleModel { VehicleModelID = toyotaModel2.ID, ReplacementItemID = 12, StandaloneAllowedTime = 0.35m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002212", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };

        // Lexus RX replacement-item mappings
        var rOil = new ReplacementItemVehicleModel { VehicleModelID = lexusModel2.ID, ReplacementItemID = 1, StandaloneAllowedTime = 0.38m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0262986102", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var rFilter = new ReplacementItemVehicleModel { VehicleModelID = lexusModel2.ID, ReplacementItemID = 3, StandaloneAllowedTime = 0.24m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "040000010C", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };
        var rWasher = new ReplacementItemVehicleModel { VehicleModelID = lexusModel2.ID, ReplacementItemID = 32, StandaloneAllowedTime = 0.12m, DefaultParts = [ new ReplacementItemVehicleModelPart { SortOrder = 0, PartNumber = "0400002230", DefaultPeriodicQuantity = 1, DefaultStandaloneQuantity = 1 } ] };

        db.Set<ReplacementItemVehicleModel>().AddRange(
            tOil, tPlug, tFilter, tAcFilter, tBrakeFluid, tWasher, tHvbFilter,
            lOil, lPlug, lFilter, lAcFilter, lBrakeFluid, lWasher, lHvbFilter,
            cOil, cFilter, cBrakeFluid,
            rOil, rFilter, rWasher);

        await db.SaveChangesAsync();

        var toyotaGroup = new MenuEntity { BasicModelCode = "MZEA10", VehicleModelID = toyotaModel.ID, BrandID = toyotaBrandID };
        var toyotaGroup2 = new MenuEntity { BasicModelCode = "MZEA11", VehicleModelID = toyotaModel.ID, BrandID = toyotaBrandID };
        var lexusGroup = new MenuEntity { BasicModelCode = "AXZH10", VehicleModelID = lexusModel.ID, BrandID = lexusBrandID };
        var lexusGroup2 = new MenuEntity { BasicModelCode = "AXZH11", VehicleModelID = lexusModel.ID, BrandID = lexusBrandID };
        var camryGroup = new MenuEntity { BasicModelCode = "AXVA70", VehicleModelID = toyotaModel2.ID, BrandID = toyotaBrandID };
        var rxGroup = new MenuEntity { BasicModelCode = "AALH10", VehicleModelID = lexusModel2.ID, BrandID = lexusBrandID };

        db.Set<MenuEntity>().AddRange(toyotaGroup, toyotaGroup2, lexusGroup, lexusGroup2, camryGroup, rxGroup);
        await db.SaveChangesAsync();

        var toyotaV1 = new MenuVariant
        {
            MenuID = toyotaGroup.ID,
            Name = "V1",
            MenuPrefix = "SER",
            MenuPostfix = "A",
            LabourRate = 45,
            DiscountPercentage = 5,
            HasStandaloneItems = true
        };

        var toyotaV2 = new MenuVariant
        {
            MenuID = toyotaGroup.ID,
            Name = "V2",
            MenuPrefix = "SER",
            MenuPostfix = "P",
            LabourRate = 45,
            DiscountPercentage = 7.5m,
            HasStandaloneItems = true
        };

        var toyotaV3 = new MenuVariant
        {
            MenuID = toyotaGroup.ID,
            Name = "V3",
            MenuPrefix = "MNT",
            MenuPostfix = "X",
            LabourRate = 47,
            DiscountPercentage = 4,
            HasStandaloneItems = true
        };

        var lexusV1 = new MenuVariant
        {
            MenuID = lexusGroup.ID,
            Name = "V1",
            MenuPrefix = "PMS",
            MenuPostfix = null,
            LabourRate = 62,
            DiscountPercentage = 3,
            HasStandaloneItems = false
        };

        var lexusV2 = new MenuVariant
        {
            MenuID = lexusGroup.ID,
            Name = "V2",
            MenuPrefix = "PMS",
            MenuPostfix = "L",
            LabourRate = 64,
            DiscountPercentage = 2.5m,
            HasStandaloneItems = true
        };

        var toyotaB1 = new MenuVariant
        {
            MenuID = toyotaGroup2.ID,
            Name = "V1",
            MenuPrefix = "SRV",
            MenuPostfix = "B",
            LabourRate = 46,
            DiscountPercentage = 6,
            HasStandaloneItems = true
        };

        var lexusB1 = new MenuVariant
        {
            MenuID = lexusGroup2.ID,
            Name = "V1",
            MenuPrefix = "PRE",
            MenuPostfix = "R",
            LabourRate = 63,
            DiscountPercentage = 2,
            HasStandaloneItems = true
        };

        var camryV1 = new MenuVariant
        {
            MenuID = camryGroup.ID,
            Name = "V1",
            MenuPrefix = "CMP",
            MenuPostfix = "H",
            LabourRate = 47,
            DiscountPercentage = 5,
            HasStandaloneItems = true
        };

        var rxV1 = new MenuVariant
        {
            MenuID = rxGroup.ID,
            Name = "V1",
            MenuPrefix = "RXS",
            MenuPostfix = "T",
            LabourRate = 68,
            DiscountPercentage = 3,
            HasStandaloneItems = true
        };

        db.Set<MenuVariant>().AddRange(toyotaV1, toyotaV2, toyotaV3, lexusV1, lexusV2, toyotaB1, lexusB1, camryV1, rxV1);
        await db.SaveChangesAsync();

        db.Set<MenuVariantLabourRate>().AddRange(
            CreateMenuVariantLabourRates(toyotaV1.ID, toyotaV1.LabourRate)
                .Concat(CreateMenuVariantLabourRates(toyotaV2.ID, toyotaV2.LabourRate))
                .Concat(CreateMenuVariantLabourRates(toyotaV3.ID, toyotaV3.LabourRate))
                .Concat(CreateMenuVariantLabourRates(lexusV1.ID, lexusV1.LabourRate))
                .Concat(CreateMenuVariantLabourRates(lexusV2.ID, lexusV2.LabourRate))
                .Concat(CreateMenuVariantLabourRates(toyotaB1.ID, toyotaB1.LabourRate))
                .Concat(CreateMenuVariantLabourRates(lexusB1.ID, lexusB1.LabourRate))
                .Concat(CreateMenuVariantLabourRates(camryV1.ID, camryV1.LabourRate))
                .Concat(CreateMenuVariantLabourRates(rxV1.ID, rxV1.LabourRate))
        );
        await db.SaveChangesAsync();

        var serviceIntervals = await db.Set<ServiceInterval>()
            .Where(x => !x.IsDeleted && (x.ID == 5 || x.ID == 10 || x.ID == 20 || x.ID == 40))
            .Select(x => x.ID)
            .ToListAsync();

        void AddVariantCoreData(MenuVariant variant)
        {
            foreach (var groupID in serviceGroupIDs)
            {
                db.Set<MenuLabourDetails>().Add(new MenuLabourDetails
                {
                    MenuVariantID = variant.ID,
                    ServiceIntervalGroupID = groupID,
                    AllowedTime = variant.LabourRate > 50 ? 1.4m : 1.2m,
                    Consumable = variant.LabourRate > 50 ? 18 : 14
                });
            }

            foreach (var intervalID in serviceIntervals)
            {
                db.Set<MenuPeriodicAvailability>().Add(new MenuPeriodicAvailability
                {
                    MenuVariantID = variant.ID,
                    ServiceIntervalID = intervalID
                });
            }
        }

        AddVariantCoreData(toyotaV1);
        AddVariantCoreData(toyotaV2);
        AddVariantCoreData(toyotaV3);
        AddVariantCoreData(lexusV1);
        AddVariantCoreData(lexusV2);
        AddVariantCoreData(toyotaB1);
        AddVariantCoreData(lexusB1);
        AddVariantCoreData(camryV1);
        AddVariantCoreData(rxV1);

        // Toyota V1 items (includes intentional part-number reuse)
        db.Set<MenuItem>().AddRange(
            new MenuItem
            {
                MenuVariantID = toyotaV1.ID,
                ReplacementItemVehicleModelID = tOil.ID,
                StandaloneAllowedTime = 0.30m,
                Parts =
                [
                    new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(32)},
                    new MenuItemPart { SortOrder = 1, PartNumber = "0888083806", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(31)}
                ]
            },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tPlug.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0263086102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(4)} ] },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tFilter.ID, StandaloneAllowedTime = 0.20m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(10)} ] },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tAcFilter.ID, StandaloneAllowedTime = 0.25m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000020C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(18)} ] },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tBrakeFluid.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002212", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(14)} ] },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tWasher.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002230", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(6)} ] },
            new MenuItem { MenuVariantID = toyotaV1.ID, ReplacementItemVehicleModelID = tHvbFilter.ID, StandaloneAllowedTime = 0.25m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002312", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(20)} ] }
        );

        // Toyota V2 items (reuses given part numbers)
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = toyotaV2.ID, ReplacementItemVehicleModelID = tOil.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(33)} ] },
            new MenuItem { MenuVariantID = toyotaV2.ID, ReplacementItemVehicleModelID = tFilter.ID, StandaloneAllowedTime = 0.20m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(11)} ] },
            new MenuItem { MenuVariantID = toyotaV2.ID, ReplacementItemVehicleModelID = tAcFilter.ID, StandaloneAllowedTime = 0.25m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400003152", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(19)} ] },
            new MenuItem { MenuVariantID = toyotaV2.ID, ReplacementItemVehicleModelID = tBrakeFluid.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002212", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(14.5m)} ] },
            new MenuItem { MenuVariantID = toyotaV2.ID, ReplacementItemVehicleModelID = tWasher.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002230", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(6)} ] }
        );

        // Toyota V3 items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = toyotaV3.ID, ReplacementItemVehicleModelID = tOil.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(34)} ] },
            new MenuItem { MenuVariantID = toyotaV3.ID, ReplacementItemVehicleModelID = tPlug.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0263086102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(4.2m)} ] },
            new MenuItem { MenuVariantID = toyotaV3.ID, ReplacementItemVehicleModelID = tFilter.ID, StandaloneAllowedTime = 0.20m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(11.5m)} ] },
            new MenuItem { MenuVariantID = toyotaV3.ID, ReplacementItemVehicleModelID = tHvbFilter.ID, StandaloneAllowedTime = 0.25m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002312", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(21.5m)} ] }
        );

        // Lexus V1 items (reuses same part numbers across another model/brand)
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lOil.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(35)} ] },
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lPlug.ID, StandaloneAllowedTime = 0.12m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0263086102", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(4.5m)} ] },
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lFilter.ID, StandaloneAllowedTime = 0.22m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(12)} ] },
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lAcFilter.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400003152", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(21)} ] },
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lBrakeFluid.ID, StandaloneAllowedTime = 0.40m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002212", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(15.5m)} ] },
            new MenuItem { MenuVariantID = lexusV1.ID, ReplacementItemVehicleModelID = lHvbFilter.ID, StandaloneAllowedTime = 0.28m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002312", PeriodicQuantity = 1, StandaloneQuantity = null, CountryPrices = CreateMenuItemPartCountryPrices(23)} ] }
        );

        // Lexus V2 items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = lexusV2.ID, ReplacementItemVehicleModelID = lOil.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(36)} ] },
            new MenuItem { MenuVariantID = lexusV2.ID, ReplacementItemVehicleModelID = lFilter.ID, StandaloneAllowedTime = 0.22m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(12.5m)} ] },
            new MenuItem { MenuVariantID = lexusV2.ID, ReplacementItemVehicleModelID = lAcFilter.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400003152", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(22)} ] },
            new MenuItem { MenuVariantID = lexusV2.ID, ReplacementItemVehicleModelID = lWasher.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002230", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(6.5m)} ] }
        );

        // Toyota alternate menu items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = toyotaB1.ID, ReplacementItemVehicleModelID = tOil.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(33.5m)} ] },
            new MenuItem { MenuVariantID = toyotaB1.ID, ReplacementItemVehicleModelID = tFilter.ID, StandaloneAllowedTime = 0.20m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(11.25m)} ] },
            new MenuItem { MenuVariantID = toyotaB1.ID, ReplacementItemVehicleModelID = tBrakeFluid.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002212", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(14.2m)} ] }
        );

        // Lexus alternate menu items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = lexusB1.ID, ReplacementItemVehicleModelID = lOil.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(36.8m)} ] },
            new MenuItem { MenuVariantID = lexusB1.ID, ReplacementItemVehicleModelID = lFilter.ID, StandaloneAllowedTime = 0.22m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(12.8m)} ] },
            new MenuItem { MenuVariantID = lexusB1.ID, ReplacementItemVehicleModelID = lWasher.ID, StandaloneAllowedTime = 0.10m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002230", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(6.8m)} ] }
        );

        // Camry menu items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = camryV1.ID, ReplacementItemVehicleModelID = cOil.ID, StandaloneAllowedTime = 0.30m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(34m)} ] },
            new MenuItem { MenuVariantID = camryV1.ID, ReplacementItemVehicleModelID = cFilter.ID, StandaloneAllowedTime = 0.20m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(11.7m)} ] },
            new MenuItem { MenuVariantID = camryV1.ID, ReplacementItemVehicleModelID = cBrakeFluid.ID, StandaloneAllowedTime = 0.35m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002212", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(14.4m)} ] }
        );

        // RX menu items
        db.Set<MenuItem>().AddRange(
            new MenuItem { MenuVariantID = rxV1.ID, ReplacementItemVehicleModelID = rOil.ID, StandaloneAllowedTime = 0.38m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0262986102", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(38m)} ] },
            new MenuItem { MenuVariantID = rxV1.ID, ReplacementItemVehicleModelID = rFilter.ID, StandaloneAllowedTime = 0.24m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "040000010C", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(13.2m)} ] },
            new MenuItem { MenuVariantID = rxV1.ID, ReplacementItemVehicleModelID = rWasher.ID, StandaloneAllowedTime = 0.12m, Parts = [ new MenuItemPart { SortOrder = 0, PartNumber = "0400002230", PeriodicQuantity = 1, StandaloneQuantity = 1, CountryPrices = CreateMenuItemPartCountryPrices(7m)} ] }
        );

        await db.SaveChangesAsync();
    }

    private static IEnumerable<ReplacementItem> ReplacementItemData()
    {
        List<ReplacementItem> items = new List<ReplacementItem>
        {
            new ReplacementItem(1) { Name = "Engine Oil", FriendlyName = "Engine Oil", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="EO", StandaloneOperationCode=Loc("EO"), AllowMultiplePartNumbers = true },
            new ReplacementItem(2) { Name = "Drain Plug", FriendlyName = "Drain Plug", Type = ReplacementItemType.Component, StandaloneLabourCode="DP", StandaloneOperationCode=Loc("DP") },
            new ReplacementItem(3) { Name = "Oil Filter", FriendlyName = "Oil Filter", Type = ReplacementItemType.Component, StandaloneLabourCode="OF", StandaloneOperationCode=Loc("OF") },
            new ReplacementItem(4) { Name = "Air Cleaner Element", FriendlyName = "Air Cleaner Element", Type = ReplacementItemType.Component, StandaloneLabourCode="ACE", StandaloneOperationCode=Loc("ACE") },
            new ReplacementItem(5) { Name = "A/C Filter", FriendlyName = "A/C Filter", Type = ReplacementItemType.Component, StandaloneLabourCode="ACF", StandaloneOperationCode=Loc("ACF") },
            new ReplacementItem(6) { Name = "Fuel Filter", FriendlyName = "Fuel Filter", Type = ReplacementItemType.Component, StandaloneLabourCode="FF", StandaloneOperationCode=Loc("FF") },
            new ReplacementItem(7) { Name = "Spark Plug", FriendlyName = "Spark Plug", Type = ReplacementItemType.Component, StandaloneLabourCode="SP", StandaloneOperationCode=Loc("SP") },
            new ReplacementItem(8) { Name = "MT Fluid (75W90)", FriendlyName = "MT Fluid (75W90)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="MTF", StandaloneOperationCode=Loc("MTF") },
            new ReplacementItem(9) { Name = "LSD Differential Oil RR (85W90) \"GL-5\"", FriendlyName = "LSD Differential Oil RR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="LDO", StandaloneOperationCode=Loc("LDO") },
            new ReplacementItem(10) { Name = "Differential Oil FR (85W90) \"GL-5\"", FriendlyName = "Differential Oil FR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="DOF", StandaloneOperationCode=Loc("DOF") },
            new ReplacementItem(11) { Name = "Differential Oil RR (85W90) \"GL-5\"", FriendlyName = "Differential Oil RR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="DOR", StandaloneOperationCode=Loc("DOR") },
            new ReplacementItem(12) { Name = "Brake Fluid DOT 3 (330 ML)", FriendlyName = "Brake Fluid DOT 3 (330 ML)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="BF", StandaloneOperationCode=Loc("BF") },
            new ReplacementItem(13) { Name = "AT Fluid WS (4Ltr)", FriendlyName = "AT Fluid WS (4Ltr)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="ATF", StandaloneOperationCode=Loc("ATF") },
            new ReplacementItem(14) { Name = "AT Fluid WS (1Ltr)", FriendlyName = "AT Fluid WS (1Ltr)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="ATF", StandaloneOperationCode=Loc("ATF") },
            new ReplacementItem(15) { Name = "ATF Strainer", FriendlyName = "ATF Strainer", Type = ReplacementItemType.Component, StandaloneLabourCode="AS", StandaloneOperationCode=Loc("AS") },
            new ReplacementItem(16) { Name = "ATF Strainer Ring", FriendlyName = "ATF Strainer Ring", Type = ReplacementItemType.Component, StandaloneLabourCode="ASR", StandaloneOperationCode=Loc("ASR") },
            new ReplacementItem(17) { Name = "ATF Pan Gasket", FriendlyName = "ATF Pan Gasket", Type = ReplacementItemType.Component, StandaloneLabourCode="APG", StandaloneOperationCode=Loc("APG") },
            new ReplacementItem(18) { Name = "CVT Fluid FE", FriendlyName = "CVT Fluid FE", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="CFF", StandaloneOperationCode=Loc("CFF") },
            new ReplacementItem(19) { Name = "CVT Strainer", FriendlyName = "CVT Strainer", Type = ReplacementItemType.Component, StandaloneLabourCode="CS", StandaloneOperationCode=Loc("CS") },
            new ReplacementItem(20) { Name = "CVT Strainer O Ring", FriendlyName = "CVT Strainer O Ring", Type = ReplacementItemType.Component, StandaloneLabourCode="CSO", StandaloneOperationCode=Loc("CSO") },
            new ReplacementItem(21) { Name = "CVT Pan Gasket", FriendlyName = "CVT Pan Gasket", Type = ReplacementItemType.Component, StandaloneLabourCode="CPG", StandaloneOperationCode=Loc("CPG") },
            new ReplacementItem(22) { Name = "Transfer Fluid (75W90)", FriendlyName = "Transfer Fluid (75W90)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode="TF", StandaloneOperationCode=Loc("TF") },
            new ReplacementItem(23) { Name = "Gasket Plug ATM Filler", FriendlyName = "Gasket Plug ATM Filler", Type = ReplacementItemType.Component, StandaloneLabourCode="GPA", StandaloneOperationCode=Loc("GPA") },
            new ReplacementItem(24) { Name = "Gasket Plug ATM Drain", FriendlyName = "Gasket Plug ATM Drain", Type = ReplacementItemType.Component, StandaloneLabourCode="GPD", StandaloneOperationCode=Loc("GPD") },
            new ReplacementItem(25) { Name = "Gasket Plug MTM", FriendlyName = "Gasket Plug MTM", Type = ReplacementItemType.Component, StandaloneLabourCode="GPM", StandaloneOperationCode=Loc("GPM") },
            new ReplacementItem(26) { Name = "Gasket Plug FR Diff Darin", FriendlyName = "Gasket Plug FR Diff Darin", Type = ReplacementItemType.Component, StandaloneLabourCode="GPF", StandaloneOperationCode=Loc("GPF") },
            new ReplacementItem(27) { Name = "Gasket Plug FR Diff Filler", FriendlyName = "Gasket Plug FR Diff Filler", Type = ReplacementItemType.Component, StandaloneLabourCode="GPF", StandaloneOperationCode=Loc("GPF") },
            new ReplacementItem(28) { Name = "Gasket Plug RR Diff", FriendlyName = "Gasket Plug RR Diff", Type = ReplacementItemType.Component, StandaloneLabourCode="GPR", StandaloneOperationCode=Loc("GPR") },
            new ReplacementItem(29) { Name = "Gasket Plug Transfer", FriendlyName = "Gasket Plug Transfer", Type = ReplacementItemType.Component, StandaloneLabourCode="GPT", StandaloneOperationCode=Loc("GPT") },
            new ReplacementItem(30) { Name = "FR Brake Pad", FriendlyName = "FR Brake Pad", Type = ReplacementItemType.Component, StandaloneLabourCode="FBP", StandaloneOperationCode=Loc("FBP") },
            new ReplacementItem(31) { Name = "RR Brake Pad / Shoe", FriendlyName = "RR Brake Pad / Shoe", Type = ReplacementItemType.Component, StandaloneLabourCode="RBP", StandaloneOperationCode=Loc("RBP") },
            new ReplacementItem(32) { Name = "Screen Washer", FriendlyName = "Screen Washer", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode="SW", StandaloneOperationCode=Loc("SW") },
            new ReplacementItem(33) { Name = "BG 44K Fuel System Cleaner", FriendlyName = "BG 44K Fuel System Cleaner", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode="B4F", StandaloneOperationCode=Loc("B4F") },
            new ReplacementItem(34) { Name = "BG EPR", FriendlyName = "BG EPR", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode="BE", StandaloneOperationCode=Loc("BE") },
            new ReplacementItem(35) { Name = "BG EFI System", FriendlyName = "BG EFI System", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode="BES", StandaloneOperationCode=Loc("BES") },
            new ReplacementItem(36) { Name = "Brake Cleaner", FriendlyName = "Brake Cleaner", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode="BC", StandaloneOperationCode=Loc("BC") },
            new ReplacementItem(37) { Name = "FR Brake Disc", FriendlyName = "FR Brake Disc", Type = ReplacementItemType.Component, StandaloneLabourCode="FBD", StandaloneOperationCode=Loc("FBD") },
            new ReplacementItem(38) { Name = "RR Brake Disc", FriendlyName = "RR Brake Disc", Type = ReplacementItemType.Component, StandaloneLabourCode="RBD", StandaloneOperationCode=Loc("RBD") },
            new ReplacementItem(39) { Name = "HV Battery Filter", FriendlyName = "HV Battery Filter", Type = ReplacementItemType.Component, StandaloneLabourCode="HBF", StandaloneOperationCode=Loc("HBF") },
            new ReplacementItem(40) { Name = "Battery", FriendlyName = "Battery", Type = ReplacementItemType.Component, StandaloneLabourCode="B", StandaloneOperationCode=Loc("B") },
            new ReplacementItem(41) { Name = "Tires", FriendlyName = "Tires", Type = ReplacementItemType.Component, StandaloneLabourCode="T", StandaloneOperationCode=Loc("T") },
            new ReplacementItem(42) { Name = "Wiper Rubber LHS", FriendlyName = "Wiper Rubber LHS", Type = ReplacementItemType.Component, StandaloneLabourCode="WRL", StandaloneOperationCode=Loc("WRL") },
            new ReplacementItem(43) { Name = "Wiper Rubber RHS", FriendlyName = "Wiper Rubber RHS", Type = ReplacementItemType.Component, StandaloneLabourCode="WRR", StandaloneOperationCode=Loc("WRR") },
            new ReplacementItem(44) { Name = "RR Wiper Rubber", FriendlyName = "RR Wiper Rubber", Type = ReplacementItemType.Component, StandaloneLabourCode="RWR", StandaloneOperationCode=Loc("RWR") },
        };

        return items;
    }

    private static IEnumerable<ServiceInterval> ServiceIntervalData()
    {
        List<ServiceInterval> serviceIntervals = new List<ServiceInterval>();

        for (int i = 5; i <= 200; i += 5)
        {
            var fullName = $"{(i * 1000).ToString("N0")} KM";

            var serviceInterval = new ServiceInterval(i)
            {
                Code = $"{i}K",
                FullName = fullName,
                ValueInMeter = i * 1000,
                Description = $"CARRY OUT {fullName} SERVICE"
            };

            if (i == 120)
                serviceInterval.ServiceIntervalGroupID = 7;
            else if (i == 80 || i == 160)
                serviceInterval.ServiceIntervalGroupID = 6;
            else if (i == 60 || i == 180)
                serviceInterval.ServiceIntervalGroupID = 5;
            else if (i == 40 || i == 200)
                serviceInterval.ServiceIntervalGroupID = 4;
            else if (i == 20 || i == 100 || i == 140)
                serviceInterval.ServiceIntervalGroupID = 3;
            else if (i % 20 == 10)
                serviceInterval.ServiceIntervalGroupID = 2;
            else if (i % 10 == 5)
                serviceInterval.ServiceIntervalGroupID = 1;

            serviceIntervals.Add(serviceInterval);
        }

        return serviceIntervals;
    }

    private static IEnumerable<ServiceIntervalGroup> ServiceIntervalGroupData()
    {
        List<ServiceIntervalGroup> serviceIntervalGroups = new List<ServiceIntervalGroup>();

        serviceIntervalGroups.Add(new ServiceIntervalGroup(1)
        {
            Name = "ServiceIntervals that ends with five",
            LabourCode = "0B",
            LabourDescription = "PM SUPER LIGHT SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(2)
        {
            Name = "Twenty step sequence start at 10K",
            LabourCode = "0C",
            LabourDescription = "PM LIGHT SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(3)
        {
            Name = "20K, 100K, 140K",
            LabourCode = "0D",
            LabourDescription = "PM MEDIUM SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(4)
        {
            Name = "40K, 200K",
            LabourCode = "0E",
            LabourDescription = "PM HEAVY SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(5)
        {
            Name = "60K, 180K",
            LabourCode = "0D",
            LabourDescription = "PM MEDIUM SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(6)
        {
            Name = "80K, 160K",
            LabourCode = "0E",
            LabourDescription = "PM HEAVY SERVICE"
        });

        serviceIntervalGroups.Add(new ServiceIntervalGroup(7)
        {
            Name = "120K",
            LabourCode = "0E",
            LabourDescription = "PM HEAVY SERVICE"
        });

        return serviceIntervalGroups;
    }

    private static IEnumerable<LabourRateMapping> LabourRateMappingData()
    {
        List<LabourRateMapping> mappings = new List<LabourRateMapping>
        {
            new LabourRateMapping(2) { Code = "A", LabourRate = 44, BrandID = 2 }, // Toyota
            new LabourRateMapping(3) { Code = "A", LabourRate = 65, BrandID = 3 }, // Lexus
            new LabourRateMapping(6) { Code = "B", LabourRate = 40, BrandID = 2 }, // Toyota
            new LabourRateMapping(7) { Code = "B", LabourRate = 45, BrandID = 3 }, // Lexus
            new LabourRateMapping(8) { Code = "B2", LabourRate = 35, BrandID = 2 }, // Toyota
            new LabourRateMapping(10) { Code = "C", LabourRate = 2, BrandID = 3 }, // Lexus
            new LabourRateMapping(11) { Code = "C", LabourRate = 2, BrandID = 2 }, // Toyota
            new LabourRateMapping(12) { Code = "D", LabourRate = 50, BrandID = 3 }, // Lexus
            new LabourRateMapping(13) { Code = "D", LabourRate = 50, BrandID = 2 }, // Toyota
            new LabourRateMapping(16) { Code = "G", LabourRate = 32, BrandID = 2 }, // Toyota
            new LabourRateMapping(17) { Code = "G", LabourRate = 48, BrandID = 3 }, // Lexus
            new LabourRateMapping(18) { Code = "H", LabourRate = 25, BrandID = 3 }, // Lexus
            new LabourRateMapping(19) { Code = "H", LabourRate = 25, BrandID = 2 }, // Toyota
            new LabourRateMapping(21) { Code = "I", LabourRate = 15, BrandID = 3 }, // Lexus
            new LabourRateMapping(22) { Code = "I", LabourRate = 15, BrandID = 2 }, // Toyota
            new LabourRateMapping(23) { Code = "J", LabourRate = 100, BrandID = 3 }, // Lexus
            new LabourRateMapping(24) { Code = "J", LabourRate = 100, BrandID = 2 }, // Toyota
            new LabourRateMapping(26) { Code = "K", LabourRate = 26, BrandID = 3 }, // Lexus
            new LabourRateMapping(27) { Code = "K", LabourRate = 26, BrandID = 2 }, // Toyota
            new LabourRateMapping(29) { Code = "L", LabourRate = 29, BrandID = 2 }, // Toyota
            new LabourRateMapping(31) { Code = "M", LabourRate = 32, BrandID = 3 }, // Lexus
            new LabourRateMapping(34) { Code = "S", LabourRate = 1, BrandID = 3 }, // Lexus
            new LabourRateMapping(35) { Code = "S", LabourRate = 1, BrandID = 2 }, // Toyota
            new LabourRateMapping(36) { Code = "U", LabourRate = 45, BrandID = 2 }, // Toyota
            new LabourRateMapping(38) { Code = "V", LabourRate = 60, BrandID = 3 }, // Lexus
            new LabourRateMapping(40) { Code = "W", LabourRate = 60, BrandID = 2 }, // Toyota
            new LabourRateMapping(41) { Code = "W", LabourRate = 80, BrandID = 3 }, // Lexus
            new LabourRateMapping(42) { Code = "W1", LabourRate = 24, BrandID = 2 }, // Toyota
            new LabourRateMapping(43) { Code = "W2", LabourRate = 25.92M, BrandID = 2 }, // Toyota
            new LabourRateMapping(44) { Code = "W3", LabourRate = 27.58M, BrandID = 2 }, // Toyota
            // Seeded demo data coverage
            new LabourRateMapping(45) { Code = "U1", LabourRate = 46, BrandID = 2 }, // Toyota
            new LabourRateMapping(46) { Code = "U2", LabourRate = 47, BrandID = 2 }, // Toyota
            new LabourRateMapping(47) { Code = "V1", LabourRate = 62, BrandID = 3 }, // Lexus
            new LabourRateMapping(48) { Code = "V2", LabourRate = 63, BrandID = 3 }, // Lexus
            new LabourRateMapping(49) { Code = "V3", LabourRate = 64, BrandID = 3 }, // Lexus
            new LabourRateMapping(50) { Code = "V4", LabourRate = 68, BrandID = 3 }, // Lexus
        };

        return mappings;
    }

    private static IEnumerable<BrandMapping> BrandMappingData()
    {
        List<BrandMapping> mappings = new List<BrandMapping>
        {
            new BrandMapping(1) { BrandID = 2, Code = "00", BrandAbbreviation = "T" },
            new BrandMapping(2) { BrandID = 3, Code = "11", BrandAbbreviation = "L" },
        };

        return mappings;
    }

    private static IEnumerable<ReplacementItemServiceIntervalGroup> GetReplacementItemServiceIntervalGroupData()
    {
        var data = new List<ReplacementItemServiceIntervalGroup>()
        {
            // Item 1
            new ReplacementItemServiceIntervalGroup(1) { ReplacementItemID = 1, ServiceIntervalGroupID = 1 },
            new ReplacementItemServiceIntervalGroup(2) { ReplacementItemID = 1, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(3) { ReplacementItemID = 1, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(4) { ReplacementItemID = 1, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(5) { ReplacementItemID = 1, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(6) { ReplacementItemID = 1, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(7) { ReplacementItemID = 1, ServiceIntervalGroupID = 7 },

            // Item 2
            new ReplacementItemServiceIntervalGroup(8) { ReplacementItemID = 2, ServiceIntervalGroupID = 1 },
            new ReplacementItemServiceIntervalGroup(9) { ReplacementItemID = 2, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(10) { ReplacementItemID = 2, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(11) { ReplacementItemID = 2, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(12) { ReplacementItemID = 2, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(13) { ReplacementItemID = 2, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(14) { ReplacementItemID = 2, ServiceIntervalGroupID = 7 },

            // Item 3
            new ReplacementItemServiceIntervalGroup(15) { ReplacementItemID = 3, ServiceIntervalGroupID = 1 },
            new ReplacementItemServiceIntervalGroup(16) { ReplacementItemID = 3, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(17) { ReplacementItemID = 3, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(18) { ReplacementItemID = 3, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(19) { ReplacementItemID = 3, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(20) { ReplacementItemID = 3, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(21) { ReplacementItemID = 3, ServiceIntervalGroupID = 7 },

            // Item 4
            new ReplacementItemServiceIntervalGroup(22) { ReplacementItemID = 4, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(23) { ReplacementItemID = 4, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(24) { ReplacementItemID = 4, ServiceIntervalGroupID = 7 },

            // Item 5
            new ReplacementItemServiceIntervalGroup(25) { ReplacementItemID = 5, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(26) { ReplacementItemID = 5, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(27) { ReplacementItemID = 5, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(28) { ReplacementItemID = 5, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(29) { ReplacementItemID = 5, ServiceIntervalGroupID = 7 },

            // Item 6
            new ReplacementItemServiceIntervalGroup(30) { ReplacementItemID = 6, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(31) { ReplacementItemID = 6, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(32) { ReplacementItemID = 6, ServiceIntervalGroupID = 7 },

            // Item 7
            new ReplacementItemServiceIntervalGroup(33) { ReplacementItemID = 7, ServiceIntervalGroupID = 6 },

            // Item 8
            new ReplacementItemServiceIntervalGroup(34) { ReplacementItemID = 8, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(35) { ReplacementItemID = 8, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(36) { ReplacementItemID = 8, ServiceIntervalGroupID = 7 },

            // Item 9
            new ReplacementItemServiceIntervalGroup(37) { ReplacementItemID = 9, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(38) { ReplacementItemID = 9, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(39) { ReplacementItemID = 9, ServiceIntervalGroupID = 7 },

            // Item 10
            new ReplacementItemServiceIntervalGroup(40) { ReplacementItemID = 10, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(41) { ReplacementItemID = 10, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(42) { ReplacementItemID = 10, ServiceIntervalGroupID = 7 },

            // Item 11
            new ReplacementItemServiceIntervalGroup(43) { ReplacementItemID = 11, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(44) { ReplacementItemID = 11, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(45) { ReplacementItemID = 11, ServiceIntervalGroupID = 7 },

            // Item 12
            new ReplacementItemServiceIntervalGroup(46) { ReplacementItemID = 12, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(47) { ReplacementItemID = 12, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(48) { ReplacementItemID = 12, ServiceIntervalGroupID = 7 },

            // Item 13
            new ReplacementItemServiceIntervalGroup(49) { ReplacementItemID = 13, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(50) { ReplacementItemID = 13, ServiceIntervalGroupID = 7 },

            // Item 14
            new ReplacementItemServiceIntervalGroup(51) { ReplacementItemID = 14, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(52) { ReplacementItemID = 14, ServiceIntervalGroupID = 7 },

            // Item 15
            new ReplacementItemServiceIntervalGroup(53) { ReplacementItemID = 15, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(54) { ReplacementItemID = 15, ServiceIntervalGroupID = 7 },

            // Item 16
            new ReplacementItemServiceIntervalGroup(55) { ReplacementItemID = 16, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(56) { ReplacementItemID = 16, ServiceIntervalGroupID = 7 },

            // Item 17
            new ReplacementItemServiceIntervalGroup(57) { ReplacementItemID = 17, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(58) { ReplacementItemID = 17, ServiceIntervalGroupID = 7 },

            // Item 18
            new ReplacementItemServiceIntervalGroup(59) { ReplacementItemID = 18, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(60) { ReplacementItemID = 18, ServiceIntervalGroupID = 7 },

            // Item 19
            new ReplacementItemServiceIntervalGroup(61) { ReplacementItemID = 19, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(62) { ReplacementItemID = 19, ServiceIntervalGroupID = 7 },

            // Item 20
            new ReplacementItemServiceIntervalGroup(63) { ReplacementItemID = 20, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(64) { ReplacementItemID = 20, ServiceIntervalGroupID = 7 },

            // Item 21
            new ReplacementItemServiceIntervalGroup(65) { ReplacementItemID = 21, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(66) { ReplacementItemID = 21, ServiceIntervalGroupID = 7 },

            // Item 22
            new ReplacementItemServiceIntervalGroup(67) { ReplacementItemID = 22, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(68) { ReplacementItemID = 22, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(69) { ReplacementItemID = 22, ServiceIntervalGroupID = 7 },

            // Item 23
            new ReplacementItemServiceIntervalGroup(70) { ReplacementItemID = 23, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(71) { ReplacementItemID = 23, ServiceIntervalGroupID = 7 },

            // Item 24
            new ReplacementItemServiceIntervalGroup(72) { ReplacementItemID = 24, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(73) { ReplacementItemID = 24, ServiceIntervalGroupID = 7 },

            // Item 25
            new ReplacementItemServiceIntervalGroup(74) { ReplacementItemID = 25, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(75) { ReplacementItemID = 25, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(76) { ReplacementItemID = 25, ServiceIntervalGroupID = 7 },

            // Item 26
            new ReplacementItemServiceIntervalGroup(77) { ReplacementItemID = 26, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(78) { ReplacementItemID = 26, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(79) { ReplacementItemID = 26, ServiceIntervalGroupID = 7 },

            // Item 28
            new ReplacementItemServiceIntervalGroup(80) { ReplacementItemID = 28, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(81) { ReplacementItemID = 28, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(82) { ReplacementItemID = 28, ServiceIntervalGroupID = 7 },

            // Item 29
            new ReplacementItemServiceIntervalGroup(83) { ReplacementItemID = 29, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(84) { ReplacementItemID = 29, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(85) { ReplacementItemID = 29, ServiceIntervalGroupID = 7 },

            // Item 32
            new ReplacementItemServiceIntervalGroup(86) { ReplacementItemID = 32, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(87) { ReplacementItemID = 32, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(88) { ReplacementItemID = 32, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(89) { ReplacementItemID = 32, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(90) { ReplacementItemID = 32, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(91) { ReplacementItemID = 32, ServiceIntervalGroupID = 7 },

            // Item 33
            new ReplacementItemServiceIntervalGroup(92) { ReplacementItemID = 33, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(93) { ReplacementItemID = 33, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(94) { ReplacementItemID = 33, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(95) { ReplacementItemID = 33, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(96) { ReplacementItemID = 33, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(97) { ReplacementItemID = 33, ServiceIntervalGroupID = 7 },

            // Item 36
            new ReplacementItemServiceIntervalGroup(98) { ReplacementItemID = 36, ServiceIntervalGroupID = 2 },
            new ReplacementItemServiceIntervalGroup(99) { ReplacementItemID = 36, ServiceIntervalGroupID = 3 },
            new ReplacementItemServiceIntervalGroup(100) { ReplacementItemID = 36, ServiceIntervalGroupID = 4 },
            new ReplacementItemServiceIntervalGroup(101) { ReplacementItemID = 36, ServiceIntervalGroupID = 5 },
            new ReplacementItemServiceIntervalGroup(102) { ReplacementItemID = 36, ServiceIntervalGroupID = 6 },
            new ReplacementItemServiceIntervalGroup(103) { ReplacementItemID = 36, ServiceIntervalGroupID = 7 },
        };

        return data;
    }
}

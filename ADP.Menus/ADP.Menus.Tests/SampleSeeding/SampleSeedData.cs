using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftIdentity.Data.Entities;

using System.Text.Json;

using MenuEntity = ShiftSoftware.ADP.Menus.Data.Entities.Menu;

namespace ShiftSoftware.ADP.Menus.Tests.SampleSeeding;

/// <summary>What one table came out of a seeding run with.</summary>
/// <param name="Table">The entity type name.</param>
/// <param name="AlreadyPresent">Seed rows that were already there and were left exactly as they were.</param>
/// <param name="Inserted">Seed rows this run added.</param>
public sealed record SampleSeedTableResult(string Table, int AlreadyPresent, int Inserted);

/// <summary>The outcome of one <see cref="SampleSeedData.SeedMissingAsync"/> call.</summary>
public sealed record SampleSeedReport(IReadOnlyList<SampleSeedTableResult> Tables)
{
    public int Inserted => Tables.Sum(table => table.Inserted);

    public int AlreadyPresent => Tables.Sum(table => table.AlreadyPresent);

    /// <summary>True when everything the seed describes was already in the database.</summary>
    public bool NothingToDo => Inserted == 0;
}

/// <summary>
/// The sample database's demo data, and the one rule that governs writing it: <b>insert what is missing,
/// touch nothing that is there.</b>
///
/// <para>This used to run from the sample API's startup. It does not any more, and the reason is worth
/// keeping: seeding at boot wrote demo rows into whatever catalogue happened to be loaded, the
/// replication trigger copied them into Cosmos, and then the dev data import emptied the <c>[Menu]</c>
/// schema in raw SQL — no EF, so no trigger, so the documents stayed. The visible result is a basic model
/// code the lookup keeps serving that cannot be found anywhere in the database. Seeding is a thing you
/// should do on purpose, not something a restart does behind you, so it lives in a test you trigger:
/// <see cref="SampleDataSeedingTests"/>.</para>
///
/// <para><b>Every row is matched before it is written.</b> The catalogue tables carry authored ids and are
/// matched on them; the demo menu graph gets its ids from IDENTITY and is matched on its natural key —
/// a vehicle model by name, a menu by basic model code, a variant by (menu, name), and so on down. So
/// running it against a real imported catalogue adds only the demo rows that catalogue does not already
/// have, and running it twice does nothing the second time.</para>
///
/// <para><b>It never updates.</b> A row that exists is left exactly as it is, whatever its values. That
/// keeps the seeder incapable of overwriting imported data, which is the property that matters here —
/// the alternative would let a test quietly rewrite a real catalogue's prices.</para>
/// </summary>
public static class SampleSeedData
{
    private const long ToyotaBrandID = 2;
    private const long LexusBrandID = 3;

    // The sample API's LabourRateCountries, duplicated rather than referenced: pulling a web app's
    // project reference into a test project to share three constants is the worse trade.
    private const long Uzbekistan = 3;
    private const long Turkmenistan = 4;
    private const long Tajikistan = 5;

    // USD-to-local FX snapshot for seeded demo data (source timestamp: 2026-04-02 UTC).
    private const decimal UsdToUzsRate = 12191.552819m;
    private const decimal UsdToTmtRate = 3.501513m;
    private const decimal UsdToTjsRate = 9.557886m;

    /// <summary>
    /// Adds every seed row the database does not already have, in dependency order.
    /// </summary>
    public static async Task<SampleSeedReport> SeedMissingAsync(SampleSeedDB db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var tables = new List<SampleSeedTableResult>
        {
            // Authored ids throughout, so these are matched on the id and written with IDENTITY_INSERT.
            await AddMissingByIdAsync(db, "[ShiftIdentity].[Brands]", BrandData(), cancellationToken),
            await AddMissingByIdAsync(db, "[Menu].[ReplacementItem]", ReplacementItemData(), cancellationToken),
            await AddMissingByIdAsync(db, "[Menu].[ServiceIntervalGroup]", ServiceIntervalGroupData(), cancellationToken),
            await AddMissingByIdAsync(db, "[Menu].[ServiceInterval]", ServiceIntervalData(), cancellationToken),
            await AddMissingByIdAsync(db, "[Menu].[ReplacementItemServiceIntervalGroup]", ReplacementItemServiceIntervalGroupData(), cancellationToken),

            // The labour-rate catalogue is NOT completed for an imported one. A (brand, rate) pair with no
            // mapping is supposed to make generation THROW rather than resolve to a code the deployment
            // never issued, so only the seed's own authored ids are filled in — never invented ones.
            await AddMissingByIdAsync(db, "[Menu].[LabourRateMapping]", LabourRateMappingData(), cancellationToken),
            await AddMissingByIdAsync(db, "[Menu].[BrandMapping]", BrandMappingData(), cancellationToken),
        };

        tables.AddRange(await SeedDemoMenuDataAsync(db, cancellationToken));

        return new SampleSeedReport(tables);
    }

    // ---- authored-id tables --------------------------------------------------------------------------

    /// <summary>
    /// Inserts the rows whose authored id is not already taken. A soft-deleted row still owns its id, so
    /// it counts as present — re-inserting over it would be a primary-key violation, and resurrecting it
    /// is not this seeder's call.
    /// </summary>
    private static async Task<SampleSeedTableResult> AddMissingByIdAsync<TEntity>(
        SampleSeedDB db,
        string tableName,
        IEnumerable<TEntity> rows,
        CancellationToken cancellationToken)
        where TEntity : ShiftEntity<TEntity>
    {
        var candidates = rows.ToList();
        var ids = candidates.Select(row => row.ID).ToList();

        var present = await db.Set<TEntity>()
            .AsNoTracking()
            .Where(row => ids.Contains(row.ID))
            .Select(row => row.ID)
            .ToListAsync(cancellationToken);

        var taken = present.ToHashSet();
        var missing = candidates.Where(row => !taken.Contains(row.ID)).ToList();

        if (missing.Count > 0)
            await SaveWithIdentityInsertAsync(db, tableName, missing, cancellationToken);

        return new SampleSeedTableResult(typeof(TEntity).Name, present.Count, missing.Count);
    }

    /// <summary>
    /// Writes rows that carry explicit ids. SQL Server rejects those on an IDENTITY column unless
    /// IDENTITY_INSERT is on, and it can only be on for one table at a time — hence one save per table.
    /// </summary>
    private static async Task SaveWithIdentityInsertAsync<T>(
        SampleSeedDB db, string tableName, IEnumerable<T> entities, CancellationToken cancellationToken)
        where T : class
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON", cancellationToken);
        try
        {
            await db.Set<T>().AddRangeAsync(entities, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF", cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    // ---- the demo menu graph -------------------------------------------------------------------------

    /// <summary>
    /// The four vehicle models, their replacement-item mappings, six menus, nine variants and everything
    /// hanging off them. None of it carries an authored id, so each level is matched on its natural key
    /// and the level below is hung off whatever row that match produced — existing or just inserted.
    /// </summary>
    private static async Task<List<SampleSeedTableResult>> SeedDemoMenuDataAsync(
        SampleSeedDB db, CancellationToken cancellationToken)
    {
        var results = new List<SampleSeedTableResult>();

        // ---- vehicle models -------------------------------------------------------------------------
        var modelsByName = await db.Set<VehicleModel>()
            .Where(model => VehicleModelNames.Contains(model.Name))
            .ToDictionaryAsync(model => model.Name, cancellationToken);

        var newModels = VehicleModelSeeds
            .Where(seed => !modelsByName.ContainsKey(seed.Name))
            .Select(seed => new VehicleModel { Name = seed.Name, BrandID = seed.BrandID, LabourRate = seed.LabourRate })
            .ToList();

        results.Add(await AddAsync(db, newModels, VehicleModelSeeds.Length - newModels.Count, cancellationToken));

        foreach (var model in newModels)
            modelsByName[model.Name] = model;

        VehicleModel Model(string name) => modelsByName[name];

        // ---- per-country labour rates for those models ----------------------------------------------
        var modelIds = modelsByName.Values.Select(model => model.ID).ToList();

        var existingModelRates = (await db.Set<VehicleModelLabourRate>()
                .Where(rate => modelIds.Contains(rate.VehicleModelID))
                .Select(rate => new { rate.VehicleModelID, rate.CountryID })
                .ToListAsync(cancellationToken))
            .Select(rate => (rate.VehicleModelID, rate.CountryID))
            .ToHashSet();

        var newModelRates = VehicleModelSeeds
            .SelectMany(seed => CountryRates(seed.LabourRate).Select(rate => new VehicleModelLabourRate
            {
                VehicleModelID = Model(seed.Name).ID,
                CountryID = rate.CountryID,
                LabourRate = rate.Rate,
            }))
            .Where(rate => !existingModelRates.Contains((rate.VehicleModelID, rate.CountryID)))
            .ToList();

        results.Add(await AddAsync(db, newModelRates, existingModelRates.Count, cancellationToken));

        // ---- per-group labour details for those models ----------------------------------------------
        // The groups are read back rather than assumed: an imported catalogue has its own, and the demo
        // rows have to hang off whatever is actually there.
        var serviceGroupIDs = await db.Set<ServiceIntervalGroup>()
            .Where(group => !group.IsDeleted)
            .OrderBy(group => group.ID)
            .Select(group => group.ID)
            .ToListAsync(cancellationToken);

        var existingModelLabour = (await db.Set<VehicleModelLabourDetails>()
                .Where(labour => modelIds.Contains(labour.VehicleModelID))
                .Select(labour => new { labour.VehicleModelID, labour.ServiceIntervalGroupID })
                .ToListAsync(cancellationToken))
            .Select(labour => (labour.VehicleModelID, labour.ServiceIntervalGroupID))
            .ToHashSet();

        var newModelLabour = VehicleModelSeeds
            .SelectMany(seed => serviceGroupIDs.Select(groupID => new VehicleModelLabourDetails
            {
                VehicleModelID = Model(seed.Name).ID,
                ServiceIntervalGroupID = groupID,
                AllowedTime = groupID <= 3 ? seed.LightAllowedTime : seed.HeavyAllowedTime,
                Consumable = groupID <= 3 ? seed.LightConsumable : seed.HeavyConsumable,
            }))
            .Where(labour => !existingModelLabour.Contains((labour.VehicleModelID, labour.ServiceIntervalGroupID)))
            .ToList();

        results.Add(await AddAsync(db, newModelLabour, existingModelLabour.Count, cancellationToken));

        // ---- replacement items applied to those models ----------------------------------------------
        var linksByKey = await db.Set<ReplacementItemVehicleModel>()
            .Where(link => modelIds.Contains(link.VehicleModelID))
            .ToDictionaryAsync(link => (link.VehicleModelID, link.ReplacementItemID), cancellationToken);

        var newLinks = ReplacementItemVehicleModelSeeds
            .Where(seed => !linksByKey.ContainsKey((Model(seed.Model).ID, seed.ReplacementItemID)))
            .Select(seed => new ReplacementItemVehicleModel
            {
                VehicleModelID = Model(seed.Model).ID,
                ReplacementItemID = seed.ReplacementItemID,
                StandaloneAllowedTime = seed.StandaloneAllowedTime,
                DefaultParts =
                [
                    new ReplacementItemVehicleModelPart
                    {
                        SortOrder = 0,
                        PartNumber = seed.PartNumber,
                        DefaultPeriodicQuantity = 1,
                        DefaultStandaloneQuantity = 1,
                    },
                ],
            })
            .ToList();

        results.Add(await AddAsync(db, newLinks, ReplacementItemVehicleModelSeeds.Length - newLinks.Count, cancellationToken));

        foreach (var link in newLinks)
            linksByKey[(link.VehicleModelID, link.ReplacementItemID)] = link;

        ReplacementItemVehicleModel Link(string modelName, long replacementItemID) =>
            linksByKey[(Model(modelName).ID, replacementItemID)];

        // ---- menus ----------------------------------------------------------------------------------
        var menusByCode = await db.Set<MenuEntity>()
            .Where(menu => DemoBasicModelCodes.Contains(menu.BasicModelCode))
            .ToDictionaryAsync(menu => menu.BasicModelCode, cancellationToken);

        var newMenus = MenuSeeds
            .Where(seed => !menusByCode.ContainsKey(seed.BasicModelCode))
            .Select(seed => new MenuEntity
            {
                BasicModelCode = seed.BasicModelCode,
                VehicleModelID = Model(seed.Model).ID,
                BrandID = seed.BrandID,
            })
            .ToList();

        results.Add(await AddAsync(db, newMenus, MenuSeeds.Length - newMenus.Count, cancellationToken));

        foreach (var menu in newMenus)
            menusByCode[menu.BasicModelCode] = menu;

        // ---- variants -------------------------------------------------------------------------------
        var menuIds = menusByCode.Values.Select(menu => menu.ID).ToList();

        var variantsByKey = await db.Set<MenuVariant>()
            .Where(variant => menuIds.Contains(variant.MenuID))
            .ToDictionaryAsync(variant => (variant.MenuID, variant.Name), cancellationToken);

        var newVariants = VariantSeeds
            .Where(seed => !variantsByKey.ContainsKey((menusByCode[seed.BasicModelCode].ID, seed.Name)))
            .Select(seed => new MenuVariant
            {
                MenuID = menusByCode[seed.BasicModelCode].ID,
                Name = seed.Name,
                MenuPrefix = Loc(seed.MenuPrefix),
                MenuPostfix = seed.MenuPostfix is null ? null : Loc(seed.MenuPostfix),
                StandaloneMenuPrefix = seed.StandaloneMenuPrefix is null ? null : Loc(seed.StandaloneMenuPrefix),
                StandaloneMenuPostfix = seed.StandaloneMenuPostfix is null ? null : Loc(seed.StandaloneMenuPostfix),
                LabourRate = seed.LabourRate,
                DiscountPercentage = seed.DiscountPercentage,
                HasStandaloneItems = seed.HasStandaloneItems,
            })
            .ToList();

        results.Add(await AddAsync(db, newVariants, VariantSeeds.Length - newVariants.Count, cancellationToken));

        foreach (var variant in newVariants)
            variantsByKey[(variant.MenuID, variant.Name)] = variant;

        MenuVariant Variant(VariantSeed seed) => variantsByKey[(menusByCode[seed.BasicModelCode].ID, seed.Name)];

        var variantIds = VariantSeeds.Select(seed => Variant(seed).ID).ToList();

        // ---- per-country labour rates for those variants --------------------------------------------
        var existingVariantRates = (await db.Set<MenuVariantLabourRate>()
                .Where(rate => variantIds.Contains(rate.MenuVariantID))
                .Select(rate => new { rate.MenuVariantID, rate.CountryID })
                .ToListAsync(cancellationToken))
            .Select(rate => (rate.MenuVariantID, rate.CountryID))
            .ToHashSet();

        var newVariantRates = VariantSeeds
            .SelectMany(seed => CountryRates(seed.LabourRate).Select(rate => new MenuVariantLabourRate
            {
                MenuVariantID = Variant(seed).ID,
                CountryID = rate.CountryID,
                LabourRate = rate.Rate,
            }))
            .Where(rate => !existingVariantRates.Contains((rate.MenuVariantID, rate.CountryID)))
            .ToList();

        results.Add(await AddAsync(db, newVariantRates, existingVariantRates.Count, cancellationToken));

        // ---- labour details and periodic availability per variant -----------------------------------
        var existingVariantLabour = (await db.Set<MenuLabourDetails>()
                .Where(labour => variantIds.Contains(labour.MenuVariantID))
                .Select(labour => new { labour.MenuVariantID, labour.ServiceIntervalGroupID })
                .ToListAsync(cancellationToken))
            .Select(labour => (labour.MenuVariantID, labour.ServiceIntervalGroupID))
            .ToHashSet();

        var newVariantLabour = VariantSeeds
            .SelectMany(seed => serviceGroupIDs.Select(groupID => new MenuLabourDetails
            {
                MenuVariantID = Variant(seed).ID,
                ServiceIntervalGroupID = groupID,
                AllowedTime = seed.LabourRate > 50 ? 1.4m : 1.2m,
                Consumable = seed.LabourRate > 50 ? 18 : 14,
            }))
            .Where(labour => !existingVariantLabour.Contains((labour.MenuVariantID, labour.ServiceIntervalGroupID)))
            .ToList();

        results.Add(await AddAsync(db, newVariantLabour, existingVariantLabour.Count, cancellationToken));

        var demoIntervalIDs = await db.Set<ServiceInterval>()
            .Where(interval => !interval.IsDeleted && DemoServiceIntervalIDs.Contains(interval.ID))
            .Select(interval => interval.ID)
            .ToListAsync(cancellationToken);

        var existingPeriods = (await db.Set<MenuPeriodicAvailability>()
                .Where(period => variantIds.Contains(period.MenuVariantID))
                .Select(period => new { period.MenuVariantID, period.ServiceIntervalID })
                .ToListAsync(cancellationToken))
            .Select(period => (period.MenuVariantID, period.ServiceIntervalID))
            .ToHashSet();

        var newPeriods = VariantSeeds
            .SelectMany(seed => demoIntervalIDs.Select(intervalID => new MenuPeriodicAvailability
            {
                MenuVariantID = Variant(seed).ID,
                ServiceIntervalID = intervalID,
            }))
            .Where(period => !existingPeriods.Contains((period.MenuVariantID, period.ServiceIntervalID)))
            .ToList();

        results.Add(await AddAsync(db, newPeriods, existingPeriods.Count, cancellationToken));

        // ---- menu items, with their parts and country prices ----------------------------------------
        var existingItems = (await db.Set<MenuItem>()
                .Where(item => variantIds.Contains(item.MenuVariantID))
                .Select(item => new { item.MenuVariantID, item.ReplacementItemVehicleModelID })
                .ToListAsync(cancellationToken))
            .Select(item => (item.MenuVariantID, item.ReplacementItemVehicleModelID))
            .ToHashSet();

        var newItems = new List<MenuItem>();

        foreach (var seed in ItemSeeds)
        {
            var variantSeed = VariantSeeds.Single(v => v.BasicModelCode == seed.BasicModelCode && v.Name == seed.VariantName);
            var variant = Variant(variantSeed);
            var link = Link(MenuSeeds.Single(menu => menu.BasicModelCode == seed.BasicModelCode).Model, seed.ReplacementItemID);

            if (existingItems.Contains((variant.ID, link.ID)))
                continue;

            newItems.Add(new MenuItem
            {
                MenuVariantID = variant.ID,
                ReplacementItemVehicleModelID = link.ID,
                StandaloneAllowedTime = seed.StandaloneAllowedTime,
                Parts = seed.Parts.Select((part, index) => new MenuItemPart
                {
                    SortOrder = index,
                    PartNumber = part.PartNumber,
                    PeriodicQuantity = 1,

                    // Null for a variant that sells nothing standalone — the source's own shape, and what
                    // makes the demo cover the "periodic only" case.
                    StandaloneQuantity = variantSeed.HasStandaloneItems ? 1 : null,
                    CountryPrices = CountryPartPrices(part.Price),
                }).ToList(),
            });
        }

        results.Add(await AddAsync(db, newItems, ItemSeeds.Length - newItems.Count, cancellationToken));

        return results;
    }

    /// <summary>Adds rows whose ids come from IDENTITY, and reports how many. A no-op for an empty set.</summary>
    /// <param name="alreadyPresent">
    /// Seed rows the caller's natural-key match found already there — reported, never touched.
    /// </param>
    private static async Task<SampleSeedTableResult> AddAsync<TEntity>(
        SampleSeedDB db, List<TEntity> rows, int alreadyPresent, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (rows.Count > 0)
        {
            await db.Set<TEntity>().AddRangeAsync(rows, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new SampleSeedTableResult(typeof(TEntity).Name, alreadyPresent, rows.Count);
    }

    // ---- the demo graph, as data ---------------------------------------------------------------------

    private sealed record VehicleModelSeed(
        string Name,
        long BrandID,
        decimal LabourRate,
        decimal LightAllowedTime,
        decimal HeavyAllowedTime,
        decimal LightConsumable,
        decimal HeavyConsumable);

    private sealed record ReplacementItemVehicleModelSeed(
        string Model, long ReplacementItemID, decimal StandaloneAllowedTime, string PartNumber);

    private sealed record MenuSeed(string BasicModelCode, string Model, long BrandID);

    private sealed record VariantSeed(
        string BasicModelCode,
        string Name,
        string MenuPrefix,
        string? MenuPostfix,
        string? StandaloneMenuPrefix,
        string? StandaloneMenuPostfix,
        decimal LabourRate,
        decimal? DiscountPercentage,
        bool HasStandaloneItems);

    private sealed record ItemPartSeed(string PartNumber, decimal Price);

    private sealed record ItemSeed(
        string BasicModelCode,
        string VariantName,
        long ReplacementItemID,
        decimal StandaloneAllowedTime,
        ItemPartSeed[] Parts);

    private const string CorollaCross = "Corolla Cross Hybrid 2.0L";
    private const string LexusES = "Lexus ES 300h 2.5L";
    private const string Camry = "Camry Hybrid 2.5L";
    private const string LexusRX = "Lexus RX 350 2.4T";

    private static readonly VehicleModelSeed[] VehicleModelSeeds =
    [
        new(CorollaCross, ToyotaBrandID, 45, 1.1m, 1.6m, 12m, 16m),
        new(LexusES, LexusBrandID, 62, 1.3m, 1.8m, 15m, 20m),
        new(Camry, ToyotaBrandID, 47, 1.2m, 1.7m, 13m, 17m),
        new(LexusRX, LexusBrandID, 68, 1.4m, 1.9m, 16m, 22m),
    ];

    private static readonly string[] VehicleModelNames =
        VehicleModelSeeds.Select(seed => seed.Name).ToArray();

    private static readonly ReplacementItemVehicleModelSeed[] ReplacementItemVehicleModelSeeds =
    [
        new(CorollaCross, 1, 0.30m, "0262986102"),
        new(CorollaCross, 2, 0.10m, "0263086102"),
        new(CorollaCross, 3, 0.20m, "040000010C"),
        new(CorollaCross, 5, 0.25m, "040000020C"),
        new(CorollaCross, 12, 0.35m, "0400002212"),
        new(CorollaCross, 32, 0.10m, "0400002230"),
        new(CorollaCross, 39, 0.25m, "0400002312"),

        new(LexusES, 1, 0.35m, "0262986102"),
        new(LexusES, 2, 0.12m, "0263086102"),
        new(LexusES, 3, 0.22m, "040000010C"),
        new(LexusES, 5, 0.30m, "0400003152"),
        new(LexusES, 12, 0.40m, "0400002212"),
        new(LexusES, 32, 0.10m, "0400002230"),
        new(LexusES, 39, 0.28m, "0400002312"),

        new(Camry, 1, 0.30m, "0262986102"),
        new(Camry, 3, 0.20m, "040000010C"),
        new(Camry, 12, 0.35m, "0400002212"),

        new(LexusRX, 1, 0.38m, "0262986102"),
        new(LexusRX, 3, 0.24m, "040000010C"),
        new(LexusRX, 32, 0.12m, "0400002230"),
    ];

    private static readonly MenuSeed[] MenuSeeds =
    [
        new("MZEA10", CorollaCross, ToyotaBrandID),
        new("MZEA11", CorollaCross, ToyotaBrandID),
        new("AXZH10", LexusES, LexusBrandID),
        new("AXZH11", LexusES, LexusBrandID),
        new("AXVA70", Camry, ToyotaBrandID),
        new("AALH10", LexusRX, LexusBrandID),
    ];

    /// <summary>
    /// The demo menus this seeds, by basic model code — what a lookup can be pointed at afterwards.
    ///
    /// Declared here rather than up with the rest of the public surface because static field
    /// initializers run in TEXTUAL order: above <see cref="MenuSeeds"/> this would read a null array.
    /// </summary>
    public static IReadOnlyList<string> DemoBasicModelCodes { get; } =
        MenuSeeds.Select(menu => menu.BasicModelCode).ToList();

    private static readonly VariantSeed[] VariantSeeds =
    [
        new("MZEA10", "V1", "SER", "A", "STD", "A", 45, 5, true),
        new("MZEA10", "V2", "SER", "P", "STD", "P", 45, 7.5m, true),
        new("MZEA10", "V3", "MNT", "X", "STD", "X", 47, 4, true),

        // No standalone items at all — the "periodic only" case, and the reason its items carry a null
        // standalone quantity.
        new("AXZH10", "V1", "PMS", null, null, null, 62, 3, false),
        new("AXZH10", "V2", "PMS", "L", "STD", "L", 64, 2.5m, true),

        new("MZEA11", "V1", "SRV", "B", "STD", "B", 46, 6, true),
        new("AXZH11", "V1", "PRE", "R", "STD", "R", 63, 2, true),
        new("AXVA70", "V1", "CMP", "H", "STD", "H", 47, 5, true),
        new("AALH10", "V1", "RXS", "T", "STD", "T", 68, 3, true),
    ];

    /// <summary>The intervals the demo variants are periodically available for.</summary>
    private static readonly long[] DemoServiceIntervalIDs = [5, 10, 20, 40];

    private static readonly ItemSeed[] ItemSeeds =
    [
        // MZEA10 V1 — the only item with two parts, which is what makes part-number reuse visible.
        new("MZEA10", "V1", 1, 0.30m, [new("0262986102", 32), new("0888083806", 31)]),
        new("MZEA10", "V1", 2, 0.10m, [new("0263086102", 4)]),
        new("MZEA10", "V1", 3, 0.20m, [new("040000010C", 10)]),
        new("MZEA10", "V1", 5, 0.25m, [new("040000020C", 18)]),
        new("MZEA10", "V1", 12, 0.35m, [new("0400002212", 14)]),
        new("MZEA10", "V1", 32, 0.10m, [new("0400002230", 6)]),
        new("MZEA10", "V1", 39, 0.25m, [new("0400002312", 20)]),

        // MZEA10 V2 — reuses the same part numbers at different prices.
        new("MZEA10", "V2", 1, 0.30m, [new("0262986102", 33)]),
        new("MZEA10", "V2", 3, 0.20m, [new("040000010C", 11)]),
        new("MZEA10", "V2", 5, 0.25m, [new("0400003152", 19)]),
        new("MZEA10", "V2", 12, 0.35m, [new("0400002212", 14.5m)]),
        new("MZEA10", "V2", 32, 0.10m, [new("0400002230", 6)]),

        new("MZEA10", "V3", 1, 0.30m, [new("0262986102", 34)]),
        new("MZEA10", "V3", 2, 0.10m, [new("0263086102", 4.2m)]),
        new("MZEA10", "V3", 3, 0.20m, [new("040000010C", 11.5m)]),
        new("MZEA10", "V3", 39, 0.25m, [new("0400002312", 21.5m)]),

        // AXZH10 V1 — same part numbers again, on another model and brand.
        new("AXZH10", "V1", 1, 0.35m, [new("0262986102", 35)]),
        new("AXZH10", "V1", 2, 0.12m, [new("0263086102", 4.5m)]),
        new("AXZH10", "V1", 3, 0.22m, [new("040000010C", 12)]),
        new("AXZH10", "V1", 5, 0.30m, [new("0400003152", 21)]),
        new("AXZH10", "V1", 12, 0.40m, [new("0400002212", 15.5m)]),
        new("AXZH10", "V1", 39, 0.28m, [new("0400002312", 23)]),

        new("AXZH10", "V2", 1, 0.35m, [new("0262986102", 36)]),
        new("AXZH10", "V2", 3, 0.22m, [new("040000010C", 12.5m)]),
        new("AXZH10", "V2", 5, 0.30m, [new("0400003152", 22)]),
        new("AXZH10", "V2", 32, 0.10m, [new("0400002230", 6.5m)]),

        new("MZEA11", "V1", 1, 0.30m, [new("0262986102", 33.5m)]),
        new("MZEA11", "V1", 3, 0.20m, [new("040000010C", 11.25m)]),
        new("MZEA11", "V1", 12, 0.35m, [new("0400002212", 14.2m)]),

        new("AXZH11", "V1", 1, 0.35m, [new("0262986102", 36.8m)]),
        new("AXZH11", "V1", 3, 0.22m, [new("040000010C", 12.8m)]),
        new("AXZH11", "V1", 32, 0.10m, [new("0400002230", 6.8m)]),

        new("AXVA70", "V1", 1, 0.30m, [new("0262986102", 34m)]),
        new("AXVA70", "V1", 3, 0.20m, [new("040000010C", 11.7m)]),
        new("AXVA70", "V1", 12, 0.35m, [new("0400002212", 14.4m)]),

        new("AALH10", "V1", 1, 0.38m, [new("0262986102", 38m)]),
        new("AALH10", "V1", 3, 0.24m, [new("040000010C", 13.2m)]),
        new("AALH10", "V1", 32, 0.12m, [new("0400002230", 7m)]),
    ];

    // ---- shared helpers ------------------------------------------------------------------------------

    /// <summary>
    /// Multi-language seed helper: a JSON object keyed by the 2-letter codes the sample is configured
    /// for. Each non-English value is the English one with a culture suffix, so a language switch is
    /// visible at a glance.
    /// </summary>
    private static string Loc(string en) => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["en"] = en,
        ["ru"] = $"{en}-RU",
        ["uz"] = $"{en}-UZ",
        ["tg"] = $"{en}-TG",
        ["tk"] = $"{en}-TM",
    });

    private static IEnumerable<(long CountryID, decimal Rate)> CountryRates(decimal primaryRate)
    {
        yield return (Uzbekistan, Math.Round(primaryRate * UsdToUzsRate, 2));
        yield return (Turkmenistan, Math.Round(primaryRate * UsdToTmtRate, 2));
        yield return (Tajikistan, Math.Round(primaryRate * UsdToTjsRate, 2));
    }

    private static List<MenuItemPartCountryPrice> CountryPartPrices(decimal primaryPrice) =>
        CountryRates(primaryPrice)
            .Select(price => new MenuItemPartCountryPrice
            {
                CountryID = price.CountryID,
                PartPrice = price.Rate,
                PartPriceMarginPercentage = 0,
                PartFinalPrice = price.Rate,
            })
            .ToList();

    // ---- the catalogue, verbatim ---------------------------------------------------------------------

    private static IEnumerable<Brand> BrandData() =>
    [
        new Brand { ID = 1, Name = "Hino" },
        new Brand { ID = 2, Name = "Toyota" },
        new Brand { ID = 3, Name = "Lexus" },
    ];

    private static IEnumerable<BrandMapping> BrandMappingData() =>
    [
        new BrandMapping(1) { BrandID = ToyotaBrandID, Code = "00", BrandAbbreviation = "T" },
        new BrandMapping(2) { BrandID = LexusBrandID, Code = "11", BrandAbbreviation = "L" },
    ];

    private static IEnumerable<LabourRateMapping> LabourRateMappingData() =>
    [
        new LabourRateMapping(2) { Code = "A", LabourRate = 44, BrandID = ToyotaBrandID },
        new LabourRateMapping(3) { Code = "A", LabourRate = 65, BrandID = LexusBrandID },
        new LabourRateMapping(6) { Code = "B", LabourRate = 40, BrandID = ToyotaBrandID },
        new LabourRateMapping(7) { Code = "B", LabourRate = 45, BrandID = LexusBrandID },
        new LabourRateMapping(8) { Code = "B2", LabourRate = 35, BrandID = ToyotaBrandID },
        new LabourRateMapping(10) { Code = "C", LabourRate = 2, BrandID = LexusBrandID },
        new LabourRateMapping(11) { Code = "C", LabourRate = 2, BrandID = ToyotaBrandID },
        new LabourRateMapping(12) { Code = "D", LabourRate = 50, BrandID = LexusBrandID },
        new LabourRateMapping(13) { Code = "D", LabourRate = 50, BrandID = ToyotaBrandID },
        new LabourRateMapping(16) { Code = "G", LabourRate = 32, BrandID = ToyotaBrandID },
        new LabourRateMapping(17) { Code = "G", LabourRate = 48, BrandID = LexusBrandID },
        new LabourRateMapping(18) { Code = "H", LabourRate = 25, BrandID = LexusBrandID },
        new LabourRateMapping(19) { Code = "H", LabourRate = 25, BrandID = ToyotaBrandID },
        new LabourRateMapping(21) { Code = "I", LabourRate = 15, BrandID = LexusBrandID },
        new LabourRateMapping(22) { Code = "I", LabourRate = 15, BrandID = ToyotaBrandID },
        new LabourRateMapping(23) { Code = "J", LabourRate = 100, BrandID = LexusBrandID },
        new LabourRateMapping(24) { Code = "J", LabourRate = 100, BrandID = ToyotaBrandID },
        new LabourRateMapping(26) { Code = "K", LabourRate = 26, BrandID = LexusBrandID },
        new LabourRateMapping(27) { Code = "K", LabourRate = 26, BrandID = ToyotaBrandID },
        new LabourRateMapping(29) { Code = "L", LabourRate = 29, BrandID = ToyotaBrandID },
        new LabourRateMapping(31) { Code = "M", LabourRate = 32, BrandID = LexusBrandID },
        new LabourRateMapping(34) { Code = "S", LabourRate = 1, BrandID = LexusBrandID },
        new LabourRateMapping(35) { Code = "S", LabourRate = 1, BrandID = ToyotaBrandID },
        new LabourRateMapping(36) { Code = "U", LabourRate = 45, BrandID = ToyotaBrandID },
        new LabourRateMapping(38) { Code = "V", LabourRate = 60, BrandID = LexusBrandID },
        new LabourRateMapping(40) { Code = "W", LabourRate = 60, BrandID = ToyotaBrandID },
        new LabourRateMapping(41) { Code = "W", LabourRate = 80, BrandID = LexusBrandID },
        new LabourRateMapping(42) { Code = "W1", LabourRate = 24, BrandID = ToyotaBrandID },
        new LabourRateMapping(43) { Code = "W2", LabourRate = 25.92M, BrandID = ToyotaBrandID },
        new LabourRateMapping(44) { Code = "W3", LabourRate = 27.58M, BrandID = ToyotaBrandID },

        // The pairs the demo variants above resolve through. Without these the generator throws on them,
        // which is the correct behaviour for an unmapped pair and useless as a demo.
        new LabourRateMapping(45) { Code = "U1", LabourRate = 46, BrandID = ToyotaBrandID },
        new LabourRateMapping(46) { Code = "U2", LabourRate = 47, BrandID = ToyotaBrandID },
        new LabourRateMapping(47) { Code = "V1", LabourRate = 62, BrandID = LexusBrandID },
        new LabourRateMapping(48) { Code = "V2", LabourRate = 63, BrandID = LexusBrandID },
        new LabourRateMapping(49) { Code = "V3", LabourRate = 64, BrandID = LexusBrandID },
        new LabourRateMapping(50) { Code = "V4", LabourRate = 68, BrandID = LexusBrandID },
    ];

    private static IEnumerable<ServiceIntervalGroup> ServiceIntervalGroupData() =>
    [
        new ServiceIntervalGroup(1) { Name = "ServiceIntervals that ends with five", LabourCode = "0B", LabourDescription = "PM SUPER LIGHT SERVICE" },
        new ServiceIntervalGroup(2) { Name = "Twenty step sequence start at 10K", LabourCode = "0C", LabourDescription = "PM LIGHT SERVICE" },
        new ServiceIntervalGroup(3) { Name = "20K, 100K, 140K", LabourCode = "0D", LabourDescription = "PM MEDIUM SERVICE" },
        new ServiceIntervalGroup(4) { Name = "40K, 200K", LabourCode = "0E", LabourDescription = "PM HEAVY SERVICE" },
        new ServiceIntervalGroup(5) { Name = "60K, 180K", LabourCode = "0D", LabourDescription = "PM MEDIUM SERVICE" },
        new ServiceIntervalGroup(6) { Name = "80K, 160K", LabourCode = "0E", LabourDescription = "PM HEAVY SERVICE" },
        new ServiceIntervalGroup(7) { Name = "120K", LabourCode = "0E", LabourDescription = "PM HEAVY SERVICE" },
    ];

    private static IEnumerable<ServiceInterval> ServiceIntervalData()
    {
        for (var i = 5; i <= 200; i += 5)
        {
            var fullName = $"{(i * 1000).ToString("N0")} KM";

            var serviceInterval = new ServiceInterval(i)
            {
                Code = $"{i}K",
                FullName = fullName,
                ValueInMeter = i * 1000,
                Description = $"CARRY OUT {fullName} SERVICE",
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

            yield return serviceInterval;
        }
    }

    private static IEnumerable<ADP.Menus.Data.Entities.ReplacementItem> ReplacementItemData() =>
    [
        new(1) { Name = "Engine Oil", FriendlyName = "Engine Oil", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "EO", StandaloneOperationCode = Loc("EO"), AllowMultiplePartNumbers = true },
        new(2) { Name = "Drain Plug", FriendlyName = "Drain Plug", Type = ReplacementItemType.Component, StandaloneLabourCode = "DP", StandaloneOperationCode = Loc("DP") },
        new(3) { Name = "Oil Filter", FriendlyName = "Oil Filter", Type = ReplacementItemType.Component, StandaloneLabourCode = "OF", StandaloneOperationCode = Loc("OF") },
        new(4) { Name = "Air Cleaner Element", FriendlyName = "Air Cleaner Element", Type = ReplacementItemType.Component, StandaloneLabourCode = "ACE", StandaloneOperationCode = Loc("ACE") },
        new(5) { Name = "A/C Filter", FriendlyName = "A/C Filter", Type = ReplacementItemType.Component, StandaloneLabourCode = "ACF", StandaloneOperationCode = Loc("ACF") },
        new(6) { Name = "Fuel Filter", FriendlyName = "Fuel Filter", Type = ReplacementItemType.Component, StandaloneLabourCode = "FF", StandaloneOperationCode = Loc("FF") },
        new(7) { Name = "Spark Plug", FriendlyName = "Spark Plug", Type = ReplacementItemType.Component, StandaloneLabourCode = "SP", StandaloneOperationCode = Loc("SP") },
        new(8) { Name = "MT Fluid (75W90)", FriendlyName = "MT Fluid (75W90)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "MTF", StandaloneOperationCode = Loc("MTF") },
        new(9) { Name = "LSD Differential Oil RR (85W90) \"GL-5\"", FriendlyName = "LSD Differential Oil RR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "LDO", StandaloneOperationCode = Loc("LDO") },
        new(10) { Name = "Differential Oil FR (85W90) \"GL-5\"", FriendlyName = "Differential Oil FR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "DOF", StandaloneOperationCode = Loc("DOF") },
        new(11) { Name = "Differential Oil RR (85W90) \"GL-5\"", FriendlyName = "Differential Oil RR (85W90) \"GL-5\"", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "DOR", StandaloneOperationCode = Loc("DOR") },
        new(12) { Name = "Brake Fluid DOT 3 (330 ML)", FriendlyName = "Brake Fluid DOT 3 (330 ML)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "BF", StandaloneOperationCode = Loc("BF") },
        new(13) { Name = "AT Fluid WS (4Ltr)", FriendlyName = "AT Fluid WS (4Ltr)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "ATF", StandaloneOperationCode = Loc("ATF") },
        new(14) { Name = "AT Fluid WS (1Ltr)", FriendlyName = "AT Fluid WS (1Ltr)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "ATF", StandaloneOperationCode = Loc("ATF") },
        new(15) { Name = "ATF Strainer", FriendlyName = "ATF Strainer", Type = ReplacementItemType.Component, StandaloneLabourCode = "AS", StandaloneOperationCode = Loc("AS") },
        new(16) { Name = "ATF Strainer Ring", FriendlyName = "ATF Strainer Ring", Type = ReplacementItemType.Component, StandaloneLabourCode = "ASR", StandaloneOperationCode = Loc("ASR") },
        new(17) { Name = "ATF Pan Gasket", FriendlyName = "ATF Pan Gasket", Type = ReplacementItemType.Component, StandaloneLabourCode = "APG", StandaloneOperationCode = Loc("APG") },
        new(18) { Name = "CVT Fluid FE", FriendlyName = "CVT Fluid FE", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "CFF", StandaloneOperationCode = Loc("CFF") },
        new(19) { Name = "CVT Strainer", FriendlyName = "CVT Strainer", Type = ReplacementItemType.Component, StandaloneLabourCode = "CS", StandaloneOperationCode = Loc("CS") },
        new(20) { Name = "CVT Strainer O Ring", FriendlyName = "CVT Strainer O Ring", Type = ReplacementItemType.Component, StandaloneLabourCode = "CSO", StandaloneOperationCode = Loc("CSO") },
        new(21) { Name = "CVT Pan Gasket", FriendlyName = "CVT Pan Gasket", Type = ReplacementItemType.Component, StandaloneLabourCode = "CPG", StandaloneOperationCode = Loc("CPG") },
        new(22) { Name = "Transfer Fluid (75W90)", FriendlyName = "Transfer Fluid (75W90)", Type = ReplacementItemType.Lubricant, StandaloneLabourCode = "TF", StandaloneOperationCode = Loc("TF") },
        new(23) { Name = "Gasket Plug ATM Filler", FriendlyName = "Gasket Plug ATM Filler", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPA", StandaloneOperationCode = Loc("GPA") },
        new(24) { Name = "Gasket Plug ATM Drain", FriendlyName = "Gasket Plug ATM Drain", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPD", StandaloneOperationCode = Loc("GPD") },
        new(25) { Name = "Gasket Plug MTM", FriendlyName = "Gasket Plug MTM", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPM", StandaloneOperationCode = Loc("GPM") },
        new(26) { Name = "Gasket Plug FR Diff Darin", FriendlyName = "Gasket Plug FR Diff Darin", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPF", StandaloneOperationCode = Loc("GPF") },
        new(27) { Name = "Gasket Plug FR Diff Filler", FriendlyName = "Gasket Plug FR Diff Filler", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPF", StandaloneOperationCode = Loc("GPF") },
        new(28) { Name = "Gasket Plug RR Diff", FriendlyName = "Gasket Plug RR Diff", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPR", StandaloneOperationCode = Loc("GPR") },
        new(29) { Name = "Gasket Plug Transfer", FriendlyName = "Gasket Plug Transfer", Type = ReplacementItemType.Component, StandaloneLabourCode = "GPT", StandaloneOperationCode = Loc("GPT") },
        new(30) { Name = "FR Brake Pad", FriendlyName = "FR Brake Pad", Type = ReplacementItemType.Component, StandaloneLabourCode = "FBP", StandaloneOperationCode = Loc("FBP") },
        new(31) { Name = "RR Brake Pad / Shoe", FriendlyName = "RR Brake Pad / Shoe", Type = ReplacementItemType.Component, StandaloneLabourCode = "RBP", StandaloneOperationCode = Loc("RBP") },
        new(32) { Name = "Screen Washer", FriendlyName = "Screen Washer", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode = "SW", StandaloneOperationCode = Loc("SW") },
        new(33) { Name = "BG 44K Fuel System Cleaner", FriendlyName = "BG 44K Fuel System Cleaner", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode = "B4F", StandaloneOperationCode = Loc("B4F") },
        new(34) { Name = "BG EPR", FriendlyName = "BG EPR", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode = "BE", StandaloneOperationCode = Loc("BE") },
        new(35) { Name = "BG EFI System", FriendlyName = "BG EFI System", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode = "BES", StandaloneOperationCode = Loc("BES") },
        new(36) { Name = "Brake Cleaner", FriendlyName = "Brake Cleaner", Type = ReplacementItemType.ValueAdded, StandaloneLabourCode = "BC", StandaloneOperationCode = Loc("BC") },
        new(37) { Name = "FR Brake Disc", FriendlyName = "FR Brake Disc", Type = ReplacementItemType.Component, StandaloneLabourCode = "FBD", StandaloneOperationCode = Loc("FBD") },
        new(38) { Name = "RR Brake Disc", FriendlyName = "RR Brake Disc", Type = ReplacementItemType.Component, StandaloneLabourCode = "RBD", StandaloneOperationCode = Loc("RBD") },
        new(39) { Name = "HV Battery Filter", FriendlyName = "HV Battery Filter", Type = ReplacementItemType.Component, StandaloneLabourCode = "HBF", StandaloneOperationCode = Loc("HBF") },
        new(40) { Name = "Battery", FriendlyName = "Battery", Type = ReplacementItemType.Component, StandaloneLabourCode = "B", StandaloneOperationCode = Loc("B") },
        new(41) { Name = "Tires", FriendlyName = "Tires", Type = ReplacementItemType.Component, StandaloneLabourCode = "T", StandaloneOperationCode = Loc("T") },
        new(42) { Name = "Wiper Rubber LHS", FriendlyName = "Wiper Rubber LHS", Type = ReplacementItemType.Component, StandaloneLabourCode = "WRL", StandaloneOperationCode = Loc("WRL") },
        new(43) { Name = "Wiper Rubber RHS", FriendlyName = "Wiper Rubber RHS", Type = ReplacementItemType.Component, StandaloneLabourCode = "WRR", StandaloneOperationCode = Loc("WRR") },
        new(44) { Name = "RR Wiper Rubber", FriendlyName = "RR Wiper Rubber", Type = ReplacementItemType.Component, StandaloneLabourCode = "RWR", StandaloneOperationCode = Loc("RWR") },
    ];

    /// <summary>
    /// Which interval groups each replacement item serves — the links that decide whose parts join a
    /// periodic line. Authored ids, so they are matched and inserted like the rest of the catalogue.
    /// </summary>
    private static IEnumerable<ReplacementItemServiceIntervalGroup> ReplacementItemServiceIntervalGroupData()
    {
        // (replacement item, the groups it serves), flattened to rows with the original authored ids.
        (long Item, long[] Groups)[] links =
        [
            (1, [1, 2, 3, 4, 5, 6, 7]),
            (2, [1, 2, 3, 4, 5, 6, 7]),
            (3, [1, 2, 3, 4, 5, 6, 7]),
            (4, [4, 6, 7]),
            (5, [3, 4, 5, 6, 7]),
            (6, [4, 6, 7]),
            (7, [6]),
            (8, [4, 6, 7]),
            (9, [4, 6, 7]),
            (10, [4, 6, 7]),
            (11, [4, 6, 7]),
            (12, [4, 6, 7]),
            (13, [5, 7]),
            (14, [5, 7]),
            (15, [5, 7]),
            (16, [5, 7]),
            (17, [5, 7]),
            (18, [5, 7]),
            (19, [5, 7]),
            (20, [5, 7]),
            (21, [5, 7]),
            (22, [4, 6, 7]),
            (23, [5, 7]),
            (24, [5, 7]),
            (25, [4, 6, 7]),
            (26, [4, 6, 7]),
            (28, [4, 6, 7]),
            (29, [4, 6, 7]),
            (32, [2, 3, 4, 5, 6, 7]),
            (33, [2, 3, 4, 5, 6, 7]),
            (36, [2, 3, 4, 5, 6, 7]),
        ];

        long id = 0;

        foreach (var (item, groups) in links)
            foreach (var group in groups)
                yield return new ReplacementItemServiceIntervalGroup(++id)
                {
                    ReplacementItemID = item,
                    ServiceIntervalGroupID = group,
                };
    }
}

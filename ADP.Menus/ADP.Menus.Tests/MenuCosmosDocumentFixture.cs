using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Sync.Replication;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Shared.Enums;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Turns the shared <see cref="MenuGraphFixture"/> EF graph into the Cosmos documents replication would
/// write for it — the input the vehicle lookup reads.
///
/// <para><b>It goes through the PRODUCTION mappers on purpose</b>, unlike
/// <see cref="MenuGenerationRequestFixture"/>, which is hand-built to stay independent of the code it
/// checks. The two fixtures answer different questions. The hand-built one asks "is the generator a
/// faithful port?", where an independent second opinion is the whole value. This one asks "does the
/// replicate-then-read ROUND TRIP preserve the export's menu codes?" — and there the mappers are part
/// of the round trip being tested, so using them is not cheating, it is the point. Hand-authoring the
/// documents here would test a document shape nothing writes.</para>
///
/// <para>What that leaves untested is Cosmos itself: serialization, the partition query and the
/// container. Those are exercised by <c>ServiceMenusProvisioningTests</c> against a real emulator.
/// Everything between the EF row and the generated line is covered here, offline.</para>
/// </summary>
internal static class MenuCosmosDocumentFixture
{
    /// <param name="brandId">Use <see cref="MenuGraphFixture.UnmappedBrandId"/> for the "Z" fallback.</param>
    /// <param name="includeBrandMapping">False drops the brand-mapping row.</param>
    /// <param name="includeLabourRateMapping">False drops the labour-rate-mapping row.</param>
    internal static ServiceMenuDocuments Build(
        long brandId = MenuGraphFixture.BrandId,
        bool includeBrandMapping = true,
        bool includeLabourRateMapping = true)
    {
        var fixture = MenuGraphFixture.Build(brandId, includeBrandMapping, includeLabourRateMapping);
        return From(fixture);
    }

    /// <summary>Projects an already-built graph, for tests that mutate the graph first.</summary>
    internal static ServiceMenuDocuments From(MenuGraphFixture.Fixture fixture)
    {
        // What the replication maps through in a host that registered no mapper: ADP.Menus.Data's own generated
        // mapper, one per projection — the tests using this fixture run in parallel, and a mapper is not shared
        // across threads.
        var mapper = MenuCosmosDocuments.ResolveMapper(new ServiceCollection().BuildServiceProvider());

        var documents = new ServiceMenuDocuments
        {
            BasicModelCode = fixture.Variants.FirstOrDefault()?.Menu?.BasicModelCode,
        };

        // The catalogues are searched with the SAME selection rule the trigger and the catch-up sweep
        // use, so a change to "which mapping does a variant embed" reaches this fixture automatically.
        var labourRateMappings = fixture.LabourRateMappings.Values.AsQueryable();
        var brandMappings = fixture.BrandMappings.Values.AsQueryable();

        foreach (var variant in fixture.Variants)
        {
            var brandId = variant.Menu?.VehicleModel?.BrandID;

            documents.Variants.Add(MenuCosmosDocuments.Variant(
                mapper,
                variant,
                MenuReplicationReload.SelectLabourRateMapping(labourRateMappings, brandId, variant.LabourRate),
                MenuReplicationReload.SelectBrandMapping(brandMappings, brandId)));

            foreach (var period in variant.PeriodicAvailabilities)
                documents.Periods.Add(MenuCosmosDocuments.Period(mapper, period));

            foreach (var labour in variant.LabourDetails)
                documents.Labours.Add(MenuCosmosDocuments.Labour(mapper, labour));

            foreach (var item in variant.Items)
                documents.Items.Add(MenuCosmosDocuments.Item(mapper, item));
        }

        return documents;
    }

    /// <summary>The fixture's own variant id — left exactly as authored, so <see cref="MenuType.Paid"/>.</summary>
    internal const long PaidVariantID = 4471;

    /// <summary>The <see cref="MenuType.Free"/> re-keyed copy's variant id in <see cref="WithEveryMenuType"/>.</summary>
    internal const long FreeVariantID = 4472;

    /// <summary>The <see cref="MenuType.Contract"/> re-keyed copy's variant id in <see cref="WithEveryMenuType"/>.</summary>
    internal const long ContractVariantID = 4473;

    /// <summary>
    /// One partition holding THREE variants, one per <see cref="MenuType"/>: the fixture's own
    /// (<see cref="PaidVariantID"/>, paid) and two re-keyed copies of it, <see cref="FreeVariantID"/> typed
    /// free and <see cref="ContractVariantID"/> typed contract.
    ///
    /// <para>Copies rather than hand-authored variants on purpose: they differ in their id and in the type
    /// and in NOTHING else, so a test can assert both that the filter selects the right ones and that the
    /// type moved no money — every variant's lines must price identically. Adding variants to
    /// <see cref="MenuGraphFixture"/> itself would re-baseline every golden.</para>
    /// </summary>
    internal static ServiceMenuDocuments WithEveryMenuType()
    {
        var documents = From(MenuGraphFixture.Build());

        foreach (var (variantID, menuType, idOffset) in new[]
        {
            (FreeVariantID, MenuType.Free, 1000L),
            (ContractVariantID, MenuType.Contract, 2000L),
        })
        {
            var copy = MenuGraphFixture.Build();
            Retype(copy.Variants.Single(), variantID, menuType, idOffset);

            var copyDocuments = From(copy);
            documents.Variants.AddRange(copyDocuments.Variants);
            documents.Periods.AddRange(copyDocuments.Periods);
            documents.Labours.AddRange(copyDocuments.Labours);
            documents.Items.AddRange(copyDocuments.Items);
        }

        return documents;
    }

    /// <summary>
    /// Types a freshly built copy of the fixture variant and moves it, with its children, onto its own
    /// ids — the child rows carry the variant id AND supply their own document ids, and two documents
    /// cannot share an id in one partition.
    /// </summary>
    private static void Retype(MenuVariant variant, long variantID, MenuType menuType, long idOffset)
    {
        variant.ID = variantID;
        variant.MenuType = menuType;

        // The children's MenuVariant navigation already points at this same object, so only the foreign
        // key and their own ids need moving.
        foreach (var period in variant.PeriodicAvailabilities)
        {
            period.ID += idOffset;
            period.MenuVariantID = variant.ID;
        }

        foreach (var labour in variant.LabourDetails)
        {
            labour.ID += idOffset;
            labour.MenuVariantID = variant.ID;
        }

        foreach (var item in variant.Items)
        {
            item.ID += idOffset;
            item.MenuVariantID = variant.ID;
        }
    }

    /// <summary>
    /// The same documents in reverse order, standing in for the fact that a Cosmos partition query
    /// makes NO ordering guarantee. Used to prove the aggregator's ordering is its own, not the
    /// storage's.
    /// </summary>
    internal static ServiceMenuDocuments Reversed(ServiceMenuDocuments documents) => new()
    {
        BasicModelCode = documents.BasicModelCode,
        Variants = documents.Variants.AsEnumerable().Reverse().ToList(),
        Periods = documents.Periods.AsEnumerable().Reverse().ToList(),
        Labours = documents.Labours.AsEnumerable().Reverse().ToList(),
        Items = documents.Items.AsEnumerable().Reverse().ToList(),
    };
}

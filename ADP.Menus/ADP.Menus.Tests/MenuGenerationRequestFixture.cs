// CS8714: Dictionary<long?, ...> mirrors MenuGenerationReferenceData.BrandMappings, whose nullable key
// deliberately matches the production brand-mapping lookup. Not ours to change here.
#pragma warning disable CS8714

using ShiftSoftware.ADP.Menus.Generation;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// The generic (layer-1) twin of <see cref="MenuGraphFixture"/> — the SAME logical menu data expressed
/// as a <see cref="MenuGenerationRequest"/> instead of EF entities.
///
/// Hand-built on purpose. The differential tests feed this to <see cref="MenuCodeGenerator"/> and the
/// EF graph to the original <c>MenuExportService</c>, then compare the two outputs field by field. If
/// this were produced by a shared adapter, a bug in that adapter would cancel out on both sides and
/// the comparison would prove nothing.
///
/// Keep the two fixtures in lockstep: a change to one without the other makes the differential tests
/// fail for the wrong reason.
///
/// <b>This holds LIVE ROWS ONLY</b>, which is why it is not a row-for-row copy of the EF fixture. The
/// contract carries no <c>IsDeleted</c> — the adapters filter — so the soft-deleted rows
/// <see cref="MenuGraphFixture"/> deliberately contains (a deleted item, link, part, country price and
/// country labour rate) simply have no representation here. That the ADAPTER removes them is tested by
/// <see cref="EfToGenerationAggregatorTests"/> and by the Phase 0 goldens, which run the EF graph
/// end to end.
/// </summary>
internal static class MenuGenerationRequestFixture
{
    internal static MenuGenerationRequest Build(
        long brandId = MenuGraphFixture.BrandId,
        bool includeBrandMapping = true,
        bool includeLabourRateMapping = true)
    {
        var standaloneGroup = new MenuGenerationStandaloneGroup
        {
            ID = 800,
            MenuCode = """{"en":"GMC","ar":"GMCA"}""",
            LabourCode = "GLC",
            Name = "Standalone Group One",
        };

        var variant = new MenuGenerationVariant
        {
            VariantID = 4471,
            BasicModelCode = MenuGraphFixture.BasicModelCode,
            BrandID = brandId,
            Model = "Model One",
            VariantName = "Variant A",
            MenuPrefix = """{"en":"MEN","ar":"MENA"}""",
            MenuPostfix = "PX",
            StandaloneMenuPrefix = """{"en":"STD","ar":"STDA"}""",
            StandaloneMenuPostfix = "SPX",
            LabourRate = MenuGraphFixture.PrimaryLabourRate,
            DiscountPercentage = 10m,
            HasStandaloneItems = true,

            CountryLabourRates =
            [
                new MenuGenerationCountryLabourRate { CountryID = 2, LabourRate = 20.00m },
                new MenuGenerationCountryLabourRate { CountryID = 3, LabourRate = 30.00m },
            ],

            // 503 belongs to group 20, for which there are no labour details → no periodic line.
            Periods =
            [
                new MenuGenerationPeriod { ServiceIntervalID = 501 },
                new MenuGenerationPeriod { ServiceIntervalID = 502 },
                new MenuGenerationPeriod { ServiceIntervalID = 503 },
            ],

            Labours =
            [
                new MenuGenerationLabour { ServiceIntervalGroupID = 10, AllowedTime = 0.50m, Consumable = 4.00m },
            ],

            Items =
            [
                // Item A — ungrouped standalone; serves interval group 10.
                new MenuGenerationItem
                {
                    MenuItemID = 900,
                    StandaloneAllowedTime = 0.25m,
                    ReplacementItemServiceIntervalGroupIDs = [10],
                    StandaloneOperationCode = """{"en":"OPA","ar":"OPAA"}""",
                    StandaloneLabourCode = "SLA",
                    FriendlyName = "Item A Friendly",
                    StandaloneGroup = null,
                    Parts =
                    [
                        new MenuGenerationPart
                        {
                            PartNumber = "PN-0001", SortOrder = 0, PeriodicQuantity = 2m, StandaloneQuantity = 1m,
                            CountryPrices =
                            [
                                new MenuGenerationPartPrice { CountryID = 2, PartPrice = 5.500m, PartFinalPrice = 7.250m },
                                new MenuGenerationPartPrice { CountryID = 3, PartPrice = 6.000m, PartFinalPrice = 8.000m },
                            ],
                        },
                        new MenuGenerationPart
                        {
                            PartNumber = "PN-0002", SortOrder = 1, PeriodicQuantity = 0m, StandaloneQuantity = 3m,
                            CountryPrices = [new MenuGenerationPartPrice { CountryID = 2, PartPrice = 2.000m, PartFinalPrice = 3.000m }],
                        },
                        new MenuGenerationPart
                        {
                            PartNumber = "PN-0004", SortOrder = 3, PeriodicQuantity = null, StandaloneQuantity = 2m,
                            CountryPrices = [new MenuGenerationPartPrice { CountryID = 2, PartPrice = 1.250m, PartFinalPrice = 1.750m }],
                        },
                    ],
                },

                // Item B — grouped standalone; serves interval group 10. First in its group, so its
                // allowed time is the one the grouped line uses.
                new MenuGenerationItem
                {
                    MenuItemID = 901,
                    StandaloneAllowedTime = 0.75m,
                    ReplacementItemServiceIntervalGroupIDs = [10],
                    StandaloneOperationCode = "OPB",
                    StandaloneLabourCode = "SLB",
                    FriendlyName = "Item B Friendly",
                    StandaloneGroup = standaloneGroup,
                    Parts =
                    [
                        new MenuGenerationPart
                        {
                            PartNumber = "PN-0011", PeriodicQuantity = 1m, StandaloneQuantity = 2m,
                            CountryPrices = [new MenuGenerationPartPrice { CountryID = 2, PartPrice = 10.000m, PartFinalPrice = 12.500m }],
                        },
                    ],
                },

                // Item C — same standalone group as B, but serves interval group 20, so it is absent
                // from the group-10 periodic lines while still folding into the grouped standalone line.
                new MenuGenerationItem
                {
                    MenuItemID = 902,
                    StandaloneAllowedTime = 0.90m,
                    ReplacementItemServiceIntervalGroupIDs = [20],
                    StandaloneOperationCode = "OPC",
                    StandaloneLabourCode = "SLC",
                    FriendlyName = "Item C Friendly",
                    StandaloneGroup = standaloneGroup,
                    Parts =
                    [
                        new MenuGenerationPart
                        {
                            PartNumber = "PN-0021", PeriodicQuantity = 4m, StandaloneQuantity = 1m,
                            CountryPrices = [new MenuGenerationPartPrice { CountryID = 2, PartPrice = 1.000m, PartFinalPrice = 2.000m }],
                        },
                    ],
                },

            ],
        };

        var labourRateCodes = new Dictionary<MenuGenerationLabourRateKey, string>();
        if (includeLabourRateMapping)
            labourRateCodes[new MenuGenerationLabourRateKey(brandId, MenuGraphFixture.PrimaryLabourRate)] = "LR1";

        var brandMappings = new Dictionary<long?, MenuGenerationBrandMapping>();
        if (includeBrandMapping)
            brandMappings[brandId] = new MenuGenerationBrandMapping { Code = "BC1", Abbreviation = "A" };

        return new MenuGenerationRequest
        {
            Variants = [variant],
            Reference = new MenuGenerationReferenceData
            {
                Intervals = new Dictionary<long, MenuGenerationServiceInterval>
                {
                    [501] = new MenuGenerationServiceInterval { Code = "S01", Description = "Plain description", ValueInMeter = 10000, GroupID = 10 },
                    [502] = new MenuGenerationServiceInterval { Code = "S02", Description = """{"en":"Description EN","ar":"Description AR"}""", ValueInMeter = 20000, GroupID = 10 },
                    [503] = new MenuGenerationServiceInterval { Code = "S03", Description = "Third description", ValueInMeter = 30000, GroupID = 20 },
                },
                Groups = new Dictionary<long, MenuGenerationServiceIntervalGroup>
                {
                    [10] = new MenuGenerationServiceIntervalGroup { LabourCode = "GRPA", ServiceIntervalIDs = [501, 502] },
                    [20] = new MenuGenerationServiceIntervalGroup { LabourCode = "GRPB", ServiceIntervalIDs = [503] },
                },
                LabourRateCodes = labourRateCodes,
                BrandMappings = brandMappings,
            },
        };
    }
}

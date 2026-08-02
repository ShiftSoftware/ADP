using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Models.Service.Cosmos;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 5 — proves the vehicle lookup's read path produces the DMS export's menu codes.
///
/// <para>The headline test is a ROUND TRIP: one menu graph goes to the export directly, and the same
/// graph goes through the production replication mappers into Cosmos documents, back out through
/// <see cref="CosmosToGenerationAggregator"/>, and into the shared generator. The two outputs must be
/// identical, character for character, across the configuration matrix.</para>
///
/// <para>That is a stronger claim than "both call the same generator", which is true by construction
/// and proves nothing on its own. The generator cannot disagree with itself; the two ADAPTERS can, and
/// every soft-delete and ordering rule in the Cosmos adapter exists to mirror one on the EF side. This
/// is where a divergence between them shows up — as a wrong menu code, at the point where a wrong menu
/// code would actually be issued.</para>
/// </summary>
public class CosmosToGenerationAggregatorTests
{
    // The export's own config: dealer cost ON, because that is what the comparison baseline emits.
    // The lookup's config leaves it off — asserted separately in ServiceMenuEvaluatorTests.
    private static MenuGenerationConfig Config(long countryId, decimal transferRate, string? language, bool usePrimaryLabourRate) => new()
    {
        CountryID = countryId,
        TransferRate = transferRate,
        Language = language,
        UsePrimaryLabourRate = usePrimaryLabourRate,
        IncludePartCost = true,
    };

    private static string ViaExport(long countryId, decimal transferRate, string? language, bool usePrimaryLabourRate, long brandId = MenuGraphFixture.BrandId, bool includeBrandMapping = true)
    {
        var fixture = MenuGraphFixture.Build(brandId, includeBrandMapping);
        return MenuLineFormatter.FormatCore(MenuExportService.GenerateMenuLines(
            fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings,
            countryId, transferRate, language, usePrimaryLabourRate));
    }

    private static string ViaCosmos(long countryId, decimal transferRate, string? language, bool usePrimaryLabourRate, long brandId = MenuGraphFixture.BrandId, bool includeBrandMapping = true)
    {
        var documents = MenuCosmosDocumentFixture.Build(brandId, includeBrandMapping);
        var request = CosmosToGenerationAggregator.Build(documents);
        return MenuLineFormatter.FormatCore(
            MenuCodeGenerator.Generate(request, Config(countryId, transferRate, language, usePrimaryLabourRate)));
    }

    private static List<GeneratedMenuLine> Generate(ServiceMenuDocuments documents, string? language = "en", long countryId = 2) =>
        MenuCodeGenerator.Generate(CosmosToGenerationAggregator.Build(documents), Config(countryId, 1m, language, false)).ToList();

    // ---- the round trip ----------------------------------------------------------------------------

    [Theory]
    // country, transferRate, language, usePrimaryLabourRate — the Phase 1 matrix, re-run over Cosmos.
    [InlineData(2, 1.0, "en", false)]     // baseline: country prices, country labour rate
    [InlineData(0, 2.5, "ar", true)]      // no country rows, scaled consumable, primary rate, Arabic
    [InlineData(2, 1.0, "ar", false)]     // language only
    [InlineData(3, 1.0, "en", false)]     // the other country's prices and labour rate
    [InlineData(2, 1.0, null, false)]     // null language → English
    [InlineData(2, 1.0, "fr", false)]     // unknown language → English fallback
    [InlineData(2, 3.3333, "en", false)]  // rounding of the scaled consumable
    [InlineData(2, 1.0, "en", true)]      // primary labour rate with country prices present
    [InlineData(9, 1.0, "en", false)]     // country with no price and no labour rate rows
    public void ReplicateThenRead_ProducesTheExportsLines(double countryId, double transferRate, string? language, bool usePrimaryLabourRate)
    {
        var expected = ViaExport((long)countryId, (decimal)transferRate, language, usePrimaryLabourRate);
        var actual = ViaCosmos((long)countryId, (decimal)transferRate, language, usePrimaryLabourRate);

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
    }

    /// <summary>
    /// The round trip again, but with one row of each replicated table SOFT-DELETED — the rules that
    /// were added when soft deletes started excluding rows from generated menus.
    ///
    /// <para>This is the guard that matters for those rules. Both adapters merely CARRY the flags and
    /// the generator decides, so agreement should be structural — but "should be" is exactly the claim a
    /// test exists to check, and each flag has to travel a different route: some ride on the document
    /// itself, some on an embedded master copy, and the variant's is two document flags OR-ed into one.
    /// A flag dropped on either route shows up here as export and lookup disagreeing.</para>
    /// </summary>
    [Theory]
    [InlineData("period")]
    [InlineData("labour")]
    [InlineData("interval")]
    [InlineData("intervalGroup")]
    [InlineData("intervalGroupLink")]
    [InlineData("replacementItem")]
    [InlineData("standaloneGroup")]
    [InlineData("variant")]
    [InlineData("menu")]
    public void ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted(string deleted)
    {
        var fixture = MenuGraphFixture.Build();
        var variant = fixture.Variants[0];
        var replacementItem = variant.Items
            .Select(item => item.ReplacementItemVehicleModel?.ReplacementItem)
            .First(item => item?.StandaloneReplacementItemGroup is not null);

        // Mutating the EF graph covers every embedded copy at once: EF's identity map means one instance
        // per row, and MenuCosmosDocumentFixture re-projects the whole graph afterwards.
        switch (deleted)
        {
            case "period": variant.PeriodicAvailabilities.First().IsDeleted = true; break;
            case "labour": variant.LabourDetails.First().IsDeleted = true; break;
            case "interval": variant.PeriodicAvailabilities.First().ServiceInterval.IsDeleted = true; break;
            case "intervalGroup": variant.LabourDetails.First().ServiceIntervalGroup.IsDeleted = true; break;

            // The replacement-item ↔ interval-group LINK. The only row with no flag anywhere in the
            // document shape — it contributes a bare id to a flat list — so replication has to drop it
            // at projection time or the lookup keeps pricing parts the export has stopped pricing.
            case "intervalGroupLink": replacementItem.ReplacementItemServiceIntervalGroups.First().IsDeleted = true; break;

            case "replacementItem": replacementItem.IsDeleted = true; break;
            case "standaloneGroup": replacementItem.StandaloneReplacementItemGroup.IsDeleted = true; break;
            case "variant": variant.IsDeleted = true; break;
            case "menu": variant.Menu.IsDeleted = true; break;
        }

        var expected = MenuLineFormatter.FormatCore(MenuExportService.GenerateMenuLines(
            fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings, 2, 1m, "en", false));

        var actual = MenuLineFormatter.FormatCore(MenuCodeGenerator.Generate(
            CosmosToGenerationAggregator.Build(MenuCosmosDocumentFixture.From(fixture)),
            Config(2, 1m, "en", false)));

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);

        // ...and the delete actually changed something, so a rule silently doing nothing on BOTH sides
        // cannot pass this by agreeing on the unchanged output.
        var baseline = ViaExport(2, 1m, "en", false);
        Assert.NotEqual(baseline, expected);
    }

    /// <summary>The "Z" brand-abbreviation fallback survives the round trip too.</summary>
    [Fact]
    public void ReplicateThenRead_ProducesTheExportsLines_ForUnmappedBrand()
    {
        var expected = ViaExport(2, 1m, "en", false, MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);
        var actual = ViaCosmos(2, 1m, "en", false, MenuGraphFixture.UnmappedBrandId, includeBrandMapping: false);

        Assert.Equal(expected, actual, ignoreLineEndingDifferences: true);
        Assert.Contains("LR1Z", actual);
    }

    /// <summary>
    /// A Cosmos partition query guarantees NO order, so the aggregator imposes its own. Feeding the
    /// documents in reverse must change nothing — if it does, generated codes depend on document
    /// layout, which is the one thing this design cannot tolerate (open item O8).
    /// </summary>
    [Fact]
    public void DocumentOrder_DoesNotAffectOutput()
    {
        var documents = MenuCosmosDocumentFixture.Build();

        var forward = MenuLineFormatter.FormatCore(Generate(documents));
        var reversed = MenuLineFormatter.FormatCore(Generate(MenuCosmosDocumentFixture.Reversed(documents)));

        Assert.Equal(forward, reversed, ignoreLineEndingDifferences: true);
    }

    /// <summary>
    /// Every source-row id survives the round trip. <see cref="MenuLineFormatter.FormatCore"/> does not
    /// render <see cref="GeneratedMenuLine.LineKey"/>, so the parity test above cannot see a swapped or
    /// zeroed id — and a wrong standalone GROUP id in particular would still fold the same items into
    /// one line while quietly breaking cross-language correlation. Same expectation as
    /// <c>MenuCodeGeneratorPortTests.LineKeys_AreUniqueAndLanguageInvariant</c>.
    /// </summary>
    [Fact]
    public void SourceRowIds_SurviveTheRoundTrip()
    {
        Assert.Equal(
            ["P|4471|501", "P|4471|502", "S|4471|900", "G|4471|800"],
            Generate(MenuCosmosDocumentFixture.Build()).Select(line => line.LineKey));
    }

    // ---- what the aggregator filters, and what it must not -----------------------------------------

    /// <summary>The export selects <c>!variant.IsDeleted</c>; so does the lookup.</summary>
    [Fact]
    public void DeletedVariant_GeneratesNothing()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Variants[0].IsDeleted = true;

        Assert.Empty(Generate(documents));
    }

    /// <summary>
    /// The export also selects <c>!variant.Menu.IsDeleted</c>. Deleting a menu does not cascade to its
    /// variant rows, so without the flattened <c>MenuIsDeleted</c> flag the lookup would keep serving
    /// menu codes for a deleted menu — with nothing anywhere to indicate why.
    /// </summary>
    [Fact]
    public void DeletedParentMenu_GeneratesNothing()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Variants[0].MenuIsDeleted = true;

        Assert.Empty(Generate(documents));
    }

    /// <summary>
    /// A soft-deleted MENU row really does set the flag on the document — the mapper reads it, so a
    /// re-replicated variant carries it. Guards the projection, not just the reader.
    /// </summary>
    [Fact]
    public void MenuIsDeleted_IsProjectedFromTheParentMenu()
    {
        var fixture = MenuGraphFixture.Build();
        fixture.Variants[0].Menu.IsDeleted = true;

        var documents = MenuCosmosDocumentFixture.From(fixture);

        Assert.True(documents.Variants[0].MenuIsDeleted);
        Assert.Empty(Generate(documents));
    }

    /// <summary>
    /// A soft-deleted PERIOD document generates no line. The flag reaches the generator, which owns the
    /// rule — this adapter drops nothing itself.
    /// </summary>
    [Fact]
    public void SoftDeletedPeriodDocument_GeneratesNoLine()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Periods.Single(period => period.ServiceIntervalID == 501).IsDeleted = true;

        var periodic = Generate(documents).Where(line => !line.IsStandalone).ToList();

        Assert.Equal(["S02"], periodic.Select(line => line.ServiceIntervalCode));
    }

    /// <summary>
    /// A soft-deleted LABOUR DETAIL supplies nothing, so its intervals lose their lines — the same
    /// silent skip as having no labour detail at all.
    /// </summary>
    [Fact]
    public void SoftDeletedLabourDocument_LosesThePeriodicLinesItSupplied()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        foreach (var labour in documents.Labours)
            labour.IsDeleted = true;

        Assert.DoesNotContain(Generate(documents), line => !line.IsStandalone);
    }

    /// <summary>A soft-deleted SERVICE INTERVAL generates no line, whatever its availability rows say.</summary>
    [Fact]
    public void SoftDeletedServiceInterval_GeneratesNoLine()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Periods.Single(period => period.ServiceIntervalID == 501).ServiceInterval!.IsDeleted = true;

        var periodic = Generate(documents).Where(line => !line.IsStandalone).ToList();

        Assert.Equal(["S02"], periodic.Select(line => line.ServiceIntervalCode));
    }

    /// <summary>
    /// A soft-deleted INTERVAL GROUP matches nothing, so the intervals it supplied labour for lose their
    /// lines. Every embedded copy is flagged here, which is what a completed fan-out produces.
    ///
    /// Standalone lines are untouched on purpose — interval groups govern the PERIODIC side only, so the
    /// items keep selling individually. Asserted so the blast radius is pinned, not just the effect.
    /// </summary>
    [Fact]
    public void SoftDeletedIntervalGroup_LosesThePeriodicLines_ButNotTheStandaloneOnes()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        DeleteIntervalGroup(documents, groupId: 10);

        var lines = Generate(documents);

        Assert.DoesNotContain(lines, line => !line.IsStandalone);
        Assert.Equal(2, lines.Count(line => line.IsStandalone));
    }

    /// <summary>
    /// A master row is embedded in several documents and the reference dictionaries are last-write-wins,
    /// so a PARTIALLY landed fan-out leaves the copies disagreeing — a real state here, since a sweep
    /// can silently write only some documents (§18). Deletion is sticky: any copy saying deleted wins,
    /// whichever order the walk happens to visit them in. Without that, whether a menu code is issued
    /// for a withdrawn group would depend on document layout.
    /// </summary>
    [Fact]
    public void PartiallyPropagatedDelete_IsStickyRegardlessOfDocumentOrder()
    {
        foreach (var reverse in new[] { false, true })
        {
            var documents = MenuCosmosDocumentFixture.Build();

            // Only the labour document's copy got the delete; the items' copies are stale.
            foreach (var labour in documents.Labours.Where(x => x.ServiceIntervalGroupID == 10))
                labour.ServiceIntervalGroup!.IsDeleted = true;

            if (reverse)
                documents = MenuCosmosDocumentFixture.Reversed(documents);

            Assert.DoesNotContain(Generate(documents), line => !line.IsStandalone);
        }
    }

    private static void DeleteIntervalGroup(ServiceMenuDocuments documents, long groupId)
    {
        foreach (var labour in documents.Labours.Where(x => x.ServiceIntervalGroupID == groupId))
            labour.ServiceIntervalGroup!.IsDeleted = true;

        foreach (var group in documents.Items.SelectMany(item => item.ServiceIntervalGroups)
                                             .Where(group => group.ServiceIntervalGroupID == groupId))
            group.IsDeleted = true;
    }

    /// <summary>
    /// A soft-deleted REPLACEMENT ITEM withdraws it from every menu that applies it — distinct from the
    /// link row's own flag, which the fixture already covers.
    /// </summary>
    [Fact]
    public void SoftDeletedReplacementItem_WithdrawsItsMenuItem()
    {
        var documents = MenuCosmosDocumentFixture.Build();

        // Item A (menu item 900) is the ungrouped standalone line and contributes parts periodically.
        documents.Items.Single(item => item.MenuItemID == 900).ReplacementItem!.IsDeleted = true;

        var lines = Generate(documents);

        Assert.DoesNotContain(lines, line => line.MenuItemID == 900);
        Assert.DoesNotContain(lines.SelectMany(line => line.Parts), part => part.MenuItemID == 900);
    }

    /// <summary>
    /// A soft-deleted STANDALONE GROUP is treated as no group: its items fall back to ungrouped lines
    /// rather than disappearing. Deleting a grouping withdraws the grouping, not the items — they are
    /// separate rows and still sellable. This is a judgement call, so it is pinned explicitly.
    /// </summary>
    [Fact]
    public void SoftDeletedStandaloneGroup_FallsBackToUngroupedLines()
    {
        var documents = MenuCosmosDocumentFixture.Build();

        foreach (var item in documents.Items.Where(item => item.StandaloneGroup is not null))
            item.StandaloneGroup!.IsDeleted = true;

        var standalone = Generate(documents).Where(line => line.IsStandalone).ToList();

        Assert.DoesNotContain(standalone, line => line.LineType == MenuLineType.StandaloneGrouped);

        // Items B (901) and C (902) were the group's members; both now emit their own line.
        Assert.Equal(
            [900L, 901L, 902L],
            standalone.Select(line => line.MenuItemID!.Value).OrderBy(id => id));
    }

    /// <summary>
    /// The two catalogues the export DOES filter. A soft-deleted labour-rate mapping reaches the
    /// document through the replication fan-out (which carries the flag rather than removing the copy),
    /// so it has to be dropped here — leaving the generator to throw exactly as it would for a mapping
    /// the export's catalogue no longer contains (open item O1).
    /// </summary>
    [Fact]
    public void SoftDeletedLabourRateMapping_IsDropped_SoGenerationFailsLoudly()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Variants[0].LabourRateMapping!.IsDeleted = true;

        var request = CosmosToGenerationAggregator.Build(documents);

        Assert.Empty(request.Reference.LabourRateCodes);
        Assert.Throws<KeyNotFoundException>(() => Generate(documents));
    }

    /// <summary>Same live-only rule for the brand mapping — but an absent one is valid, so it falls back to "Z".</summary>
    [Fact]
    public void SoftDeletedBrandMapping_IsDropped_AndFallsBackToZ()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Variants[0].BrandMapping!.IsDeleted = true;

        var lines = Generate(documents);

        Assert.NotEmpty(lines);
        Assert.All(lines, line =>
        {
            Assert.Equal("Z", line.BrandAbbreviation);
            Assert.Null(line.BrandCode);
        });
    }

    /// <summary>
    /// A variant with no mapping embedded at all is the same case as a deleted one: the entry is left
    /// out rather than invented, so the generator throws instead of issuing a code composed from data
    /// that is not there.
    /// </summary>
    [Fact]
    public void MissingLabourRateMapping_LeavesTheEntryOut_RatherThanInventingACode()
    {
        var documents = MenuCosmosDocumentFixture.Build(includeLabourRateMapping: false);

        Assert.Null(documents.Variants[0].LabourRateMapping);
        Assert.Empty(CosmosToGenerationAggregator.Build(documents).Reference.LabourRateCodes);
        Assert.Throws<KeyNotFoundException>(() => Generate(documents));
    }

    // ---- partial / malformed partitions ------------------------------------------------------------

    /// <summary>
    /// The trap Phase 2 flagged for this reader: a missing document TYPE degrades to missing lines, not
    /// to an error — exactly as a missing <c>Include</c> does on the export side. Without labour
    /// documents there is nothing to match an interval against, so every periodic line disappears
    /// silently while the standalone ones survive.
    /// </summary>
    [Fact]
    public void PartitionMissingLabourDocuments_LosesPeriodicLinesSilently()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Labours.Clear();

        var lines = Generate(documents);

        Assert.DoesNotContain(lines, line => !line.IsStandalone);
        Assert.Contains(lines, line => line.IsStandalone);
    }

    /// <summary>
    /// A period document written without its embedded interval is a replication fault, and it fails
    /// loudly rather than emitting a line with a blank code segment.
    /// </summary>
    [Fact]
    public void PeriodDocumentMissingItsEmbeddedInterval_FailsLoudly()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Periods[0].ServiceInterval = null;

        Assert.Throws<KeyNotFoundException>(() => Generate(documents));
    }

    /// <summary>
    /// Children whose variant is not in the partition (a variant deleted without its children being
    /// cascaded — the normal case per §17) contribute nothing, because the aggregator groups children
    /// under variants rather than folding them loose.
    /// </summary>
    [Fact]
    public void OrphanedChildDocuments_AreIgnored()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        var expected = MenuLineFormatter.FormatCore(Generate(documents));

        documents.Periods.Add(new MenuPeriodCosmosModel
        {
            id = "99999",
            BasicModelCode = documents.BasicModelCode,
            VariantID = 123456,
            ServiceIntervalID = 501,
        });
        documents.Items.Add(new MenuItemCosmosModel
        {
            id = "99998",
            BasicModelCode = documents.BasicModelCode,
            MenuItemID = 99998,
            VariantID = 123456,
        });

        Assert.Equal(expected, MenuLineFormatter.FormatCore(Generate(documents)), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void EmptyPartition_ProducesAnEmptyRequest()
    {
        var request = CosmosToGenerationAggregator.Build(new ServiceMenuDocuments());

        Assert.Empty(request.Variants);
        Assert.Empty(request.Reference.Intervals);
        Assert.Empty(request.Reference.Groups);
        Assert.Empty(request.Reference.LabourRateCodes);
        Assert.Empty(request.Reference.BrandMappings);
    }

    [Fact]
    public void Build_RejectsNullDocuments() =>
        Assert.Throws<ArgumentNullException>(() => CosmosToGenerationAggregator.Build(null!));

    // ---- reference data assembly -------------------------------------------------------------------

    /// <summary>
    /// Interval-group membership has to travel with the MENU ITEM, not be recovered from the sibling
    /// labour documents: generation asks "does group G contain interval I" for every group an item
    /// serves, including groups the variant has no labour detail for. Group 20 is exactly that case in
    /// the fixture — it reaches the reference data only through item C.
    /// </summary>
    [Fact]
    public void IntervalGroupMembership_ComesFromTheItemsToo_NotOnlyFromLabourDocuments()
    {
        var documents = MenuCosmosDocumentFixture.Build();

        var fromLabourOnly = CosmosToGenerationAggregator
            .Build(documents.Variants, documents.Periods, documents.Labours, [])
            .Reference.Groups;

        var withItems = CosmosToGenerationAggregator.Build(documents).Reference.Groups;

        Assert.DoesNotContain(20L, fromLabourOnly.Keys);
        Assert.Contains(20L, withItems.Keys);
        Assert.Contains(503L, withItems[20].ServiceIntervalIDs);
    }

    /// <summary>
    /// The labour-rate dictionary is keyed by the MAPPING's own (brand, rate) pair, and the key keeps
    /// decimal-VALUE semantics across the Cosmos hop — 12.5 and 12.50 are one key. A string key would
    /// silently miss mappings the export resolves.
    /// </summary>
    [Fact]
    public void LabourRateKey_SurvivesTheCosmosHop_WithDecimalValueSemantics()
    {
        var documents = MenuCosmosDocumentFixture.Build();
        documents.Variants[0].LabourRateMapping!.LabourRate = 12.5m;   // authored at a different scale

        var request = CosmosToGenerationAggregator.Build(documents);

        Assert.True(request.Reference.LabourRateCodes.ContainsKey(
            new MenuGenerationLabourRateKey(MenuGraphFixture.BrandId, 12.50m)));
        Assert.All(Generate(documents), line => Assert.Contains("LR1", line.LabourCode));
    }

    /// <summary>
    /// The consumable is stored UNSCALED and scaled at generation time (open item O6). Pinned because
    /// scaling it during replication instead would bake one deployment's transfer rate into the
    /// documents.
    /// </summary>
    [Fact]
    public void Consumable_IsStoredUnscaledAndScaledAtGeneration()
    {
        var documents = MenuCosmosDocumentFixture.Build();

        Assert.Equal(4.00m, documents.Labours.Single().Consumable);

        var scaled = MenuCodeGenerator
            .Generate(CosmosToGenerationAggregator.Build(documents), Config(2, 2.5m, "en", false))
            .First(line => !line.IsStandalone);

        Assert.Equal(10.00m, scaled.Consumable);
        Assert.Equal(4.00m, scaled.RawConsumable);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;
using ShiftSoftware.ADP.Menus.Generation;
using ShiftSoftware.ADP.Menus.Tests.SampleSeeding;
using ShiftSoftware.ADP.Models.Enums;

using MenuType = ShiftSoftware.ADP.Models.Enums.MenuType;
using MenuTypeSet = ShiftSoftware.ADP.Menus.Shared.Enums.MenuTypeSet;
using SharedMenuType = ShiftSoftware.ADP.Menus.Shared.Enums.MenuType;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// A variant offered under several menu types: the export generates exactly what it did, and the lookup
/// reads the same lines once per type, each type covering only the services assigned to it.
/// </summary>
public class MenuTypeSplitTests
{
    private static readonly MenuGenerationConfig Config = new() { CountryID = 2, Language = "en" };

    /// <summary>Paid and free, with the first interval and the first standalone item paid only.</summary>
    private static MenuGraphFixture.Fixture PaidAndFree(out long paidOnlyIntervalID, out long paidOnlyItemID)
    {
        var fixture = MenuGraphFixture.Build();
        var variant = fixture.Variants.Single();

        variant.MenuTypes = [SharedMenuType.Paid, SharedMenuType.Free];

        foreach (var period in variant.PeriodicAvailabilities)
            period.MenuTypes = [SharedMenuType.Paid, SharedMenuType.Free];

        var paidOnly = variant.PeriodicAvailabilities.First();
        paidOnly.MenuTypes = [SharedMenuType.Paid];
        paidOnlyIntervalID = paidOnly.ServiceIntervalID;

        var ungrouped = variant.Items.First(item =>
            item.ReplacementItemVehicleModel?.ReplacementItem?.StandaloneReplacementItemGroup is null
            && item.Parts.Any(part => part.StandaloneQuantity > 0));
        ungrouped.StandaloneMenuTypes = [SharedMenuType.Paid];
        paidOnlyItemID = ungrouped.ID;

        return fixture;
    }

    private static List<GeneratedMenuLine> Lookup(MenuGraphFixture.Fixture fixture, params MenuType[] menuTypes) =>
        new ServiceMenuGenerationEvaluator(Options.Create(new ServiceMenuLookupOptions()))
            .Evaluate(MenuCosmosDocumentFixture.From(fixture), Config, menuTypes)
            .ToList();

    [Fact]
    public void TheExport_GeneratesTheSameLines_WhateverTheTypes()
    {
        static List<(string, string, string, int)> Export(MenuGraphFixture.Fixture fixture) =>
            MenuCodeGenerator
                .Generate(EfToGenerationAggregator.Build(fixture.Variants, fixture.LabourRateMappings, fixture.BrandMappings), Config)
                .Select(line => (line.LineKey, line.Code, line.LabourCode, line.Parts.Count))
                .ToList();

        Assert.Equal(Export(MenuGraphFixture.Build()), Export(PaidAndFree(out _, out _)));
    }

    [Fact]
    public void TheLookup_ReturnsEachTypeItsOwnServices()
    {
        var fixture = PaidAndFree(out var paidOnlyIntervalID, out var paidOnlyItemID);

        var paid = Lookup(fixture, MenuType.Paid);
        var free = Lookup(fixture, MenuType.Free);

        Assert.All(paid, line => Assert.Equal(MenuType.Paid, line.MenuType));
        Assert.All(free, line => Assert.Equal(MenuType.Free, line.MenuType));

        // Paid covers everything; free is the same lines minus the two that are paid only.
        Assert.Contains(paid, line => line.ServiceIntervalID == paidOnlyIntervalID);
        Assert.Contains(paid, line => line.MenuItemID == paidOnlyItemID);
        Assert.Equal(
            paid.Where(line => line.ServiceIntervalID != paidOnlyIntervalID && line.MenuItemID != paidOnlyItemID)
                .Select(line => (line.LineKey, line.Code)),
            free.Select(line => (line.LineKey, line.Code)));

        // No filter returns both readings of the variant.
        Assert.Equal(paid.Count + free.Count, Lookup(fixture).Count);
    }

    [Fact]
    public void APeriodicLine_TakesTheDescriptionOfTheTypeItIsReadUnder()
    {
        var fixture = PaidAndFree(out _, out _);
        var free = Lookup(fixture, MenuType.Free).First(line => !line.IsStandalone);
        var paid = Lookup(fixture, MenuType.Paid).Single(line => line.LineKey == free.LineKey);

        var interval = fixture.Variants.Single().PeriodicAvailabilities
            .Single(period => period.ServiceIntervalID == free.ServiceIntervalID).ServiceInterval;

        Assert.Equal(interval.PaidDescription, paid.Description);
        Assert.Equal(interval.FreeDescription, free.Description);
        Assert.NotEqual(paid.Description, free.Description);
    }

    [Fact]
    public void TheSchedule_ListsTheVariantOncePerType()
    {
        var variants = ServiceMenuScheduleEvaluator.Evaluate(Lookup(PaidAndFree(out _, out _)));

        Assert.Equal([MenuType.Paid, MenuType.Free], variants.Select(variant => variant.MenuType));
        Assert.Single(variants.Select(variant => variant.VariantID).Distinct());
    }

    /// <summary>
    /// The column EF writes holds the enum values, and it is the same text the DuckDB rows carry and the lookup's
    /// reader parses. Built from the model alone; no database is opened.
    /// </summary>
    [Fact]
    public void TheMenusDatabase_StoresTheEnumValues_AsTheJsonTheReadersParse()
    {
        using var db = new SampleSeedDB(new DbContextOptionsBuilder<SampleSeedDB>().UseSqlServer("Server=unused").Options);

        var converter = db.Model.FindEntityType(typeof(MenuVariant))!
            .FindProperty(nameof(MenuVariant.MenuTypes))!
            .GetTypeMapping().Converter!;

        List<SharedMenuType> types = [SharedMenuType.Paid, SharedMenuType.Contract];
        var stored = (string)converter.ConvertToProvider(types)!;

        Assert.Equal("[0,2]", stored);
        Assert.Equal(stored, MenuTypeSet.ToJson(types));
        Assert.Equal(types, (List<SharedMenuType>)converter.ConvertFromProvider(stored)!);
        Assert.Equal([MenuType.Paid, MenuType.Contract], MenuTypeList.Parse(stored));
    }
}

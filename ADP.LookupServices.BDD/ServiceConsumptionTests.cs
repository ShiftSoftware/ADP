using NSubstitute;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using Xunit;

namespace LookupServices.BDD;

public class ServiceConsumptionTests
{
    private static readonly DateTime Start = new(2026, 2, 1);
    private static readonly DateTime End = new(2026, 3, 1);
    private static readonly DateTime Now = new(2026, 4, 1);
    private const string Vin = "1FDKF37GXVEB34368";

    private static CompanyDataAggregateModel Aggregate(DateTime? date) => new()
    {
        VIN = Vin,
        CampaignVinEntries = [new() { id = "entry", VIN = Vin, CampaignID = 500, RecordedDate = Start }],
        LaborLines = [new OrderLaborLineModel { InvoiceDate = date, InvoiceNumber = "INV", OrderDocumentNumber = "JOB", PackageCode = "BASIC" }],
    };

    private static ServiceItemModel Offer() => new()
    {
        IntegrationID = "offer", CampaignID = 500,
        CampaignStartDate = Start, CampaignEndDate = End,
        CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.ManualVinEntry,
        CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
        ValidityMode = ClaimableItemValidityMode.RelativeToActivation,
        ActiveFor = 1, ActiveForDurationType = DurationType.Months,
        FixedCost = 123, ServiceConsumption = new(),
    };

    private static async Task<List<VehicleServiceItemDTO>> Evaluate(CompanyDataAggregateModel aggregate,
        bool endOfDay = false, params ServiceItemModel[] items)
    {
        var storage = Substitute.For<IVehicleLookupStorageService>();
        storage.GetServiceItemsAsync(Arg.Any<bool>()).Returns(items.Length == 0 ? [Offer()] : items.ToList());
        var options = new LookupOptions { TimeProvider = new FixedClock(), TreatServiceItemExpiryAsEndOfDay = endOfDay };
        var evaluator = new VehicleServiceItemEvaluator(storage, aggregate, options, Substitute.For<IServiceProvider>());
        var result = await evaluator.Evaluate(null!, new VehicleOwnership(), null, "en");
        return result.serviceItems.ToList();
    }

    [Theory]
    [InlineData("2026-01-31", false)]
    [InlineData("2026-02-01", true)]
    [InlineData("2026-03-01", true)]
    [InlineData("2026-03-02", false)]
    [InlineData("2026-05-01", false)]
    [InlineData(null, false)]
    public async Task Only_dated_visits_inside_the_offer_window_consume(string? date, bool consumed)
    {
        var aggregate = Aggregate(date is null ? null : DateTime.Parse(date));
        var item = Assert.Single(await Evaluate(aggregate));
        Assert.Equal(consumed ? "processed" : "expired", item.Status);
        Assert.Equal(consumed, item.ServiceConsumptionEvidence is not null);
        Assert.Null(item.ClaimDate);
        Assert.False(item.Claimable);
        Assert.Empty(aggregate.ItemClaims);
        if (consumed) Assert.Null(item.Cost);
    }

    [Fact]
    public async Task Expiry_end_of_day_option_is_shared_with_consumption()
    {
        var aggregate = Aggregate(End.AddHours(12));
        Assert.Equal("expired", Assert.Single(await Evaluate(aggregate)).Status);
        Assert.Equal("processed", Assert.Single(await Evaluate(aggregate, true)).Status);
    }

    [Fact]
    public async Task Real_claim_wins_and_preserves_its_cost_and_metadata()
    {
        var aggregate = Aggregate(Start.AddDays(1));
        aggregate.ItemClaims.Add(new() { ServiceItemID = "offer", CampaignVinEntryID = "entry", ClaimDate = Start.AddDays(2), Cost = 27, InvoiceNumber = "REAL" });
        var item = Assert.Single(await Evaluate(aggregate));
        Assert.Equal("processed", item.Status);
        Assert.Equal(27, item.Cost);
        Assert.Equal("REAL", item.InvoiceNumber);
        Assert.Null(item.ServiceConsumptionEvidence);
    }

    [Fact]
    public async Task Incomplete_invoice_does_not_consume_and_a_later_complete_visit_does()
    {
        var aggregate = Aggregate(Start.AddDays(1));
        aggregate.LaborLines[0].NumberOfPartLines = 1;
        Assert.Null(Assert.Single(await Evaluate(aggregate)).ServiceConsumptionEvidence);
        aggregate.LaborLines.Add(new() { InvoiceDate = Start.AddDays(2), InvoiceNumber = "COMPLETE", OrderDocumentNumber = "JOB2" });
        Assert.Equal("COMPLETE", Assert.Single(await Evaluate(aggregate)).ServiceConsumptionEvidence.InvoiceNumber);
    }

    [Theory]
    [InlineData("basic", true)]
    [InlineData("OTHER", false)]
    [InlineData("", false)]
    public async Task Package_filter_matches_exactly_ignoring_case(string package, bool consumed)
    {
        var item = Offer();
        item.ServiceConsumption.PackageCodes = [package];
        Assert.Equal(consumed, Assert.Single(await Evaluate(Aggregate(Start), false, item)).ServiceConsumptionEvidence is not null);
    }

    [Fact]
    public async Task Missing_entry_does_not_offer_a_benefit()
    {
        var aggregate = Aggregate(Start);
        aggregate.CampaignVinEntries.Clear();
        Assert.Empty(await Evaluate(aggregate));
    }

    [Theory]
    [InlineData("VIN-MENU", false)]
    [InlineData(" basic ", true)]
    public async Task Explicit_entry_menu_survives_missing_vehicle_model_and_filters_consumption(string menu, bool consumed)
    {
        var aggregate = Aggregate(Start);
        aggregate.CampaignVinEntries[0].PackageCode = menu;
        var offer = Offer();
        offer.ModelCosts = [new() { Katashiki = "UNRESOLVED", PackageCode = "MODEL-MENU" }];
        var result = Assert.Single(await Evaluate(aggregate, false, offer));
        Assert.Equal(menu, result.PackageCode);
        Assert.Equal(consumed ? "processed" : "expired", result.Status);
        Assert.Equal(consumed, result.ServiceConsumptionEvidence is not null);
        Assert.Null(result.ModelCostID);
        Assert.Null(offer.ServiceConsumption.PackageCodes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Blank_menu_preserves_configured_consumption_filter(string? menu)
    {
        var aggregate = Aggregate(Start);
        aggregate.CampaignVinEntries[0].PackageCode = menu;
        var offer = Offer();
        offer.ServiceConsumption.PackageCodes = ["OTHER"];
        Assert.Null(Assert.Single(await Evaluate(aggregate, false, offer)).ServiceConsumptionEvidence);
        offer.ServiceConsumption.PackageCodes = null;
        Assert.NotNull(Assert.Single(await Evaluate(aggregate, false, offer)).ServiceConsumptionEvidence);
    }

    [Fact]
    public async Task Entry_menu_overrides_catalog_filter_without_affecting_other_activations_or_vehicles()
    {
        var aggregate = Aggregate(Start);
        aggregate.CampaignVinEntries[0].PackageCode = "BASIC";
        aggregate.CampaignVinEntries.Add(new() { id = "other-entry", VIN = Vin, CampaignID = 500, RecordedDate = Start });
        var offer = Offer();
        offer.CampaignActivationType = ClaimableItemCampaignActivationTypes.EveryTrigger;
        offer.ServiceConsumption.PackageCodes = ["OTHER"];
        var results = await Evaluate(aggregate, false, offer);
        Assert.NotNull(results.Single(x => x.CampaignVinEntryID == "entry").ServiceConsumptionEvidence);
        Assert.Null(results.Single(x => x.CampaignVinEntryID == "other-entry").ServiceConsumptionEvidence);
        Assert.Equal("OTHER", Assert.Single(offer.ServiceConsumption.PackageCodes));
        Assert.Null(Assert.Single(await Evaluate(Aggregate(Start), false, offer)).ServiceConsumptionEvidence);
    }

    [Fact]
    public async Task Entry_menu_does_not_enable_consumption_when_catalog_rule_is_absent()
    {
        var aggregate = Aggregate(Start);
        aggregate.CampaignVinEntries[0].PackageCode = "BASIC";
        var offer = Offer();
        offer.ServiceConsumption = null;
        var result = Assert.Single(await Evaluate(aggregate, false, offer));
        Assert.Null(result.ServiceConsumption);
        Assert.Null(result.ServiceConsumptionEvidence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Blank_entry_menu_keeps_ordinary_model_applicability(string? menu)
    {
        var aggregate = Aggregate(null);
        aggregate.CampaignVinEntries[0].PackageCode = menu;
        var offer = Offer();
        offer.ModelCosts = [new() { Katashiki = "UNRESOLVED" }];
        Assert.Empty(await Evaluate(aggregate, false, offer));
        offer.ModelCosts = [];
        offer.PackageCode = "CATALOG-MENU";
        Assert.Equal("CATALOG-MENU", Assert.Single(await Evaluate(aggregate, false, offer)).PackageCode);
    }

    [Fact]
    public async Task A_menu_on_one_activation_does_not_grant_other_activations_model_applicability()
    {
        var aggregate = Aggregate(null);
        aggregate.CampaignVinEntries[0].PackageCode = "VIN-MENU";
        aggregate.CampaignVinEntries.Add(new() { id = "without-menu", VIN = Vin, CampaignID = 500, RecordedDate = Start });
        var offer = Offer();
        offer.ModelCosts = [new() { Katashiki = "UNRESOLVED" }];
        offer.CampaignActivationType = ClaimableItemCampaignActivationTypes.EveryTrigger;
        Assert.Equal("entry", Assert.Single(await Evaluate(aggregate, false, offer)).CampaignVinEntryID);
        aggregate.CampaignVinEntries[0].IsDeleted = true;
        Assert.Empty(await Evaluate(aggregate, false, offer));
    }

    [Fact]
    public async Task Opted_in_entry_menu_keeps_a_gray_offer_visible_even_while_the_menu_is_blank()
    {
        var aggregate = Aggregate(null);
        var offer = Offer();
        offer.UseCampaignVinEntryPackageCode = true;
        offer.ModelCosts = [new() { Katashiki = "UNRESOLVED", PackageCode = "MODEL-MENU", Cost = 999 }];
        offer.PackageCode = "CATALOG-MENU";
        var result = Assert.Single(await Evaluate(aggregate, false, offer));
        Assert.Null(result.PackageCode);
        Assert.Null(result.ModelCostID);
        Assert.Equal(offer.FixedCost, result.Cost);
        aggregate.CampaignVinEntries.Clear();
        Assert.Empty(await Evaluate(aggregate, false, offer));
    }

    [Fact]
    public async Task Inferred_consumption_does_not_cancel_other_pending_items()
    {
        var high = Offer();
        high.MaximumMileage = 10000;
        var low = Offer();
        low.IntegrationID = "other";
        low.ServiceConsumption = null;
        low.MaximumMileage = 5000;
        low.ActiveFor = 12;
        var items = await Evaluate(Aggregate(Start), false, high, low);
        Assert.Equal("pending", items.Single(x => x.ServiceItemID == "other").Status);
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }
}

using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

/// <summary>
/// Several campaigns activated by one inspection type, each limited to its own inspecting companies
/// by a condition that names no type: an inspection earns only the benefits of the company that did it.
/// </summary>
public class SharedInspectionTypeTests
{
    private const string Vin = "SYNTHETIC-VEHICLE";
    private const long SharedType = 15;
    private static readonly DateTime Start = new(2026, 1, 1);

    private static ServiceItemModel Voucher(string id, long campaign, params long[] companies) => new()
    {
        IntegrationID = id, CampaignID = campaign, CampaignStartDate = Start, CampaignEndDate = Start.AddYears(1),
        VehicleInspectionTypeID = SharedType,
        CampaignActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
        CampaignActivationType = ClaimableItemCampaignActivationTypes.FirstTriggerOnly,
        ValidityMode = ClaimableItemValidityMode.FixedDateRange, ValidFrom = Start, ValidTo = Start.AddYears(1),
        EligibilityConditions = [new() { AnyOf = companies.Select(company => new EligibilityConditionModel
        {
            Field = "vehicleInspections.companyID", Values = [company.ToString()],
            Scope = new() { Selection = EligibilityConditionSelection.All },
        }).ToList() }],
    };

    [Theory]
    [InlineData(new long[] { 5 }, new[] { "a" })]
    [InlineData(new long[] { 7 }, new[] { "a" })]
    [InlineData(new long[] { 10 }, new[] { "b" })]
    [InlineData(new long[] { 9 }, new[] { "c" })]
    [InlineData(new long[] { 5, 10 }, new[] { "a", "b" })]
    [InlineData(new long[] { 99 }, new string[0])]
    public async Task Each_inspection_earns_only_its_own_companies_benefits_online_and_in_bulk(long[] inspectingCompanies, string[] expected)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin };
        var day = 1;
        foreach (var company in inspectingCompanies)
            aggregate.VehicleInspections.Add(new() { id = $"inspection-{company}", VIN = Vin, VehicleInspectionTypeID = SharedType,
                CompanyID = company, InspectionDate = Start.AddDays(day++) });
        var items = new List<ServiceItemModel> { Voucher("a", 1, 5, 6, 7), Voucher("b", 2, 10), Voucher("c", 3, 8, 9) };
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(items);
        var service = new VehicleLookupService(storage.Object, options: new LookupOptions { TimeProvider = new Clock() });

        var online = await service.LookupAsync(Vin);
        var bulk = await service.LookupAsync(aggregate);

        Assert.Equal(expected, online.ServiceItems.Select(x => x.ServiceItemID).Order());
        Assert.Equal(expected, bulk.ServiceItems.Select(x => x.ServiceItemID).Order());
    }

    [Fact]
    public async Task Moving_the_campaign_to_another_type_moves_the_condition_with_it()
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin, VehicleInspections =
            [new() { id = "old", VIN = Vin, VehicleInspectionTypeID = 12, CompanyID = 5, InspectionDate = Start.AddDays(1) }] };
        var item = Voucher("a", 1, 5);
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel> { item });
        var service = new VehicleLookupService(storage.Object, options: new LookupOptions { TimeProvider = new Clock() });

        Assert.Empty((await service.LookupAsync(Vin)).ServiceItems);
        item.VehicleInspectionTypeID = 12;
        Assert.Equal("old", Assert.Single((await service.LookupAsync(Vin)).ServiceItems).VehicleInspectionID);
    }

    // The same split by the campaigns' own Companies, with no conditions: the companies are the
    // inspecting ones, so a vehicle owned elsewhere or by no one still earns its inspector's benefit.
    private static ServiceItemModel CompanyScopedVoucher(string id, long campaign, params long[] companies)
    {
        var item = Voucher(id, campaign);
        item.EligibilityConditions = null;
        item.CompanyIDs = companies.Select(c => (long?)c).ToList();
        return item;
    }

    [Theory]
    [InlineData(new long[] { 5 }, null, new[] { "a" })]
    [InlineData(new long[] { 10 }, 5L, new[] { "b" })]
    [InlineData(new long[] { 9 }, 5L, new[] { "c" })]
    [InlineData(new long[] { 5, 10 }, null, new[] { "a", "b" })]
    [InlineData(new long[] { 99 }, 5L, new string[0])]
    public async Task Campaign_companies_match_the_inspecting_company_not_the_owner(long[] inspectingCompanies, long? ownerCompany, string[] expected)
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin };
        if (ownerCompany is not null)
            aggregate.VehicleEntries.Add(new() { VIN = Vin, CompanyID = ownerCompany, CountryID = 1 });
        var day = 1;
        foreach (var company in inspectingCompanies)
            aggregate.VehicleInspections.Add(new() { id = $"inspection-{company}", VIN = Vin, VehicleInspectionTypeID = SharedType,
                CompanyID = company, InspectionDate = Start.AddDays(day++) });
        var items = new List<ServiceItemModel> { CompanyScopedVoucher("a", 1, 5, 6, 7), CompanyScopedVoucher("b", 2, 10), CompanyScopedVoucher("c", 3, 8, 9) };
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(items);
        var service = new VehicleLookupService(storage.Object, options: new LookupOptions { TimeProvider = new Clock() });

        var online = await service.LookupAsync(Vin);
        var bulk = await service.LookupAsync(aggregate);

        Assert.Equal(expected, online.ServiceItems.Select(x => x.ServiceItemID).Order());
        Assert.Equal(expected, bulk.ServiceItems.Select(x => x.ServiceItemID).Order());
    }

    [Fact]
    public async Task First_trigger_is_the_first_inspection_by_one_of_the_campaigns_companies()
    {
        var aggregate = new CompanyDataAggregateModel { VIN = Vin, VehicleInspections =
        [
            new() { id = "other-market", VIN = Vin, VehicleInspectionTypeID = SharedType, CompanyID = 10, InspectionDate = Start.AddDays(1) },
            new() { id = "own-market", VIN = Vin, VehicleInspectionTypeID = SharedType, CompanyID = 5, InspectionDate = Start.AddDays(2) },
        ] };
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel> { CompanyScopedVoucher("a", 1, 5) });
        var service = new VehicleLookupService(storage.Object, options: new LookupOptions { TimeProvider = new Clock() });

        Assert.Equal("own-market", Assert.Single((await service.LookupAsync(Vin)).ServiceItems).VehicleInspectionID);
    }

    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Start.AddDays(10), TimeSpan.Zero); }
}

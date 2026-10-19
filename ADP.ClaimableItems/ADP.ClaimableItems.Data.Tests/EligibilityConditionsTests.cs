using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ShiftSoftware.ADP.Cases.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ADP.ClaimableItems.Shared.Localization;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.Vehicle;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

public class EligibilityConditionsTests
{
    private const string Conditions = """
        [
          {"field":"campaignVinEntries.campaignID","operator":"ContainsAll","values":["500"]},
          {"field":"serviceItems.baseSchedule.maximumMileage","operator":"Equals","values":["40000"]},
          {"field":"serviceHistory.laborLines.packageCode","operator":"ContainsAll",
           "valueMatch":"EndsWith","scope":{"selection":"Latest","count":2},"values":[" 45K"," 50K"]},
          {"field":"serviceHistory.laborLines.packageCode","operator":"ContainsAll","valueMatch":"Milestone",
           "program":["SER"],"qualifier":{"selection":"Only","values":["PLUS"]},
           "scope":{"selection":"All"},"values":["45000","50000"],"whenUnmet":"Lock"},
          {"field":"serviceHistory.laborLines.maximumMilestone","operator":"Equals","values":["50000"],
           "program":["SER"],"qualifier":{"selection":"Any"},"whenUnmet":"Miss"},
          {"anyOf":[{"field":"vehicleClassifications.code","values":["member"]},{"field":"campaignVinEntries.campaignID","values":["500"]}],"whenUnmet":"Hide"}
        ]
        """;

    [Fact]
    public async Task Authored_json_survives_the_api_mapper_database_and_reopening_then_replicates_every_field()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ShiftDbContext, ConditionsDb>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IModelBuildingContributor>(new CasesModelBuildingContributor(schema: null));
        services.AddSingleton<IModelBuildingContributor>(new ClaimableItemsModelBuildingContributor(schema: null));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShiftDbContext>();
        var mapper = ModuleMapper.For<ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>();
        var item = new ClaimableItem { Name = "{}", Costs = "[]" };
        var dto = mapper.MapToView(item);
        // The API serializes the authored JSON as a DTO string, without rewriting its contents.
        dto.EligibilityConditions = Conditions;
        dto = JsonSerializer.Deserialize<ClaimableItemDTO>(JsonSerializer.Serialize(dto))!;
        mapper.MapToEntity(dto, item);
        db.Add(item);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var saved = await db.Set<ClaimableItem>().SingleAsync();
        Assert.Equal(Conditions, mapper.MapToView(saved).EligibilityConditions);

        saved.Campaign = new Campaign
        {
            ID = 500, ActivationTrigger = ClaimableItemCampaignActivationTrigger.VehicleInspection,
            VehicleInspectionTypeID = 10,
        };
        saved.ValidityMode = ClaimableItemValidityMode.RelativeToActivation;
        saved.ActiveFor = 3;
        saved.ActiveForDurationType = DurationType.Months;
        var document = ClaimableItemsReplicationExtensions.ToServiceItemModel(saved);
        Assert.Equal(JsonSerializer.Serialize(EligibilityConditionsJson.Parse(Conditions)), JsonSerializer.Serialize(document.EligibilityConditions));
        Assert.Equal(6, document.EligibilityConditions.Count());
        Assert.True(EligibilityConditionGroup.IsValid(document.EligibilityConditions.Last()));
        Assert.True(EligibilityConditionsJson.IsValid(JsonSerializer.Serialize(document.EligibilityConditions)));
        Assert.True(EligibilityConditionsJson.IsValid(Newtonsoft.Json.JsonConvert.SerializeObject(document.EligibilityConditions)));
        Assert.Equal(" 45K", document.EligibilityConditions.ElementAt(2).Values.First());
        Assert.Equal(ClaimableItemCampaignActivationTrigger.VehicleInspection, document.CampaignActivationTrigger);
        Assert.Equal(10, document.VehicleInspectionTypeID);
        Assert.Equal(3, document.ActiveFor);
        Assert.Equal(DurationType.Months, document.ActiveForDurationType);

        // The campaign above belongs only to the projection check, not this database fixture.
        saved.Campaign = null;
        dto = mapper.MapToView(saved);
        dto.EligibilityConditions = null;
        mapper.MapToEntity(dto, saved);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Null((await db.Set<ClaimableItem>().SingleAsync()).EligibilityConditions);
        Assert.Null(ClaimableItemsReplicationExtensions.ToServiceItemModel(saved).EligibilityConditions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    [InlineData("null")]
    [InlineData("[]")]
    public void Ordinary_items_round_trip_without_extra_conditions(string? json)
    {
        var mapper = ModuleMapper.For<ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>();
        var item = new ClaimableItem { Name = "{}", Costs = "[]", EligibilityConditions = json };
        var dto = mapper.MapToView(item);
        Assert.Equal(json, dto.EligibilityConditions);
        mapper.MapToEntity(dto, item);
        Assert.Equal(json, item.EligibilityConditions);
        Assert.True(EligibilityConditionsJson.IsValid(json));
        var published = ClaimableItemsReplicationExtensions.ToServiceItemModel(item).EligibilityConditions;
        if (json == "[]") Assert.Empty(published); else Assert.Null(published);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[] }]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\" \"]}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[500]}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\"500\"],\"operator\":\"Typo\"}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\"500\"],\"operator\":99}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\"500\"],\"whenUnmett\":\"Lock\"}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\"500\"],\"scope\":{\"countt\":2}}]")]
    [InlineData("[{\"field\":\"x\",\"values\":[\"500\"],\"field\":\"y\"}]")]
    [InlineData("""[{"anyOf":[]}]""")]
    [InlineData("""[{"anyOf":null}]""")]
    [InlineData("""[{"anyOf":[null]}]""")]
    [InlineData("""[{"anyOf":[{"field":"unknown","values":["x"]}]}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"],"whenUnmet":"Lock"}]}]""")]
    [InlineData("""[{"anyOf":[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"]}]}]}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"]}],"field":"vehicleClassifications.code","values":["member"]}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"],"operator":"Or"}]}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"]}],"unknown":true}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"],"scope":{"selection":"All","typo":true}}]}]""")]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"],"field":"unknown"}]}]""")]
    // Top-level leaves follow the same shape rules as alternatives; each of these would hide the item for every vehicle.
    [InlineData("""[{"field":"campaignVinEntries.campaignId","values":["500"]}]""")]
    [InlineData("""[{"field":"isAuthorized","operator":"Equals","values":["maybe"]}]""")]
    [InlineData("""[{"field":"vehicleInspections.companyID","values":["5"],"scope":{"selection":"All","inspectionTypeID":0}}]""")]
    [InlineData("""[{"field":"serviceHistory.laborLines.packageCode","values":["45000"]}]""")]
    public void Api_validator_rejects_invalid_json_instead_of_dropping_conditions(string json)
    {
        // Both the API and form discover validators alongside their DTOs.
        Assert.Contains(AssemblyScanner.FindValidatorsInAssembly(typeof(ClaimableItemDTO).Assembly),
            v => v.ValidatorType == typeof(ClaimableItemValidator));
        var validator = new ClaimableItemValidator(new TestLocalizer());
        var result = validator.Validate(new ClaimableItemDTO { EligibilityConditions = json });
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ClaimableItemDTO.EligibilityConditions));
        Assert.Throws<JsonException>(() => ClaimableItemsReplicationExtensions.ToServiceItemModel(
            new ClaimableItem { Name = "{}", Costs = "[]", EligibilityConditions = json }));
    }

    [Fact]
    public void Existing_pascal_case_and_numeric_enums_remain_supported()
    {
        var condition = Assert.Single(EligibilityConditionsJson.Parse("""[{"Field":"campaignVinEntries.campaignID","Operator":0,"Values":["500"]}]""")!);
        Assert.Equal(EligibilityConditionOperator.ContainsAll, condition.Operator);
        Assert.Equal(EligibilityConditionUnmetBehavior.Hide, condition.WhenUnmet);
    }

    [Theory]
    [InlineData("""[{"field":"isAuthorized","operator":"Equals","values":["false"]}]""")]
    [InlineData("""[{"field":"vehicleInspections.companyID","values":["5"],"scope":{"selection":"All","inspectionTypeID":10},"whenUnmet":"Lock"}]""")]
    [InlineData("""[{"field":"vehicleInspections.companyID","values":["5"],"scope":{"selection":"All"}}]""")]
    [InlineData("""[{"field":"vehicleClassifications.code","values":["member"],"whenUnmet":"Miss"}]""")]
    public void Supported_top_level_leaves_keep_their_own_unmet_behavior(string json) =>
        Assert.True(EligibilityConditionsJson.IsValid(json));

    private class ConditionsDb(DbContextOptions<ConditionsDb> options) : ShiftDbContext(options);

    private sealed class TestLocalizer : IStringLocalizer<ClaimableItemsResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}

using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Extensions;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>
/// The replication projections in <see cref="ClaimableItemsReplicationExtensions"/>. The trigger and a consumer's
/// catch-up both map through them, and both swallow a projection that throws (the row stays dirty, no document is
/// written), so a projection that cannot handle a row shows up here or nowhere.
/// </summary>
public class ReplicationProjectionTests
{
    private static ClaimableItem AnItem(Campaign? campaign) => new()
    {
        ID = 7,
        Name = """{"en":"Oil change","ru":"Замена масла"}""",
        CampaignID = campaign?.ID,
        Campaign = campaign,
        CostingType = ClaimableItemCostingType.Fixed,
        FixedCost = 12.5m,
        Costs = "[]",
        ValidityMode = ClaimableItemValidityMode.RelativeToActivation,
        ActiveFor = 12,
        ActiveForDurationType = DurationType.Months,
    };

    [Fact]
    public void An_item_without_its_campaign_still_gets_a_document_with_default_campaign_fields()
    {
        // The original host's AutoMapper map null-propagated through the navigation.
        var document = ClaimableItemsReplicationExtensions.ToServiceItemModel(AnItem(campaign: null));

        Assert.Equal("7", document.id);
        Assert.Equal("7", document.IntegrationID);
        Assert.Equal(default, document.CampaignStartDate);
        Assert.Equal(default, document.CampaignEndDate);
        Assert.Null(document.CampaignUniqueReference);
        Assert.Empty(document.CampaignName);
        Assert.Empty(document.BrandIDs);
        Assert.Empty(document.CountryIDs);
        Assert.Empty(document.CompanyIDs);
        Assert.Null(document.VehicleInspectionTypeID);
        Assert.Equal(12.5m, document.FixedCost);
    }

    [Fact]
    public void An_item_with_its_campaign_carries_the_campaign_fields()
    {
        var campaign = new Campaign
        {
            ID = 3,
            Name = """{"en":"Winter","ru":"Зима"}""",
            UniqueReference = "winter-2026",
            StartDate = new DateTime(2026, 1, 1),
            ExpireDate = new DateTime(2026, 3, 31),
            Brands = [2],
            Countries = [3, 5],
            Companies = [],
            VehicleInspectionTypeID = 9,
        };

        var document = ClaimableItemsReplicationExtensions.ToServiceItemModel(AnItem(campaign));

        Assert.Equal(new DateTime(2026, 1, 1), document.CampaignStartDate);
        Assert.Equal(new DateTime(2026, 3, 31), document.CampaignEndDate);
        Assert.Equal("winter-2026", document.CampaignUniqueReference);
        Assert.Equal("Winter", document.CampaignName["en"]);
        Assert.Equal([2L], document.BrandIDs.Select(x => x!.Value));
        Assert.Equal([3L, 5L], document.CountryIDs.Select(x => x!.Value));
        Assert.Empty(document.CompanyIDs);
        Assert.Equal(9, document.VehicleInspectionTypeID);
    }
}

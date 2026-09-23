using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Xunit;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Tests;

/// <summary>
/// The maps in <c>Mappers/ClaimableItemsMapper.cs</c>, run through the mapper a host resolves. Each rule here was a
/// repository's <c>UseGeneratedMapper</c> configuration before it became a <c>CreateMap</c>; the assertions are what the
/// previous generated maps did, so a map that stopped doing it shows up here.
/// </summary>
public class ClaimableItemsMapperTests
{
    private static IEnumerable<(Type Entity, Type List, Type View)> RepositoryTriples() =>
        typeof(Marker).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.BaseType)
            .Where(b => b is { IsGenericType: true } && b.GetGenericTypeDefinition() == typeof(ShiftRepository<,,,>))
            .Select(b => b!.GetGenericArguments())
            .Select(a => (a[1], a[2], a[3]));

    /// <summary>A repository resolves its mapper only when all four of its maps exist.</summary>
    [Fact]
    public void Every_repository_triple_is_mapped_in_all_four_directions()
    {
        var mapper = ModuleMapper.Mapper();

        var triples = RepositoryTriples().ToList();
        Assert.NotEmpty(triples);

        foreach (var (entity, list, view) in triples)
        {
            Assert.True(mapper.CanMap(entity, view), $"{entity.Name} -> {view.Name}");
            Assert.True(mapper.CanMap(view, entity), $"{view.Name} -> {entity.Name}");
            Assert.True(mapper.CanMap(entity, list), $"{entity.Name} -> {list.Name}");
            Assert.True(mapper.CanMap(entity, entity), $"{entity.Name} -> {entity.Name}");
        }
    }

    [Fact]
    public void Startup_mapping_validation_passes() =>
        ShiftEntityMapperValidation.Validate(ModuleMapper.Services(), [typeof(Marker).Assembly]);

    // ───── Campaign ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_campaigns_id_lists_are_value_only_selects_and_save_back_as_ids()
    {
        var mapper = ModuleMapper.For<Campaign, CampaignListDTO, CampaignDTO>();
        var stored = new Campaign { Name = "C", Brands = [1, 2], Companies = [3], Countries = [4] };

        var dto = mapper.MapToView(stored);

        Assert.Equal(["1", "2"], dto.Brands.Select(x => x.Value));
        Assert.All(dto.Brands, x => Assert.Null(x.Text));
        Assert.Equal("3", Assert.Single(dto.Companies).Value);
        Assert.Equal("4", Assert.Single(dto.Countries).Value);

        dto.Brands = [new ShiftEntitySelectDTO { Value = "9" }];
        dto.Countries = [];
        mapper.MapToEntity(dto, stored);

        Assert.Equal([9L], stored.Brands);
        Assert.Equal([3L], stored.Companies);
        Assert.Empty(stored.Countries);
    }

    // ───── CampaignVinEntry ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void A_vin_entry_list_row_carries_its_campaign()
    {
        var mapper = ModuleMapper.For<CampaignVinEntry, CampaignVinEntryListDTO, CampaignVinEntryDTO>();

        var row = mapper.MapToList(new[] { new CampaignVinEntry { Campaign = new Campaign { Name = "C", UniqueReference = "REF" } } }.AsQueryable()).Single();
        Assert.Equal("C", row.CampaignName);
        Assert.Equal("REF", row.CampaignUniqueReference);

        var orphan = mapper.MapToList(new[] { new CampaignVinEntry() }.AsQueryable()).Single();
        Assert.Null(orphan.CampaignName);
        Assert.Null(orphan.CampaignUniqueReference);
    }

    // ───── ClaimableItem ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void The_costs_column_round_trips_with_default_serializer_options()
    {
        var mapper = ModuleMapper.For<ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>();

        // PascalCase: what every stored row already holds. Other options would orphan these names.
        var stored = new ClaimableItem { Costs = "[{\"Katashiki\":\"K1\",\"Variant\":null,\"Cost\":12.5,\"PackageCode\":\"P\"}]" };

        var dto = mapper.MapToView(stored);

        var cost = Assert.Single(dto.Costs);
        Assert.Equal("K1", cost.Katashiki);
        Assert.Equal(12.5m, cost.Cost);

        mapper.MapToEntity(dto, stored);

        Assert.Equal("[{\"Katashiki\":\"K1\",\"Variant\":null,\"Cost\":12.5,\"PackageCode\":\"P\"}]", stored.Costs);
    }

    [Fact]
    public void A_claimable_item_list_row_carries_its_campaign()
    {
        var mapper = ModuleMapper.For<ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO>();
        var campaign = new Campaign
        {
            Name = "C",
            StartDate = new DateTime(2026, 1, 1),
            ExpireDate = new DateTime(2026, 12, 31),
            ActivationTrigger = Models.Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry,
        };

        var row = mapper.MapToList(new[] { new ClaimableItem { Costs = "[]", Campaign = campaign } }.AsQueryable()).Single();

        Assert.Equal("C", row.CampaignName);
        Assert.Equal(new DateTime(2026, 1, 1), row.CampaignStartDate);
        Assert.Equal(new DateTime(2026, 12, 31), row.CampaignExpireDate);
        Assert.Equal(Models.Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry, row.CampaignActivationTrigger);

        var orphan = mapper.MapToList(new[] { new ClaimableItem { Costs = "[]" } }.AsQueryable()).Single();
        Assert.Null(orphan.CampaignName);
        Assert.Null(orphan.CampaignStartDate);
    }

    // ───── ItemClaim ────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null, YesNoOptions.No)]
    [InlineData("[]", YesNoOptions.No)]
    [InlineData("[{\"Name\":\"a.jpg\"}]", YesNoOptions.Yes)]
    public void An_empty_attachment_array_is_no_attachment(string? attachments, YesNoOptions expected)
    {
        var row = ModuleMapper.For<ItemClaim, ItemClaimListDTO, ItemClaimDTO>()
            .MapToList(new[] { new ItemClaim { Attachments = attachments } }.AsQueryable())
            .Single();

        Assert.Equal(expected, row.HasAttachment);
    }

    /// <summary>
    /// The certificate view runs this list projection in memory over claims loaded WITHOUT their campaign, so every
    /// flattening has to survive a null navigation.
    /// </summary>
    [Fact]
    public void An_item_claim_list_row_survives_unloaded_navigations()
    {
        var claim = new ItemClaim
        {
            ClaimableItem = new ClaimableItem { Name = "Oil change", Costs = "[]" },
            ReimbursementCertificate = new Certificate { CertificateDate = new DateTime(2020, 3, 1) },
        };

        var row = ModuleMapper.For<ItemClaim, ItemClaimListDTO, ItemClaimDTO>()
            .MapToList(new[] { claim }.AsQueryable())
            .Single();

        Assert.Null(row.CampaignName);
        Assert.Equal("Oil change", row.ClaimableItemName);
        Assert.Equal(new DateTime(2020, 3, 1), row.ReimbursementCertificateCertificateDate);
        Assert.Null(row.ContributionCertificateCertificateDate);

        // Left for a consumer's derived repository to fill.
        Assert.Null(row.VehicleInspectionResultVehicleInspectionTypeID);
        Assert.Null(row.VehicleInspectionResultVehicleInspectionTypeName);
    }

    /// <summary>
    /// `CampaignVINEntry` on the DTO, `CampaignVinEntryID` on the entity: the case differs, and the key has to cross in
    /// both directions anyway - the claim controller saves a claim by naming the VIN entry this way.
    /// </summary>
    [Fact]
    public void The_vin_entry_crosses_both_ways_despite_the_case_difference()
    {
        var mapper = ModuleMapper.For<ItemClaim, ItemClaimListDTO, ItemClaimDTO>();
        var stored = new ItemClaim { VIN = "V", CampaignVinEntryID = 42 };

        var dto = mapper.MapToView(stored);
        Assert.Equal("42", dto.CampaignVINEntry!.Value);
        Assert.Null(dto.CampaignVINEntry.Text);

        dto.CampaignVINEntry = new ShiftEntitySelectDTO { Value = "43" };
        mapper.MapToEntity(dto, stored);
        Assert.Equal(43, stored.CampaignVinEntryID);

        // A blank select clears it, as the previous generated map did.
        dto.CampaignVINEntry = null;
        mapper.MapToEntity(dto, stored);
        Assert.Null(stored.CampaignVinEntryID);
    }

    [Fact]
    public void An_item_claim_view_never_sends_the_resubmit_command_back()
    {
        var dto = ModuleMapper.For<ItemClaim, ItemClaimListDTO, ItemClaimDTO>().MapToView(new ItemClaim { VIN = "V" });

        Assert.False(dto.ReSubmitForDistributorReview);
    }

    // ───── Item-claim certificate ───────────────────────────────────────────────────────────────

    [Fact]
    public void A_certificate_view_leaves_the_claims_to_the_repository()
    {
        var dto = ModuleMapper.For<Certificate, CertificateListDTO, ItemClaimCertificateDTO>().MapToView(new Certificate { ID = 3 });

        Assert.Empty(dto.ReimbursementItemClaims);
        Assert.Null(dto.Notes);
    }
}

using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The maps in <c>Mappers/WarrantyClaimsMapper.cs</c> that are not about the dealer/distributor split (that is
/// <see cref="DealerFinancialExposureTests"/>): the claim's shared write map and a certificate's claim lines.
/// </summary>
public class WarrantyClaimsMapperTests
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

    private static Entities.WarrantyClaim AStoredClaim()
    {
        var claim = new Entities.WarrantyClaim
        {
            ID = 7,
            VIN = "MAPPERTESTVIN0001",
            ClaimNumber = "CLAIM-7",
            WarrantyType = "W",
            Franchise = "F",
            RepairOrderNo = "RO-1",
            DataID = "D",
            Condition = "C",
            Cause = "C",
            Remedy = "R",
            VIN_WMI = "A",
            VIN_VDS = "B",
            VIN_CD = "C",
            VIN_VIS = "D",
            ProcessDate = new DateTime(2024, 3, 4),
        };

        claim.WarrantyClaimLaborLines.Add(new Entities.WarrantyClaimLaborLine { ID = 11, PayCode = "OLD", OperationNumber = "OP-1", Hour = 1 });
        claim.WarrantyClaimPartLines.Add(new Entities.WarrantyClaimPartLine { ID = 21, PartNumber = "OLD-PART", Price = 5 });

        return claim;
    }

    /// <summary>
    /// A save REPLACES the line collections: <c>UpsertAsync</c> has already deleted the stored rows, so the write map
    /// builds a fresh set - scalars only, never the line's identity.
    /// </summary>
    [Fact]
    public void A_save_replaces_the_line_collections_with_new_lines()
    {
        var mapper = ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>();
        var stored = AStoredClaim();

        var dto = mapper.MapToView(stored);
        dto.WarrantyClaimLaborLines = [new WarrantyClaimLaborLineDTO { ID = 11, PayCode = "NEW", OperationNumber = "OP-2", Hour = 2.5m }];
        dto.WarrantyClaimPartLines = [new WarrantyClaimPartLineDTO { ID = 21, PartNumber = "NEW-PART", Price = null, Qty = 3, Loading = true }];

        mapper.MapToEntity(dto, stored);

        var labor = Assert.Single(stored.WarrantyClaimLaborLines);
        Assert.Equal(0, labor.ID);
        Assert.Equal("NEW", labor.PayCode);
        Assert.Equal("OP-2", labor.OperationNumber);
        Assert.Equal(2.5m, labor.Hour);

        // A blank price is zero on the non-nullable column, as the old line writer wrote it.
        var part = Assert.Single(stored.WarrantyClaimPartLines);
        Assert.Equal(0, part.ID);
        Assert.Equal("NEW-PART", part.PartNumber);
        Assert.Equal(0m, part.Price);
        Assert.Equal(3, part.Qty);
    }

    /// <summary>A form whose grid was cleared sends an empty list, and the save empties the collection.</summary>
    [Fact]
    public void A_save_that_sends_an_empty_line_collection_empties_it()
    {
        var mapper = ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>();
        var stored = AStoredClaim();

        var dto = mapper.MapToView(stored);
        dto.WarrantyClaimPartLines = [];

        mapper.MapToEntity(dto, stored);

        Assert.Empty(stored.WarrantyClaimPartLines);
        Assert.Equal("OLD", Assert.Single(stored.WarrantyClaimLaborLines).PayCode);
    }

    [Fact]
    public void The_view_carries_the_lines()
    {
        var dto = ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>().MapToView(AStoredClaim());

        Assert.Equal("OLD", Assert.Single(dto.WarrantyClaimLaborLines).PayCode);

        var part = Assert.Single(dto.WarrantyClaimPartLines);
        Assert.Equal("OLD-PART", part.PartNumber);
        Assert.False(part.Loading);
    }

    /// <summary>
    /// A certificate's claim line: the claim's id as Value and its claim NUMBER as Text - the business identifier the
    /// printout shows - with the rest matched by name.
    /// </summary>
    [Fact]
    public void A_certificate_line_names_the_claim_by_its_number()
    {
        var line = ModuleMapper.Mapper().Map<Entities.WarrantyClaim, WarrantyCertificateLineDTO>(AStoredClaim());

        Assert.Equal("7", line.WarrantyClaim!.Value);
        Assert.Equal("CLAIM-7", line.WarrantyClaim.Text);
        Assert.Equal(new DateTime(2024, 3, 4), line.ProcessDate);
        Assert.Equal("W", line.WarrantyType);
    }

    [Fact]
    public void A_certificate_view_leaves_the_claims_to_the_repository()
    {
        var dto = ModuleMapper.For<Certificate, ShiftSoftware.ADP.Cases.Shared.DTOs.Certificate.CertificateListDTO, CertificateDTO>()
            .MapToView(new Certificate { ID = 3 });

        Assert.Empty(dto.WarrantyClaims);
        Assert.Null(dto.Notes);
    }

    /// <summary>The claim list's own columns: the TimeSpan.Zero dates and the "[]"-aware attachment flag.</summary>
    [Fact]
    public void The_claim_list_carries_its_pinned_columns()
    {
        var claim = AStoredClaim();
        claim.Attachments = "[]";
        claim.ReferenceWarrantyClaim = new Entities.WarrantyClaim { ClaimNumber = "REF-1" };

        var row = ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>()
            .MapToList(new[] { claim }.AsQueryable())
            .Single();

        Assert.Equal(new DateTimeOffset(2024, 3, 4, 0, 0, 0, TimeSpan.Zero), row.ProcessDate);
        Assert.Equal(Shared.Enums.YesNoOptions.No, row.HasAttachment);
        Assert.Equal("REF-1", row.ReferenceWarrantyClaimNumber);
    }
}

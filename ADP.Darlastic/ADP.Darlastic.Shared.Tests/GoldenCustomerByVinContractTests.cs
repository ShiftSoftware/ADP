using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Shared.Tests;

/// <summary>
/// The by-VIN read (<c>GET GoldenCustomer/by-vin/{vin}</c>) is a golden read like the list and the
/// detail: names and phones leave the server masked, for every caller, and the raw value of one
/// field comes only from the reveal route. These tests pin the DTO's declarations and run the
/// framework's own protector over a response shaped exactly as the endpoint builds it (raw values
/// in <see cref="PiiFieldDTO.Value"/>, protected just before it is returned). Synthetic values only.
/// </summary>
public class GoldenCustomerByVinContractTests
{
    private const string RawName = "Synthname Testfamily Placeholder";
    private const string RawPhone = "0000 555 0101";
    private const string OtherRawName = "Othername Testfamily";

    private static PiiDtoProtector Protector() =>
        new(new PiiFieldProtection(new DefaultPiiMasker(Options.Create(new PiiOptions()))));

    /// <summary>The response as the endpoint fills it before protecting it.</summary>
    private static GoldenCustomerByVinDTO Response() => new()
    {
        VIN = "SYNTHVIN000000011",
        Goldens =
        [
            new GoldenCustomerVinLinkDTO
            {
                ID = "100", LinkedID = "100", Role = "owner",
                EffectiveFrom = new DateOnly(2024, 3, 1), EffectiveTo = new DateOnly(2025, 3, 1), SaleCount = 1,
                FullName = new PiiFieldDTO { Value = RawName },
                Phone = new PiiFieldDTO { Value = RawPhone },
            },
            new GoldenCustomerVinLinkDTO
            {
                ID = "200", LinkedID = "150", WasRedirected = true, Role = "owner",
                EffectiveFrom = new DateOnly(2025, 3, 1),
                FullName = new PiiFieldDTO { Value = OtherRawName },
                Phone = new PiiFieldDTO { Value = null },
            },
            new GoldenCustomerVinLinkDTO
            {
                ID = "300", LinkedID = "300", Role = "service-contact", AwaitingResolve = true,
                FullName = new PiiFieldDTO { Value = null },
                Phone = new PiiFieldDTO { Value = null },
            },
        ],
    };

    [Theory]
    [InlineData(nameof(GoldenCustomerVinLinkDTO.FullName), PiiKind.Name)]
    [InlineData(nameof(GoldenCustomerVinLinkDTO.Phone), PiiKind.Phone)]
    public void NameAndPhone_AreRevealableProtectedFields(string member, PiiKind kind)
    {
        var property = typeof(GoldenCustomerVinLinkDTO).GetProperty(member)!;
        var declaration = property.GetCustomAttribute<PiiAttribute>();

        Assert.Equal(typeof(PiiFieldDTO), property.PropertyType);
        Assert.True(property.CanWrite);
        Assert.NotNull(declaration);
        Assert.Equal(kind, declaration.Kind);
        Assert.True(declaration.Revealable);

        // The reveal route decides by the detail DTO's declarations: the same member must be
        // revealable there, or a masked value here could never be revealed.
        var detail = typeof(GoldenCustomerDetailDTO).GetProperty(member)!;
        Assert.Equal(kind, detail.GetCustomAttribute<PiiAttribute>()!.Kind);
    }

    [Fact]
    public void OnlyNameAndPhone_AreProtected_AndNothingElseCarriesThem()
    {
        Assert.Equal(["FullName", "Phone"],
            PiiDtoProtector.GetProtectedMemberNames(typeof(GoldenCustomerByVinDTO)).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(typeof(GoldenCustomerVinLinkDTO).GetProperties(),
            p => p.PropertyType == typeof(string) && p.Name.Contains("Name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Protect_MasksEveryLink_AndNoRawValueLeavesTheServer()
    {
        var dto = Response();

        Protector().Protect(dto);
        string json = JsonSerializer.Serialize(dto);

        Assert.DoesNotContain(RawName, json);
        Assert.DoesNotContain(OtherRawName, json);
        Assert.DoesNotContain(RawPhone, json);
        Assert.DoesNotContain("555", json);                               // only the masked tail survives
        Assert.All(dto.Goldens, g =>
        {
            Assert.Null(g.FullName!.Value);
            Assert.Null(g.Phone!.Value);
            Assert.Equal("keep", g.FullName.Write);
        });
        Assert.Equal("S T P", dto.Goldens[0].FullName!.Display);           // initials only
        Assert.Equal("•••• 0101", dto.Goldens[0].Phone!.Display);
        Assert.Equal("O T", dto.Goldens[1].FullName!.Display);
        Assert.Null(dto.Goldens[2].FullName!.Display);                      // nothing to show, nothing invented
    }

    [Fact]
    public void Protect_LeavesTheVehicleFactsAlone()
    {
        var dto = Response();

        Protector().Protect(dto);

        Assert.Equal("SYNTHVIN000000011", dto.VIN);
        Assert.Equal(["100", "200", "300"], dto.Goldens.Select(g => g.ID).ToArray());
        Assert.Equal("150", dto.Goldens[1].LinkedID);
        Assert.True(dto.Goldens[1].WasRedirected);
        Assert.Equal(new DateOnly(2025, 3, 1), dto.Goldens[0].EffectiveTo);
        Assert.Equal("service-contact", dto.Goldens[2].Role);
    }
}

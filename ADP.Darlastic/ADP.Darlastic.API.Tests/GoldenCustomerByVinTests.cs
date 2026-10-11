using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Controllers;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Data.Extensions;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.TypeAuth.Core;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// <c>GET GoldenCustomer/by-vin/{vin}</c> against an in-memory registry: input validation (400), the
/// not-found paths (404), masking of every link, and one entry per surviving identity when linked
/// goldens were merged since the resolve. Synthetic values only.
/// </summary>
public class GoldenCustomerByVinTests
{
    private const string Vin = "SYNTHVIN000000011";
    private const string RawName = "Synthname Testfamily";
    private const string RawPhone = "0000 555 0101";
    private const string OtherRawName = "Othername Placeholder";
    private const string OtherRawPhone = "0000 555 0202";

    [Theory]
    [InlineData("")]
    [InlineData("SYNTHVIN00000001")]                 // 16
    [InlineData("SYNTHVIN0000000111")]               // 18
    [InlineData("SYNTHVIN00000001-")]
    [InlineData("SYNTHVIN 00000001")]
    [InlineData("SYNTHVIN00000001٣")]           // a non-ASCII digit
    public async Task NotSeventeenAsciiLettersOrDigits_Is400(string vin)
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", ""));

        var result = await registry.Controller().ByVin(vin);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UnknownVin_Is404()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", ""));

        var result = await registry.Controller().ByVin("SYNTHVIN000000099");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ImplausibleVin_Is404_WithoutAStagedLookup()
    {
        // Seventeen letters, but too few distinct characters for the engine to ever have linked it.
        using var registry = new Registry();
        await registry.StageVinAsync("AAAAAAAAAAAAAAAAA", Owner(100, "2024-01-01", ""));

        var result = await registry.Controller().ByVin("AAAAAAAAAAAAAAAAA");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task TombstonedVin_Is404()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", ""));
        await registry.TombstoneAsync("vinowner", Vin);

        var result = await registry.Controller().ByVin(Vin);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task VinIsTrimmedAndUpperCased_BeforeTheLookup()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", ""));

        var dto = Ok(await registry.Controller().ByVin("  synthvin000000011 "));

        Assert.Equal(Vin, dto.VIN);
        Assert.Equal("100", Assert.Single(dto.Goldens).ID);
    }

    [Fact]
    public async Task WithoutReadAccess_IsForbidden_BeforeAnyValidation()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", ""));

        var controller = registry.Controller(new DarlasticApiOptions { EnableDarlasticActionTreeAuthorization = true });

        Assert.IsType<ForbidResult>((await controller.ByVin(Vin)).Result);
        Assert.IsType<ForbidResult>((await controller.ByVin("bad")).Result);
    }

    [Fact]
    public async Task EveryLink_IsMasked_AndNoRawValueLeavesTheServer()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin,
            Owner(100, "2020-01-01", "2023-01-01"),
            Owner(200, "2023-01-01", ""),
            Contact(300, "2022-05-01", "2022-06-01"));
        await registry.StageGoldenAsync(100, RawName, RawPhone);
        await registry.StageGoldenAsync(200, OtherRawName, OtherRawPhone);
        // 300 has no staged golden: minted since the resolve.

        var dto = Ok(await registry.Controller().ByVin(Vin));
        var json = JsonSerializer.Serialize(dto);

        Assert.Equal(["100", "200", "300"], dto.Goldens.Select(g => g.ID).ToArray());
        Assert.DoesNotContain(RawName, json);
        Assert.DoesNotContain(OtherRawName, json);
        Assert.DoesNotContain(RawPhone, json);
        Assert.DoesNotContain(OtherRawPhone, json);
        Assert.DoesNotContain("Synthname", json);
        Assert.DoesNotContain("Othername", json);
        Assert.DoesNotContain("555", json);                                  // only the masked tail survives
        Assert.All(dto.Goldens, g =>
        {
            Assert.Null(g.FullName!.Value);
            Assert.Null(g.Phone!.Value);
        });
        Assert.Equal("S T", dto.Goldens[0].FullName!.Display);              // initials only
        Assert.Equal("•••• 0101", dto.Goldens[0].Phone!.Display);
        Assert.False(dto.Goldens[0].AwaitingResolve);
        Assert.True(dto.Goldens[2].AwaitingResolve);
        Assert.Null(dto.Goldens[2].FullName!.Display);                      // nothing to show, nothing invented
    }

    [Fact]
    public async Task StagedNameInTheVinPayload_IsNeverServed()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin, Owner(100, "2024-01-01", "", name: RawName));

        var json = JsonSerializer.Serialize(Ok(await registry.Controller().ByVin(Vin)));

        Assert.DoesNotContain(RawName, json);
        Assert.DoesNotContain("Synthname", json);
    }

    [Fact]
    public async Task TwoLinkedGoldensMergedIntoOne_ListTheSurvivorOnce_ByItsStrongestLink()
    {
        using var registry = new Registry();
        await registry.StageVinAsync(Vin,
            Owner(100, "2018-01-01", "2019-01-01", sources: ["s|a"]),         // one year
            Owner(150, "2019-01-01", "2020-01-01", sources: ["s|x"]),         // someone else in between
            Owner(200, "2020-01-01", "", sources: ["s|b"]),                   // still the owner
            Contact(300, "2017-01-01", "2024-01-01", sources: ["s|c"]));      // a long service window
        await registry.RedirectAsync(100, 900);
        await registry.RedirectAsync(200, 800);
        await registry.RedirectAsync(800, 900);                             // a chain: 200 → 800 → 900
        await registry.RedirectAsync(300, 900);
        await registry.StageGoldenAsync(900, RawName, RawPhone);

        var dto = Ok(await registry.Controller().ByVin(Vin));

        Assert.Equal(["150", "900"], dto.Goldens.Select(g => g.ID).ToArray());
        var survivor = dto.Goldens[1];
        Assert.Equal("200", survivor.LinkedID);
        Assert.True(survivor.WasRedirected);
        Assert.Equal("owner", survivor.Role);
        Assert.Equal(new DateOnly(2020, 1, 1), survivor.EffectiveFrom);
        Assert.Null(survivor.EffectiveTo);
        Assert.Equal(["s|b"], survivor.Sources);                            // the kept link's own facts
        Assert.False(survivor.AwaitingResolve);
        Assert.Null(survivor.FullName!.Value);
    }

    private static GoldenCustomerByVinDTO Ok(ActionResult<GoldenCustomerByVinDTO> result) =>
        Assert.IsType<GoldenCustomerByVinDTO>(Assert.IsType<OkObjectResult>(result.Result).Value);

    private static object Owner(long golden, string from, string to, string? name = null, string[]? sources = null) =>
        Link(golden, "owner", from, to, sales: 1, services: 0, name, sources ?? []);

    private static object Contact(long golden, string from, string to, string[]? sources = null) =>
        Link(golden, "service-contact", from, to, sales: 0, services: 1, name: null, sources ?? []);

    private static object Link(long golden, string role, string from, string to, int sales, int services, string? name, string[] sources) => new
    {
        vin = Vin,
        goldenId = golden,
        name = name ?? "",
        role,
        from,
        to,
        sales,
        services,
        firstSale = sales > 0 ? from : "",
        lastSale = sales > 0 ? from : "",
        firstService = services > 0 ? from : "",
        lastService = services > 0 ? to : "",
        sources,
    };

    /// <summary>An in-memory registry with the module's entities, as a host DbContext maps them.</summary>
    private sealed class Registry : IDisposable
    {
        private readonly ServiceProvider services;
        private readonly IServiceScope scope;

        public Registry()
        {
            var collection = new ServiceCollection();
            var databaseName = Guid.NewGuid().ToString();
            collection.AddDbContext<ShiftDbContext, RegistryTestDbContext>(o => o.UseInMemoryDatabase(databaseName));
            collection.AddSingleton<IModelBuildingContributor>(new DarlasticModelBuildingContributor(schema: null));
            // Read access is refused when the module's authorization is switched on.
            collection.AddSingleton(DispatchProxy.Create<ITypeAuthService, DeniesEverything>());
            services = collection.BuildServiceProvider();
            scope = services.CreateScope();
        }

        private ShiftDbContext Db => scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        public GoldenCustomerController Controller(DarlasticApiOptions? options = null)
        {
            var protector = new PiiDtoProtector(new PiiFieldProtection(new DefaultPiiMasker(Options.Create(new PiiOptions()))));
            return new GoldenCustomerController(Db, Options.Create(options ?? new DarlasticApiOptions()), protector)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider },
                },
            };
        }

        public Task StageVinAsync(string vin, params object[] owners) =>
            StageAsync("vinowner", vin, JsonSerializer.Serialize(new { vin, owners }));

        public Task StageGoldenAsync(long golden, string fullName, string phone) =>
            StageAsync("golden", golden.ToString(), JsonSerializer.Serialize(new
            {
                goldenId = golden,
                attrs = new[] { new { t = "full_name", v = fullName }, new { t = "phone", v = phone } },
                members = new[] { "s|1" },
            }));

        public async Task TombstoneAsync(string artifactType, string key)
        {
            var row = await Db.Set<ProjectionState>().SingleAsync(x => x.ArtifactType == artifactType && x.ArtifactKey == key);
            row.ContentHash = "TOMBSTONE";
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        public async Task RedirectAsync(long from, long to)
        {
            Db.Add(new IdentityRedirect { OldIdentityID = from, NewIdentityID = to, RunID = 2 });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        private async Task StageAsync(string artifactType, string key, string payload)
        {
            Db.Add(new ProjectionState
            {
                ArtifactType = artifactType,
                ArtifactKey = key,
                ContentHash = "hash-" + key,
                UpdatedRunID = 1,
                Payload = payload,
            });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            scope.Dispose();
            services.Dispose();
        }
    }

    private sealed class RegistryTestDbContext(DbContextOptions<RegistryTestDbContext> options) : ShiftDbContext(options);

    /// <summary>A TypeAuth service that grants nothing: every yes/no question answers no.</summary>
    public class DeniesEverything : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType == typeof(bool)
                ? false
                : throw new NotSupportedException($"{targetMethod?.Name} is not used by these tests.");
    }
}

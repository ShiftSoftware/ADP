using System.Security.Claims;
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
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.API.Tests;

/// <summary>
/// The case browser's customer view names the golden as the resolve staged it — the value every other surface
/// serves — while it still recomputes the chain decision from the staged records. A resolve may show names as the
/// records wrote them (<see cref="GoldenNameOptions"/>), which the serving process cannot know; without a staged
/// golden the recomputed match form stands. Synthetic values only (ordinary Arabic words); the caller holds no
/// reveal action, so names come back as initials, which tell the two forms apart.
/// </summary>
public class CaseBrowserClusterNameTests
{
    private const string Written = "أرنب قرطاس";

    [Fact]
    public async Task TheGoldensNameIsTheStagedOne_NotTheRecomputedMatchForm()
    {
        using var registry = new Registry();
        await registry.SeedAsync(stagedName: Written);

        var (goldenName, attr) = await registry.ClusterNameAsync();

        Assert.Equal("أ ق", goldenName);   // the initials of the staged, record-written name
        Assert.Equal("أ ق", attr);
    }

    [Fact]
    public async Task WithoutAStagedGolden_TheRecomputedNameStands()
    {
        using var registry = new Registry();
        await registry.SeedAsync(stagedName: null);

        var (goldenName, attr) = await registry.ClusterNameAsync();

        // The engine's title-cased match form of the same records (a Latin transliteration).
        string matchForm = Merge.SurviveGolden([Rec("1"), Rec("2")]).Single(g => g.AttrType == "full_name").Value;
        string initials = string.Join(' ', matchForm.Split(' ').Select(w => w[..1]));
        Assert.Equal(initials, goldenName);
        Assert.Equal(initials, attr);
        Assert.DoesNotMatch("[؀-ۿ]", goldenName!);
    }

    [Fact]
    public async Task AStagedGoldenWithNoName_ShowsNoName_ThoughItsRecordsHaveOne()
    {
        // The resolve's name options refused every chain (a placeholder that names nobody): no surface shows one.
        using var registry = new Registry();
        await registry.SeedAsync(stagedName: null, stageNamelessGolden: true);

        var (goldenName, attr) = await registry.ClusterNameAsync();

        Assert.Null(goldenName);
        Assert.Null(attr);
    }

    [Fact]
    public async Task WhenStagedAndRecomputedAgree_TheViewIsUnchanged()
    {
        // A tenant on the defaults stages exactly what the view recomputes.
        string matchForm = Merge.SurviveGolden([Rec("1"), Rec("2")]).Single(g => g.AttrType == "full_name").Value;
        using var staged = new Registry();
        await staged.SeedAsync(stagedName: matchForm);
        using var unstaged = new Registry();
        await unstaged.SeedAsync(stagedName: null);

        Assert.Equal(await unstaged.ClusterNameAsync(), await staged.ClusterNameAsync());
    }

    private static RealRecord Rec(string id) => new(0, "dealer-a", id, Written, Norm.Name(Written), ["7000000101"], [], null, null);

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
            services = collection.BuildServiceProvider();
            scope = services.CreateScope();
        }

        private ShiftDbContext Db => scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        public async Task SeedAsync(string? stagedName, bool stageNamelessGolden = false)
        {
            Db.Add(new GoldenIdentity { IdentityID = 1, Status = IdentityStatus.Active, CreatedRunID = 1, LastChangedRunID = 1 });
            foreach (var id in new[] { "1", "2" })
            {
                Db.Add(new SourceProfile { SourceSystem = "dealer-a", SourceRecordId = id, IdentityID = 1, ContentHash = "h" + id, FirstRunID = 1, LastChangedRunID = 1 });
                Db.Add(new StewardRecord { SourceSystem = "dealer-a", SourceRecordId = id, RunID = 1, Payload = JsonSerializer.Serialize(Rec(id)) });
            }
            if (stagedName is not null)
                Db.Add(new ProjectionState
                {
                    ArtifactType = "golden",
                    ArtifactKey = "1",
                    ContentHash = "hash-1",
                    UpdatedRunID = 1,
                    Payload = JsonSerializer.Serialize(new
                    {
                        goldenId = 1,
                        attrs = new[] { new { t = "full_name", v = stagedName }, new { t = "phone", v = "7000000101" } },
                        members = new[] { "dealer-a|1", "dealer-a|2" },
                    }),
                });
            else if (stageNamelessGolden)
                Db.Add(new ProjectionState
                {
                    ArtifactType = "golden",
                    ArtifactKey = "1",
                    ContentHash = "hash-1",
                    UpdatedRunID = 1,
                    Payload = JsonSerializer.Serialize(new
                    {
                        goldenId = 1,
                        attrs = new[] { new { t = "phone", v = "7000000101" } },
                        members = new[] { "dealer-a|1", "dealer-a|2" },
                    }),
                });
            Db.Add(new ResolveRun { RunID = 1, StartedUtc = new DateTime(2026, 1, 1), FinishedUtc = new DateTime(2026, 1, 1) });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        /// <summary>The view's golden name and its full_name attribute, as a caller without the reveal action reads them.</summary>
        public async Task<(string? GoldenName, string? Attribute)> ClusterNameAsync()
        {
            var controller = new CaseBrowserCompatController(Db, Options.Create(new DarlasticApiOptions()),
                new DefaultPiiMasker(Options.Create(new PiiOptions())), Options.Create(new PiiOptions()), new RegistryRunCache())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        RequestServices = scope.ServiceProvider,
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "steward")], "test")),
                    },
                },
            };
            var result = Assert.IsType<JsonResult>(await controller.Cluster(1, CancellationToken.None));
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            var root = doc.RootElement;
            var names = root.GetProperty("golden").EnumerateArray()
                .Where(g => g.GetProperty("attrType").GetString() == "full_name").ToList();
            Assert.True(names.Count <= 1, "at most one full_name attribute");
            string? attr = names.Count == 1 ? names[0].GetProperty("value").GetString() : null;
            return (root.GetProperty("goldenName").GetString(), attr);
        }

        public void Dispose()
        {
            scope.Dispose();
            services.Dispose();
        }
    }

    private sealed class RegistryTestDbContext(DbContextOptions<RegistryTestDbContext> options) : ShiftDbContext(options);
}

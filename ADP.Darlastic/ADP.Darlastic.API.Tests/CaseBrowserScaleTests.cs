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
/// The case browser's reads that were rewritten for registries of millions of records answer as before: the pairs
/// list chooses its page before reading records, a page's goldens are read by key, and the summary's counts are
/// kept per resolve run only when the host asks. Values are synthetic.
/// </summary>
public class CaseBrowserScaleTests
{
    private const string Name = "Synth Person";

    private static RealRecord Rec(string sys, string id, int idx) =>
        new(idx, sys, id, Name, Norm.Name(Name), ["7000000101"], [], null, null);

    // ------------------------------------------------------------ pairs list

    [Fact]
    public async Task ThePairsList_ChoosesItsPageFirst_AndAnswersAsTheContentPathDoes()
    {
        using var registry = new Registry();
        await registry.SeedPairsAsync(25);

        foreach (var sort in new string?[] { null, "score-asc" })
            for (int page = 0; page < 4; page++)
            {
                // q names every record (they share one name), so the content path keeps every pair: the same list.
                string quick = await registry.CasesAsync(q: null, sort, page, size: 10);
                string content = await registry.CasesAsync(q: Name, sort, page, size: 10);
                Assert.Equal(content, quick);
            }

        using var first = JsonDocument.Parse(await registry.CasesAsync(q: null, sort: null, page: 0, size: 10));
        Assert.Equal(25, first.RootElement.GetProperty("total").GetInt32());
        var scores = first.RootElement.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("score").GetDouble()).ToList();
        Assert.Equal(10, scores.Count);
        Assert.Equal(scores.OrderByDescending(s => s), scores);
        Assert.All(first.RootElement.GetProperty("items").EnumerateArray(), i =>
        {
            Assert.Equal(JsonValueKind.Object, i.GetProperty("a").ValueKind);
            Assert.Equal(JsonValueKind.Object, i.GetProperty("b").ValueKind);
        });

        using var last = JsonDocument.Parse(await registry.CasesAsync(q: null, sort: null, page: 2, size: 10));
        Assert.Equal(5, last.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ThePairsList_FiltersByCategoryAndScore_BeforeItPages()
    {
        using var registry = new Registry();
        await registry.SeedPairsAsync(25);

        using var doc = JsonDocument.Parse(await registry.CasesAsync(q: null, sort: null, page: 0, size: 50, minScore: 0.5));
        int kept = doc.RootElement.GetProperty("total").GetInt32();
        Assert.InRange(kept, 1, 24);
        Assert.All(doc.RootElement.GetProperty("items").EnumerateArray(), i => Assert.True(i.GetProperty("score").GetDouble() >= 0.5));
    }

    // -------------------------------------------------------- goldens by key

    [Fact]
    public async Task APagesGoldens_AreReadByKey_LastValueWins_AndATombstoneIsNoGolden()
    {
        using var registry = new Registry();
        registry.Db.Add(Golden("7", "hash", new[] { ("full_name", "Aaa Synth"), ("full_name", "Zzz Synth"), ("phone", "7000000007"), ("city", "Synth City") }));
        registry.Db.Add(Golden("8", StagedGoldenPayload.TombstoneHash, new[] { ("full_name", "Gone Synth") }));
        registry.Db.Add(new ProjectionState { ArtifactType = "golden", ArtifactKey = "9", ContentHash = "hash", UpdatedRunID = 1, Payload = null });
        registry.Db.Add(new ProjectionState { ArtifactType = "events", ArtifactKey = "10", ContentHash = "hash", UpdatedRunID = 1, Payload = "{\"attrs\":[]}" });
        await registry.Db.SaveChangesAsync();

        var found = await StagedGoldenPayload.AttributesAsync(registry.Db, [7, 8, 9, 10, 11], CancellationToken.None);

        Assert.Equal([7L], found.Keys);
        Assert.Equal("Zzz Synth", found[7]["full_name"]);
        Assert.Equal("7000000007", found[7]["phone"]);
        Assert.Equal("Synth City", found[7]["city"]);
        Assert.False(found[7].ContainsKey("email"));
        Assert.Empty(await StagedGoldenPayload.AttributesAsync(registry.Db, [], CancellationToken.None));
    }

    private static ProjectionState Golden(string key, string hash, (string t, string v)[] attrs) => new()
    {
        ArtifactType = "golden",
        ArtifactKey = key,
        ContentHash = hash,
        UpdatedRunID = 1,
        Payload = JsonSerializer.Serialize(new { goldenId = long.Parse(key), attrs = attrs.Select(a => new { a.t, a.v }), members = new[] { "src-a|1" } }),
    };

    // ---------------------------------------------------------- summary counts

    [Fact]
    public async Task TheSummarysCounts_CountTheRegistry()
    {
        using var registry = new Registry();
        await registry.SeedPairsAsync(6);
        await registry.SeedSummariesAsync();

        var counts = await SummaryCounts.CountAsync(registry.Db, CancellationToken.None);

        Assert.Equal(12, counts.StagedRecords);
        Assert.Equal(new Dictionary<string, int> { ["src-a"] = 6, ["src-b"] = 6 }, counts.Sources);
        Assert.Equal(6, counts.CatalogCategories.Count);
        Assert.Equal(12, counts.Profiles);
        Assert.Equal(2, counts.MultiRecordIdentities);
        // Every cluster category is reported, counted over multi-record identities only.
        var firstCat = Enum.GetValues<ClusterCat>().First(c => c != ClusterCat.None);
        Assert.Equal(Enum.GetValues<ClusterCat>().Count(c => c != ClusterCat.None), counts.ClusterCategories.Count);
        Assert.Equal(2, counts.ClusterCategories[firstCat.ToString()]);
        Assert.False(counts.ReasonsStaged);
    }

    [Fact]
    public async Task KeptPerRun_TheCountsAreReadOncePerRun_AndAgainAfterTheNextRun()
    {
        using var registry = new Registry();
        await registry.SeedPairsAsync(3);
        var cache = new RegistryRunCache();

        var first = await cache.GetAsync(registry.Db, SummaryCounts.CacheName, SummaryCounts.CountAsync, CancellationToken.None);
        Assert.Equal(6, first.StagedRecords);

        // More staged records without a finished run: the same counts, the same object.
        registry.Db.Add(new StewardRecord { SourceSystem = "src-c", SourceRecordId = "1", RunID = 2, Payload = JsonSerializer.Serialize(Rec("src-c", "1", 900)) });
        await registry.Db.SaveChangesAsync();
        Assert.Same(first, await cache.GetAsync(registry.Db, SummaryCounts.CacheName, SummaryCounts.CountAsync, CancellationToken.None));

        // The run finishes: counted again.
        registry.Db.Add(new ResolveRun { RunID = 2, StartedUtc = new DateTime(2026, 1, 2), FinishedUtc = new DateTime(2026, 1, 2) });
        await registry.Db.SaveChangesAsync();
        var second = await cache.GetAsync(registry.Db, SummaryCounts.CacheName, SummaryCounts.CountAsync, CancellationToken.None);
        Assert.Equal(7, second.StagedRecords);
        Assert.Equal(1, second.Sources["src-c"]);
    }

    [Fact]
    public async Task TheAwaitingResolveCount_IsCountedAgainOnlyWhenTheNewestIdentityMoved()
    {
        var kept = new AwaitingResolveCount();
        int counted = 0;
        Task<int> Count(int answer) { counted++; return Task.FromResult(answer); }

        Assert.Equal(0, await kept.GetAsync(newest: 100, () => Count(0)));
        Assert.Equal(0, await kept.GetAsync(newest: 100, () => Count(99)));
        Assert.Equal(1, counted);

        // A host created a customer: the next identity number.
        Assert.Equal(1, await kept.GetAsync(newest: 101, () => Count(1)));
        Assert.Equal(1, await kept.GetAsync(newest: 101, () => Count(99)));
        Assert.Equal(2, counted);
    }

    // ------------------------------------------------------------------ warm-up

    [Fact]
    public async Task TheWarmUp_ReadsNothingWithEveryOptionOff_AndOnlyWhatAnOptionAsksFor()
    {
        using var registry = new Registry();
        await registry.SeedPairsAsync(3);

        var cache = new RegistryRunCache();
        Assert.Empty(await new RegistryRunWarmup(registry.Db, Options.Create(new DarlasticApiOptions()), cache).WarmAsync());

        var notes = await new RegistryRunWarmup(registry.Db, Options.Create(new DarlasticApiOptions { SummaryPerRun = true }), cache).WarmAsync();
        var note = Assert.Single(notes);
        Assert.StartsWith("summary counts: 6 staged records", note);

        // What the warm-up read is what a request then reads: no second count.
        bool counted = false;
        await cache.GetAsync<SummaryCounts>(registry.Db, SummaryCounts.CacheName, (_, _) => { counted = true; return Task.FromResult<SummaryCounts>(null!); }, CancellationToken.None);
        Assert.False(counted);
    }

    [Fact]
    public void TheSummarysPerRunCounts_AreOffByDefault() => Assert.False(new DarlasticApiOptions().SummaryPerRun);

    // ------------------------------------------------------------------ registry

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

        public ShiftDbContext Db => scope.ServiceProvider.GetRequiredService<ShiftDbContext>();

        /// <summary><paramref name="pairs"/> catalogued pairs (src-a:i ~ src-b:i) with their staged records and profiles,
        /// scores spread over 0..1 with ties, and one finished run.</summary>
        public async Task SeedPairsAsync(int pairs)
        {
            for (int i = 1; i <= pairs; i++)
            {
                string id = i.ToString();
                foreach (var (sys, idx) in new[] { ("src-a", i), ("src-b", 1000 + i) })
                {
                    Db.Add(new SourceProfile { SourceSystem = sys, SourceRecordId = id, IdentityID = i, ContentHash = "h", FirstRunID = 1, LastChangedRunID = 1 });
                    Db.Add(new StewardRecord { SourceSystem = sys, SourceRecordId = id, RunID = 1, Payload = JsonSerializer.Serialize(Rec(sys, id, idx)) });
                }
                Db.Add(new CaseCatalogEntry
                {
                    PairKey = $"src-a:{id}~src-b:{id}",
                    RunID = 1,
                    Score = (i * 7 % 10) / 10f,   // repeats, so the order of ties is exercised
                    Categories = 1,
                    Flags = 0,
                    SourceSystemA = "src-a",
                    SourceRecordIdA = id,
                    SourceSystemB = "src-b",
                    SourceRecordIdB = id,
                });
            }
            Db.Add(new ResolveRun { RunID = 1, StartedUtc = new DateTime(2026, 1, 1), FinishedUtc = new DateTime(2026, 1, 1) });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        /// <summary>Two multi-record identities carrying the first cluster category and one single-record identity
        /// carrying it too (which the cluster counts leave out).</summary>
        public async Task SeedSummariesAsync()
        {
            long cat = (long)Enum.GetValues<ClusterCat>().First(c => c != ClusterCat.None);
            Db.Add(new IdentitySummary { IdentityID = 1, RunID = 1, MemberCount = 2, SourceCount = 2, Categories = cat });
            Db.Add(new IdentitySummary { IdentityID = 2, RunID = 1, MemberCount = 3, SourceCount = 2, Categories = cat });
            Db.Add(new IdentitySummary { IdentityID = 3, RunID = 1, MemberCount = 1, SourceCount = 1, Categories = cat });
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
        }

        public async Task<string> CasesAsync(string? q, string? sort, int page, int size, double? minScore = null)
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
            var result = Assert.IsType<JsonResult>(await controller.Cases(cat: null, flag: null, dealer: null, dob: null,
                q: q, minScore: minScore, maxScore: null, sort: sort, page: page, size: size, ct: CancellationToken.None));
            return JsonSerializer.Serialize(result.Value);
        }

        public void Dispose()
        {
            scope.Dispose();
            services.Dispose();
        }
    }

    private sealed class RegistryTestDbContext(DbContextOptions<RegistryTestDbContext> options) : ShiftDbContext(options);
}

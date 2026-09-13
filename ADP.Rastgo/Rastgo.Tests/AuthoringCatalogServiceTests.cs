using ShiftSoftware.ADP.Rastgo;

namespace Rastgo.Tests;

public sealed class AuthoringCatalogServiceTests
{
    [Fact]
    public async Task Discovery_is_cached_and_one_source_failure_is_contained()
    {
        var good = new CatalogSource("warehouse");
        var bad = new CatalogSource("archive", fail: true);
        var options = new RastgoOptions
        {
            Authoring = new AuthoringCatalogOptions
            {
                CacheDuration = TimeSpan.FromHours(1),
                ErrorCacheDuration = TimeSpan.FromHours(1),
            },
        };
        var service = new AuthoringCatalogService(new SourceRegistry([good, bad]), options);
        const string yaml = """
        - name: volume.orders
          domain: operations
          category: volume
          measures: [{ key: total, source: warehouse }]
          assert: { type: threshold, of: total, min: 1 }
        """;

        var first = await service.BuildAsync(yaml, "checks.yaml", ct: TestContext.Current.CancellationToken);
        var second = await service.BuildAsync(yaml, "checks.yaml", ct: TestContext.Current.CancellationToken);

        Assert.Equal(1, good.DiscoveryCalls);
        Assert.Equal(1, bad.DiscoveryCalls);
        Assert.Single(first.Sources.Single(s => s.SourceName == "warehouse").Datasets);
        Assert.NotNull(first.Sources.Single(s => s.SourceName == "archive").Error);
        Assert.DoesNotContain("supersecret", first.Sources.Single(s => s.SourceName == "archive").Error!, StringComparison.OrdinalIgnoreCase);
        Assert.All(second.Sources, source => Assert.True(source.FromCache));

        var refreshed = await service.BuildAsync(yaml, "checks.yaml", forceRefresh: true,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, good.DiscoveryCalls);
        Assert.Equal(2, bad.DiscoveryCalls);
        Assert.All(refreshed.Sources, source => Assert.False(source.FromCache));
    }

    [Fact]
    public async Task Discovery_reports_transport_failures_without_exception_details()
    {
        var source = new CatalogSource("remote", error: new HttpRequestException(
            "Endpoint=https://secret.invalid",
            new System.Security.Authentication.AuthenticationException("certificate detail")));
        var service = new AuthoringCatalogService(new SourceRegistry([source]), new RastgoOptions());

        var catalog = await service.BuildAsync("[]", "checks.yaml", ct: TestContext.Current.CancellationToken);

        var error = Assert.Single(catalog.Sources).Error;
        Assert.Equal("Metadata discovery could not establish a secure connection to this source.", error);
        Assert.DoesNotContain("secret", error!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("certificate detail", error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Host_catalog_hints_enrich_discovery_without_record_access()
    {
        var options = new RastgoOptions();
        options.Authoring.CatalogHints.Add(new SourceCatalogHint(
            "warehouse",
            new CatalogDataset("orders", "table", "main", [new CatalogField("declared_total", "decimal")], "Host-declared schema")));
        var service = new AuthoringCatalogService(new SourceRegistry([new CatalogSource("warehouse")]), options);

        var catalog = await service.BuildAsync("[]", "checks.yaml", ct: TestContext.Current.CancellationToken);

        var dataset = Assert.Single(Assert.Single(catalog.Sources).Datasets);
        Assert.Contains(dataset.Fields, field => field.Name == "id");
        Assert.Contains(dataset.Fields, field => field.Name == "declared_total");
        Assert.Equal("Host-declared schema", dataset.Detail);
    }

    [Fact]
    public async Task Host_catalog_hints_remain_available_when_dynamic_discovery_fails()
    {
        var options = new RastgoOptions();
        options.Authoring.CatalogHints.Add(new SourceCatalogHint(
            "warehouse",
            new CatalogDataset("orders", "table", "main", [new CatalogField("declared_total", "decimal")])));
        var service = new AuthoringCatalogService(
            new SourceRegistry([new CatalogSource("warehouse", fail: true)]), options);

        var catalog = await service.BuildAsync("[]", "checks.yaml", ct: TestContext.Current.CancellationToken);

        var source = Assert.Single(catalog.Sources);
        Assert.NotNull(source.Error);
        Assert.Equal("orders", Assert.Single(source.Datasets).Name);
    }

    private sealed class CatalogSource(string name, bool fail = false, Exception? error = null) : ICheckSource, ICheckSourceCatalog
    {
        public string Name => name;
        public int DiscoveryCalls { get; private set; }

        public Task<MeasureOutcome> MeasureAsync(MeasureSpec spec, bool grouped, CancellationToken ct) =>
            Task.FromResult(new MeasureOutcome());

        public Task<SourceCatalogSnapshot> DiscoverAsync(SourceCatalogRequest request, CancellationToken ct)
        {
            DiscoveryCalls++;
            if (error is not null) throw error;
            if (fail) throw new InvalidOperationException("Password=supersecret");
            return Task.FromResult(new SourceCatalogSnapshot(
                Name,
                "Test database",
                [new CatalogDataset("orders", "table", "main", [new CatalogField("id", "integer", false)])],
                DateTimeOffset.UtcNow));
        }
    }
}

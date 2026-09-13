using DuckDB.NET.Data;
using ShiftSoftware.ADP.Rastgo;

namespace Rastgo.Tests;

public sealed class BuiltInCatalogTests
{
    [Fact]
    public async Task DuckDb_catalog_reads_schema_without_querying_rows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rastgo-catalog-{Guid.NewGuid():N}.duckdb");
        try
        {
            using (var connection = new DuckDBConnection($"DataSource={path}"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE orders (id INTEGER NOT NULL, created_at TIMESTAMP, private_value VARCHAR)";
                command.ExecuteNonQuery();
            }

            var source = new DuckDbCheckSource($"DataSource={path};access_mode=read_only");
            var snapshot = await source.DiscoverAsync(new SourceCatalogRequest(), TestContext.Current.CancellationToken);

            var table = Assert.Single(snapshot.Datasets, d => d.Name == "orders");
            Assert.Equal("main", table.Namespace);
            Assert.Contains(table.Fields, f => f.Name == "created_at" && f.Type.Contains("TIMESTAMP"));
            Assert.Contains(table.Fields, f => f.Name == "private_value");
            Assert.Contains("not queried", snapshot.Note, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task File_share_catalog_is_bounded_and_never_reads_file_contents()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rastgo-files-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "first.csv"), "Password=must-not-appear",
                TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, "second.csv"), "another private record",
                TestContext.Current.CancellationToken);

            var source = new FileShareCheckSource(root);
            var snapshot = await source.DiscoverAsync(new SourceCatalogRequest
            {
                MaxFiles = 1,
                MaxFileDepth = 0,
            }, TestContext.Current.CancellationToken);

            Assert.Single(snapshot.Datasets);
            Assert.True(snapshot.Truncated);
            Assert.NotNull(snapshot.Datasets[0].SizeBytes);
            Assert.NotNull(snapshot.Datasets[0].ModifiedAtUtc);
            Assert.DoesNotContain("must-not-appear", snapshot.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain("private record", snapshot.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task File_share_catalog_skips_declared_top_level_folders_but_reports_them()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rastgo-files-{Guid.NewGuid():N}");
        var excluded = Path.Combine(root, ".internal-sync");
        var included = Path.Combine(root, "BusinessData");
        Directory.CreateDirectory(excluded);
        Directory.CreateDirectory(included);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(excluded, "private.bin"), "not read",
                TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(included, "orders.csv"), "not read",
                TestContext.Current.CancellationToken);

            var snapshot = await new FileShareCheckSource(root).DiscoverAsync(new SourceCatalogRequest
            {
                ExcludedTopLevelFolders = [".internal-sync"],
            }, TestContext.Current.CancellationToken);

            Assert.Equal("orders.csv", Assert.Single(snapshot.Datasets).Name);
            Assert.Equal(".internal-sync", Assert.Single(snapshot.ExcludedTopLevelFolders));

            var withExplicitReference = await new FileShareCheckSource(root).DiscoverAsync(new SourceCatalogRequest
            {
                ExcludedTopLevelFolders = [".internal-sync"],
                ReferencedMeasures =
                [
                    new MeasureSpec { Source = "fileshare", Path = ".internal-sync/private.bin" },
                ],
            }, TestContext.Current.CancellationToken);

            Assert.Contains(withExplicitReference.Datasets,
                dataset => dataset.Name == "private.bin" && dataset.Referenced);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task File_share_catalog_prioritizes_pack_references_before_the_general_cap()
    {
        var root = Path.Combine(Path.GetTempPath(), $"rastgo-files-{Guid.NewGuid():N}");
        var stock = Path.Combine(root, "Stock");
        Directory.CreateDirectory(stock);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "unrelated.csv"), "not catalogued",
                TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(stock, "KRG.csv"), "not read",
                TestContext.Current.CancellationToken);

            var source = new FileShareCheckSource(root);
            var snapshot = await source.DiscoverAsync(new SourceCatalogRequest
            {
                MaxFiles = 1,
                MaxFileDepth = 0,
                ReferencedMeasures = [new MeasureSpec { Source = "fileshare", Path = "Stock/*.csv" }],
            }, TestContext.Current.CancellationToken);

            var dataset = Assert.Single(snapshot.Datasets);
            Assert.Equal("KRG.csv", dataset.Name);
            Assert.Equal("Stock", dataset.Namespace);
            Assert.True(dataset.Referenced);
            Assert.True(snapshot.Truncated);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

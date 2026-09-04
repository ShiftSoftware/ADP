using System.Text.Json;
using System.Text.Json.Serialization;
using DuckDB.NET.Data;
using ShiftSoftware.ADP.Hawta;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// The source binding's contract: over a Hawta store or a Hawta published set, the stream carries
/// the LIVE rows of the serving tables — tombstones out, the family's own filter still applied —
/// in the same VIN order and with the same reference data as over a read snapshot, and a serving
/// table the source does not carry is a loud failure naming it.
/// </summary>
public sealed class BulkLookupSourceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "lookup-source-tests", Guid.NewGuid().ToString("N"));
    private readonly string storePath;

    private static readonly IReadOnlyList<AggregateFamily> Families =
        [AggregateFamilies.VehicleEntry, AggregateFamilies.OrderLaborLine, AggregateFamilies.WarrantyClaim];

    // The serving tables a Hawta host types from the ADP models, with Hawta's bookkeeping: the
    // columns the engine reads, plus _Deleted (a tombstone stays a row) and _ChangeSequence.
    private const string Bookkeeping = "\"_Deleted\" BOOLEAN, \"_ChangeSequence\" BIGINT";

    public BulkLookupSourceTests()
    {
        Directory.CreateDirectory(root);
        storePath = Path.Combine(root, "serving-store.duckdb");
        using var connection = new DuckDBConnection($"Data Source={storePath}");
        connection.Open();
        Execute(connection, $$"""
            CREATE SCHEMA data;
            CREATE TABLE data.ServingVehicleEntry (id VARCHAR, VIN VARCHAR, CompanyID BIGINT, BrandID BIGINT, VariantCode VARCHAR, InvoiceDate TIMESTAMP, {{Bookkeeping}});
            CREATE TABLE data.ServingOrderLaborLine (id VARCHAR, VIN VARCHAR, LaborCode VARCHAR, ExtendedPrice DECIMAL(18,6), {{Bookkeeping}});
            CREATE TABLE data.ServingWarrantyClaim (id VARCHAR, VIN VARCHAR, IsDeleted BOOLEAN, ClaimStatus INTEGER, LaborLines VARCHAR, {{Bookkeeping}});
            CREATE TABLE data.ServingServiceItem (id VARCHAR, IsDeleted BOOLEAN, PackageCode VARCHAR, {{Bookkeeping}});
            CREATE TABLE data.ServingVehicleModel (id VARCHAR, VariantCode VARCHAR, BrandID BIGINT, ModelCode VARCHAR, {{Bookkeeping}});
            CREATE TABLE data.ServingExteriorColor (id VARCHAR, Code VARCHAR, BrandID BIGINT, Description VARCHAR, {{Bookkeeping}});
            CREATE TABLE data.ServingInteriorColor (id VARCHAR, Code VARCHAR, BrandID BIGINT, Description VARCHAR, {{Bookkeeping}});
            INSERT INTO data.ServingVehicleEntry VALUES
                ('e2', 'VIN00000000000002', 3, 1, 'V2', TIMESTAMP '2025-01-02 00:00:00', false, 10),
                ('e1', 'VIN00000000000001', 3, 1, 'V1', TIMESTAMP '2025-01-01 00:00:00', false, 11),
                ('e3', 'VIN00000000000003', 3, 2, 'V3', NULL, true, 12);
            INSERT INTO data.ServingOrderLaborLine VALUES
                ('l1a', 'VIN00000000000001', 'OP1', 1.25, false, 20),
                ('l1x', 'VIN00000000000001', 'GONE', 9.99, true, 21),
                ('l1b', 'VIN00000000000001', 'OP1B', 1.75, false, 22),
                ('l1n', ' vin00000000000001 ', 'NOTMINE', 0.1, false, 24),
                ('l3', 'VIN00000000000003', 'OP3', 3.5, false, 23);
            INSERT INTO data.ServingWarrantyClaim VALUES
                ('w1', 'VIN00000000000001', false, 2, '[{"ID": 1, "LaborCode": "ZGG40D", "Hour": 1.5}]', false, 30),
                ('w1s', 'VIN00000000000001', true, 2, '[]', false, 31),
                ('w2', 'VIN00000000000002', false, 1, NULL, true, 32);
            INSERT INTO data.ServingServiceItem VALUES ('s1', false, 'PKG', false, 40), ('s2', false, 'OLD', true, 41);
            INSERT INTO data.ServingVehicleModel VALUES ('m1', 'V1', 1, 'MODEL1', false, 50);
            INSERT INTO data.ServingExteriorColor VALUES ('c1', '1F7', 1, 'Silver', false, 60), ('c2', '202', 1, 'Black', true, 61);
            INSERT INTO data.ServingInteriorColor VALUES ('i1', 'LA', 1, 'Beige', false, 70);
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
    }

    private static void Execute(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void AssertLiveRowsOnly(BulkLookupSource source)
    {
        using var stream = source.OpenStream();
        var aggregates = stream.ToList();

        // The tombstoned entry is gone with its vehicle; the rest are in VIN order.
        Assert.Equal(["VIN00000000000001", "VIN00000000000002"], aggregates.Select(a => a.VIN));
        Assert.Equal(1, stream.Statistics.SkippedWithoutEntry);      // VIN3's labor line has no live entry
        Assert.Equal(1, stream.Statistics.NonCanonicalVinRows);      // the line stored as ' vin00000000000001 ': no path serves it

        var first = aggregates[0];
        Assert.Equal("e1", Assert.Single(first.VehicleEntries).id);
        Assert.Equal(3, first.VehicleEntries[0].CompanyID);          // the id the projection stamped, no hash decoding
        Assert.Equal(["OP1", "OP1B"], first.LaborLines.Select(l => l.LaborCode));   // tombstone out, non-canonical out, source order kept
        // The family's own filter (IsDeleted) still applies on top of the live-row predicate.
        var claim = Assert.Single(first.WarrantyClaims);
        Assert.Equal("w1", claim.id);
        Assert.Equal("ZGG40D", Assert.Single(claim.LaborLines).LaborCode);
        Assert.Empty(aggregates[1].WarrantyClaims);                  // w2 is a tombstone

        var reference = source.LoadReference();
        Assert.Equal(1, reference.Report.ServiceItems);              // s2 is a tombstone
        Assert.Equal(1, reference.Report.VehicleModels);
        Assert.Equal(1, reference.Report.ExteriorColors);            // c2 is a tombstone
        Assert.Equal(1, reference.Report.InteriorColors);
        Assert.Equal(0, reference.Report.BrokerStockRows);           // no brokers at a Hawta host by default
        Assert.Equal(0, reference.Report.Customers);
        Assert.Equal("MODEL1", reference.GetVehicleModelsAsync("V1", 1).Result!.ModelCode);
        Assert.Equal("Silver", reference.GetExteriorColorsAsync("1F7", 1).Result!.Description);
        Assert.Null(reference.GetExteriorColorsAsync("202", 1).Result);
    }

    [Fact]
    public void HawtaStore_StreamsLiveRowsOnly_AndReadsReferenceFromTheServingTables()
    {
        var source = BulkLookupSource.HawtaStore(storePath, Families);
        Assert.Null(source.HashIds);
        Assert.All(source.Families, family => Assert.Contains("\"_Deleted\" = false", family.Where));
        AssertLiveRowsOnly(source);
    }

    [Fact]
    public void HawtaStore_AServingTableTheStoreLacks_FailsAtOpen_NamingIt()
    {
        var source = BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry, AggregateFamilies.ItemClaim]);
        var exception = Assert.ThrowsAny<Exception>(() => source.OpenStream().ToList());
        Assert.Contains("ServingItemClaim", exception.Message);
    }

    [Fact]
    public void HawtaPublish_ReadsTheSameAggregatesFromTheManifestParquet()
    {
        var manifestPath = Publish("company-data-read");
        var source = BulkLookupSource.HawtaPublish(manifestPath, Families);

        Assert.Null(source.HashIds);
        Assert.All(source.Families, family =>
        {
            Assert.StartsWith("read_parquet([", family.From);
            Assert.Equal("filename, file_row_number", family.RowOrder);
        });
        Assert.Contains("watermark 70", source.Description);
        AssertLiveRowsOnly(source);
    }

    [Fact]
    public void HawtaPublish_AServingTableTheSetLacks_FailsWhenBound_NamingIt()
    {
        var manifestPath = Publish("company-data-read");
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BulkLookupSource.HawtaPublish(manifestPath, [AggregateFamilies.VehicleEntry, AggregateFamilies.ItemClaim]));
        Assert.Contains("ServingItemClaim", exception.Message);
        Assert.Contains("ServingVehicleEntry", exception.Message);   // the manifest's own list, so the reader sees what IS there
    }

    /// <summary>
    /// A published set the way the publisher lays one out: one folder per table, one parquet per
    /// export (bookkeeping columns included), and a manifest at the root naming them.
    /// </summary>
    private string Publish(string snapshotName)
    {
        var directory = Path.Combine(root, "publish");
        Directory.CreateDirectory(directory);
        const string stamp = "20260904120000000";
        var tables = new List<PublishedTableManifest>();
        using (var connection = new DuckDBConnection($"Data Source={storePath};ACCESS_MODE=READ_ONLY"))
        {
            connection.Open();
            foreach (var table in new[] { "ServingVehicleEntry", "ServingOrderLaborLine", "ServingWarrantyClaim", "ServingServiceItem", "ServingVehicleModel", "ServingExteriorColor", "ServingInteriorColor" })
            {
                Directory.CreateDirectory(Path.Combine(directory, table));
                var file = Path.Combine(directory, table, stamp + ".parquet").Replace('\\', '/');
                Execute(connection, $"COPY (SELECT * FROM data.\"{table}\" ORDER BY rowid) TO '{file}' (FORMAT parquet)");
                tables.Add(new PublishedTableManifest(table, PublishedTableLocation.Parquet($"{table}/{stamp}.parquet"), stamp, 0, "0", null, DateTime.UtcNow)
                {
                    SourceCatalog = [new PublishedSourceCatalogEntry("serving-" + table.ToLowerInvariant(), null, SourceRecordIdentityDescriptor.DatabaseKey("id"))],
                });
            }
        }

        var manifest = new PublishedSnapshot(
            PublishedSnapshot.CurrentManifestVersion, snapshotName, stamp, DateTime.UtcNow, 5, "test", ".", "latest-per-table", tables)
        {
            ChangeSequenceHighWatermark = 70,
        };
        var manifestPath = Path.Combine(directory, $"{snapshotName}-{stamp}.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }));
        return manifestPath;
    }
}

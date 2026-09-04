using DuckDB.NET.Data;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Models.Service;
using ShiftSoftware.ADP.Models.Vehicle;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// The bulk data plane's contract: one aggregate per VIN, in VIN order, carrying exactly the rows
/// the per-VIN storage would have loaded (same tables, same filters), and loud about what it will
/// not serve — a VIN with no entry, a family that is not there.
/// </summary>
public sealed class VinOrderedAggregateStreamTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), "lookup-stream-tests", $"{Guid.NewGuid():N}.duckdb");
    private readonly string readOnlyConnectionString;

    public VinOrderedAggregateStreamTests()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        using var connection = new DuckDBConnection($"Data Source={databasePath}");
        connection.Open();
        Execute(connection, """
            CREATE TABLE VehicleEntry (id VARCHAR, VIN VARCHAR, CompanyID BIGINT, BrandID BIGINT, VariantCode VARCHAR, InvoiceDate TIMESTAMP);
            CREATE TABLE OrderLaborLine (id VARCHAR, VIN VARCHAR, LaborCode VARCHAR, ExtendedPrice DECIMAL(38,10));
            CREATE TABLE WarrantyClaim (id VARCHAR, VIN VARCHAR, IsDeleted BOOLEAN, ClaimStatus INTEGER, LaborLines JSON);
            INSERT INTO VehicleEntry VALUES
                ('e2', 'VIN00000000000002', 5, 1, 'V2', TIMESTAMP '2025-01-02 00:00:00'),
                ('e1', 'VIN00000000000001', 5, 1, 'V1', TIMESTAMP '2025-01-01 00:00:00'),
                ('e3', 'VIN00000000000003', 5, 2, 'V3', NULL),
                ('e0', NULL, 5, 1, 'V0', NULL);
            INSERT INTO OrderLaborLine VALUES
                ('l3', 'VIN00000000000003', 'OP3', 3.5),
                ('l1a', 'VIN00000000000001', 'OP1', 1.25),
                ('l1b', 'VIN00000000000001', 'OP1B', 1.75),
                ('l1n', ' vin00000000000001 ', 'OPN', 0.5),
                ('l9', 'VIN00000000000009', 'OP9', 9.0);
            INSERT INTO WarrantyClaim VALUES
                ('w1', 'VIN00000000000001', false, 2, '[{"ID": 1, "LaborCode": "ZGG40D", "Hour": 1.5}]'),
                ('w1d', 'VIN00000000000001', true, 2, '[]'),
                ('w2', 'VIN00000000000002', false, 1, NULL);
            """);
        readOnlyConnectionString = $"Data Source={databasePath};ACCESS_MODE=READ_ONLY";
    }

    public void Dispose()
    {
        try { File.Delete(databasePath); } catch { /* best effort */ }
    }

    private static void Execute(DuckDBConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static readonly IReadOnlyList<AggregateFamily> Families =
        [AggregateFamilies.VehicleEntry, AggregateFamilies.OrderLaborLine, AggregateFamilies.WarrantyClaim];

    [Fact]
    public void OneAggregatePerVin_InVinOrder_WithEveryFamilyAttached()
    {
        using var stream = new VinOrderedAggregateStream(readOnlyConnectionString, Families);
        var aggregates = stream.ToList();

        Assert.Equal(["VIN00000000000001", "VIN00000000000002", "VIN00000000000003"], aggregates.Select(a => a.VIN));

        var first = aggregates[0];
        Assert.Equal("e1", Assert.Single(first.VehicleEntries).id);
        // The line stored as ' vin00000000000001 ' is not the vehicle's on the per-VIN path (exact match
        // on the stored VIN), so it is not the vehicle's here either — counted, never attached.
        Assert.Equal(["OP1", "OP1B"], first.LaborLines.Select(l => l.LaborCode).OrderBy(c => c));
        Assert.Equal(1.25m, first.LaborLines.Single(l => l.LaborCode == "OP1").ExtendedPrice);
        // The soft-deleted claim is filtered by the family's own WHERE, exactly as the per-VIN storage filters it.
        var claim = Assert.Single(first.WarrantyClaims);
        Assert.Equal("w1", claim.id);
        Assert.Equal("ZGG40D", Assert.Single(claim.LaborLines).LaborCode);

        Assert.Empty(aggregates[1].LaborLines);
        Assert.Single(aggregates[1].WarrantyClaims);
        Assert.Equal("OP3", Assert.Single(aggregates[2].LaborLines).LaborCode);
        Assert.Empty(aggregates[2].WarrantyClaims);
    }

    [Fact]
    public void RowsWithoutAnEntry_AreSkippedAndCounted_AndBlankVinsAreCounted()
    {
        using var stream = new VinOrderedAggregateStream(readOnlyConnectionString, Families);
        var count = stream.Count();

        Assert.Equal(3, count);
        Assert.Equal(3, stream.Statistics.Aggregates);
        Assert.Equal(1, stream.Statistics.SkippedWithoutEntry);        // VIN...009 has a labor line and no entry
        Assert.Equal(1, stream.Statistics.BlankVinRows);               // the entry with a NULL VIN
        Assert.Equal(1, stream.Statistics.NonCanonicalVinRows);        // the labor line stored as ' vin00000000000001 '
        Assert.Equal(4, stream.Statistics.RowsRead[AggregateFamilies.VehicleEntry]);
        Assert.Equal(5, stream.Statistics.RowsRead[AggregateFamilies.OrderLaborLine]);
        Assert.Equal(2, stream.Statistics.RowsRead[AggregateFamilies.WarrantyClaim]);   // the deleted one never leaves DuckDB
        Assert.Equal(3, stream.Statistics.RowsAttached[AggregateFamilies.OrderLaborLine]);
    }

    [Fact]
    public void WithoutTheEntryRequirement_EveryVinWithRowsIsServed()
    {
        using var stream = new VinOrderedAggregateStream(readOnlyConnectionString, Families, requireVehicleEntry: false);
        var vins = stream.Select(a => a.VIN).ToList();

        Assert.Equal(["VIN00000000000001", "VIN00000000000002", "VIN00000000000003", "VIN00000000000009"], vins);
    }

    [Fact]
    public void ADeclaredFamilyThatIsNotThere_FailsAtOpen()
    {
        var families = new[] { AggregateFamilies.VehicleEntry, AggregateFamilies.ItemClaim };
        using var stream = new VinOrderedAggregateStream(readOnlyConnectionString, families);

        Assert.ThrowsAny<Exception>(() => stream.ToList());
    }

    [Fact]
    public void TheMapper_ReadsEveryShapeTheStorageReads()
    {
        using var connection = new DuckDBConnection(readOnlyConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM WarrantyClaim WHERE id = 'w1'";
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        var claim = ReadOne<WarrantyClaimModel>(reader);

        Assert.Equal("VIN00000000000001", claim.VIN);
        Assert.False(claim.IsDeleted);
        Assert.Equal(2, (int)claim.ClaimStatus);
        Assert.Equal(1.5m, Assert.Single(claim.LaborLines).Hour);
    }

    private static T ReadOne<T>(System.Data.Common.DbDataReader reader) where T : new()
    {
        var mapperType = typeof(AggregateFamily).Assembly.GetType("ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming.DuckDBModelMapper`1")!
            .MakeGenericType(typeof(T));
        var mapper = mapperType.GetMethod("For")!.Invoke(null, [reader])!;
        return (T)mapperType.GetMethod("Read")!.Invoke(mapper, [reader])!;
    }
}

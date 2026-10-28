using DuckDB.NET.Data;
using Newtonsoft.Json.Linq;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// A pending inspection (<c>IsPending</c>) never reaches the aggregate, on any of the three paths that load
/// inspections: Cosmos, DuckDB per VIN, and the DuckDB bulk stream. A snapshot written before the column
/// existed — no column at all, or NULL in it — still loads every inspection.
/// </summary>
public sealed class PendingVehicleInspectionTests : IDisposable
{
    private const string Vin = "VIN00000000000001";

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), "lookup-pending-inspection-tests", $"{Guid.NewGuid():N}.duckdb");

    public PendingVehicleInspectionTests() => Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

    public void Dispose()
    {
        try { File.Delete(databasePath); } catch { /* best effort */ }
    }

    [Fact]
    public void Cosmos_DropsPendingInspections_AndKeepsDocumentsWithoutTheProperty()
    {
        var items = new List<dynamic>
        {
            Inspection("ready", isPending: false),
            Inspection("pending", isPending: true),
            Inspection("written-before-the-flag", isPending: null),
        };

        var aggregate = CosmosVehicleLookupStorageService.ConvertDynamicListItemsToCompanyData(items);

        Assert.Equal(["ready", "written-before-the-flag"], aggregate.VehicleInspections.Select(x => x.id).OrderBy(x => x));
    }

    [Fact]
    public async Task DuckDBPerVin_DropsPendingInspections_AndReadsNullAsNotPending()
    {
        CreateDatabase(withPendingColumn: true);

        var aggregate = await LoadPerVinAsync();

        Assert.Equal(["null", "ready"], aggregate.VehicleInspections.Select(x => x.id).OrderBy(x => x));
    }

    [Fact]
    public async Task DuckDBPerVin_SnapshotWithoutTheColumn_LoadsEveryInspection()
    {
        CreateDatabase(withPendingColumn: false);

        var aggregate = await LoadPerVinAsync();

        Assert.Equal(["null", "pending", "ready"], aggregate.VehicleInspections.Select(x => x.id).OrderBy(x => x));
    }

    [Fact]
    public void DuckDBStream_DropsPendingInspections_AndReadsNullAsNotPending()
    {
        CreateDatabase(withPendingColumn: true);

        var aggregate = Assert.Single(Stream());

        Assert.Equal(["null", "ready"], aggregate.VehicleInspections.Select(x => x.id).OrderBy(x => x));
    }

    [Fact]
    public void DuckDBStream_SnapshotWithoutTheColumn_LoadsEveryInspection()
    {
        CreateDatabase(withPendingColumn: false);

        var aggregate = Assert.Single(Stream());

        Assert.Equal(["null", "pending", "ready"], aggregate.VehicleInspections.Select(x => x.id).OrderBy(x => x));
    }

    private static JObject Inspection(string id, bool? isPending)
    {
        var document = new JObject
        {
            ["id"] = id,
            ["VIN"] = Vin,
            ["ItemType"] = ModelTypes.VehicleInspection.ToString(),
            ["IsDeleted"] = false,
        };

        if (isPending is not null)
            document["IsPending"] = isPending.Value;

        return document;
    }

    // Without the column, the "pending" and "null" rows are ordinary inspections: nothing marks them.
    private void CreateDatabase(bool withPendingColumn)
    {
        using var connection = new DuckDBConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = withPendingColumn
            ? $"""
                CREATE TABLE VehicleInspection (id VARCHAR, VIN VARCHAR, VehicleInspectionTypeID BIGINT, IsDeleted BOOLEAN, IsPending BOOLEAN);
                INSERT INTO VehicleInspection VALUES
                    ('ready', '{Vin}', 1, false, false),
                    ('pending', '{Vin}', 1, false, true),
                    ('null', '{Vin}', 1, false, NULL),
                    ('deleted', '{Vin}', 1, true, false);
                """
            : $"""
                CREATE TABLE VehicleInspection (id VARCHAR, VIN VARCHAR, VehicleInspectionTypeID BIGINT, IsDeleted BOOLEAN);
                INSERT INTO VehicleInspection VALUES
                    ('ready', '{Vin}', 1, false),
                    ('pending', '{Vin}', 1, false),
                    ('null', '{Vin}', 1, false),
                    ('deleted', '{Vin}', 1, true);
                """;
        command.ExecuteNonQuery();
    }

    private async Task<Aggregate.CompanyDataAggregateModel> LoadPerVinAsync()
    {
        using var connection = new DuckDBConnection($"Data Source={databasePath};ACCESS_MODE=READ_ONLY");
        connection.Open();

        var storage = new DuckDBVehicleLookupStorageService(connection, null!);
        var aggregates = await storage.GetAggregatedCompanyData([Vin], [ModelTypes.VehicleInspection]);

        return Assert.Single(aggregates);
    }

    private List<Aggregate.CompanyDataAggregateModel> Stream()
    {
        using var stream = new VinOrderedAggregateStream($"Data Source={databasePath};ACCESS_MODE=READ_ONLY",
            [AggregateFamilies.VehicleInspection], requireVehicleEntry: false);

        return stream.ToList();
    }
}

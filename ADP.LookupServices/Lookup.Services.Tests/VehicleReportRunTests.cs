using DuckDB.NET.Data;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Reports;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Lookup.Services.Services;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// The host stage's contract: the requested files land in the report layout, complete and
/// together; a run that fails leaves the previous files untouched and no partial behind.
/// </summary>
public sealed class VehicleReportRunTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "lookup-report-run-tests", Guid.NewGuid().ToString("N"));
    private readonly string storePath;
    private readonly string outputDirectory;

    public VehicleReportRunTests()
    {
        Directory.CreateDirectory(root);
        storePath = Path.Combine(root, "serving-store.duckdb");
        outputDirectory = Path.Combine(root, "reports");
        using var connection = new DuckDBConnection($"Data Source={storePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE SCHEMA data;
            CREATE TABLE data.ServingVehicleEntry (id VARCHAR, VIN VARCHAR, CompanyID BIGINT, BrandID BIGINT, VariantCode VARCHAR, InvoiceDate TIMESTAMP, "_Deleted" BOOLEAN);
            CREATE TABLE data.ServingOrderLaborLine (id VARCHAR, VIN VARCHAR, LaborCode VARCHAR, ExtendedPrice DECIMAL(18,6), "_Deleted" BOOLEAN);
            CREATE TABLE data.ServingServiceItem (id VARCHAR, IsDeleted BOOLEAN, PackageCode VARCHAR, "_Deleted" BOOLEAN);
            CREATE TABLE data.ServingVehicleModel (id VARCHAR, VariantCode VARCHAR, BrandID BIGINT, ModelCode VARCHAR, "_Deleted" BOOLEAN);
            CREATE TABLE data.ServingExteriorColor (id VARCHAR, Code VARCHAR, BrandID BIGINT, Description VARCHAR, "_Deleted" BOOLEAN);
            CREATE TABLE data.ServingInteriorColor (id VARCHAR, Code VARCHAR, BrandID BIGINT, Description VARCHAR, "_Deleted" BOOLEAN);
            INSERT INTO data.ServingVehicleEntry VALUES
                ('e2', 'VIN00000000000002', 3, 1, 'V2', TIMESTAMP '2025-01-02 00:00:00', false),
                ('e1', 'VIN00000000000001', 3, 1, 'V1', TIMESTAMP '2025-01-01 00:00:00', false),
                ('e3', 'VIN00000000000003', 3, 1, 'V3', NULL, true);
            INSERT INTO data.ServingOrderLaborLine VALUES ('l1', 'VIN00000000000001', 'OP1', 1.25, false);
            INSERT INTO data.ServingVehicleModel VALUES ('m1', 'V1', 1, 'MODEL1', false);
            """;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
    }

    private BulkLookupSource Source() =>
        BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry, AggregateFamilies.OrderLaborLine]);

    private static VehicleLookupService Lookup(IVehicleLookupStorageService storage) =>
        new(storage, null, null, new LookupOptions { VehicleLookupStorageSource = Enums.StorageSources.DuckDB }, null);

    private static long RowsIn(string parquetPath)
    {
        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM read_parquet('{parquetPath.Replace('\\', '/')}')";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    [Fact]
    public async Task ProducesTheRequestedFiles_InTheReportLayout_WithNoPartialLeftBehind()
    {
        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = Source(),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = VehicleReports.WithoutBrokerStock,
            Degree = 2,
            FlushEvery = 1,
        });

        Assert.Equal(2, result.Vehicles);
        Assert.Equal(2, result.Evaluations);                        // one request shape, evaluated once per vehicle
        Assert.Equal(0, result.SkippedWithoutEntry);                // the tombstoned entry's vehicle has no other rows, so it is never seen
        var topLevel = Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet");
        var serviceItems = Path.Combine(outputDirectory, "ServiceItem", "vehicle-service-items-report.parquet");
        Assert.Equal([serviceItems, topLevel], result.Files.Select(f => f.Path));
        Assert.True(File.Exists(topLevel));
        Assert.True(File.Exists(serviceItems));
        Assert.Equal(2, RowsIn(topLevel));
        Assert.Equal(2, result.Files.Single(f => f.Report == VehicleReports.TopLevel).Rows);
        Assert.Equal(0, RowsIn(serviceItems));                      // no service items in this universe: an empty file with the schema
        Assert.Empty(Directory.GetFiles(outputDirectory, "*.partial", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ReportsSharingARequest_EvaluateOnce_AndDistinctRequestsEvaluateSeparately()
    {
        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = Source(),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = VehicleReports.All,                           // default, ignore-broker-stock, default
            Degree = 1,
        });

        Assert.Equal(2, result.Vehicles);
        Assert.Equal(4, result.Evaluations);                        // two distinct requests per vehicle
        Assert.Equal(3, result.Files.Count);
    }

    /// <summary>
    /// A case production data has: an activation with no country, by a company with no
    /// entry of its own for the vehicle. The ownership evaluator refuses it rather than guess.
    /// </summary>
    private BulkLookupSource SourceWithARefusedVehicle()
    {
        using var connection = new DuckDBConnection($"Data Source={storePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE data.ServingVehicleServiceActivation (id VARCHAR, VIN VARCHAR, CompanyID BIGINT, CountryID BIGINT, IsDeleted BOOLEAN, "_Deleted" BOOLEAN);
            INSERT INTO data.ServingVehicleServiceActivation VALUES ('a2', 'VIN00000000000002', 9, NULL, false, false);
            """;
        command.ExecuteNonQuery();
        return BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry, AggregateFamilies.OrderLaborLine, AggregateFamilies.VehicleServiceActivation]);
    }

    [Fact]
    public async Task ARefusedVehicle_WithinTheBound_IsLeftOutOfEveryFile_AndListed()
    {
        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = SourceWithARefusedVehicle(),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = VehicleReports.All,
            MaxFailedVehicles = 1,
        });

        var failure = Assert.Single(result.Failures);
        Assert.Equal("VIN00000000000002", failure.Vin);
        Assert.IsType<IncompleteVehicleServiceActivationException>(failure.Exception);
        Assert.Equal(2, result.Vehicles);                           // streamed and evaluated
        Assert.Equal(1, RowsIn(Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet")));   // written: the other one only
    }

    [Fact]
    public async Task ARefusedVehicle_OverTheBound_FailsTheRun()
    {
        var exception = await Assert.ThrowsAsync<IncompleteVehicleServiceActivationException>(() => VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = SourceWithARefusedVehicle(),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = [VehicleReports.TopLevel],
            Degree = 1,                                             // the default bound: none tolerated
        }));

        Assert.Contains("VIN00000000000002", exception.Message);
        Assert.Empty(Directory.GetFiles(outputDirectory, "*.parquet", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(outputDirectory, "*.partial", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AFailedRun_LeavesThePreviousFilesUntouched_AndNoPartial()
    {
        var topLevel = Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet");
        Directory.CreateDirectory(Path.GetDirectoryName(topLevel)!);
        File.WriteAllText(topLevel, "the previous run's file");

        await Assert.ThrowsAnyAsync<Exception>(() => VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = Source(),
            Lookup = _ => throw new InvalidOperationException("the host's lookup cannot be built"),
            OutputDirectory = outputDirectory,
            Reports = [VehicleReports.TopLevel],
        }));

        Assert.Equal("the previous run's file", File.ReadAllText(topLevel));
        Assert.Empty(Directory.GetFiles(outputDirectory, "*.partial", SearchOption.AllDirectories));
    }
}

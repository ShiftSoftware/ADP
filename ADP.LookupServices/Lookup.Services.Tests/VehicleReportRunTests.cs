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

    [Theory]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"]},{"field":"campaignVinEntries.campaignID","values":["500"]}]}]""", 2)]
    [InlineData("""[{"AnyOf":[{"Field":"vehicleClassifications.code","Values":["member"]},{"Field":"campaignVinEntries.campaignID","Values":["500"]}]}]""", 2)]
    [InlineData("""[{"field":"vehicleClassifications.code","values":["member"]},{"field":"campaignVinEntries.campaignID","values":["500"]}]""", 1)]
    [InlineData("{", 0)]
    [InlineData("""[{"anyOf":[]}]""", 0)]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"],"operator":"Unsupported"}]}]""", 0)]
    [InlineData("""[{"anyOf":[{"field":"vehicleClassifications.code","values":["member"]}],"unknown":true}]""", 0)]
    [InlineData("""[{"field":"vehicleClassifications.code","values":["member"],"values":["member"]}]""", 0)]
    public async Task Or_conditions_survive_reference_preloading_and_emit_one_row_per_qualifying_vehicle(string conditionsJson, int expectedRows)
    {
        using (var connection = new DuckDBConnection($"Data Source={storePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $$"""
                CREATE TABLE data.ServingVehicleInspection (id VARCHAR, VIN VARCHAR, VehicleInspectionTypeID BIGINT, InspectionDate TIMESTAMP, IsDeleted BOOLEAN, "_Deleted" BOOLEAN);
                CREATE TABLE data.ServingCampaignVinEntry (id VARCHAR, VIN VARCHAR, CampaignID BIGINT, IsDeleted BOOLEAN, "_Deleted" BOOLEAN);
                INSERT INTO data.ServingVehicleInspection VALUES
                    ('i1', 'VIN00000000000001', 10, TIMESTAMP '2026-01-05', false, false),
                    ('i2', 'VIN00000000000002', 10, TIMESTAMP '2026-01-05', false, false);
                INSERT INTO data.ServingCampaignVinEntry VALUES
                    ('t1', 'VIN00000000000001', 500, false, false),
                    ('t2', 'VIN00000000000002', 500, false, false);
                ALTER TABLE data.ServingServiceItem ADD COLUMN IntegrationID VARCHAR;
                ALTER TABLE data.ServingServiceItem ADD COLUMN CampaignID BIGINT;
                ALTER TABLE data.ServingServiceItem ADD COLUMN CampaignStartDate TIMESTAMP;
                ALTER TABLE data.ServingServiceItem ADD COLUMN CampaignEndDate TIMESTAMP;
                ALTER TABLE data.ServingServiceItem ADD COLUMN VehicleInspectionTypeID BIGINT;
                ALTER TABLE data.ServingServiceItem ADD COLUMN CampaignActivationTrigger INTEGER;
                ALTER TABLE data.ServingServiceItem ADD COLUMN CampaignActivationType INTEGER;
                ALTER TABLE data.ServingServiceItem ADD COLUMN ValidityMode INTEGER;
                ALTER TABLE data.ServingServiceItem ADD COLUMN ActiveFor INTEGER;
                ALTER TABLE data.ServingServiceItem ADD COLUMN ActiveForDurationType INTEGER;
                ALTER TABLE data.ServingServiceItem ADD COLUMN EligibilityConditions VARCHAR;
                INSERT INTO data.ServingServiceItem (id, IntegrationID, IsDeleted, "_Deleted", CampaignID, CampaignStartDate,
                    CampaignEndDate, VehicleInspectionTypeID, CampaignActivationTrigger, CampaignActivationType,
                    ValidityMode, ActiveFor, ActiveForDurationType, EligibilityConditions) VALUES
                    ('voucher', 'voucher', false, false, 500, TIMESTAMP '2026-01-01', TIMESTAMP '2027-01-01', 10,
                    {{(int)Models.Enums.ClaimableItemCampaignActivationTrigger.VehicleInspection}},
                    {{(int)Models.Enums.ClaimableItemCampaignActivationTypes.FirstTriggerOnly}},
                    {{(int)Models.Enums.ClaimableItemValidityMode.RelativeToActivation}}, 1, {{(int)Models.Enums.DurationType.Months}},
                    '{{conditionsJson.Replace("'", "''")}}');
                """;
            command.ExecuteNonQuery();
        }
        var calls = 0;
        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry,
                AggregateFamilies.VehicleInspection, AggregateFamilies.CampaignVinEntry]),
            Lookup = storage => new VehicleLookupService(storage, options: new LookupOptions
            {
                TimeProvider = new OrClock(),
                VehicleClassificationResolver = context =>
                {
                    Interlocked.Increment(ref calls);
                    Assert.Equal(Enums.ConsistencyLevels.Strong, context.ServiceHistoryConsistencyLevel);
                    return context.Aggregate.VIN.EndsWith("1") ? [new() { Code = "member" }] : [];
                },
            }),
            OutputDirectory = outputDirectory,
            Reports = VehicleReports.WithoutBrokerStock,
            Degree = 1,
        });
        Assert.Equal(2, calls);
        Assert.Equal(2, result.Evaluations);
        Assert.Equal(expectedRows, RowsIn(Path.Combine(outputDirectory, "ServiceItem", "vehicle-service-items-report.parquet")));
    }

    private sealed class OrClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public async Task ReportsSharingARequest_EvaluateOnce_AndDistinctRequestsEvaluateSeparately()
    {
        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = Source(),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = VehicleReports.All,                           // default, provisioning, default
            Degree = 1,
        });

        Assert.Equal(2, result.Vehicles);
        Assert.Equal(4, result.Evaluations);                        // two distinct requests per vehicle
        Assert.Equal(3, result.Files.Count);
    }

    [Fact]
    public async Task TheTopLevelReport_TakesTheDistributorFromTheOptionsTheVehiclesWereEvaluatedWith()
    {
        using (var connection = new DuckDBConnection($"Data Source={storePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE data.ServingExtendedWarranty (id VARCHAR, VIN VARCHAR, CompanyID BIGINT, StartDate TIMESTAMP, EndDate TIMESTAMP, IsActive BOOLEAN, IsDeleted BOOLEAN, "_Deleted" BOOLEAN);
                INSERT INTO data.ServingExtendedWarranty VALUES
                    ('w1', 'VIN00000000000001', 5, TIMESTAMP '2028-01-01 00:00:00', TIMESTAMP '2029-01-01 00:00:00', true, false, false),
                    ('w2', 'VIN00000000000001', 3, TIMESTAMP '2029-01-01 00:00:00', TIMESTAMP '2030-01-01 00:00:00', true, false, false);
                """;
            command.ExecuteNonQuery();
        }

        // The distributor is named nowhere but the host's options, which only its lookup factory holds.
        await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry, AggregateFamilies.ExtendedWarranty]),
            Lookup = storage => new VehicleLookupService(storage, null, null,
                new LookupOptions { VehicleLookupStorageSource = Enums.StorageSources.DuckDB, DistributorCompanyID = 5 }, null),
            OutputDirectory = outputDirectory,
            Reports = [VehicleReports.TopLevel],
            Degree = 2,
        });

        using var reader = new DuckDBConnection("Data Source=:memory:");
        reader.Open();
        using var query = reader.CreateCommand();
        var topLevel = Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet").Replace('\\', '/');
        query.CommandText = $"""
            SELECT VIN,
                   strftime(WarrantyDistributorExtendedStartDate, '%Y-%m-%d'),
                   strftime(WarrantyDistributorExtendedEndDate, '%Y-%m-%d'),
                   strftime(WarrantyExtendedEndDate, '%Y-%m-%d')
            FROM read_parquet('{topLevel}') ORDER BY VIN
            """;
        using var rows = query.ExecuteReader();
        var read = new List<(string Vin, string? Start, string? End, string? LegacyEnd)>();
        while (rows.Read())
            read.Add((rows.GetString(0), Text(rows, 1), Text(rows, 2), Text(rows, 3)));

        Assert.Equal(
        [
            ("VIN00000000000001", "2028-01-01", "2029-01-01", "2030-01-01"),    // the dealer's later entry stays out; the legacy end is it
            ("VIN00000000000002", null, null, null),
        ], read);

        static string? Text(System.Data.IDataRecord record, int ordinal) => record.IsDBNull(ordinal) ? null : record.GetString(ordinal);
    }

    [Fact]
    public async Task TheTopLevelReport_CarriesTheClassificationsEachVehicleResolved()
    {
        await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = Source(),
            Lookup = storage => new VehicleLookupService(storage, null, null, new LookupOptions
            {
                VehicleLookupStorageSource = Enums.StorageSources.DuckDB,
                VehicleClassificationResolver = context => context.Aggregate.VIN.EndsWith("1")
                    ? [new() { Code = "segment:7", Label = "Segment 7" }, new() { Code = "member" }]
                    : [],
            }, null),
            OutputDirectory = outputDirectory,
            Reports = [VehicleReports.TopLevel],
            Degree = 2,
        });

        using var reader = new DuckDBConnection("Data Source=:memory:");
        reader.Open();
        using var query = reader.CreateCommand();
        var topLevel = Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet").Replace('\\', '/');
        query.CommandText = $"SELECT VIN, ClassificationCodes, ClassificationLabels FROM read_parquet('{topLevel}') ORDER BY VIN";
        using var rows = query.ExecuteReader();
        var read = new List<(string Vin, string Codes, string Labels)>();
        while (rows.Read())
            read.Add((rows.GetString(0), rows.GetString(1), rows.GetString(2)));

        Assert.Equal(
        [
            ("VIN00000000000001", "segment:7; member", "Segment 7; member"),
            ("VIN00000000000002", "", ""),     // nothing resolved: empty text, like any other missing value
        ], read);
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
    public async Task TheSscReport_ServesAVehicleWithACampaignAndNoEntry_InItsOwnFileOnly()
    {
        using (var connection = new DuckDBConnection($"Data Source={storePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE data.ServingSscAffectedVin (id VARCHAR, VIN VARCHAR, CampaignCode VARCHAR, Description VARCHAR, LaborCode1 VARCHAR, RepairDate TIMESTAMP, "_Deleted" BOOLEAN);
                INSERT INTO data.ServingSscAffectedVin VALUES
                    ('s1', 'VIN00000000000001', 'SSC-1', 'Airbag', 'OP1', NULL, false),
                    ('s9', 'VIN00000000000009', 'SSC-1', 'Airbag', 'OP1', TIMESTAMP '2024-03-15 00:00:00', false);
                INSERT INTO data.ServingOrderLaborLine VALUES ('l8', 'VIN00000000000008', 'OP8', 1, false);
                """;
            command.ExecuteNonQuery();
        }

        var result = await VehicleReportRun.RunAsync(new VehicleReportRun.Options
        {
            Source = BulkLookupSource.HawtaStore(storePath, [AggregateFamilies.VehicleEntry, AggregateFamilies.OrderLaborLine, AggregateFamilies.SSCAffectedVIN]),
            Lookup = Lookup,
            OutputDirectory = outputDirectory,
            Reports = [VehicleReports.TopLevel, VehicleReports.Ssc],
            Degree = 2,
            FlushEvery = 1,
        });

        Assert.Equal(3, result.Vehicles);                           // the two entries, and the campaign vehicle without one
        Assert.Equal(1, result.ServedWithoutEntry);
        Assert.Equal(1, result.SkippedWithoutEntry);                // a labor line alone is not a vehicle the SSC report serves
        Assert.Equal(2, RowsIn(Path.Combine(outputDirectory, "Vehicle", "vehicle-top-level-report.parquet")));   // the entries only

        using var reader = new DuckDBConnection("Data Source=:memory:");
        reader.Open();
        using var query = reader.CreateCommand();
        var ssc = Path.Combine(outputDirectory, "Ssc", "vehicle-ssc-report.parquet").Replace('\\', '/');
        query.CommandText = $"SELECT VIN, SSCCode, Repaired, RepairSource, SaleCompanyId FROM read_parquet('{ssc}') ORDER BY VIN";
        using var rows = query.ExecuteReader();
        var read = new List<(string Vin, string Code, bool Repaired, string Source, string Owner)>();
        while (rows.Read())
            read.Add((rows.GetString(0), rows.GetString(1), rows.GetBoolean(2), rows.GetString(3), rows.GetString(4)));

        Assert.Equal(
        [
            ("VIN00000000000001", "SSC-1", false, "None", "3"),
            ("VIN00000000000009", "SSC-1", true, "SSCRecord", ""),     // no entry: no sale information, so no owner
        ], read);
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

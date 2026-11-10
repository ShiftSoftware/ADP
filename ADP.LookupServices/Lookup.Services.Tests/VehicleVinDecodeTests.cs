using DuckDB.NET.Data;
using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Lookup.Services.Services;
using ShiftSoftware.ADP.Models.Vehicle;

namespace Lookup.Services.Tests;

public class VehicleVinDecodeTests
{
    private const string Vin = "SYNTHETICVEHICLE1";

    private static Mock<IVehicleLookupStorageService> Storage(CompanyDataAggregateModel aggregate)
    {
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetAggregatedCompanyData(Vin)).ReturnsAsync(aggregate);
        storage.Setup(s => s.GetServiceItemsAsync(It.IsAny<bool>())).ReturnsAsync(new List<ServiceItemModel>());
        storage.Setup(s => s.GetVinModelMappingAsync("SYN", "THETI"))
            .ReturnsAsync(new VinModelMappingModel { WMI = "SYN", VDS = "THETI", Katashiki = "ABC100", ModelDescription = "Sample Model" });
        return storage;
    }

    private static CompanyDataAggregateModel Unauthorized() => new() { VIN = Vin };

    [Fact]
    public async Task Unauthorized_vin_with_a_mapping_gets_the_values_apart_from_its_record()
    {
        var storage = Storage(Unauthorized());

        var result = await new VehicleLookupService(storage.Object, options: new LookupOptions())
            .LookupAsync(Vin, new VehicleLookupRequestOptions { DecodeUnauthorizedVin = true });

        Assert.False(result.IsAuthorized);
        Assert.Equal("ABC100", result.VinDecode.Katashiki);
        Assert.Equal("Sample Model", result.VinDecode.ModelDescription);
        Assert.NotNull(result.VinDecode.ModelYear);
        Assert.Null(result.Identifiers?.Katashiki);
        Assert.Null(result.VehicleSpecification?.ModelDescription);
        Assert.Null(result.VehicleSpecification?.ModelYear);
        Assert.Null(result.VehicleVariantInfo?.ModelYear);
        Assert.Null(result.BasicModelCode);
    }

    [Theory]
    // The first cycle, read in 2026: the window is 1998 to 2027.
    [InlineData("SYNTHETICW0000001", 2026, 1998)]
    [InlineData("SYNTHETICY0000001", 2026, 2000)]
    [InlineData("SYNTHETIC10000001", 2026, 2001)]
    [InlineData("SYNTHETIC90000001", 2026, 2009)]
    // The second cycle.
    [InlineData("SYNTHETICA0000001", 2026, 2010)]
    [InlineData("SYNTHETICR0000001", 2026, 2024)]
    [InlineData("SYNTHETICT0000001", 2026, 2026)]
    [InlineData("SYNTHETICV0000001", 2026, 2027)]
    // The window moves with the reference year: next year W is the 2028 model year, not 1998.
    [InlineData("SYNTHETICW0000001", 2027, 2028)]
    // Trimmed and upper-cased like the rest of the decode.
    [InlineData("  syntheticr0000001 ", 2026, 2024)]
    public void Year_code_reads_as_the_latest_year_up_to_next_year(string vin, int referenceYear, int expected)
    {
        Assert.Equal(expected, VinModelYear.Decode(vin, referenceYear));
    }

    [Theory]
    [InlineData("SYNTHETIC00000001")] // 0: not encoded
    [InlineData("SYNTHETICU0000001")] // letters that are never year codes
    [InlineData("SYNTHETICZ0000001")]
    [InlineData("SYNTHETICI0000001")]
    [InlineData("SYNTHETICO0000001")]
    [InlineData("SYNTHETICQ0000001")]
    [InlineData("SYNTHETICR000001")] // 16 characters
    [InlineData("")]
    [InlineData(null)]
    public void No_year_without_a_year_code(string vin)
    {
        Assert.Null(VinModelYear.Decode(vin, 2026));
    }

    [Fact]
    public async Task The_year_window_follows_the_lookup_clock()
    {
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetVinModelMappingAsync("SYN", "THETI"))
            .ReturnsAsync(new VinModelMappingModel { WMI = "SYN", VDS = "THETI", ModelDescription = "Sample Model" });

        LookupOptions At(int year) => new() { TimeProvider = new FixedClock(new DateTimeOffset(year, 6, 1, 0, 0, 0, TimeSpan.Zero)) };

        Assert.Equal(1998, (await new VehicleVinDecodeEvaluator(storage.Object, At(2026)).Evaluate("SYNTHETICW0000001")).ModelYear);
        Assert.Equal(2028, (await new VehicleVinDecodeEvaluator(storage.Object, At(2027)).Evaluate("SYNTHETICW0000001")).ModelYear);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task A_vin_with_a_mapping_but_no_year_code_keeps_its_model_and_has_no_year()
    {
        var storage = new Mock<IVehicleLookupStorageService>();
        storage.Setup(s => s.GetVinModelMappingAsync("SYN", "THETI"))
            .ReturnsAsync(new VinModelMappingModel { WMI = "SYN", VDS = "THETI", Katashiki = "ABC100", ModelDescription = "Sample Model" });

        var result = await new VehicleVinDecodeEvaluator(storage.Object).Evaluate("SYNTHETIC00000001");

        Assert.Equal("Sample Model", result.ModelDescription);
        Assert.Null(result.ModelYear);
    }

    [Fact]
    public async Task A_vin_with_no_mapping_gets_no_year_either()
    {
        var storage = new Mock<IVehicleLookupStorageService>();

        Assert.Null(await new VehicleVinDecodeEvaluator(storage.Object).Evaluate("SYNTHETICR0000001"));
    }

    [Fact]
    public async Task Authorized_vin_never_reads_a_mapping()
    {
        var aggregate = Unauthorized();
        aggregate.InitialOfficialVINs = [new() { VIN = Vin }];
        var storage = Storage(aggregate);

        var result = await new VehicleLookupService(storage.Object, options: new LookupOptions())
            .LookupAsync(Vin, new VehicleLookupRequestOptions { DecodeUnauthorizedVin = true });

        Assert.True(result.IsAuthorized);
        Assert.Null(result.VinDecode);
        storage.Verify(s => s.GetVinModelMappingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Without_the_option_no_mapping_is_read()
    {
        var storage = Storage(Unauthorized());

        var result = await new VehicleLookupService(storage.Object, options: new LookupOptions())
            .LookupAsync(Vin, new VehicleLookupRequestOptions());

        Assert.Null(result.VinDecode);
        storage.Verify(s => s.GetVinModelMappingAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Both_duckdb_storages_tell_a_vds_apart_by_its_wmi()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.duckdb");
        try
        {
            using (var writer = new DuckDBConnection($"Data Source={path}"))
            {
                writer.Open();
                using var command = writer.CreateCommand();
                command.CommandText = """
                    CREATE TABLE ServiceItem (id VARCHAR);
                    CREATE TABLE VehicleModel (id VARCHAR);
                    CREATE TABLE ExteriorColor (id VARCHAR);
                    CREATE TABLE InteriorColor (id VARCHAR);
                    CREATE TABLE VinModelMapping (id VARCHAR, WMI VARCHAR, VDS VARCHAR, Katashiki VARCHAR, ModelDescription VARCHAR, Source VARCHAR);
                    INSERT INTO VinModelMapping VALUES
                        ('SYNTHETI', 'SYN', 'THETI', 'ABC100', 'Sample Model', 'test'),
                        ('OTHTHETI', 'OTH', 'THETI', 'XYZ200', 'Other Model', 'test');
                    """;
                command.ExecuteNonQuery();
            }

            var connectionString = $"Data Source={path};ACCESS_MODE=READ_ONLY";
            using var reader = new DuckDBConnection(connectionString);
            reader.Open();

            IVehicleLookupStorageService[] storages =
            [
                new DuckDBVehicleLookupStorageService(reader, null!),
                PreloadedReferenceStorage.Load(connectionString, new() { LoadBrokerStock = false, LoadCustomers = false, LoadVinModelMappings = true }).ForWorker(),
            ];

            foreach (var storage in storages)
            {
                Assert.Equal("ABC100", (await storage.GetVinModelMappingAsync("SYN", "THETI")).Katashiki);
                Assert.Equal("XYZ200", (await storage.GetVinModelMappingAsync("OTH", "THETI")).Katashiki);
                Assert.Null(await storage.GetVinModelMappingAsync("SYN", "NONE0"));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Top_level_report_keeps_decoded_values_in_their_own_columns()
    {
        var row = VehicleReportRows.TopLevel(Vin, new VehicleLookupDTO { VinDecode = new() { Katashiki = "ABC100", ModelDescription = "Sample Model", ModelYear = 2024 } }, null);

        Assert.Equal("ABC100", row.VinDecodeKatashiki);
        Assert.Equal("Sample Model", row.VinDecodeModelDescription);
        Assert.Equal(2024, row.VinDecodeModelYear);
        Assert.Null(row.VehicleSpecModelYear);
        Assert.Equal(string.Empty, row.IdentifiersKatashiki);
        Assert.Equal(string.Empty, row.VehicleSpecModelDescription);
    }
}

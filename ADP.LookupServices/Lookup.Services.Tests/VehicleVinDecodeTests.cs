using DuckDB.NET.Data;
using Moq;
using ShiftSoftware.ADP.Lookup.Services;
using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.DuckDB.Streaming;
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
        Assert.Null(result.Identifiers?.Katashiki);
        Assert.Null(result.VehicleSpecification?.ModelDescription);
        Assert.Null(result.BasicModelCode);
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
        var row = VehicleReportRows.TopLevel(Vin, new VehicleLookupDTO { VinDecode = new() { Katashiki = "ABC100", ModelDescription = "Sample Model" } }, null);

        Assert.Equal("ABC100", row.VinDecodeKatashiki);
        Assert.Equal("Sample Model", row.VinDecodeModelDescription);
        Assert.Equal(string.Empty, row.IdentifiersKatashiki);
        Assert.Equal(string.Empty, row.VehicleSpecModelDescription);
    }
}

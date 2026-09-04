using DuckDB.NET.Data;
using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Services;
using Xunit;

namespace ShiftSoftware.ADP.Lookup.Services.Tests;

/// <summary>
/// The report rows the per-VIN report service and the bulk engine share: one row per service item
/// id (the best of the duplicates), in the report's order; and the parquet file those rows land
/// in, readable back with the same columns, in the order they were appended.
/// </summary>
public sealed class VehicleReportRowsTests
{
    [Fact]
    public void ServiceItems_OneRowPerItemId_TheLatestClaimWins_OrderedNumerically()
    {
        var lookup = new VehicleLookupDTO
        {
            VIN = "JTDBR32E0X0000001",
            Warranty = new VehicleWarrantyDTO { FreeServiceStartDate = new DateTime(2024, 3, 1) },
            ServiceItems =
            [
                new VehicleServiceItemDTO { ServiceItemID = "10", Name = "ten", Status = "pending", ActivatedAt = new DateTime(2024, 3, 1) },
                new VehicleServiceItemDTO { ServiceItemID = "9", Name = "nine, older claim", ClaimDate = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero), InvoiceNumber = "OLD" },
                new VehicleServiceItemDTO { ServiceItemID = "9 ", Name = "nine, latest claim", ClaimDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero), InvoiceNumber = "NEW" },
                new VehicleServiceItemDTO { ServiceItemID = "", Name = "no id, dropped" },
            ],
        };

        var rows = VehicleReportRows.ServiceItems("JTDBR32E0X0000001", lookup);

        Assert.Equal(["9", "10"], rows.Select(r => r.ServiceItemId));
        Assert.Equal("NEW", rows[0].InvoiceNumber);
        Assert.Equal(new DateTime(2024, 6, 1), rows[0].ClaimDate!.Value.UtcDateTime);
        Assert.All(rows, r => Assert.Equal(new DateTime(2024, 3, 1), r.FreeServiceItemStartDate));
        // The row builder reads `item?.ActivatedAt == default` — a lifted comparison against null, so an
        // unactivated item's ActivatedAt reaches the report as 0001-01-01, never as null. Production's
        // files carry exactly that (no null ActivatedAt in 1.2 M rows); pinned here so the bulk engine
        // keeps writing what the per-VIN report writes until the report's owner changes both.
        Assert.Equal(DateTime.MinValue, rows[0].ActivatedAt);
        Assert.Equal(new DateTime(2024, 3, 1), rows[1].ActivatedAt);
        Assert.Empty(VehicleReportRows.ServiceItems("JTDBR32E0X0000001", null));
    }

    [Fact]
    public void TopLevel_IsOneRowWithTheLookupsHeadlineFields_AndEmptyStringsForWhatIsMissing()
    {
        var row = VehicleReportRows.TopLevel("JTDBR32E0X0000001", new VehicleLookupDTO
        {
            VIN = "JTDBR32E0X0000001",
            IsAuthorized = true,
            Warranty = new VehicleWarrantyDTO { HasActiveWarranty = true, WarrantyEndDate = new DateTime(2027, 1, 1) },
        });

        Assert.Equal("JTDBR32E0X0000001", row.VIN);
        Assert.True(row.IsAuthorized);
        Assert.True(row.WarrantyHasActiveWarranty);
        Assert.Equal(new DateTime(2027, 1, 1), row.WarrantyEndDate);
        Assert.Equal(string.Empty, row.SaleCompanyName);
        Assert.Null(row.SaleBrokerId);
    }

    [Fact]
    public async Task ParquetReportFile_AppendsRowGroupsInOrder_AndReadsBackWithTheReportsColumns()
    {
        var path = Path.Combine(Path.GetTempPath(), "lookup-report-tests", $"{Guid.NewGuid():N}.parquet");
        var file = new ParquetReportFile<VehicleServiceItemReportModel>(path);
        await file.AppendAsync(
        [
            new VehicleServiceItemReportModel { VIN = "A", ServiceItemId = "1", StatusEnum = VehcileServiceItemStatuses.Pending, ClaimDate = new DateTimeOffset(2024, 6, 1, 3, 0, 0, TimeSpan.FromHours(3)), Price = 12.5m },
            new VehicleServiceItemReportModel { VIN = "A", ServiceItemId = "2" },
        ]);
        await file.AppendAsync([]);                                         // nothing to add, nothing changes
        await file.AppendAsync([new VehicleServiceItemReportModel { VIN = "B", ServiceItemId = "1" }]);
        await file.CompleteAsync();

        Assert.Equal(3, file.RowCount);
        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT VIN, ServiceItemId, StatusEnum, ClaimDate, Price FROM read_parquet('{path.Replace('\\', '/')}')";
        using var reader = command.ExecuteReader();
        var rows = new List<(string Vin, string Item, object Status, object Claim, object Price)>();
        while (reader.Read())
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetValue(2), reader.GetValue(3), reader.GetValue(4)));

        Assert.Equal([("A", "1"), ("A", "2"), ("B", "1")], rows.Select(r => (r.Vin, r.Item)));
        Assert.Equal((int)VehcileServiceItemStatuses.Pending, Convert.ToInt32(rows[0].Status));
        Assert.Equal(new DateTime(2024, 6, 1, 0, 0, 0), Assert.IsType<DateTime>(rows[0].Claim)); // the instant, in UTC
        Assert.Equal(12.5m, Convert.ToDecimal(rows[0].Price));
        Assert.IsType<DBNull>(rows[1].Status);

        File.Delete(path);
    }

    [Fact]
    public async Task ParquetReportFile_WithNothingAppended_StillLeavesAReadableEmptyFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "lookup-report-tests", $"{Guid.NewGuid():N}.parquet");
        var file = new ParquetReportFile<VehicleLookupTopLevelReportModel>(path);
        await file.CompleteAsync();

        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM read_parquet('{path.Replace('\\', '/')}')";
        Assert.Equal(0L, Convert.ToInt64(command.ExecuteScalar()));

        File.Delete(path);
    }
}

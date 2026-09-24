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

    [Theory]
    [InlineData(VehcileServiceItemStatuses.Pending, VehicleServiceItemLockState.Locked, "locked", 5)]
    [InlineData(VehcileServiceItemStatuses.Expired, VehicleServiceItemLockState.Locked, "locked", 5)]
    [InlineData(VehcileServiceItemStatuses.Cancelled, VehicleServiceItemLockState.Locked, "locked", 5)]
    [InlineData(VehcileServiceItemStatuses.Pending, VehicleServiceItemLockState.Missed, "missed", 6)]
    [InlineData(VehcileServiceItemStatuses.Expired, VehicleServiceItemLockState.Missed, "missed", 6)]
    [InlineData(VehcileServiceItemStatuses.Cancelled, VehicleServiceItemLockState.Missed, "missed", 6)]
    public void ServiceItems_DisplayLockStateOverLifecycleStatus_WithoutMutatingLookup(
        VehcileServiceItemStatuses lifecycle, VehicleServiceItemLockState lockState, string expectedStatus, int expectedCode)
    {
        var item = new VehicleServiceItemDTO
        {
            ServiceItemID = "1",
            Status = lifecycle.ToString().ToLowerInvariant(),
            StatusEnum = lifecycle,
            Lock = new VehicleServiceItemLockDTO { State = lockState },
            Claimable = false,
        };
        var lookup = new VehicleLookupDTO { ServiceItems = [item] };

        var row = Assert.Single(VehicleReportRows.ServiceItems("VIN", lookup));

        Assert.Equal(expectedStatus, row.Status);
        Assert.Equal(expectedCode, (int)row.StatusEnum!.Value);
        Assert.False(row.Claimable);
        Assert.Null(row.ExpiresAt);
        Assert.Equal(lifecycle, item.StatusEnum);
        Assert.Equal(lifecycle.ToString().ToLowerInvariant(), item.Status);
        Assert.Equal(lockState, item.Lock.State);
    }

    [Theory]
    [InlineData(VehcileServiceItemStatuses.Processed, "processed", 0)]
    [InlineData(VehcileServiceItemStatuses.Expired, "expired", 1)]
    [InlineData(VehcileServiceItemStatuses.Pending, "pending", 2)]
    [InlineData(VehcileServiceItemStatuses.Cancelled, "cancelled", 3)]
    [InlineData(VehcileServiceItemStatuses.ActivationRequired, "activationRequired", 4)]
    public void ServiceItems_WithoutLock_PreserveExistingStatusAndNumericCodes(
        VehcileServiceItemStatuses lifecycle, string status, int expectedCode)
    {
        var row = VehicleReportRows.ServiceItem("VIN", new VehicleServiceItemDTO { Status = status, StatusEnum = lifecycle }, null);

        Assert.Equal(status, row.Status);
        Assert.Equal(expectedCode, (int)row.StatusEnum!.Value);
    }

    [Fact]
    public void ReportStatuses_MirrorEveryLifecycleStatus_ByNameAndCode()
    {
        // ServiceItem carries the lifecycle status into the report enum by its number, so a lifecycle
        // status added without its report twin would be reported under whichever state holds that code.
        foreach (var lifecycle in Enum.GetValues<VehcileServiceItemStatuses>())
            Assert.Equal(lifecycle.ToString(), ((VehicleServiceItemReportStatuses)(int)lifecycle).ToString());
    }

    [Fact]
    public void TopLevel_IsOneRowWithTheLookupsHeadlineFields_AndEmptyStringsForWhatIsMissing()
    {
        var row = VehicleReportRows.TopLevel("JTDBR32E0X0000001", new VehicleLookupDTO
        {
            VIN = "JTDBR32E0X0000001",
            IsAuthorized = true,
            Warranty = new VehicleWarrantyDTO { HasActiveWarranty = true, WarrantyEndDate = new DateTime(2027, 1, 1) },
        }, distributorCompanyID: null);

        Assert.Equal("JTDBR32E0X0000001", row.VIN);
        Assert.True(row.IsAuthorized);
        Assert.True(row.WarrantyHasActiveWarranty);
        Assert.Equal(new DateTime(2027, 1, 1), row.WarrantyEndDate);
        Assert.Equal(string.Empty, row.SaleCompanyName);
        Assert.Null(row.SaleBrokerId);
    }

    [Fact]
    public void TopLevel_DistributorExtendedWarranty_SpansTheDistributorsOwnCoverage_AndNoOneElses()
    {
        var lookup = new VehicleLookupDTO
        {
            VIN = "JTDBR32E0X0000001",
            Warranty = new VehicleWarrantyDTO
            {
                // The legacy pair is the latest-ending persisted entry, whoever provides it: here the other company's.
                ExtendedWarrantyStartDate = new DateTime(2029, 2, 1),
                ExtendedWarrantyEndDate = new DateTime(2031, 2, 1),
                ExtendedWarranties =
                [
                    new VehicleExtendedWarrantyDTO { ID = "EARNED", ProviderCompanyID = "901", StartDate = new DateTime(2027, 2, 1), EndDate = new DateTime(2028, 2, 1) },
                    new VehicleExtendedWarrantyDTO { ID = "PURCHASED", ProviderCompanyID = "901", StartDate = new DateTime(2028, 2, 1), EndDate = new DateTime(2030, 2, 1) },
                    new VehicleExtendedWarrantyDTO { ID = "OTHER", ProviderCompanyID = "101", StartDate = new DateTime(2029, 2, 1), EndDate = new DateTime(2031, 2, 1) },
                ],
            },
        };

        var row = VehicleReportRows.TopLevel("JTDBR32E0X0000001", lookup, distributorCompanyID: 901);

        Assert.Equal(new DateTime(2027, 2, 1), row.WarrantyDistributorExtendedStartDate);
        Assert.Equal(new DateTime(2030, 2, 1), row.WarrantyDistributorExtendedEndDate);
        Assert.Equal(new DateTime(2029, 2, 1), row.WarrantyExtendedStartDate);
        Assert.Equal(new DateTime(2031, 2, 1), row.WarrantyExtendedEndDate);

        // No distributor configured, or none of the coverage is its own: both columns stay empty.
        foreach (var distributorCompanyID in new long?[] { null, 555 })
        {
            var empty = VehicleReportRows.TopLevel("JTDBR32E0X0000001", lookup, distributorCompanyID);
            Assert.Null(empty.WarrantyDistributorExtendedStartDate);
            Assert.Null(empty.WarrantyDistributorExtendedEndDate);
        }
    }

    [Fact]
    public async Task ParquetReportFile_AppendsRowGroupsInOrder_AndReadsBackWithTheReportsColumns()
    {
        var path = Path.Combine(Path.GetTempPath(), "lookup-report-tests", $"{Guid.NewGuid():N}.parquet");
        var file = new ParquetReportFile<VehicleServiceItemReportModel>(path);
        await file.AppendAsync(
        [
            new VehicleServiceItemReportModel { VIN = "A", ServiceItemId = "1", StatusEnum = VehicleServiceItemReportStatuses.Pending, ClaimDate = new DateTimeOffset(2024, 6, 1, 3, 0, 0, TimeSpan.FromHours(3)), Price = 12.5m },
            new VehicleServiceItemReportModel { VIN = "A", ServiceItemId = "2" },
        ]);
        await file.AppendAsync([]);                                         // nothing to add, nothing changes
        await file.AppendAsync(
        [
            VehicleReportRows.ServiceItem("B", new VehicleServiceItemDTO { ServiceItemID = "1", Status = "pending", StatusEnum = VehcileServiceItemStatuses.Pending, Lock = new VehicleServiceItemLockDTO { State = VehicleServiceItemLockState.Locked } }, null),
            VehicleReportRows.ServiceItem("B", new VehicleServiceItemDTO { ServiceItemID = "2", Status = "expired", StatusEnum = VehcileServiceItemStatuses.Expired, Lock = new VehicleServiceItemLockDTO { State = VehicleServiceItemLockState.Missed } }, null),
        ]);
        await file.CompleteAsync();

        Assert.Equal(4, file.RowCount);
        using var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM read_parquet('{path.Replace('\\', '/')}')";
        using var reader = command.ExecuteReader();
        Assert.Equal(29, reader.FieldCount); // Display states add values, never report columns.
        Assert.Equal(typeof(int), reader.GetFieldType(reader.GetOrdinal("StatusEnum")));
        var rows = new List<(string Vin, string Item, object Status, object Claim, object Price, object StatusText)>();
        while (reader.Read())
            rows.Add((reader.GetString(reader.GetOrdinal("VIN")), reader.GetString(reader.GetOrdinal("ServiceItemId")), reader["StatusEnum"], reader["ClaimDate"], reader["Price"], reader["Status"]));

        Assert.Equal([("A", "1"), ("A", "2"), ("B", "1"), ("B", "2")], rows.Select(r => (r.Vin, r.Item)));
        Assert.Equal((int)VehcileServiceItemStatuses.Pending, Convert.ToInt32(rows[0].Status));
        Assert.Equal(new DateTime(2024, 6, 1, 0, 0, 0), Assert.IsType<DateTime>(rows[0].Claim)); // the instant, in UTC
        Assert.Equal(12.5m, Convert.ToDecimal(rows[0].Price));
        Assert.IsType<DBNull>(rows[1].Status);
        Assert.Equal("locked", rows[2].StatusText);
        Assert.Equal(5, Convert.ToInt32(rows[2].Status));
        Assert.Equal("missed", rows[3].StatusText);
        Assert.Equal(6, Convert.ToInt32(rows[3].Status));

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

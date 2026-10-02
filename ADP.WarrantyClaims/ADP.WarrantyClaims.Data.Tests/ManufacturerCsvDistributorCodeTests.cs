using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Constants;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The manufacturer CSV export takes the distributor code from the host
/// (<see cref="WarrantyClaimsDataOptions.DistributorCode"/>). The module has no built-in code.
/// </summary>
public class ManufacturerCsvDistributorCodeTests
{
    private static Entities.WarrantyClaim Claim() => new()
    {
        ClaimNumber = "0000001",
        WarrantyType = WarrantyTypes.A1.Key,
        DataID = "W",
        OperationType = OperationTypes.General,
        ProcessFlg = ProcessFlags.FirstSubmissionFromDealer,
        VIN_WMI = "AAA", VIN_VDS = "BBBBB", VIN_CD = "0", VIN_VIS = "00000001",
        RepairDate = new DateTime(2026, 1, 1),
        RepairCompletionDate = new DateTime(2026, 1, 1),
        Odometer = 100,
        KMFlg = KMFlags.K,
        RepairOrderNo = "RO-1",
        Condition = "", Cause = "", Remedy = "",
        HourTotalDistributor = 0m,
    };

    /// <summary>Every row carries the configured code in both FDIST and CLMNT.</summary>
    [Fact]
    public void Configured_distributor_code_fills_both_code_columns()
    {
        var service = new WarrantyClaimService(dataOptions: new WarrantyClaimsDataOptions { DistributorCode = "12345" });

        var row = Assert.Single(service.GenerateCSV(Claim()));

        Assert.Equal("12345", row.DistCode);
        Assert.Equal("12345", row.ClaimantCode);
    }

    /// <summary>Without a configured code, both columns stay empty.</summary>
    [Fact]
    public void Missing_distributor_code_leaves_both_code_columns_empty()
    {
        var row = Assert.Single(new WarrantyClaimService().GenerateCSV(Claim()));

        Assert.Null(row.DistCode);
        Assert.Null(row.ClaimantCode);
    }
}

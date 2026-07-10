using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Cases.Shared.Services;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;

namespace ShiftSoftware.ADP.Cases.Shared.Tests;

/// <summary>
/// CHARACTERIZATION tests for the VERBATIM-moved SharedClaimService (Phase 2, D16).
/// These encode the CURRENT production behavior of the original host application's claim status machine — including the
/// mutate-then-throw convention and the 10-error aggregation cap. If one of these fails after a
/// change, production behavior changed: that is a bug in the change, not in the test.
/// </summary>
public class SharedClaimServiceCharacterizationTests
{
    private class TestClaim : IClaim
    {
        public int Number { get; init; }
        public ClaimStatus? ClaimStatus { get; set; }
        public string? DistributorErrorMessage { get; set; }
        public DateTime? ProcessDate { get; set; }
        public string? InvoiceNo { get; set; }
        public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
        public string GetClaimIdentifier() => Number.ToString();
    }

    private static readonly SharedClaimService Service = new();

    private static TestClaim Claim(ClaimStatus? status, WarrantyManufacturerClaimStatus? manufacturer = null, int number = 1) =>
        new() { Number = number, ClaimStatus = status, ManufacturerStatus = manufacturer };

    private static void Update(TestClaim claim, UpdateStatusActionTypes action, string? inputText = null) =>
        Service.UpdateClaimStatus([claim], action, inputText);

    // ---- SubmitToDistributor -------------------------------------------------------------

    [Theory]
    [InlineData(ClaimStatus.Draft)]
    [InlineData(ClaimStatus.RejectedWithError)]
    public void Submit_FromDraftOrRejectedWithError_MovesToPendingProcess_AndStampsProcessDate(ClaimStatus from)
    {
        var claim = Claim(from);
        var before = DateTime.UtcNow;

        Update(claim, UpdateStatusActionTypes.SubmitToDistributor);

        Assert.Equal(ClaimStatus.PendingProcess, claim.ClaimStatus);
        Assert.NotNull(claim.ProcessDate);
        Assert.InRange(claim.ProcessDate!.Value, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(ClaimStatus.PendingProcess)]
    [InlineData(ClaimStatus.Accepted)]
    [InlineData(ClaimStatus.Certified)]
    public void Submit_FromOtherStatuses_Throws_ButStillMutates(ClaimStatus from)
    {
        var claim = Claim(from);

        var ex = Assert.Throws<ShiftEntityException>(() => Update(claim, UpdateStatusActionTypes.SubmitToDistributor));

        // Mutate-then-throw: the status HAS changed even though the batch failed.
        Assert.Equal(ClaimStatus.PendingProcess, claim.ClaimStatus);
        Assert.NotNull(claim.ProcessDate);
        Assert.Equal("Error", ex.Message.Title);
        Assert.Equal("", ex.Message.Body);
        var sub = Assert.Single(ex.Message.SubMessages!);
        Assert.Equal("Claim #1", sub.Title);
    }

    // ---- Distributor transitions ---------------------------------------------------------

    [Theory]
    [InlineData(UpdateStatusActionTypes.DistributorAccepted, ClaimStatus.Accepted)]
    [InlineData(UpdateStatusActionTypes.DistributorError, ClaimStatus.RejectedWithError)]
    [InlineData(UpdateStatusActionTypes.DistributorRejected, ClaimStatus.RejectedPermanently)]
    public void DistributorActions_FromPendingProcess_Succeed(UpdateStatusActionTypes action, ClaimStatus expected)
    {
        var claim = Claim(ClaimStatus.PendingProcess);

        Update(claim, action, "some input");

        Assert.Equal(expected, claim.ClaimStatus);
    }

    [Theory]
    [InlineData(UpdateStatusActionTypes.DistributorAccepted, ClaimStatus.Draft, ClaimStatus.Accepted)]
    [InlineData(UpdateStatusActionTypes.DistributorAccepted, ClaimStatus.Certified, ClaimStatus.Accepted)]
    [InlineData(UpdateStatusActionTypes.DistributorAccepted, ClaimStatus.Invoiced, ClaimStatus.Accepted)]
    [InlineData(UpdateStatusActionTypes.DistributorError, ClaimStatus.Draft, ClaimStatus.RejectedWithError)]
    [InlineData(UpdateStatusActionTypes.DistributorError, ClaimStatus.Certified, ClaimStatus.RejectedWithError)]
    [InlineData(UpdateStatusActionTypes.DistributorError, ClaimStatus.Invoiced, ClaimStatus.RejectedWithError)]
    [InlineData(UpdateStatusActionTypes.DistributorRejected, ClaimStatus.Draft, ClaimStatus.RejectedPermanently)]
    [InlineData(UpdateStatusActionTypes.DistributorRejected, ClaimStatus.Certified, ClaimStatus.RejectedPermanently)]
    [InlineData(UpdateStatusActionTypes.DistributorRejected, ClaimStatus.Invoiced, ClaimStatus.RejectedPermanently)]
    public void DistributorActions_FromGuardedStatuses_Throw_ButStillMutate(
        UpdateStatusActionTypes action, ClaimStatus from, ClaimStatus mutatedTo)
    {
        var claim = Claim(from);

        Assert.Throws<ShiftEntityException>(() => Update(claim, action, "input"));

        Assert.Equal(mutatedTo, claim.ClaimStatus);
    }

    [Fact]
    public void DistributorError_SetsErrorMessage_AndResetsManufacturerStatus()
    {
        var claim = Claim(ClaimStatus.PendingProcess, WarrantyManufacturerClaimStatus.Exported);

        Update(claim, UpdateStatusActionTypes.DistributorError, "wrong labor code");

        Assert.Equal(ClaimStatus.RejectedWithError, claim.ClaimStatus);
        Assert.Equal(WarrantyManufacturerClaimStatus.NA, claim.ManufacturerStatus);
        Assert.Equal("wrong labor code", claim.DistributorErrorMessage);
    }

    // ---- AssignInvoiceNo (unguarded) -----------------------------------------------------

    [Theory]
    [InlineData(ClaimStatus.Draft)]
    [InlineData(ClaimStatus.Accepted)]
    [InlineData(ClaimStatus.Invoiced)]
    public void AssignInvoiceNo_IsUnguarded_FromAnyStatus(ClaimStatus from)
    {
        var claim = Claim(from);

        Update(claim, UpdateStatusActionTypes.AssignInvoiceNo, "INV-001");

        Assert.Equal("INV-001", claim.InvoiceNo);
        Assert.Equal(from, claim.ClaimStatus); // status untouched
    }

    // ---- Manufacturer axis ---------------------------------------------------------------

    [Fact]
    public void ManufacturerRejected_FromPaid_Throws_ButStillMutates()
    {
        var claim = Claim(ClaimStatus.Accepted, WarrantyManufacturerClaimStatus.Paid);

        Assert.Throws<ShiftEntityException>(() => Update(claim, UpdateStatusActionTypes.ManufacturerRejected));

        Assert.Equal(WarrantyManufacturerClaimStatus.Rejected, claim.ManufacturerStatus);
    }

    [Fact]
    public void ManufacturerPaid_FromRejected_Throws_ButStillMutates()
    {
        var claim = Claim(ClaimStatus.Accepted, WarrantyManufacturerClaimStatus.Rejected);

        Assert.Throws<ShiftEntityException>(() => Update(claim, UpdateStatusActionTypes.ManufacturerPaid));

        Assert.Equal(WarrantyManufacturerClaimStatus.Paid, claim.ManufacturerStatus);
    }

    [Theory]
    [InlineData(WarrantyManufacturerClaimStatus.Exported)]
    [InlineData(WarrantyManufacturerClaimStatus.Paid)]
    [InlineData(null)]
    public void ManufacturerReset_AlwaysMovesToNA(WarrantyManufacturerClaimStatus? from)
    {
        var claim = Claim(ClaimStatus.Accepted, from);

        Update(claim, UpdateStatusActionTypes.ManufacturerReset);

        Assert.Equal(WarrantyManufacturerClaimStatus.NA, claim.ManufacturerStatus);
    }

    // ---- Aggregation contract ------------------------------------------------------------

    [Fact]
    public void MoreThanTenErrors_AreCappedAtTen_PlusOmittedSummaryRow()
    {
        var claims = Enumerable.Range(1, 12)
            .Select(i => (IClaim)Claim(ClaimStatus.Draft, number: i))
            .ToList();

        var ex = Assert.Throws<ShiftEntityException>(() =>
            Service.UpdateClaimStatus(claims, UpdateStatusActionTypes.DistributorAccepted, null));

        Assert.Equal("Error", ex.Message.Title);
        Assert.Equal(11, ex.Message.SubMessages!.Count); // 10 shown + 1 summary
        var summary = ex.Message.SubMessages[10];
        Assert.Equal("+2 more claims", summary.Title);
        Assert.Equal("omitted in this warning dialog", summary.Body);

        // Every claim was still mutated (mutate-then-throw applies to the whole batch).
        Assert.All(claims, c => Assert.Equal(ClaimStatus.Accepted, c.ClaimStatus));
    }

    [Fact]
    public void MixedBatch_ValidAndInvalid_ThrowsButMutatesAll()
    {
        var valid = Claim(ClaimStatus.PendingProcess, number: 1);
        var invalid = Claim(ClaimStatus.Draft, number: 2);

        Assert.Throws<ShiftEntityException>(() =>
            Service.UpdateClaimStatus([valid, invalid], UpdateStatusActionTypes.DistributorAccepted, null));

        Assert.Equal(ClaimStatus.Accepted, valid.ClaimStatus);
        Assert.Equal(ClaimStatus.Accepted, invalid.ClaimStatus);
    }
}

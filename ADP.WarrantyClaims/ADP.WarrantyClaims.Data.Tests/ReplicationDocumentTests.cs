using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The warranty-claim document that <see cref="WarrantyClaimsReplicationExtensions.ToWarrantyClaimModel"/> builds.
/// A host that projects the same document from its own copy of the claims must produce the same members.
/// </summary>
public class ReplicationDocumentTests
{
    private static Entities.WarrantyClaim AClaim() => new()
    {
        ID = 9,
        VIN = "DOCUMENTTESTVIN01",
        ClaimNumber = "CLAIM-9",
        ClaimStatus = ClaimStatus.Accepted,
        ManufacturerStatus = WarrantyManufacturerClaimStatus.Paid,
        DealerClaimNo = "D-9",
        InvoiceNo = "I-9",
        LaborOperationNoMain = "OP-9",
        DistComment1 = "comment",
        CompanyID = 4,
        CompanyBranchID = 12,
    };

    /// <summary>The lookup names the claim's dealer and branch as the repairing dealer and branch of a safety recall.</summary>
    [Fact]
    public void The_document_carries_the_claim_company_and_branch()
    {
        var document = WarrantyClaimsReplicationExtensions.ToWarrantyClaimModel(AClaim());

        Assert.Equal(4, document.CompanyID);
        Assert.Equal(12, document.BranchID);
    }

    [Fact]
    public void A_claim_without_a_branch_has_no_branch_on_the_document()
    {
        var claim = AClaim();
        claim.CompanyBranchID = null;

        Assert.Null(WarrantyClaimsReplicationExtensions.ToWarrantyClaimModel(claim).BranchID);
    }
}

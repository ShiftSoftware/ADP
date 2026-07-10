using System.ComponentModel;

namespace ShiftSoftware.ADP.Cases.Shared.Enums;

/// <summary>
/// Discriminator for the shared <c>Certificate</c> entity (one table serves all claim-type cases).
/// Moved VERBATIM from the original host application's <c>Services.Shared.Enums.CertificateTypes</c> (Phase 2 Slice 4, D13).
/// FROZEN: integer values are persisted in the production Certificates table.
/// </summary>
public enum CertificateTypes
{
    [Description("Warranty Claim")]
    WarrantyClaim = 0,

    [Description("Item Claim")]
    ClaimableItemClaim = 1,
}

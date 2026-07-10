using System.ComponentModel;

namespace ShiftSoftware.ADP.Cases.Shared.Enums;

/// <summary>
/// Settlement direction of a certificate. Moved VERBATIM from the original host application
/// <c>Services.Shared.Enums.CertificateSettlementTypes</c> (Phase 2 Slice 4, D13).
/// FROZEN: integer values are persisted in the production Certificates table.
/// </summary>
public enum CertificateSettlementTypes
{
    [Description("Reimburse Servicing Dealer")]
    ReimburseServicingDealer = 0,

    [Description("Charge Contributing Dealer")]
    ChargeContributingDealer = 1,
}

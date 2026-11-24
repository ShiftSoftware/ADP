using System.ComponentModel;

namespace ShiftSoftware.ADP.Cases.Shared.Enums;

/// <summary>
/// Settlement direction of a certificate.
/// FROZEN: integer values are persisted in the production Certificates table.
/// </summary>
public enum CertificateSettlementTypes
{
    [Description("Reimburse Servicing Dealer")]
    ReimburseServicingDealer = 0,

    [Description("Charge Contributing Dealer")]
    ChargeContributingDealer = 1,
}

using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Flags;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.Cases.Data.Entities;

/// <summary>
/// The shared certificate entity: one table certifies claims of ANY claim-type case, discriminated
/// by <see cref="CertificateType"/> (+ <see cref="SettlementType"/>). Existing hosts
/// already have the table (<c>dbo.Certificates</c> + temporal history) in production.
/// </summary>
/// <remarks>
/// Deliberate choices — relationships only, never columns:
/// <list type="bullet">
/// <item>The org-side collections (<c>WarrantyClaims</c>, <c>ReimbursementItemClaims</c>,
/// <c>ContributionItemClaims</c>) are absent — a module entity cannot navigate to org/other-module
/// dependents. Dependents keep their FK columns + navs; consumers configure the relationships from
/// their side (host: <c>DB.OnModelCreating</c>) and repositories query dependents by FK.</item>
/// <item>No <c>Campaign</c> navigation (ADP.Cases must not reference ADP.ClaimableItems);
/// the plain <see cref="CampaignID"/> column stays, and the consumer configures the FK.</item>
/// <item>Numbering columns (<see cref="CertificateNo"/>, <see cref="DistributorCertificateNo"/>,
/// <see cref="DisplayDistributorCertificateNo"/>) carry prod sequences — never re-seed.</item>
/// </list>
/// </remarks>
[TemporalShiftEntity]
public class Certificate : ShiftEntity<Certificate>,
    IEntityHasCompany<Certificate>
{
    [Required]
    public DateTime? CertificateDate { get; set; }

    [Required]
    public DateTime? PeriodStartDate { get; set; }

    [Required]
    public DateTime? PeriodEndDate { get; set; }
    public long? CompanyID { get; set; }

    public DateTime? InvoiceDate { get; set; }
    public long CertificateNo { get; set; }
    public long? DistributorCertificateNo { get; set; }
    public string? DisplayDistributorCertificateNo { get; set; }

    public CertificateTypes CertificateType { get; set; }
    public CertificateSettlementTypes SettlementType { get; set; }

    public long? CampaignID { get; set; }
}

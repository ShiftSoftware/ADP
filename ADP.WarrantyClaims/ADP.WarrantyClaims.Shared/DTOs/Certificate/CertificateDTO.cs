using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;

public class CertificateDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public DateTime? CertificateDate { get; set; }

    [Required]
    public DateTime? PeriodStartDate { get; set; }

    [Required]
    public DateTime? PeriodEndDate { get; set; }

    // Now the WARRANTY certificate DTO only (Phase 2 Slice 6): the item-claim certificate flow
    // moved to ADP.ClaimableItems with its own ItemClaimCertificateDTO; the always-empty
    // ReimbursementItemClaims list this class used to carry is gone with it.
    public List<WarrantyCertificateLineDTO> WarrantyClaims { get; set; } = new();

    public string? Notes { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public long? CertificateNo { get; set; }

    public string? DisplayDistributorCertificateNo { get; set; }
}

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

    // The WARRANTY certificate DTO only: the item-claim certificate flow is in ADP.ClaimableItems with
    // its own ItemClaimCertificateDTO.
    public List<WarrantyCertificateLineDTO> WarrantyClaims { get; set; } = new();

    public string? Notes { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public long? CertificateNo { get; set; }

    public string? DisplayDistributorCertificateNo { get; set; }
}

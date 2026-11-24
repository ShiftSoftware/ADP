using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;

/// <summary>
/// Item-claim certificate form/view DTO. Carries only the item-claim lines: item-claim certificate
/// payloads have no <c>WarrantyClaims</c> list. Property names are frozen.
/// </summary>
public class ItemClaimCertificateDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public DateTime? CertificateDate { get; set; }

    [Required]
    public DateTime? PeriodStartDate { get; set; }

    [Required]
    public DateTime? PeriodEndDate { get; set; }

    public List<ItemClaimListDTO> ReimbursementItemClaims { get; set; } = new();

    public string? Notes { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public long? CertificateNo { get; set; }

    public string? DisplayDistributorCertificateNo { get; set; }
}

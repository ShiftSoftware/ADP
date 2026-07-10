using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;

/// <summary>
/// Item-claim certificate form/view DTO (Phase 2 Slice 6). Split from the original host's union-shaped
/// <c>CertificateDTO</c>, which carried BOTH warranty and item-claim line lists on one class:
/// this one keeps only the item-claim side. Property names are unchanged; the always-empty
/// <c>WarrantyClaims</c> list is simply absent from item-claim certificate payloads now
/// (reviewed micro-deviation — the form never bound it).
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

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;

public class DeliveryDateEvaluationDTO
{
    public DateTime? VerifiedDate { get; set; }
    public string? VerifiedReason { get; set; }

    public DateTime? SuggestedDate { get; set; }

    /// <summary>
    /// Count of other claims for this VIN that propagation would touch (non-self, non-verified, non-null date).
    /// Identifiers are deliberately omitted to avoid leaking claims belonging to other dealers.
    /// </summary>
    public int SiblingCount { get; set; }
}

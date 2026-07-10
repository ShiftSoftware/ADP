namespace ShiftSoftware.ADP.ClaimableItems.API;

/// <summary>
/// Consumer seam for validating the scanned QR of an inspection-based item claim (the claim
/// payload's <c>ServiceItem.VehicleInspectionID</c> is set). QR/voucher policy is org-specific —
/// The original host implementation validates a SAS token and cross-checks the inspection record (exact-match for EveryTrigger
/// campaigns, sibling-inspection acceptance otherwise); other orgs have their own rules.
/// Register an implementation in the consumer's DI. When an inspection-based claim arrives and NO
/// validator is registered, the claim endpoint fails closed (configuration error).
/// </summary>
public interface IItemClaimQRValidator
{
    /// <summary>
    /// Validates the claim's QR against the org's rules.
    /// </summary>
    /// <param name="claimDTO">The deserialized (and signature-validated) claim payload.</param>
    /// <param name="signingKey">The host's ADP signing secret (same key that validated the payload signature).</param>
    /// <returns>A user-facing error message when invalid; null when the QR is acceptable.</returns>
    Task<string?> ValidateAsync(ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup.ItemClaimDTO claimDTO, string signingKey);
}

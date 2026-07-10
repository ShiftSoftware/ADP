namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// Consumer seam for the distributor-vs-dealer capability check the warranty flow needs (DTO distributor-field
/// stripping in ViewAsync, the delivery-date override guard, and the distributor-only line-validation
/// paths). The consumer implements this over its own capability source — e.g. a distributor consumer adapts its
/// <c>ICapabilityProvider</c> (TypeAuth <c>WarrantySystem.DistributorLevel</c>).
/// </summary>
public interface IWarrantyClaimsCapabilityProvider
{
    public bool IsDistributor { get; }
}

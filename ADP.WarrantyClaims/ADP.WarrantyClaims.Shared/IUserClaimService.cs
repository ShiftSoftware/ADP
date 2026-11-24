namespace ShiftSoftware.ADP.WarrantyClaims.Shared;

/// <summary>
/// One-method seam consumed only by <c>WarrantyClaimService</c> on the insert path to stamp the
/// current user's CompanyID. The consumer supplies the HttpContext-backed implementation.
/// </summary>
public interface IUserClaimService
{
    public long? GetCompanyID();
}

namespace ShiftSoftware.ADP.WarrantyClaims.Data;

/// <summary>
/// Consumer options the Data layer reads. Set via <c>WarrantyClaimsApiOptions</c>; the API extension
/// captures the values into this object and registers it as a singleton, so the Data layer can read
/// them without referencing the API options type (same capture-at-registration pattern as
/// <c>WarrantyClaimsReportOverrides</c>).
/// </summary>
public class WarrantyClaimsDataOptions
{
    /// <summary>
    /// The code the manufacturer assigned to this distributor. The warranty claim printout shows it
    /// as the Dist Code. The manufacturer CSV export writes it to the FDIST (distributor) and CLMNT
    /// (claimant) columns. Null (default) leaves those fields empty.
    /// </summary>
    public string? DistributorCode { get; set; }
}

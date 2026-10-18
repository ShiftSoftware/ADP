namespace ShiftSoftware.ADP.ClaimableItems.Data.Printing;

/// <summary>
/// Optional absolute paths to consumer-supplied .frx templates replacing the module-embedded
/// defaults — the sanctioned rebranding path. Null (the default) renders the embedded template.
/// The embedded templates hold no consumer branding: the print code fills in the company name, logo
/// and contact details from <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/>. Set via
/// <c>ClaimableItemsApiOptions.ReportOverrides</c>; the API extension registers this object as a
/// singleton so the Data-layer repositories can read it.
/// </summary>
public class ClaimableItemsReportOverrides
{
    public string? ItemClaimVoucherFrxPath { get; set; }

    public string? ItemClaimCertificateFrxPath { get; set; }

    public string? ItemClaimInvoiceFrxPath { get; set; }
}

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Printing;

/// <summary>
/// Optional absolute paths to consumer-supplied .frx templates replacing the module-embedded
/// defaults — the sanctioned rebranding path (e.g. a consumer substitutes its own legal text in the
/// warranty certificate). Null (the default) renders the embedded template. The embedded templates
/// hold no consumer branding: the print code fills in the company name, logo and contact details
/// from <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/>. Set via
/// <c>WarrantyClaimsApiOptions.ReportOverrides</c>; the API extension registers this object as a
/// singleton so the Data-layer repositories can read it.
/// </summary>
public class WarrantyClaimsReportOverrides
{
    public string? WarrantyClaimFrxPath { get; set; }

    public string? WarrantyCertificateFrxPath { get; set; }

    public string? WarrantyInvoiceFrxPath { get; set; }

    public string? ManufacturerWarrantyInvoiceFrxPath { get; set; }

    public string? FinancialReportFrxPath { get; set; }
}

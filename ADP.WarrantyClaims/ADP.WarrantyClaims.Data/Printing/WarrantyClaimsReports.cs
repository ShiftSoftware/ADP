using ShiftSoftware.ADP.Cases.Data.Printing;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Printing;

/// <summary>
/// Resolves the .frx template path for each warranty report: the consumer's
/// <see cref="WarrantyClaimsReportOverrides"/> path when set, otherwise the module-embedded default
/// extracted to a per-process temp file (see <see cref="EmbeddedReportProvider"/>).
/// </summary>
internal static class WarrantyClaimsReports
{
    private const string ResourcePrefix = "ShiftSoftware.ADP.WarrantyClaims.Data.Reports.";

    internal static string WarrantyClaim(WarrantyClaimsReportOverrides? overrides)
        => Resolve(overrides?.WarrantyClaimFrxPath, "WarrantyClaim.frx");

    internal static string WarrantyCertificate(WarrantyClaimsReportOverrides? overrides)
        => Resolve(overrides?.WarrantyCertificateFrxPath, "WarrantyCertificate.frx");

    internal static string WarrantyInvoice(WarrantyClaimsReportOverrides? overrides)
        => Resolve(overrides?.WarrantyInvoiceFrxPath, "WarrantyInvoice.frx");

    internal static string ManufacturerWarrantyInvoice(WarrantyClaimsReportOverrides? overrides)
        => Resolve(overrides?.ManufacturerWarrantyInvoiceFrxPath, "ManufacturerWarrantyInvoice.frx");

    internal static string FinancialReport(WarrantyClaimsReportOverrides? overrides)
        => Resolve(overrides?.FinancialReportFrxPath, "FinancialReport.frx");

    private static string Resolve(string? overridePath, string fileName)
        => overridePath
            ?? EmbeddedReportProvider.GetReportPath(typeof(WarrantyClaimsReports).Assembly, ResourcePrefix + fileName);
}

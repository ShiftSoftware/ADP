using ShiftSoftware.ADP.Cases.Data.Printing;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Printing;

/// <summary>
/// Resolves the .frx template path for each item-claim report: the consumer's
/// <see cref="ClaimableItemsReportOverrides"/> path when set, otherwise the module-embedded default
/// extracted to a per-process temp file (see <see cref="EmbeddedReportProvider"/>).
/// </summary>
internal static class ClaimableItemsReports
{
    private const string ResourcePrefix = "ShiftSoftware.ADP.ClaimableItems.Data.Reports.";

    internal static string ItemClaimVoucher(ClaimableItemsReportOverrides? overrides)
        => Resolve(overrides?.ItemClaimVoucherFrxPath, "ItemClaim.frx");

    internal static string ItemClaimCertificate(ClaimableItemsReportOverrides? overrides)
        => Resolve(overrides?.ItemClaimCertificateFrxPath, "ItemClaimCertificate.frx");

    internal static string ItemClaimInvoice(ClaimableItemsReportOverrides? overrides)
        => Resolve(overrides?.ItemClaimInvoiceFrxPath, "ItemClaimInvoice.frx");

    private static string Resolve(string? overridePath, string fileName)
        => overridePath
            ?? EmbeddedReportProvider.GetReportPath(typeof(ClaimableItemsReports).Assembly, ResourcePrefix + fileName);
}

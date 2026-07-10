namespace ShiftSoftware.ADP.ClaimableItems.Data.Printing;

/// <summary>
/// Optional absolute paths to consumer-supplied .frx templates replacing the module-embedded
/// defaults — the sanctioned rebranding path. Null (the default) renders the byte-frozen embedded
/// template. Set via <c>ClaimableItemsApiOptions.ReportOverrides</c>; the API extension registers
/// this object as a singleton so the Data-layer repositories can read it.
/// </summary>
public class ClaimableItemsReportOverrides
{
    public string? ItemClaimVoucherFrxPath { get; set; }

    public string? ItemClaimCertificateFrxPath { get; set; }

    public string? ItemClaimInvoiceFrxPath { get; set; }
}

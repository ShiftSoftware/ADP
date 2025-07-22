using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemClaimingMethod
{
    [Description("Claim by Scanning QR Code")]
    ClaimByScanningQRCode = 1,

    [Description("Claim by Entering Invoice & Job Number")]
    ClaimByEnteringInvoiceAndJobNumber = 2,
}
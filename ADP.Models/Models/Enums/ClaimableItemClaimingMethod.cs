using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// The method a dealer uses to claim a service item for a vehicle.
/// </summary>
[Docable]
public enum ClaimableItemClaimingMethod
{
    /// <summary>
    /// The service item is claimed by scanning a QR code.
    /// </summary>
    [Description("Claim by Scanning QR Code")]
    ClaimByScanningQRCode = 1,

    /// <summary>
    /// The service item is claimed by manually entering the invoice and job number.
    /// </summary>
    [Description("Claim by Entering Invoice & Job Number")]
    ClaimByEnteringInvoiceAndJobNumber = 2,
}
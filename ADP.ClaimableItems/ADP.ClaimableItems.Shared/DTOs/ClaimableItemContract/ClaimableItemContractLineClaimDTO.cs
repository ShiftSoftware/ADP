namespace ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItemContract;

/// <summary>The dealer's paperwork for claiming one service of a contract.</summary>
public class ClaimableItemContractLineClaimDTO
{
    public string? InvoiceNumber { get; set; }
    public string? JobNumber { get; set; }

    /// <summary>The scanned invoice, kept as scanned.</summary>
    public string? QRCode { get; set; }

    /// <summary>The VIN on the invoice, when the caller read one. It must be the contract's.</summary>
    public string? VIN { get; set; }
}

using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

[TemporalShiftEntity]
public class WarrantyClaimSubletLine : ShiftEntity<WarrantyClaimSubletLine>
{
    public string PayCode { get; set; } = default!;
    public string? SubletType { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountJPY { get; set; }

    // Distributor amount — what the distributor claims from the manufacturer, which can differ from the dealer's
    // claimed Amount (the distributor sometimes reimburses the dealer more than it claims from the manufacturer).
    // Mirrors WarrantyClaimPartLine.DistributorPrice / DistributorPriceJPY and WarrantyClaimLaborLine.DistributorHour.
    public decimal? DistributorAmount { get; set; }
    public decimal? DistributorAmountJPY { get; set; }
    public string? Description { get; set; }
}

using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

/// <summary>
/// The warranty exchange-rate/PRR row the manufacturer CSV export math runs on: the default rates
/// persistence behind <see cref="Shared.IWarrantyRatesStore"/>. Temporal: every export audit-upserts a
/// row, so the history table keeps the rates every past export used. A host that already has the table
/// under a legacy name pins that name via its <c>DbSet</c> property (e.g. <c>Setting</c> /
/// <c>SettingHistory</c>); fresh consumers get <c>WarrantyRates</c> / <c>WarrantyRatesHistory</c>.
/// </summary>
[TemporalShiftEntity]
public class WarrantyRates : ShiftEntity<WarrantyRates>
{
    public decimal PRR { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
}

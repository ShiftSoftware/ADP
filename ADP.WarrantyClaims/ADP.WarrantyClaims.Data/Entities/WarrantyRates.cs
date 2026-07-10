using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

/// <summary>
/// The warranty exchange-rate/PRR row the manufacturer CSV export math runs on. Moved from the
/// original host application's <c>Setting</c> entity and renamed for clarity (Phase 3 Slice 3.6,
/// D24) — the module now owns the default rates persistence behind
/// <see cref="Shared.IWarrantyRatesStore"/>. Temporal: every export audit-upserts a row, so the
/// history table keeps the rates every past export used. The original host pins the legacy table
/// name via its retained <c>DbSet</c> property (<c>Setting</c> / <c>SettingHistory</c>); fresh
/// consumers get <c>WarrantyRates</c> / <c>WarrantyRatesHistory</c>.
/// </summary>
[TemporalShiftEntity]
public class WarrantyRates : ShiftEntity<WarrantyRates>
{
    public decimal PRR { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
}

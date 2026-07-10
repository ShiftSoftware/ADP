using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;

/// <summary>
/// The warranty exchange-rate/PRR inputs the module's amount calculations need, and (since Phase 3
/// Slice 3.6, D24) the view/upsert DTO of the module-owned <c>WarrantyRates</c> entity — the
/// original host's Setting entity/DTO merged into the module. Derives from
/// <see cref="ShiftEntityViewAndUpsertDTO"/> exactly like the host DTO it absorbed, so the CRUD
/// wire shape (ID + IsDeleted + audit fields + the 4 rates), the export flow's
/// <c>Additional["Rates"]</c> echo and the <c>rates</c> query-param payload all stay identical.
/// The ID has no hash-id converter (raw numeric string), matching the original.
/// </summary>
public class WarrantyRatesDTO : ShiftEntityViewAndUpsertDTO
{
    /// <summary>
    /// Also round-trips through the export dialog: the warranty claim list sends it back in the
    /// <c>rates</c> query param so <see cref="IWarrantyRatesStore.PersistExportRatesAsync"/> updates
    /// the same row instead of inserting a new one.
    /// </summary>
    public override string? ID { get; set; }

    public decimal PRR { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
}

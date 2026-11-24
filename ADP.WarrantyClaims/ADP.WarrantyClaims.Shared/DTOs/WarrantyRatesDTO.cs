using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;

/// <summary>
/// The warranty exchange-rate/PRR inputs the module's amount calculations need, and the view/upsert
/// DTO of the module-owned <c>WarrantyRates</c> entity. Derives from
/// <see cref="ShiftEntityViewAndUpsertDTO"/>, which is part of the frozen wire shapes: the CRUD
/// payload (ID + IsDeleted + audit fields + the 4 rates), the export flow's
/// <c>Additional["Rates"]</c> echo and the <c>rates</c> query-param payload.
/// The ID has no hash-id converter (raw numeric string).
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

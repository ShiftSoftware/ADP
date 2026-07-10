using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;

/// <summary>
/// OData list DTO of the module-owned <c>WarrantyRates</c> entity (Phase 3 Slice 3.6, D24 — the
/// original host's SettingListDTO merged into the module; the name registers the
/// <c>WarrantyRates</c> EntitySet). ID-only, exactly like the host DTO it absorbed: the admin list
/// page only shows the row IDs, newest first.
/// </summary>
public class WarrantyRatesListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
}

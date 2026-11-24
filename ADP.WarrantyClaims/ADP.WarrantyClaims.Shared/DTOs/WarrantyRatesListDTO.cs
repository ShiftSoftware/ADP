using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;

/// <summary>
/// OData list DTO of the module-owned <c>WarrantyRates</c> entity (the name registers the
/// <c>WarrantyRates</c> EntitySet). ID-only: the admin list page only shows the row IDs, newest first.
/// </summary>
public class WarrantyRatesListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
}

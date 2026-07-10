using ShiftSoftware.ShiftEntity.Core;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

/// <summary>
/// A labor operation code that is appended to the flat-rate lookup on top of the standard
/// (quarterly/yearly) flat-rate file. Used mainly for SSC (Special Service Campaign / safety
/// recall) codes that become available before the next flat-rate file update.
/// The single <see cref="Time"/> value is applied across every flat-rate year.
/// </summary>
[TemporalShiftEntity]
public class AdditionalLaborOperationCode : ShiftEntity<AdditionalLaborOperationCode>
{
    public string Code { get; set; } = default!;
    public decimal Time { get; set; }
    public string? Description { get; set; }
}

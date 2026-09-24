namespace ShiftSoftware.ADP.Lookup.Services.Enums;

/// <summary>
/// The status displayed in service-item reports: a lock state takes precedence over the claiming
/// lifecycle, as it does on the claimable-item card. These values belong to the report projection;
/// the evaluator continues to use <see cref="VehcileServiceItemStatuses"/> and a separate lock block.
/// </summary>
public enum VehicleServiceItemReportStatuses
{
    // Keep the existing report codes stable; append display-only states after them.
    Processed = 0,
    Expired = 1,
    Pending = 2,
    Cancelled = 3,
    ActivationRequired = 4,
    Locked = 5,
    Missed = 6,
}

namespace ShiftSoftware.ADP.Cases.Shared.Enums;

/// <summary>
/// Batch status-transition actions for claim-type cases. Moved VERBATIM from the original host application
/// <c>Services.Shared.Enums.UpdateStatusActionTypes</c> (Phase 2, D16).
/// FROZEN: member NAMES ride route URLs (<c>{controller}/UpdateStatus/{actionType}</c> —
/// deployed Blazor clients interpolate the enum), and the numeric values (including the gap at 5)
/// are part of the persisted/serialized surface. Never rename or renumber.
/// </summary>
public enum UpdateStatusActionTypes
{
    SubmitToDistributor = 0,

    DistributorAccepted = 1,
    DistributorError = 2,
    DistributorRejected = 3,

    AssignInvoiceNo = 4,

    ManufacturerRejected = 6,
    ManufacturerExportToggle = 7,
    ManufacturerPaid = 8,
    ManufacturerReset = 9,
}

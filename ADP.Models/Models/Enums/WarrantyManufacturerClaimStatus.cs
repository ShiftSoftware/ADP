using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum WarrantyManufacturerClaimStatus
{
    [Description("N/A")]
    NA = 0,

    [Description("Exported")]
    Exported = 1,

    [Description("Downloaded")]
    Downloaded = 2,

    [Description("Paid")]
    Paid = 3,

    [Description("Rejected")]
    Rejected = 4,

    [Description("On Hold")]
    OnHold = 5
}
using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

public enum ClaimableItemAttachmentFieldBehavior
{
    [Description("Do not show attachment field")]
    Hidden = 0,

    [Description("Show attachment field (optional)")]
    Optional = 1,

    [Description("Show attachment field (required)")]
    Required = 2,
}
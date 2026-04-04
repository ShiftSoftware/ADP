using System.ComponentModel;

namespace ShiftSoftware.ADP.Models.Enums;

/// <summary>
/// Controls the visibility and requirement of the attachment field when claiming a service item.
/// </summary>
[Docable]
public enum ClaimableItemAttachmentFieldBehavior
{
    /// <summary>
    /// The attachment field is not shown during the claim process.
    /// </summary>
    [Description("Do not show attachment field")]
    Hidden = 0,

    /// <summary>
    /// The attachment field is shown but not required.
    /// </summary>
    [Description("Show attachment field (optional)")]
    Optional = 1,

    /// <summary>
    /// The attachment field is shown and must be filled to complete the claim.
    /// </summary>
    [Description("Show attachment field (required)")]
    Required = 2,
}
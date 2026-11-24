using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>
/// Yes/No display option used by list DTOs (e.g. ItemClaimListDTO.HasAttachment). Serialized by name
/// (JsonStringEnumConverter) — names/values are frozen wire contract.
/// </summary>
public enum YesNoOptions
{
    [Description("No")]
    No = 0,

    [Description("Yes")]
    Yes = 1,
}

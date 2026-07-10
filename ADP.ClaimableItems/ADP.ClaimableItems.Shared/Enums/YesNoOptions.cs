using System.ComponentModel;

namespace ShiftSoftware.ADP.ClaimableItems.Shared.Enums;

/// <summary>
/// Yes/No display option used by list DTOs (e.g. ItemClaimListDTO.HasAttachment). Moved with the
/// claim DTOs from the original host application's Services.Shared.Enums (Phase 2 Slice 5). Serialized by name
/// (JsonStringEnumConverter) — names/values are frozen wire contract.
/// </summary>
public enum YesNoOptions
{
    [Description("No")]
    No = 0,

    [Description("Yes")]
    Yes = 1,
}

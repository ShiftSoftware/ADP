using System.ComponentModel;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;

// Module-owned copy of the original host application's Services.Shared.Enums.YesNoOptions used by WarrantyClaimListDTO.HasAttachment.
// The original stays in the host (also consumed by ServiceActivation / ItemClaim code), so the moved list DTO and
// its AutoMapper map carry this identical enum instead — the JSON wire uses the names ("Yes"/"No") via
// JsonStringEnumConverter and the values (No=0/Yes=1) match, so the wire contract is unchanged.
public enum YesNoOptions
{
    [Description("No")]
    No = 0,

    [Description("Yes")]
    Yes = 1,
}

using System.ComponentModel;

namespace ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;

// Used by WarrantyClaimListDTO.HasAttachment. FROZEN wire contract: the JSON uses the names
// ("Yes"/"No") via JsonStringEnumConverter, and the values are No=0/Yes=1.
public enum YesNoOptions
{
    [Description("No")]
    No = 0,

    [Description("Yes")]
    Yes = 1,
}

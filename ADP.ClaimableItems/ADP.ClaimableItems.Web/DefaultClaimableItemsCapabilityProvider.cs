using ShiftSoftware.ADP.ClaimableItems.Web;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(IClaimableItemsCapabilityProvider))]

namespace ShiftSoftware.ADP.ClaimableItems.Web;

internal class DefaultClaimableItemsCapabilityProvider : IClaimableItemsCapabilityProvider
{
    public bool IsDistributor => false;
}

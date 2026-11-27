using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.ClaimableItems.Web;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Services;

internal static class ClaimableItemsCapability
{
    /// <summary>See <see cref="Repositories.ItemClaimRepository.IsDistributor"/>.</summary>
    public static bool IsDistributor(DbContext db)
    {
        if (PrintingServices.GetService<IClaimableItemsCapabilityProvider>(db) is { } capabilityProvider)
            return capabilityProvider.IsDistributor;

        return PrintingServices.GetService<ClaimableItemsDataOptions>(db)?.CertifyingAction is { } certifyingAction
            && PrintingServices.GetService<ITypeAuthService>(db)?.CanWrite(certifyingAction) == true;
    }
}

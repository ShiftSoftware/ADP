using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Services;

internal static class WarrantyClaimsCapabilityExtensions
{
    public static void RequireDistributor(this IWarrantyClaimsCapabilityProvider capabilityProvider)
    {
        if (!capabilityProvider.IsDistributor)
            throw new ShiftEntityException(
                new Message("Operation Prevented", "Only the distributor manages the warranty rates."),
                httpStatusCode: (int)HttpStatusCode.Forbidden);
    }
}

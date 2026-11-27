using ShiftSoftware.ADP.Cases.Shared.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ShiftEntity.Model;
using System.Net;

namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>Which side may run which batch action on claims. The claim repositories check it.</summary>
public static class ClaimActionAccess
{
    /// <summary>Everything but submitting, so an action added later is the distributor's too.</summary>
    public static bool IsDistributorOnly(UpdateStatusActionTypes actionType) =>
        actionType != UpdateStatusActionTypes.SubmitToDistributor;

    /// <summary>Refuses a distributor action to anyone else, and a value outside the enum to everyone.</summary>
    public static void RequireAllowed(UpdateStatusActionTypes actionType, bool isDistributor)
    {
        if (!Enum.IsDefined(actionType) || (IsDistributorOnly(actionType) && !isDistributor))
            throw new ShiftEntityException(
                new Message("Operation Prevented", "You are not authorized to perform this action on claims."),
                httpStatusCode: (int)HttpStatusCode.Forbidden);
    }

    /// <summary>The actions on one claim, from its form. The others are the list's.</summary>
    public static bool IsFormAction(UpdateStatusActionTypes actionType) => FormActions.Contains(actionType);

    /// <summary>
    /// The form actions a side may take on a claim in <paramref name="status"/>: the ones
    /// <see cref="Services.SharedClaimService"/> accepts from it.
    /// </summary>
    public static IReadOnlyList<UpdateStatusActionTypes> ActionsFrom(ClaimStatus? status, bool isDistributor) =>
        FormActions.Where(x => (isDistributor || !IsDistributorOnly(x)) && CanApply(x, status)).ToList();

    private static readonly UpdateStatusActionTypes[] FormActions =
    [
        UpdateStatusActionTypes.SubmitToDistributor,
        UpdateStatusActionTypes.DistributorAccepted,
        UpdateStatusActionTypes.DistributorError,
        UpdateStatusActionTypes.DistributorRejected,
    ];

    private static bool CanApply(UpdateStatusActionTypes actionType, ClaimStatus? status) =>
        actionType == UpdateStatusActionTypes.SubmitToDistributor
            ? status is ClaimStatus.Draft or ClaimStatus.RejectedWithError
            : status is not (ClaimStatus.Draft or ClaimStatus.Certified or ClaimStatus.Invoiced);
}

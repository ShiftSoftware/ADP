using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using System.Linq.Expressions;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// Leaves the distributor's figures out of a claim list projection. With them a dealer could work out the
/// distributor's labor rate and margin. They are taken out of the projection itself, so a filter or a sort on them
/// works on null and gives nothing away either.
/// </summary>
internal sealed class DealerClaimList : ExpressionVisitor
{
    private static readonly string[] withheld =
    [
        nameof(WarrantyClaimListDTO.HourTotalDistributor),
        nameof(WarrantyClaimListDTO.TotalClaimAmountDistributor),
    ];

    public static IQueryable<WarrantyClaimListDTO> WithoutDistributorFigures(IQueryable<WarrantyClaimListDTO> list) =>
        list.Provider.CreateQuery<WarrantyClaimListDTO>(new DealerClaimList().Visit(list.Expression));

    // Null rather than left out: SQL can not filter or sort on a member that the projection does not set.
    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node) =>
        node.Member.DeclaringType == typeof(WarrantyClaimListDTO) && withheld.Contains(node.Member.Name)
            ? node.Update(Expression.Constant(null, typeof(decimal?)))
            : base.VisitMemberAssignment(node);
}

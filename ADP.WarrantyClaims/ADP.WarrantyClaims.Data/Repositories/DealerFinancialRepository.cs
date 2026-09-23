using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The dealer-facing financial list over the warranty claim.
///
/// <para>
/// <b>THE FIVE IGNORES ON THIS REPOSITORY'S LIST MAP (<c>Mappers/WarrantyClaimsMapper.cs</c>) ARE THE ONLY
/// THING KEEPING DISTRIBUTOR-SIDE FIGURES OUT OF A DEALER'S RESPONSE. They are not tidying. Read this before
/// changing anything there.</b>
/// </para>
///
/// <para>
/// <c>DealerFinancialListDTO</c> is declared as <c>: DistributorFinancialListDTO { }</c> - an empty
/// subclass. It adds nothing and removes nothing, so on shape alone the dealer list and the
/// distributor list are the SAME DTO. The entity carries a value for every one of these five
/// columns. What separates the two audiences is five <c>.ForMember(..., opt => opt.Ignore())</c> calls
/// on the dealer list map, which otherwise inherits the distributor list map whole.
/// </para>
///
/// <para>
/// <b>Why this is dangerous to get wrong.</b> Drop these and the endpoint still returns <b>200</b>,
/// with the <b>same response shape</b>, and <b>no compiler diagnostic fires</b> - <c>SHENGEN008</c>
/// will not complain (the member IS mapped now), and <c>SHENGEN004</c>/<c>007</c> will not either
/// (nothing is unmapped). The only visible symptom is that a dealer starts seeing the distributor's
/// margin figures. It is not data loss, it is data exposure.
/// </para>
///
/// <para>
/// <b>And the route does not save you.</b> <c>DealerFinancialController</c> is its own route with its
/// own DTO, its gate is weaker than a bare <c>CanRead</c> (a three-way conjunct that a full-access
/// principal passes, and that is inert when action-tree authorization is off or the action is left
/// null - which the controller's own doc comment records the host doing), and this repository is
/// bare <c>base(db)</c> with no <c>FilterByTypeAuthValues</c>, so there is no row scoping either.
/// The mapper is the whole control.
/// </para>
///
/// <para>
/// Guarded permanently by <c>DealerFinancialExposureTests</c>, which asserts all five come back null
/// for a claim whose entity has all five populated.
/// </para>
/// </summary>
public class DealerFinancialRepository : ShiftRepository<ShiftDbContext, WarrantyClaim, DealerFinancialListDTO, WarrantyClaimDTO>
{
    // The maps are in Mappers/WarrantyClaimsMapper.cs - the dealer list map is the distributor one minus the five
    // withheld members, by construction (IncludeBase).
    public DealerFinancialRepository(ShiftDbContext db) : base(db)
    {
    }
}

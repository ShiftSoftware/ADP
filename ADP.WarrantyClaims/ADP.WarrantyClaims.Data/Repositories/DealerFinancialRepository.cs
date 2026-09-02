using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The dealer-facing financial list over the warranty claim.
///
/// <para>
/// <b>THE FIVE <c>IgnoreList</c> CALLS BELOW ARE THE ONLY THING KEEPING DISTRIBUTOR-SIDE FIGURES OUT
/// OF A DEALER'S RESPONSE. They are not tidying. Read this before changing anything here.</b>
/// </para>
///
/// <para>
/// <c>DealerFinancialListDTO</c> is declared as <c>: DistributorFinancialListDTO { }</c> - an empty
/// subclass. It adds nothing and removes nothing, so on shape alone the dealer list and the
/// distributor list are the SAME DTO. The entity carries a value for every one of these five
/// columns. What separated the two audiences was five <c>.ForMember(..., x => x.Ignore())</c> calls
/// on the old dealer map, and this is their replacement.
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
    public DealerFinancialRepository(ShiftDbContext db) : base(db, i => i.UseGeneratedMapper(map => map

        // SHENGEN010 - resolved by taking the child write over explicitly rather than letting the
        // generator compose it. See WarrantyClaimLineWriter for why REPLACE (not business-key
        // reconciliation) is the correct answer for this aggregate: UpsertAsync deletes the existing
        // line rows before anything maps, so a fresh set is inserted rather than orphaned.
        .IgnoreEntity(e => e.WarrantyClaimLaborLines)
        .IgnoreEntity(e => e.WarrantyClaimSubletLines)
        .IgnoreEntity(e => e.WarrantyClaimPartLines)
        .AfterEntity((dto, entity, context) => Mapping.WarrantyClaimLineWriter.Write(dto, entity, context))

        // ── WITHHELD FROM THE DEALER AUDIENCE ─────────────────────────────────────────────
        // Five distributor-side figures. The entity has values for all of them; a dealer must not
        // see any of them. Exactly these five and no more - see the note on the shared members
        // below, because blanking too much is as wrong as blanking too little.
        .IgnoreList(d => d.DistComment1)
        .IgnoreList(d => d.HourTotalDistributor)
        .IgnoreList(d => d.LaborTotalAmountDistributor)
        .IgnoreList(d => d.SubletTotalAmountDistributor)
        .IgnoreList(d => d.PartsTotalAmountDistributor)

        // TWO MORE FLATTENINGS THE OLD PROFILE NEVER MENTIONED - AutoMapper derived them by name
        // convention (Certificate.CertificateNo -> CertificateCertificateNo), the generated
        // projection does not, and SHENGEN007 is what caught them.
        //
        // The pre-migration baseline pins the shape for a claim with NO certificate, which is what
        // the parity seed has: "CertificateCertificateNo": "" and "CertificateInvoiceDate": null -
        // an EMPTY STRING, not null, because AutoMapper's conversion to string renders a null source
        // as "". Reproduced literally; a plain `e.Certificate.CertificateNo` would return null there
        // and diff on every row.
        .ForList(d => d.CertificateCertificateNo, e => e.Certificate != null && e.Certificate.CertificateNo != null
            ? e.Certificate.CertificateNo.ToString()
            : "")
        .ForList(d => d.CertificateInvoiceDate, e => e.Certificate != null ? e.Certificate.InvoiceDate : null)

        // ── SHARED WITH THE DISTRIBUTOR MAP ───────────────────────────────────────────────
        // These three were configured on BOTH old maps and must stay on both. They are the reason
        // this is an IgnoreList list and not "ignore everything distributor-shaped": the dealer map
        // was never a blanket redaction, it withheld five named members and mapped the rest exactly
        // as the distributor map did.
        //
        // The DateTime -> DateTimeOffset conversion is pinned at TimeSpan.Zero, transcribed from the
        // profile. The offset is load-bearing: verification.md Rule 2 compares business dates
        // literally, so an offset, kind or precision difference shifts every timestamp on this list
        // and shows up as a diff on every row.
        .ForList(d => d.ProcessDate, e => e.ProcessDate.HasValue
            ? new DateTimeOffset(e.ProcessDate.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null)
        .ForList(d => d.DistributorProcessDate, e => e.DistributorProcessDate.HasValue
            ? new DateTimeOffset(e.DistributorProcessDate.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null)

        // Pinned flattening. The profile's own comment records that after the generic rename this
        // member no longer decomposes to a valid navigation + property path by convention
        // (ReferenceWarrantyClaim + Number), so it has to be written out. Omit it and the column
        // comes back empty.
        .ForList(d => d.ReferenceWarrantyClaimNumber, e => e.ReferenceWarrantyClaim!.ClaimNumber)))
    {
    }
}

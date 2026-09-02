using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Mapping;

/// <summary>
/// The certificate's claim lines, entity to DTO.
///
/// <para>
/// <b>Why this one has to be declared by hand.</b> Pair mappers are normally auto-generated for every
/// (child entity, child DTO) pair the generator can DISCOVER inside a view DTO. It cannot discover
/// this one: <c>CertificateDTO.WarrantyClaims</c> is a list of these, but the <c>ADP.Cases</c>
/// <c>Certificate</c> entity carries no claims navigation to compose from - which is exactly what
/// <c>SHENGEN004</c> means when it reports <c>WarrantyClaims</c> as having "no convention or deep
/// composition". So the pair is never generated, and
/// <c>WarrantyCertificateRepository.ViewAsync</c> - which queries the claims by foreign key and
/// projects them itself - has nothing to call.
/// </para>
///
/// <para>
/// Declaring a <c>[ShiftEntityMapper]</c> partial class that implements the pair contract makes the
/// generator emit the implementation into THIS type, convention included, with the one member below
/// customized. That is the whole reason to use the framework seam rather than a hand-written
/// projection: <c>ProcessDate</c>, <c>DistributorProcessDate</c> and <c>WarrantyType</c> keep coming
/// from the convention, so adding a member to the DTO later maps it without anyone remembering to
/// come back here.
/// </para>
/// </summary>
[ShiftEntityMapper]
public partial class WarrantyCertificateLineMapper
    : IShiftObjectMapper<Entities.WarrantyClaim, WarrantyCertificateLineDTO>
{
    partial void Configure(
        ShiftMapperBuilder<Entities.WarrantyClaim, WarrantyCertificateLineDTO, WarrantyCertificateLineDTO> map)
    {
        // Hand-built select DTO carrying BOTH halves: the claim's id as Value and its claim NUMBER
        // as Text. Transcribed from the deleted profile. Note this is not the usual FK + navigation
        // shape the ToSelectDTO convention produces - the Text here is a business identifier the
        // certificate printout shows, not a name - which is why it was pinned then and stays pinned.
        map.ForView(d => d.WarrantyClaim, e => new ShiftEntitySelectDTO
        {
            Value = e.ID.ToString(),
            Text = e.ClaimNumber.ToString(),
        });

        // A pair's DTO is both its view and its list shape, so ForView alone leaves the list side
        // unmapped (SHENGEN007). Same projection, minus the redundant ToString() on a string so it
        // stays EF-translatable if this pair is ever composed into a parent's list projection.
        map.ForList(d => d.WarrantyClaim, e => new ShiftEntitySelectDTO
        {
            Value = e.ID.ToString(),
            Text = e.ClaimNumber,
        });
    }
}

/// <summary>
/// The part-line pair, declared by hand ONLY to settle <c>WarrantyClaimPartLineDTO.Loading</c>.
///
/// <para>
/// Declaring this class replaces the auto-generated part-line pair mapper; everything except the one
/// ignored member below still comes from the convention.
/// </para>
/// </summary>
[ShiftEntityMapper]
public partial class WarrantyClaimPartLineMapper
    : IShiftObjectMapper<WarrantyClaimPartLine, WarrantyClaimPartLineDTO>
{
    partial void Configure(
        ShiftMapperBuilder<WarrantyClaimPartLine, WarrantyClaimPartLineDTO, WarrantyClaimPartLineDTO> map)
    {
        // The member the baseline SHENGEN004 names on this pair, and an ignore is the right answer.
        //
        // `Loading` has NO entity source - WarrantyClaimPartLine has no such column - because it is a
        // pure client-side spinner flag: the only code that touches it is the Blazor claim form
        // (WarrantyClaimForm.razor.cs sets it true/false around a lookup, and the .razor reads it to
        // show a progress indicator). It never round-trips through the database, so there is nothing
        // to read on the way out and nothing to write on the way in.
        map.IgnoreView(d => d.Loading);

        // Same member, list side - a pair's DTO is both shapes.
        map.IgnoreList(d => d.Loading);
    }
}

/// <summary>Named pair mapper for the labor lines. Declared only so <see cref="WarrantyClaimLineWriter"/>
/// has a nameable type to call; the generated convention is otherwise untouched.</summary>
[ShiftEntityMapper]
public partial class WarrantyClaimLaborLineMapper
    : IShiftObjectMapper<WarrantyClaimLaborLine, WarrantyClaimLaborLineDTO>
{
}

/// <summary>Named pair mapper for the sublet lines. Same reason as the labor one.</summary>
[ShiftEntityMapper]
public partial class WarrantyClaimSubletLineMapper
    : IShiftObjectMapper<WarrantyClaimSubletLine, WarrantyClaimSubletLineDTO>
{
}

/// <summary>
/// Writes a claim's three line collections from the DTO, explicitly.
///
/// <para>
/// <b>This exists to resolve SHENGEN010, and the resolution is REPLACE - deliberately, not by
/// business-key reconciliation.</b> The diagnostic warns that composing these children automatically
/// on the write side is replace-with-new, and that replace-with-new "will either fail on the foreign
/// key or orphan and duplicate rows". That is true in general and NOT true here, for a reason the
/// generator cannot see:
/// <c>WarrantyClaimRepository.UpsertAsync</c> calls
/// <c>db.Set&lt;WarrantyClaimLaborLine&gt;().RemoveRange(entity.WarrantyClaimLaborLines.ToList())</c>
/// - and the same for sublet and part lines - <b>before</b> delegating to the base upsert. The old
/// rows are already marked deleted by the time anything maps, so replacing the collection inserts a
/// fresh set rather than orphaning an old one. Delete-then-insert is this aggregate's established
/// persistence pattern, and <c>WarrantyClaimService.WarrantyLinesValidationAndTransformation</c>
/// depends on it (it snapshots the existing rows by ID first).
/// </para>
///
/// <para>
/// Reconciling by business key instead would be a behaviour change, not a fix: these line entities
/// have no natural unique key to reconcile on, and update-in-place would contradict the RemoveRange
/// that has already run. So the write is made EXPLICIT here - the generator no longer composes it,
/// the diagnostic no longer fires, and the behaviour is byte-for-byte what the generated code did
/// (<c>existing.X = dto.X.Select(d =&gt; pair.MapBack(d, new Child(), ctx)).ToList()</c>), which is
/// in turn what the AutoMapper reverse map did before the migration.
/// </para>
/// </summary>
public static class WarrantyClaimLineWriter
{
    private static readonly WarrantyClaimLaborLineMapper labor = new();
    private static readonly WarrantyClaimSubletLineMapper sublet = new();
    private static readonly WarrantyClaimPartLineMapper part = new();

    public static void Write(WarrantyClaimDTO dto, Entities.WarrantyClaim entity, MappingContext context)
    {
        // The null checks mirror the generated code's `dto.X == null ? null : ...` exactly.
        if (dto.WarrantyClaimLaborLines is not null)
            entity.WarrantyClaimLaborLines = dto.WarrantyClaimLaborLines
                .Select(d => labor.MapBack(d, new WarrantyClaimLaborLine(), context)).ToList();

        if (dto.WarrantyClaimSubletLines is not null)
            entity.WarrantyClaimSubletLines = dto.WarrantyClaimSubletLines
                .Select(d => sublet.MapBack(d, new WarrantyClaimSubletLine(), context)).ToList();

        if (dto.WarrantyClaimPartLines is not null)
            entity.WarrantyClaimPartLines = dto.WarrantyClaimPartLines
                .Select(d => part.MapBack(d, new WarrantyClaimPartLine(), context)).ToList();
    }
}

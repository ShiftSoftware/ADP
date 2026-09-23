using ShiftMapper;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Certificate;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ADP.WarrantyClaims.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core.Mapping;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Mappers;

// SM0017 (the three Conditions) means a map cannot be projected, and SM0030 that the framework's JSON ↔ file-list
// conversion for the attachments has no query form. Both land on the claim's view map and its reverse only, and
// neither is ever projected: the view map runs in memory on a loaded row, the write map onto a tracked one. The maps a
// repository projects are the list maps, which carry neither.
#pragma warning disable SM0017, SM0030

/// <summary>
/// The ONE place the warranty-claims module customizes its CRUD maps. Every repository triple's four maps (entity ↔
/// view, entity → list, entity → entity) are declared automatically from the repository's type arguments, nested
/// children included, and most need nothing. The pairs written here are the ones convention cannot do; each
/// <c>CreateMap</c> REPLACES the automatic map for that pair (SM0047, informational) and the other pairs of the triple
/// stay automatic. A repository says nothing about what a member maps from. The one map served outside a repository
/// triple - a certificate's claim lines - is declared here too, and reached through <c>IMapper</c>.
/// <para>
/// Two things a <c>CreateMap</c> does not inherit from the automatic map it replaces, so both are written: a write map
/// is declared as the REVERSE of the view map (a DTO is a subset of its entity, so the entity-only members it leaves
/// untouched are the quiet SM0006 rather than one SM0001 each), and flattening is off (<see cref="ConfigureDefaults"/>),
/// as it is on the automatic maps.
/// </para>
/// <para>
/// Three repositories close the claim entity - <c>WarrantyClaimRepository</c>, <c>DistributorFinancialRepository</c>
/// and <c>DealerFinancialRepository</c> - over three list DTOs and ONE view DTO. So the claim's view and write maps
/// are one pair each, shared by all three; the list maps are three pairs, and they are where the audiences differ.
/// </para>
/// </summary>
public class WarrantyClaimsMapper : ShiftMapperBase
{
    public WarrantyClaimsMapper()
    {
        // The framework's rules (the ShiftEntitySelectDTO convention, the members it owns such as ID, the
        // string → long foreign-key rule) reach a map through the repository markers only in a project that closes
        // ShiftRepository<,,,>. These maps are ALSO generated into every project that references this assembly, most
        // of which close none, so the class carries the pack itself.
        AddConversions<ShiftEntityConversions>();

        AddClaimMaps();
        AddListMaps();
        AddCertificateMaps();
    }

    /// <summary>
    /// The automatic maps these replace do not flatten; the maps written here keep that, so replacing one changes
    /// nothing but the members written.
    /// </summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // The claim (view + write), shared by all three repositories
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddClaimMaps()
    {
        // VIEW: nothing to say - the three line collections compose through the line pairs below.
        //
        // WRITE: the three line collections are REPLACED, deliberately - not reconciled by business key. The write
        // map assigns each a new collection of newly mapped lines (SM0049), and that is right here for a reason the
        // map cannot see: WarrantyClaimRepository.UpsertAsync calls
        // db.Set<WarrantyClaimLaborLine>().RemoveRange(entity.WarrantyClaimLaborLines.ToList()) - and the same for
        // sublet and part lines - BEFORE the base upsert maps anything. The old rows are already marked deleted, so
        // replacing the collection inserts a fresh set rather than orphaning an old one. Delete-then-insert is this
        // aggregate's established persistence pattern, and WarrantyClaimService.WarrantyLinesValidationAndTransformation
        // depends on it (it snapshots the existing rows by ID first). These line entities have no natural unique key
        // to reconcile on, and update-in-place would contradict the RemoveRange that has already run.
        //
        // A null collection is LEFT ALONE rather than replaced with an empty one - the Condition - which is what the
        // explicit line writer this replaces did. (A request cannot reach that today: the DTO's computed totals, which
        // this map also writes, read all three collections first.)
        CreateMap<WarrantyClaim, WarrantyClaimDTO>()
            .ReverseMap()
            .ForMember(e => e.WarrantyClaimLaborLines, opt => opt.Condition((dto, _, _) => dto.WarrantyClaimLaborLines != null))
            .ForMember(e => e.WarrantyClaimSubletLines, opt => opt.Condition((dto, _, _) => dto.WarrantyClaimSubletLines != null))
            .ForMember(e => e.WarrantyClaimPartLines, opt => opt.Condition((dto, _, _) => dto.WarrantyClaimPartLines != null));

        // `Loading` has NO entity source - WarrantyClaimPartLine has no such column - because it is a pure client-side
        // spinner flag: the Blazor claim form sets it around a lookup and reads it to show a progress indicator. It
        // never round-trips through the database, so there is nothing to read on the way out and nothing to write on
        // the way in. (The labor and sublet line pairs need nothing and stay automatic.)
        CreateMap<WarrantyClaimPartLine, WarrantyClaimPartLineDTO>()
            .ForMember(d => d.Loading, opt => opt.Ignore())
            .ReverseMap();
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // The three list maps
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddListMaps()
    {
        // ── DISTRIBUTOR ──────────────────────────────────────────────────────────────────────────
        CreateMap<WarrantyClaim, DistributorFinancialListDTO>()
            // Two flattenings through the Certificate navigation. The shape for a claim with NO certificate is pinned:
            // "CertificateCertificateNo": "" and "CertificateInvoiceDate": null - an EMPTY STRING, not null, which is
            // what the pre-migration map rendered. A plain `e.Certificate.CertificateNo` would return null there.
            .ForMember(d => d.CertificateCertificateNo, opt => opt.MapFrom(e => e.Certificate != null && e.Certificate.CertificateNo != null
                ? e.Certificate.CertificateNo.ToString()
                : ""))
            .ForMember(d => d.CertificateInvoiceDate, opt => opt.MapFrom(e => e.Certificate != null ? e.Certificate.InvoiceDate : null))

            // The DateTime -> DateTimeOffset conversion is pinned at TimeSpan.Zero, and the offset is load-bearing:
            // business dates are compared literally, so an offset, kind or precision difference shifts every
            // timestamp on this list. All three list maps carry it and it has to stay identical across them, or the
            // same claim reports a different timestamp depending on which list you asked for.
            .ForMember(d => d.ProcessDate, opt => opt.MapFrom(e => e.ProcessDate.HasValue
                ? new DateTimeOffset(e.ProcessDate.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null))
            .ForMember(d => d.DistributorProcessDate, opt => opt.MapFrom(e => e.DistributorProcessDate.HasValue
                ? new DateTimeOffset(e.DistributorProcessDate.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null))

            // Pinned flattening: the member does not decompose to a navigation + property path by name (the
            // navigation is ReferenceWarrantyClaim, the property ClaimNumber). Omit it and the column comes back empty.
            .ForMember(d => d.ReferenceWarrantyClaimNumber, opt => opt.MapFrom(e => e.ReferenceWarrantyClaim!.ClaimNumber));

        // ── DEALER ───────────────────────────────────────────────────────────────────────────────
        // THE FIVE IGNORES BELOW ARE THE ONLY THING KEEPING DISTRIBUTOR-SIDE FIGURES OUT OF A DEALER'S RESPONSE.
        //
        // DealerFinancialListDTO is declared `: DistributorFinancialListDTO { }` - an empty subclass - so on shape
        // alone the two lists are the SAME DTO, and the entity carries a value for every one of these five columns.
        // Drop an ignore and the endpoint still returns 200 with the same response shape and no diagnostic; the only
        // symptom is dealers receiving the distributor's margin figures. DealerFinancialController's gate is weaker
        // than a bare CanRead and DealerFinancialRepository applies no row scoping, so the map is the whole control.
        //
        // The dealer map is the distributor map (IncludeBase: every member configured above, inherited) minus exactly
        // these five - blanking too much is as wrong as blanking too little. Guarded by DealerFinancialExposureTests,
        // which projects one claim through both maps and asserts they differ by exactly these five members.
        CreateMap<WarrantyClaim, DealerFinancialListDTO>()
            .IncludeBase<WarrantyClaim, DistributorFinancialListDTO>()
            .ForMember(d => d.DistComment1, opt => opt.Ignore())
            .ForMember(d => d.HourTotalDistributor, opt => opt.Ignore())
            .ForMember(d => d.LaborTotalAmountDistributor, opt => opt.Ignore())
            .ForMember(d => d.SubletTotalAmountDistributor, opt => opt.Ignore())
            .ForMember(d => d.PartsTotalAmountDistributor, opt => opt.Ignore());

        // ── THE CLAIM LIST ───────────────────────────────────────────────────────────────────────
        CreateMap<WarrantyClaim, WarrantyClaimListDTO>()
            // The same TimeSpan.Zero conversion the two financial lists carry - see there.
            .ForMember(d => d.ProcessDate, opt => opt.MapFrom(e => e.ProcessDate.HasValue
                ? new DateTimeOffset(e.ProcessDate.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null))
            .ForMember(d => d.DistributorProcessDate, opt => opt.MapFrom(e => e.DistributorProcessDate.HasValue
                ? new DateTimeOffset(e.DistributorProcessDate.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null))

            // THE "[]" LITERAL IS THE POINT: an empty JSON array counts as NO attachment. Drop the comparison and every
            // row whose Attachments column holds "[]" flips from No to Yes, with no error to notice it. Note this
            // deliberately does NOT read the entity's own HasAttachment column, which exists - switching sources would
            // be a behaviour change wherever the two disagree.
            .ForMember(d => d.HasAttachment, opt => opt.MapFrom(e => e.Attachments == null || e.Attachments == "[]"
                ? YesNoOptions.No
                : YesNoOptions.Yes))

            // Pinned flattening, same as both financial lists.
            .ForMember(d => d.ReferenceWarrantyClaimNumber, opt => opt.MapFrom(e => e.ReferenceWarrantyClaim!.ClaimNumber));
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Certificate (the shared ADP.Cases Certificate)
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddCertificateMaps()
    {
        CreateMap<Certificate, CertificateDTO>()
            // WarrantyClaims: the REPOSITORY fills it. The shared ADP.Cases Certificate carries no claims collection -
            // there is no navigation to compose from - so WarrantyCertificateRepository.ViewAsync queries the claims by
            // foreign key and maps them through the line map below.
            .ForMember(d => d.WarrantyClaims, opt => opt.Ignore())

            // Notes: the DTO declares it, the Certificate entity has no Notes column at all, so there has never been
            // anything to read.
            .ForMember(d => d.Notes, opt => opt.Ignore());

        // A certificate's claim line. Hand-built select DTO carrying BOTH halves: the claim's id as Value and its claim
        // NUMBER as Text. This is not the usual foreign key + navigation shape the select convention produces - the
        // Text here is a business identifier the certificate printout shows, not a name. ProcessDate,
        // DistributorProcessDate and WarrantyType match by name.
        CreateMap<WarrantyClaim, WarrantyCertificateLineDTO>()
            .ForMember(d => d.WarrantyClaim, opt => opt.MapFrom(e => new ShiftEntitySelectDTO
            {
                Value = e.ID.ToString(),
                Text = e.ClaimNumber,
            }));
    }
}
#pragma warning restore SM0017, SM0030

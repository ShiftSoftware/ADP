using System.Text.Json;
using ShiftMapper;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Data.Entities;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.Campaign;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.CampaignVinEntry;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ClaimableItem;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaim;
using ShiftSoftware.ADP.ClaimableItems.Shared.DTOs.ItemClaimCertificate;
using ShiftSoftware.ADP.ClaimableItems.Shared.Enums;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Mapping;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.ClaimableItems.Data.Mappers;

// SM0030 is reported on the item-claim view map and its reverse for the attachments: the framework's JSON ↔ file-list
// conversion has no query form. Neither is ever projected - the view map runs in memory on a loaded row, the write map
// onto a tracked one; the one map a repository projects is the list map.
#pragma warning disable SM0030

/// <summary>
/// The ONE place the claimable-items module customizes its CRUD maps. Every repository triple's four maps (entity ↔
/// view, entity → list, entity → entity) are declared automatically from the repository's type arguments, nested
/// children included, and most need nothing. The pairs written here are the ones convention cannot do; each
/// <c>CreateMap</c> REPLACES the automatic map for that pair (SM0047, informational) and the other pairs of the
/// triple stay automatic. A repository says nothing about what a member maps from.
/// <para>
/// Two things a <c>CreateMap</c> does not inherit from the automatic map it replaces, so both are written: a write map
/// is declared as the REVERSE of the view map (a DTO is a subset of its entity, so the entity-only members it leaves
/// untouched are the quiet SM0006 rather than one SM0001 each), and flattening is off (<see cref="ConfigureDefaults"/>),
/// as it is on the automatic maps.
/// </para>
/// <para>
/// None of the entities here declares <c>[ShiftEntityKeyAndName]</c>, so every select DTO the select convention builds
/// is Value-only, and the hand-built ones below are Value-only to match.
/// </para>
/// </summary>
public class ClaimableItemsMapper : ShiftMapperBase
{
    public ClaimableItemsMapper()
    {
        // The framework's rules (the ShiftEntitySelectDTO convention, the members it owns such as ID, the
        // string → long foreign-key rule) reach a map through the repository markers only in a project that closes
        // ShiftRepository<,,,>. These maps are ALSO generated into every project that references this assembly, most
        // of which close none, so the class carries the pack itself.
        AddConversions<ShiftEntityConversions>();

        AddCampaignMaps();
        AddCampaignVinEntryMaps();
        AddClaimableItemMaps();
        AddItemClaimMaps();
        AddItemClaimCertificateMaps();
    }

    /// <summary>
    /// The automatic maps these replace do not flatten; the maps written here keep that, so replacing one changes
    /// nothing but the members written.
    /// </summary>
    protected override void ConfigureDefaults(MapOptions options) => options.Flattening = false;

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Campaign
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddCampaignMaps()
    {
        CreateMap<Campaign, CampaignDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // Their source is a plain `List<long>` on the entity - NOT a navigation - so the select convention
            // (a foreign key plus its navigation) does not reach them.
            //
            // Value-only is CORRECT, and is deliberately not a gap to be "fixed" later. These ids have no
            // navigation to read a name from, so there is nothing to put in Text.
            .ForMember(d => d.Brands, opt => opt.MapFrom(e => e.Brands.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList()))
            .ForMember(d => d.Companies, opt => opt.MapFrom(e => e.Companies.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList()))
            .ForMember(d => d.Countries, opt => opt.MapFrom(e => e.Countries.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList()))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            // Straight back to List<long>. `ToLong()` is carried over from the old reverse map rather than
            // replaced with long.Parse, so a malformed value keeps failing the way it always did.
            .ReverseMap()
            .ForMember(e => e.Brands, opt => opt.MapFrom(dto => dto.Brands.Select(s => s.Value.ToLong()).ToList()))
            .ForMember(e => e.Companies, opt => opt.MapFrom(dto => dto.Companies.Select(s => s.Value.ToLong()).ToList()))
            .ForMember(e => e.Countries, opt => opt.MapFrom(dto => dto.Countries.Select(s => s.Value.ToLong()).ToList()));

        // NOTHING is configured for `VehicleInspectionType`: it is a scalar ShiftEntitySelectDTO? over a foreign key,
        // which the select convention reads and writes. Two behaviours ride along with that convention and neither is
        // caused by anything written here:
        //   READ  - a NULL FK yields a NULL member.
        //   WRITE - a blank/null select DTO SETS THE FK TO NULL.
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // CampaignVinEntry
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddCampaignVinEntryMaps()
    {
        // LIST: two flattenings through the Campaign navigation, restated verbatim - including the null guard,
        // which is load-bearing: the list map is part of the SQL projection, and the guard is what the old map
        // shipped and what its behaviour is defined by.
        CreateMap<CampaignVinEntry, CampaignVinEntryListDTO>()
            .ForMember(d => d.CampaignName, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.Name : null))
            .ForMember(d => d.CampaignUniqueReference, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.UniqueReference : null));

        // VIEW: DisableVinValidation has NO entity source - CampaignVinEntry has no such column - and it is
        // [JsonIgnore], so it never crosses the wire in either direction. Its only reader is CampaignVinEntryValidator
        // (`.When(x => !x.DisableVinValidation)`): it exists purely to let an in-process caller construct the DTO
        // with VIN validation switched off. (The write map stays automatic: the entity has no matching target, so
        // there is nothing for it to write.)
        CreateMap<CampaignVinEntry, CampaignVinEntryDTO>()
            .ForMember(d => d.DisableVinValidation, opt => opt.Ignore());
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ClaimableItem
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddClaimableItemMaps()
    {
        // LIST: five flattenings through the Campaign navigation. Flattening is off, and a list member nobody
        // maps comes back null with nothing to notice it, so all five are written out. The Campaign navigation is
        // nullable, so each keeps its guard; the two enums fall back to default(T) for the same reason.
        // `Validity`, `ValidityModeText`, `ActivationTriggerText` and `ActivationTypeText` need nothing - they are
        // computed getters on the DTO, and the two enum members below are what make the last two correct.
        CreateMap<ClaimableItem, ClaimableItemListDTO>()
            .ForMember(d => d.CampaignName, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.Name : null!))
            .ForMember(d => d.CampaignStartDate, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.StartDate : null))
            .ForMember(d => d.CampaignExpireDate, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.ExpireDate : null))
            .ForMember(d => d.CampaignActivationTrigger, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.ActivationTrigger : default))
            .ForMember(d => d.CampaignActivationType, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.ActivationType : default));

        CreateMap<ClaimableItem, ClaimableItemDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // A JSON column on the entity, a typed list on the DTO.
            //
            // THE SERIALIZER OPTIONS ARE THE POINT. Both directions pass a bare `new JsonSerializerOptions { }` -
            // DEFAULT options, deliberately NOT the framework's configured ones. Default options are PascalCase;
            // substituting a camelCase or otherwise-configured instance would rewrite the property names inside the
            // stored column and silently orphan every cost row already in the database.
            //
            // The null/empty behaviour is likewise reproduced rather than improved: a straight Deserialize with no
            // guard, exactly as before.
            .ForMember(d => d.Costs, opt => opt.MapFrom(e => JsonSerializer.Deserialize<List<ClaimableItemCostDTO>>(e.Costs, new JsonSerializerOptions { })!))

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            // Same options instance shape, same reasoning, opposite direction.
            .ReverseMap()
            .ForMember(e => e.Costs, opt => opt.MapFrom(dto => JsonSerializer.Serialize(dto.Costs, new JsonSerializerOptions { })));

        // As on Campaign, `Campaign` is left entirely to the select convention - and a ClaimableItem may
        // legitimately have a null CampaignID, so the two convention behaviours noted there apply here in particular.
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ItemClaim
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddItemClaimMaps()
    {
        CreateMap<ItemClaim, ItemClaimListDTO>()
            // Six flattenings through navigations. Flattening is off, and no diagnostic covers a list member that
            // comes back null, so they are written out.
            //
            // EVERY ONE OF THESE IS GUARDED, INCLUDING THE TWO NON-NULLABLE NAVIGATIONS, and that is not defensive
            // padding - it is required for correctness on a second caller. This projection runs in TWO places. The
            // list endpoint runs it as SQL, where an unguarded `e.Campaign.Name` is fine.
            // ItemClaimCertificateRepository.ViewAsync runs the SAME projection IN MEMORY over an
            // already-materialized list, and that query Includes ClaimableItem but NOT Campaign - so `e.Campaign` is
            // genuinely null there and an unguarded dereference throws NullReferenceException. Adding an Include to
            // "fix" the resulting null CampaignName would be a behaviour change.
            .ForMember(d => d.CampaignName, opt => opt.MapFrom(e => e.Campaign != null ? e.Campaign.Name : null!))
            .ForMember(d => d.ClaimableItemName, opt => opt.MapFrom(e => e.ClaimableItem != null ? e.ClaimableItem.Name : null!))
            .ForMember(d => d.ReimbursementCertificateCertificateDate, opt => opt.MapFrom(e => e.ReimbursementCertificate != null ? e.ReimbursementCertificate.CertificateDate : null))
            .ForMember(d => d.ReimbursementCertificateInvoiceDate, opt => opt.MapFrom(e => e.ReimbursementCertificate != null ? e.ReimbursementCertificate.InvoiceDate : null))
            .ForMember(d => d.ContributionCertificateCertificateDate, opt => opt.MapFrom(e => e.ContributionCertificate != null ? e.ContributionCertificate.CertificateDate : null))
            .ForMember(d => d.ContributionCertificateInvoiceDate, opt => opt.MapFrom(e => e.ContributionCertificate != null ? e.ContributionCertificate.InvoiceDate : null))

            // THE "[]" LITERAL IS THE WHOLE POINT. An empty JSON array counts as NO attachment, so the comparison is
            // against both null AND the two-character string "[]". Drop it and every row whose Attachments column
            // holds an empty array flips from No to Yes. Note this deliberately does NOT read the entity's own
            // `HasAttachment` column, even though one exists and UpsertAsync maintains it: switching sources would be
            // a behaviour change wherever the two ever disagree.
            .ForMember(d => d.HasAttachment, opt => opt.MapFrom(e => e.Attachments == null || e.Attachments == "[]" ? YesNoOptions.No : YesNoOptions.Yes))

            // These two flatten a navigation this module does not own: ItemClaim has no VehicleInspectionResult
            // navigation at all, so there is nothing here to project from. They are filled by a CONSUMER's derived
            // repository overriding MapToList with its own join projection - the seam ItemClaimRepository documents.
            // The ignore does not close that seam: an override replaces the projection wholesale.
            .ForMember(d => d.VehicleInspectionResultVehicleInspectionTypeID, opt => opt.Ignore())
            .ForMember(d => d.VehicleInspectionResultVehicleInspectionTypeName, opt => opt.Ignore());

        CreateMap<ItemClaim, ItemClaimDTO>()
            // ── VIEW ──────────────────────────────────────────────────────────────────────
            // The DTO member is `CampaignVINEntry` (capital VIN); the entity's key is `CampaignVinEntryID`
            // (lowercase 'in'), so the select convention, which looks for `CampaignVINEntryID`, does not pair them.
            // Written out in both directions. Text stays null because CampaignVinEntry has no name-ish column to read.
            .ForMember(d => d.CampaignVINEntry, opt => opt.MapFrom(e => e.CampaignVinEntryID.ToSelectDTO(null)))

            // There is no ReSubmitForDistributorReview on the entity: it is a client-supplied COMMAND flag, read
            // straight off the DTO in ItemClaimRepository.UpsertAsync to push the claim to PendingProcess, and never
            // persisted. Nothing to read on the way out.
            .ForMember(d => d.ReSubmitForDistributorReview, opt => opt.Ignore())

            // ── ENTITY ────────────────────────────────────────────────────────────────────
            // The key, exactly as the previous generated map wrote it (a blank select clears it). The navigation of
            // the same name, ignoring case, is not written from the DTO: it is loaded, never assigned.
            .ReverseMap()
            .ForMember(e => e.CampaignVinEntryID, opt => opt.MapFrom(dto => dto.CampaignVINEntry.ToNullableForeignKey(nameof(ItemClaimDTO.CampaignVINEntry))))
            .ForMember(e => e.CampaignVinEntry, opt => opt.Ignore());
    }

    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Item-claim certificate (the shared ADP.Cases Certificate)
    // ────────────────────────────────────────────────────────────────────────────────────────────────────────
    private void AddItemClaimCertificateMaps()
    {
        CreateMap<Certificate, ItemClaimCertificateDTO>()
            // ReimbursementItemClaims: the REPOSITORY fills it. The shared ADP.Cases Certificate carries no claim
            // collection - there is no navigation to compose from - so ItemClaimCertificateRepository.ViewAsync loads
            // the lines by foreign key and projects them through the ItemClaim list map.
            .ForMember(d => d.ReimbursementItemClaims, opt => opt.Ignore())

            // Notes: the DTO declares it; the Certificate ENTITY has no Notes column at all. There is nothing to read.
            .ForMember(d => d.Notes, opt => opt.Ignore());
    }
}
#pragma warning restore SM0030

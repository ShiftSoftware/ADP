using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// Golden-customer list — served from the <c>[schema].[GoldenCustomer]</c> view in the HOST's
/// own database (no Cosmos round-trip; fresh as of the last resolve run). List-only by design:
/// goldens are minted and survived by the resolve engine, merged/split by stewards — consumers
/// never write them, so this is a plain authorized OData read-through, not ShiftEntity CRUD
/// (there is no ShiftEntity behind the view, and no upsert surface to generate).
///
/// <para>Name, phone, email and national ID are protected with the framework's PII protection. The list and
/// the detail always return them masked. The raw value of one field on one identity comes only
/// from <see cref="RevealPii"/>. There is no ShiftEntity CRUD handler here to do that wiring, so
/// this controller does it by hand, the same way the handler does.</para>
/// </summary>
[Route("[controller]")]
[ApiController]
public class GoldenCustomerController : ControllerBase
{
    /// <summary>Artifact family the golden documents are staged under in <c>ProjectionState</c>.</summary>
    private const string GoldenArtifactType = "golden";

    /// <summary>Artifact family of a golden's vehicle links (key = golden id).</summary>
    private const string VinLinksArtifactType = "vinlinks";

    /// <summary>Largest page the events endpoint serves.</summary>
    private const int MaxEventsPageSize = 500;

    /// <summary>Length of a vehicle identification number.</summary>
    private const int VinLength = 17;

    /// <summary>Sentinel content hash meaning "delete this downstream" — a redirected-away identity.</summary>
    private const string TombstoneHash = "TOMBSTONE";

    /// <summary>
    /// The protected members a caller may reveal, mapped to the staged attribute type that holds
    /// each raw value. Member names match the DTOs, compared without case.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> RevealableAttributes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(GoldenCustomerDetailDTO.FullName)] = "full_name",
            [nameof(GoldenCustomerDetailDTO.Phone)] = "phone",
            [nameof(GoldenCustomerDetailDTO.Email)] = "email",
            [nameof(GoldenCustomerDetailDTO.IDNumber)] = "national_id",
        };

    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;
    private readonly PiiDtoProtector piiProtector;

    public GoldenCustomerController(ShiftDbContext db, IOptions<DarlasticApiOptions> options, PiiDtoProtector piiProtector)
    {
        this.db = db;
        this.options = options.Value;
        this.piiProtector = piiProtector;
    }

    /// <summary>
    /// Golden-customer grid. Name, phone, email and national ID come back masked for every caller.
    ///
    /// <para>Filter them on <c>FullName/Display</c>, <c>Phone/Display</c>, <c>Email/Display</c> or
    /// <c>IDNumber/Display</c>.
    /// The framework's OData guard rewrites those to the raw value, which is why the projection
    /// below fills only <see cref="PiiFieldDTO.Value"/>: the filter still runs in SQL. Callers
    /// without the partial-search permission get whole-value equality; ordering on these members
    /// is rejected for everyone. Ordinary filters and sorting are unchanged.</para>
    ///
    /// <para>An exact phone lookup (an equality, or a contains narrowed to one) passes the input
    /// through the host's <c>IPhoneNumberService</c> before it reaches SQL. Without that service the
    /// lookup is a 400. It only matches when the service produces the form the engine stored, which
    /// is the tenant's canonical phone form, not necessarily the framework's default format.</para>
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ODataDTO<GoldenCustomerListDTO>>> Get(ODataQueryOptions<GoldenCustomerListDTO> oDataQueryOptions)
    {
        if (!CanReadGoldens())
            return Forbid();

        var query = db.Set<GoldenCustomer>().AsNoTracking().Select(x => new GoldenCustomerListDTO
        {
            ID = x.ID.ToString(),
            FullName = new PiiFieldDTO { Value = x.FullName },
            Phone = new PiiFieldDTO { Value = x.Phone },
            City = x.City,
            IDNumber = new PiiFieldDTO { Value = x.IDNumber },
            Email = new PiiFieldDTO { Value = x.Email },
            SourceCount = x.SourceCount,
        });

        // applySoftDeleteFilter off: the view already excludes tombstoned identities, and the
        // DTO's IsDeleted is a base-class constant here, not a real column.
        ODataDTO<GoldenCustomerListDTO> result;
        try
        {
            result = await query.ToOdataDTO(oDataQueryOptions, HttpContext.Request, applySoftDeleteFilter: false);
        }
        catch (ShiftEntityException ex)
        {
            // A rejected query: protected ordering, an unsupported protected filter, an invalid
            // phone lookup, a missing $top. Same status and body as the framework's own list action.
            return StatusCode(ex.HttpStatusCode, new { ex.Message, ex.AdditionalData });
        }

        // The rows were read with raw values so the filter could match them. Mask every row
        // before it leaves the server.
        foreach (var row in result.Value)
            piiProtector.Protect(row);

        return Ok(result);
    }

    /// <summary>
    /// One identity by ID, redirect-chased, with its survived attributes — the read a customer-detail
    /// surface opens a profile with.
    ///
    /// <para>Reads <c>ProjectionState</c> on its clustered PK <c>(ArtifactType, ArtifactKey)</c>
    /// rather than filtering the <c>GoldenCustomer</c> view, which would need
    /// <c>CAST(ArtifactKey AS bigint)</c> — non-SARGable, and OPENJSON over the whole golden slice
    /// to return one row.</para>
    ///
    /// <para>An identity minted interactively since the last resolve has no staged golden at all
    /// (see <see cref="GoldenCustomerDetailDTO.AwaitingResolve"/>). That is a 200 with the flag set,
    /// not a 404 — the identity exists, it simply has no survived attributes yet, and the caller
    /// renders its own record's values meanwhile. 404 means no such identity in the registry.</para>
    ///
    /// <para>Name, phone, email and national ID come back masked for every caller. Use
    /// <see cref="RevealPii"/> for the raw value of one of them.</para>
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerDetailDTO>> Detail(long id)
    {
        if (!CanReadGoldens())
            return Forbid();

        var live = await ChaseRedirectsAsync(id);

        var identity = await db.Set<GoldenIdentity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdentityID == live);

        if (identity is null)
            return NotFound();

        var dto = new GoldenCustomerDetailDTO
        {
            ID = live.ToString(CultureInfo.InvariantCulture),
            RequestedID = id.ToString(CultureInfo.InvariantCulture),
            WasRedirected = live != id,
            Status = identity.Status,
            CreatedRunID = identity.CreatedRunID,
            LastChangedRunID = identity.LastChangedRunID,
        };

        var staged = await LoadStagedGoldenAsync(live);

        // No staged golden — minted interactively, not yet resolved. Tombstoned counts the same for
        // a reader: there is nothing to render, and the redirect chase above already pointed at the
        // survivor if one exists.
        if (staged is null)
        {
            dto.AwaitingResolve = true;
        }
        else
        {
            dto.FullName = new PiiFieldDTO { Value = staged.Attribute("full_name") };
            dto.Phone = new PiiFieldDTO { Value = staged.Attribute("phone") };
            dto.City = staged.Attribute("city");
            dto.IDNumber = new PiiFieldDTO { Value = staged.Attribute("national_id") };
            dto.Email = new PiiFieldDTO { Value = staged.Attribute("email") };
            dto.SourceCount = staged.SourceCount;
        }

        // Mask every protected member before the response leaves the server.
        piiProtector.Protect(dto);
        return Ok(dto);
    }

    /// <summary>
    /// The raw value of one protected field on one identity. <paramref name="field"/> is
    /// <c>FullName</c>, <c>Phone</c>, <c>Email</c> or <c>IDNumber</c>, compared without case. This is the only route
    /// that returns a raw value, and it returns exactly one.
    ///
    /// <para>Needs ordinary golden read access when the module's authorization is on, and always
    /// the host's PII reveal permission (<see cref="PiiOptions.Action"/>). The PII check does not
    /// depend on the module's authorization switch. The value is read the way <see cref="Detail"/>
    /// reads it: redirect-chased, from the staged golden, last wins.</para>
    ///
    /// <para>404 for an unknown field or identity. 200 with a null value when the identity has no
    /// value for that field, or has not been resolved yet. The response is never cached, and
    /// nothing here logs the value.</para>
    /// </summary>
    [HttpPost("{id:long}/pii/{field}/reveal")]
    [Authorize]
    public async Task<ActionResult<PiiRevealDTO>> RevealPii(long id, string field)
    {
        if (!CanReadGoldens())
            return Forbid();

        // The DTO's own declarations decide what can be revealed, never the caller.
        if (!RevealableAttributes.TryGetValue(field, out var attributeType) || !IsRevealable(field))
            return NotFound();

        var typeAuthService = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        var revealAction = HttpContext.RequestServices.GetRequiredService<IOptions<PiiOptions>>().Value.Action;
        if (!typeAuthService.CanAccess(revealAction))
            return StatusCode(StatusCodes.Status403Forbidden);

        var live = await ChaseRedirectsAsync(id);

        var exists = await db.Set<GoldenIdentity>().AsNoTracking()
            .AnyAsync(x => x.IdentityID == live);

        if (!exists)
            return NotFound();

        var staged = await LoadStagedGoldenAsync(live);

        Response.Headers.CacheControl = "no-store";
        return Ok(new PiiRevealDTO { Value = staged?.Attribute(attributeType) });
    }

    /// <summary>
    /// One page of the identity's timeline events — every <see cref="GoldenEventType"/> the tenant's
    /// feeds supply (workshop visits, sales, inspections, recalls, bookings, test drives, quotations,
    /// app registrations, …) — newest first, read from the <c>events</c> artifact the last resolve
    /// staged (one clustered-PK seek). Same authorization as <see cref="Detail"/>, and
    /// redirect-chased the same way, so a merged-away ID shows its survivor's timeline.
    ///
    /// <para>Order: date descending with undated events last, then type, then event id. Filter with
    /// one or more <c>type</c> query values (e.g. <c>?type=ServiceVisit&amp;type=Inspection</c>);
    /// <see cref="GoldenCustomerEventsDTO.CountsByType"/> always covers every type. Page with
    /// <c>skip</c> (default 0) and <c>top</c> (default 50, at most 500).</para>
    ///
    /// <para>404 when the identity does not exist. An identity with no staged events — none attached,
    /// or a tenant that stages no events — is a 200 with an empty list.</para>
    /// </summary>
    [HttpGet("{id:long}/events")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerEventsDTO>> Events(long id,
        [FromQuery] GoldenEventType[]? type = null, [FromQuery] int skip = 0, [FromQuery] int top = 50)
    {
        if (!CanReadGoldens())
            return Forbid();

        if (skip < 0 || top < 1 || top > MaxEventsPageSize)
            return BadRequest(new { Message = $"skip must be 0 or more and top between 1 and {MaxEventsPageSize}." });

        var live = await ChaseRedirectsAsync(id);

        if (!await db.Set<GoldenIdentity>().AsNoTracking().AnyAsync(x => x.IdentityID == live))
            return NotFound();

        var payload = await LoadStagedPayloadAsync(GoldenEventsPayload.ArtifactType, live);
        var all = payload is null ? new List<GoldenEventsPayload.StagedEvent>() : GoldenEventsPayload.Parse(payload).Events;

        // Ordered here rather than trusting staged order: the contract belongs to this endpoint.
        all.Sort((a, b) => GoldenEventsPayload.Compare(a.Date, a.Type, a.Id, b.Date, b.Type, b.Id));

        var filter = type is { Length: > 0 } ? new HashSet<GoldenEventType>(type) : null;
        var matching = filter is null ? all : all.Where(e => filter.Contains(e.Type)).ToList();

        return Ok(new GoldenCustomerEventsDTO
        {
            ID = live.ToString(CultureInfo.InvariantCulture),
            RequestedID = id.ToString(CultureInfo.InvariantCulture),
            WasRedirected = live != id,
            Total = matching.Count,
            Skip = skip,
            Top = top,
            CountsByType = all.GroupBy(e => e.Type).OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            Events = matching.Skip(skip).Take(top).Select(e => new GoldenCustomerEventDTO
            {
                Type = e.Type,
                EventID = e.Id,
                Date = e.Date,
                VIN = e.Vin,
                Link = e.Link,
                SourceSystem = e.SourceSystem,
                SourceRecordId = e.SourceRecordId,
                DealerID = e.DealerId,
                DealerName = e.DealerName,
                Title = e.Title,
                Facts = new Dictionary<string, string>(e.Facts, StringComparer.Ordinal),
            }).ToList(),
        });
    }

    /// <summary>
    /// The identity's vehicles, from the <c>vinlinks</c> artifact the last resolve staged — keyed by
    /// golden ID, never by phone, so a shared phone cannot put someone else's car on the card. Each
    /// link carries the ownership period the engine computed (owner or service contact). Same
    /// authorization and redirect-chasing as <see cref="Detail"/>; the survived name the payload also
    /// carries is deliberately not returned.
    ///
    /// <para>Order: owners first, then service contacts; within each, latest activity first; then
    /// VIN. 404 when the identity does not exist; 200 with an empty list when it has no vehicles.</para>
    /// </summary>
    [HttpGet("{id:long}/vehicles")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerVehiclesDTO>> Vehicles(long id)
    {
        if (!CanReadGoldens())
            return Forbid();

        var live = await ChaseRedirectsAsync(id);

        if (!await db.Set<GoldenIdentity>().AsNoTracking().AnyAsync(x => x.IdentityID == live))
            return NotFound();

        var payload = await LoadStagedPayloadAsync(VinLinksArtifactType, live);
        var vehicles = payload is null ? new List<GoldenCustomerVehicleDTO>() : ParseVinLinks(payload);

        static DateOnly? Latest(GoldenCustomerVehicleDTO v) =>
            new[] { v.LastSale, v.LastService, v.EffectiveFrom }.Where(d => d is not null).Max();

        return Ok(new GoldenCustomerVehiclesDTO
        {
            ID = live.ToString(CultureInfo.InvariantCulture),
            RequestedID = id.ToString(CultureInfo.InvariantCulture),
            WasRedirected = live != id,
            Vehicles = vehicles
                .OrderBy(v => v.Role == "owner" ? 0 : 1)
                .ThenByDescending(v => Latest(v) ?? DateOnly.MinValue)
                .ThenBy(v => v.VIN, StringComparer.Ordinal)
                .ToList(),
        });
    }

    /// <summary>
    /// The golden customers linked to one vehicle, from the <c>vinowner</c> artifact the last resolve
    /// staged for the VIN (one clustered-PK seek): each with its role (<c>owner</c> or
    /// <c>service-contact</c>) and the period the engine computed, in the engine's order (owners in
    /// timeline order, then service contacts). Each link is redirect-chased to the surviving identity,
    /// so a golden merged away since the resolve answers as its survivor. Same authorization as the
    /// list and <see cref="Detail"/>.
    ///
    /// <para>Name and phone come back masked for every caller, read and protected exactly as
    /// <see cref="Detail"/> does; <see cref="RevealPii"/> with a link's ID is the only route to a raw
    /// value. The VIN is trimmed and upper-cased first: 400 when it is not 17 letters or digits, 404
    /// when the registry has no link to it.</para>
    /// </summary>
    [HttpGet("by-vin/{vin}")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerByVinDTO>> ByVin(string vin)
    {
        if (!CanReadGoldens())
            return Forbid();

        var trimmed = vin?.Trim() ?? "";
        if (trimmed.Length != VinLength || !trimmed.All(char.IsAsciiLetterOrDigit))
            return BadRequest(new { Message = $"A VIN is {VinLength} letters or digits." });

        // The engine keys every VIN artifact on this canonical form; a VIN it rejects as implausible
        // can never have been linked.
        if (Norm.Vin(trimmed) is not { } key)
            return NotFound();

        var payload = await LoadStagedPayloadAsync(VinOwnerPayload.ArtifactType, key);
        if (payload is null)
            return NotFound();

        var dto = new GoldenCustomerByVinDTO { VIN = key };
        var live = new Dictionary<long, long>();
        var goldens = new Dictionary<long, StagedGolden?>();
        foreach (var link in VinOwnerPayload.Parse(payload).Links)
        {
            if (!live.TryGetValue(link.GoldenId, out var liveId))
                live[link.GoldenId] = liveId = await ChaseRedirectsAsync(link.GoldenId);
            if (!goldens.TryGetValue(liveId, out var staged))
                goldens[liveId] = staged = await LoadStagedGoldenAsync(liveId);

            dto.Goldens.Add(new GoldenCustomerVinLinkDTO
            {
                ID = liveId.ToString(CultureInfo.InvariantCulture),
                LinkedID = link.GoldenId.ToString(CultureInfo.InvariantCulture),
                WasRedirected = liveId != link.GoldenId,
                Role = link.Role,
                EffectiveFrom = link.From,
                EffectiveTo = link.To,
                SaleCount = link.Sales,
                ServiceCount = link.Services,
                FirstSale = link.FirstSale,
                LastSale = link.LastSale,
                FirstService = link.FirstService,
                LastService = link.LastService,
                Sources = link.Sources.ToList(),
                AwaitingResolve = staged is null,
                FullName = new PiiFieldDTO { Value = staged?.Attribute("full_name") },
                Phone = new PiiFieldDTO { Value = staged?.Attribute("phone") },
            });
        }

        // Mask every protected member, in every link, before the response leaves the server.
        piiProtector.Protect(dto);
        return Ok(dto);
    }

    /// <summary>
    /// The staged vehicle-link payload — <c>{"goldenId":N,"links":[{"vin","role","from","to","sales",
    /// "services","firstSale","lastSale","firstService","lastService","sources":[...]}]}</c>, dates as
    /// <c>yyyy-MM-dd</c> or empty — mapped to the DTO. Authoritative producer: the engine's
    /// <c>Registry.AppendLink</c>.
    /// </summary>
    private static List<GoldenCustomerVehicleDTO> ParseVinLinks(string payload)
    {
        static DateOnly? Date(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(v.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d : null;
        static int Int(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

        var list = new List<GoldenCustomerVehicleDTO>();
        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var l in links.EnumerateArray())
        {
            if (!l.TryGetProperty("vin", out var vin) || vin.GetString() is not { Length: > 0 } vinValue) continue;
            list.Add(new GoldenCustomerVehicleDTO
            {
                VIN = vinValue,
                Role = l.TryGetProperty("role", out var role) ? role.GetString() ?? "" : "",
                EffectiveFrom = Date(l, "from"),
                EffectiveTo = Date(l, "to"),
                SaleCount = Int(l, "sales"),
                ServiceCount = Int(l, "services"),
                FirstSale = Date(l, "firstSale"),
                LastSale = Date(l, "lastSale"),
                FirstService = Date(l, "firstService"),
                LastService = Date(l, "lastService"),
                Sources = l.TryGetProperty("sources", out var sources) && sources.ValueKind == JsonValueKind.Array
                    ? sources.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : new(),
            });
        }
        return list;
    }

    /// <summary>Ordinary golden read access. Always true when the module's authorization is off.</summary>
    private bool CanReadGoldens()
    {
        if (!options.EnableDarlasticActionTreeAuthorization)
            return true;

        var typeAuthService = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return typeAuthService.Can(options.Actions.ResolvedGoldenCustomers, Access.Read);
    }

    /// <summary>True when the detail DTO declares the member as a revealable protected field.</summary>
    private static bool IsRevealable(string member)
    {
        var property = typeof(GoldenCustomerDetailDTO).GetProperty(member,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        return property?.PropertyType == typeof(PiiFieldDTO)
            && PiiFieldProtection.FindDeclaration(property)?.Revealable == true;
    }

    /// <summary>
    /// Follows merge redirects to the live identity. A merge never deletes, so a caller holding a
    /// merged-away ID must still land on the survivor. Bounded — a corrupt cycle must not spin a
    /// request forever.
    /// </summary>
    private async Task<long> ChaseRedirectsAsync(long id)
    {
        long live = id;
        for (int hop = 0; hop < 64; hop++)
        {
            var next = await db.Set<IdentityRedirect>().AsNoTracking()
                .Where(x => x.OldIdentityID == live)
                .Select(x => (long?)x.NewIdentityID)
                .FirstOrDefaultAsync();
            if (next is null) break;
            live = next.Value;
        }
        return live;
    }

    /// <summary>
    /// The staged golden of a live identity, or null when there is nothing to render: minted
    /// interactively and not resolved yet, or tombstoned.
    /// </summary>
    private async Task<StagedGolden?> LoadStagedGoldenAsync(long live)
    {
        var payload = await LoadStagedPayloadAsync(GoldenArtifactType, live);
        return payload is null ? null : StagedGolden.Parse(payload);
    }

    /// <summary>
    /// The staged payload of one per-golden artifact (a clustered-PK seek on
    /// <c>(ArtifactType, ArtifactKey)</c>), or null when there is none or it is tombstoned.
    /// </summary>
    private Task<string?> LoadStagedPayloadAsync(string artifactType, long live) =>
        LoadStagedPayloadAsync(artifactType, live.ToString(CultureInfo.InvariantCulture));

    /// <summary>The staged payload of one artifact by its key, or null when there is none or it is
    /// tombstoned.</summary>
    private async Task<string?> LoadStagedPayloadAsync(string artifactType, string key)
    {
        var staged = await db.Set<ProjectionState>().AsNoTracking()
            .Where(x => x.ArtifactType == artifactType && x.ArtifactKey == key)
            .Select(x => new { x.ContentHash, x.Payload })
            .FirstOrDefaultAsync();

        // ContentHash is VARCHAR(64) and may come back space-padded on some collations; compare trimmed.
        if (staged?.Payload is null || staged.ContentHash.TrimEnd() == TombstoneHash)
            return null;

        return staged.Payload;
    }

    /// <summary>
    /// An unpacked staged golden payload — <c>{"goldenId":N,"attrs":[{"t","v"}...],"members":[...]}</c>
    /// — using LAST-WINS per attribute type, which is what the Cosmos drain does. The engine stages
    /// attrs sorted by (type, value), so last-wins equals the view's <c>MAX(v)</c>: this endpoint,
    /// the view and the drained documents cannot disagree about which value survived.
    /// </summary>
    private sealed class StagedGolden
    {
        private readonly Dictionary<string, string?> attributes = new(StringComparer.Ordinal);

        /// <summary>How many source records the identity unifies. 0 when the payload has no members.</summary>
        public int SourceCount { get; private set; }

        /// <summary>The surviving value of one attribute type, or null when the golden has none.</summary>
        public string? Attribute(string type) => attributes.GetValueOrDefault(type);

        public static StagedGolden Parse(string payload)
        {
            var golden = new StagedGolden();
            using var doc = JsonDocument.Parse(payload);

            if (doc.RootElement.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
            {
                foreach (var attr in attrs.EnumerateArray())
                {
                    if (!attr.TryGetProperty("t", out var t) || !attr.TryGetProperty("v", out var v)) continue;
                    if (t.GetString() is { } type)
                        golden.attributes[type] = v.GetString();
                }
            }

            if (doc.RootElement.TryGetProperty("members", out var members) && members.ValueKind == JsonValueKind.Array)
                golden.SourceCount = members.GetArrayLength();

            return golden;
        }
    }

    /// <summary>
    /// One identity's provenance: the source records it unifies, its lifecycle, and the identities
    /// merged into it. Every read is a key seek — the identity by PK, its profiles on
    /// <c>IX_SourceProfile_Identity</c> — so this is cheap enough to open per row.
    ///
    /// Note what this is NOT: the engine's *reasoning*. Survivorship reasons and pair scores are
    /// computed during a resolve and never staged, so no read can surface them. See
    /// <see cref="GoldenCustomerSourcesDTO"/>.
    /// </summary>
    [HttpGet("{id:long}/sources")]
    [Authorize]
    public async Task<ActionResult<GoldenCustomerSourcesDTO>> Sources(long id)
    {
        if (!CanReadGoldens())
            return Forbid();

        var identity = await db.Set<GoldenIdentity>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdentityID == id);

        if (identity is null)
            return NotFound();

        var sources = await db.Set<SourceProfile>().AsNoTracking()
            .Where(x => x.IdentityID == id)
            .OrderBy(x => x.SourceSystem).ThenBy(x => x.SourceRecordId)
            .Select(x => new GoldenCustomerSourceDTO
            {
                SourceSystem = x.SourceSystem,
                SourceRecordId = x.SourceRecordId,
                Removed = x.Removed,
                FirstRunID = x.FirstRunID,
                LastChangedRunID = x.LastChangedRunID,
            })
            .ToListAsync();

        // Which identities were absorbed into this one. IdentityRedirect is keyed on OldIdentityID,
        // so this direction is a scan — fine while merges are rare, but if a tenant's redirect table
        // grows into the millions this wants its own index on NewIdentityID.
        var absorbed = await db.Set<IdentityRedirect>().AsNoTracking()
            .Where(x => x.NewIdentityID == id)
            .OrderBy(x => x.OldIdentityID)
            .Select(x => x.OldIdentityID)
            .ToListAsync();

        return Ok(new GoldenCustomerSourcesDTO
        {
            ID = id.ToString(CultureInfo.InvariantCulture),
            Status = identity.Status,
            CreatedRunID = identity.CreatedRunID,
            LastChangedRunID = identity.LastChangedRunID,
            Sources = sources,
            AbsorbedIdentityIDs = absorbed.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList(),
        });
    }
}

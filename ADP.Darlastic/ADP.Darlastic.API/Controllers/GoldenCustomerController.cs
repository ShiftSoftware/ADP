using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
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
        var key = live.ToString(CultureInfo.InvariantCulture);
        var staged = await db.Set<ProjectionState>().AsNoTracking()
            .Where(x => x.ArtifactType == GoldenArtifactType && x.ArtifactKey == key)
            .Select(x => new { x.ContentHash, x.Payload })
            .FirstOrDefaultAsync();

        if (staged?.Payload is null || staged.ContentHash == TombstoneHash)
            return null;

        return StagedGolden.Parse(staged.Payload);
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

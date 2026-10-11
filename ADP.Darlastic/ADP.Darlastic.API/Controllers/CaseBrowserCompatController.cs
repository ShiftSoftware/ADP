using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ShiftSoftware.ADP.Darlastic.API.Extensions;
using ShiftSoftware.ADP.Darlastic.Data.Entities;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ADP.Darlastic.Shared.ActionTrees;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The standalone case browser's HTTP contract, served from the registry.
///
/// <para><b>Why a second surface.</b> <see cref="CaseBrowserController"/> is the API the module wants
/// to have: DTOs, PascalCase, PairKey identity, paging by <c>skip</c>/<c>take</c>. The case browser
/// page (<c>web/cases.html</c>, 868 lines of working UI) speaks a different contract that grew
/// alongside an in-memory index: camelCase, <c>{total, page, size, items}</c> envelopes, records
/// identified by integer index. Mapping between them had to live somewhere. Putting it here rather
/// than in the page's JavaScript keeps it typed, keeps one page working against both hosts, and —
/// the deciding reason — makes it verifiable: every response below can be diffed field-for-field
/// against the same route on the running standalone server, which is not something a browserless
/// agent can do for rendered DOM.</para>
///
/// <para><b>Property names are pinned camelCase deliberately.</b> The host decides JSON casing —
/// ShiftEntity's <c>AddShiftEntityWeb</c> leaves <c>PropertyNamingPolicy</c> unset (PascalCase
/// passes through) while the standalone server's minimal APIs use web defaults (camelCase). The page
/// reads camelCase (<c>audit.newLabel</c>, <c>registry.goldenId</c>). Anonymous-object members are
/// therefore spelled in the exact wire form: already-camelCase names survive both policies
/// unchanged, so this is correct in any host rather than correct in the one it was written against.
/// Never write <c>new { last.NewLabel }</c> here — that shorthand takes the CLR name and breaks
/// under one of the two policies.</para>
///
/// <para><b>Re-scores under the resolve's configuration.</b> A pair's score and trace, a record's
/// comparison keys and a customer's name survivorship are recomputed from the staged records under the
/// match configuration the latest resolve recorded (<see cref="RecordedMatchSettingsAttribute"/>), so
/// they reach the decisions the resolve staged.</para>
///
/// <para><b>"In view" means "staged" here.</b> The page's header separates registry totals (the
/// whole tenant corpus) from what the process loaded and scored. A hosted reader scores nothing, so
/// the in-view block reports the staged set instead. The registry block is unaffected and remains
/// the honest corpus number — which is the distinction that whole header exists to make.</para>
///
/// <para><b>Personal information follows the golden contract.</b> Names, phones, ID numbers,
/// birth dates and addresses leave this controller masked with the framework's masker unless the
/// caller holds the host's PII reveal action (<see cref="PiiOptions.Action"/>) — for a token holder,
/// as measured when the token was minted. Searching by part of a name or phone needs the partial
/// search action; without it a search matches whole values only. The engine's explanation text
/// quotes the records it compares; the engine writes each quoted value through the same masker,
/// so a masked caller reads the reasoning with the values masked as they are in the records.</para>
/// </summary>
/// <remarks>
/// <para><b>On <see cref="AllowAnonymousAttribute"/>.</b> ShiftIdentity installs a global
/// <c>AuthorizeFilter</c>, so without this every route here 401s — including for the page, which
/// cannot send a bearer token. Anonymous at the MVC layer does not mean unprotected: every action
/// calls <see cref="Denied"/> first, which requires either a signed, expiring token scoped to this
/// surface or a normally authenticated caller who passes the action tree. The check moved from the
/// framework into the code; it did not go away.</para>
/// </remarks>
[Route("[controller]")]
[ApiController]
[AllowAnonymous]
[RecordedMatchSettings]
public class CaseBrowserCompatController : ControllerBase
{
    private readonly ShiftDbContext db;
    private readonly DarlasticApiOptions options;
    private readonly IPiiMasker masker;
    private readonly PiiOptions piiOptions;
    private readonly RegistryRunCache runCache;

    private static readonly JsonSerializerOptions PayloadJson = new() { PropertyNameCaseInsensitive = true };

    public CaseBrowserCompatController(ShiftDbContext db, IOptions<DarlasticApiOptions> options,
        IPiiMasker masker, IOptions<PiiOptions> piiOptions, RegistryRunCache runCache)
    {
        this.db = db;
        this.options = options.Value;
        this.masker = masker;
        this.piiOptions = piiOptions.Value;
        this.runCache = runCache;
    }

    /// <summary>
    /// Two ways in, and a caller needs exactly one of them.
    ///
    /// <para>A valid case browser token: signed, expiring, and scoped by descriptor both to this
    /// surface and to what its holder may do. It was minted by an authenticated caller, so possession
    /// of it stands in for that session for as long as it lasts. The action tree is not consulted
    /// again — it was already consulted at mint time, by a request that had a real session to ask
    /// about, and the answer is what chose the descriptor. Re-deriving it here would mean guessing at
    /// the permissions of a principal who is not present.</para>
    ///
    /// <para>Which is why the token has to carry the write bit rather than simply proving a session
    /// existed: the read and write surfaces are one action at two levels
    /// (<c>StewardQueue</c> Read to see cases, Write to record verdicts), and a token that skipped
    /// straight past that distinction would hand a read-only steward the flag and audit buttons.</para>
    ///
    /// <para>Otherwise the ordinary path: an authenticated principal, filtered by the action tree.
    /// An anonymous caller with no token has neither and is refused.</para>
    /// </summary>
    private bool Denied(bool write)
    {
        if (TokenGrant is not null) return write && !TokenGrant.CanWrite;

        if (User?.Identity?.IsAuthenticated != true) return true;

        if (!options.EnableDarlasticActionTreeAuthorization) return false;
        var typeAuth = HttpContext.RequestServices.GetRequiredService<ITypeAuthService>();
        return !typeAuth.Can(options.Actions.ResolvedStewardQueue, write ? Access.Write : Access.Read);
    }

    /// <summary>What a valid token on this request grants, or null. Read once per request.</summary>
    private CaseBrowserGrant? tokenGrant;
    private bool tokenGrantRead;
    private CaseBrowserGrant? TokenGrant
    {
        get
        {
            if (!tokenGrantRead)
            {
                tokenGrant = CaseBrowserSas.GrantOf(Request, options);
                tokenGrantRead = true;
            }
            return tokenGrant;
        }
    }

    /// <summary>
    /// Who to record on a flag or an audit. The token's actor is signed, so it is preferred over
    /// anything else the request claims about itself; a bearer-authenticated caller falls back to
    /// its own claims. Never read from an unsigned query parameter — that would let anyone author a
    /// review note as anyone.
    /// </summary>
    private string Actor() =>
        TokenGrant?.Actor
        ?? User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.Identity?.Name
        ?? "unknown";

    // ------------------------------------------------------------ personal information

    private (bool Reveal, bool PartialSearch)? piiGrant;

    /// <summary>
    /// Whether this caller sees names and phones unmasked, and may search them by partial value.
    /// A token carries both as measured at mint time; a bearer caller is asked now. Fails closed:
    /// a host without TypeAuth cannot show anyone holds the action, so everything stays masked.
    /// </summary>
    private (bool Reveal, bool PartialSearch) Pii
    {
        get
        {
            if (piiGrant is { } known) return known;
            if (TokenGrant is { } t) return (piiGrant = (t.CanRevealPii, t.CanSearchPii)).Value;
            var typeAuth = HttpContext?.RequestServices.GetService<ITypeAuthService>();
            bool reveal = typeAuth?.CanAccess(piiOptions.Action) == true;
            bool partial = typeAuth?.CanAccess(piiOptions.PartialSearchAction) == true;
            return (piiGrant = (reveal, partial)).Value;
        }
    }

    private RecordPiiView? piiView;

    /// <summary>The shared record PII rules, fed with this caller's grant (the token's, when there is one).</summary>
    private RecordPiiView PiiView => piiView ??= new RecordPiiView(masker, piiOptions, Pii.Reveal, Pii.PartialSearch,
        CityDisclosureAttribute.ShownCities(HttpContext), RecordPiiView.BlockKeyLabels(options));

    /// <summary>The value as this caller may see it. Empty stays empty, so "no name" still reads as such.</summary>
    private string? Shown(PiiKind kind, string? raw) => PiiView.Show(kind, raw);

    /// <summary>
    /// Does a record answer this search? Its key always may (not personal). Names and phones match
    /// by fragment only for callers holding the partial-search action; everyone else matches whole
    /// values — the same rule the golden list's OData guard applies.
    /// </summary>
    private bool Matches(RealRecord r, string needle)
    {
        if ((r.SourceRecordId?.Equals(needle, StringComparison.OrdinalIgnoreCase) ?? false)
            || $"{r.SourceSystem}:{r.SourceRecordId}".Equals(needle, StringComparison.OrdinalIgnoreCase))
            return true;

        if (Pii.PartialSearch)
            return (r.NormName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.RawName?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.Phones?.Any(p => p.Contains(needle, StringComparison.Ordinal)) ?? false)
                || (r.WeakPhones?.Any(p => p.Contains(needle, StringComparison.Ordinal)) ?? false);

        string digits = Digits(needle);
        return (r.NormName?.Equals(needle, StringComparison.OrdinalIgnoreCase) ?? false)
            || (r.RawName?.Equals(needle, StringComparison.OrdinalIgnoreCase) ?? false)
            || (digits.Length > 0 && ((r.Phones?.Contains(digits) ?? false) || (r.WeakPhones?.Contains(digits) ?? false)));
    }

    private static string Digits(string s) => new([.. s.Where(char.IsDigit)]);

    // ---------------------------------------------------------------- summary

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var counts = await db.Set<CaseCategoryCount>().AsNoTracking().ToListAsync(ct);
        var run = await db.Set<ResolveRun>().AsNoTracking()
            .OrderByDescending(r => r.RunID).FirstOrDefaultAsync(ct);

        var profiles = await db.Set<SourceProfile>().AsNoTracking().CountAsync(ct);
        var identities = await db.Set<GoldenIdentity>().AsNoTracking()
            .CountAsync(i => i.Status == IdentityStatus.Active, ct);

        // The catalog is capped per category by the resolve, so this is thousands of rows, not
        // millions — cheap to hold and the only way to report per-category browsable counts.
        var catalog = await db.Set<CaseCatalogEntry>().AsNoTracking()
            .Select(c => new { c.Categories }).ToListAsync(ct);

        var stagedRecords = await db.Set<StewardRecord>().AsNoTracking().CountAsync(ct);
        var queueDepth = await db.Set<StewardQueueEntry>().AsNoTracking().CountAsync(ct);

        var sources = await db.Set<StewardRecord>().AsNoTracking()
            .GroupBy(r => r.SourceSystem)
            .Select(g => new { Key = g.Key, N = g.Count() })
            .ToListAsync(ct);

        // Cluster categories are staged per identity as a bitmask. Counting them corpus-wide is a
        // bitmask test per row over 10^5 rows, so it runs in SQL rather than in memory. The
        // standalone reports its in-memory cluster counts here; this reports the corpus, which is
        // strictly better information under the same key.
        // Multi-record identities only: a run with per-golden evidence summarises single-record
        // identities too, and the identity view is about how several records came together.
        var clusterCats = new Dictionary<string, long>();
        foreach (var cc in Enum.GetValues<ClusterCat>())
        {
            if (cc == ClusterCat.None) continue;
            long mask = (long)cc;
            clusterCats[cc.ToString()] = await db.Set<IdentitySummary>().AsNoTracking()
                .CountAsync(s => s.MemberCount > 1 && (s.Categories & mask) != 0, ct);
        }

        bool reasonsStaged = await ReasonsStagedAsync(ct);

        var inView = new Dictionary<string, int>();
        var corpus = new Dictionary<string, object>();
        foreach (var cat in Enum.GetValues<CaseCat>())
        {
            if (cat == CaseCat.None) continue;
            string name = cat.ToString();
            long mask = (long)cat;
            inView[name] = catalog.Count(c => (c.Categories & mask) != 0);
            var row = counts.FirstOrDefault(c => c.Category == name);
            // Always emit a row, at zero if need be: a missing key reads to the sidebar as "not
            // measured" and silently falls back to the in-view count.
            corpus[name] = new { total = row?.Total ?? 0L, sampled = row?.Sampled ?? inView[name] };
        }

        return new JsonResult(new
        {
            records = stagedRecords,
            totalPairs = catalog.Count + queueDepth,
            identities = await db.Set<IdentitySummary>().AsNoTracking().CountAsync(s => s.MemberCount > 1, ct),
            multiRecordIdentities = await db.Set<IdentitySummary>().AsNoTracking()
                .CountAsync(s => s.MemberCount > 1, ct),
            // Golden customers: the view's own count (one row per golden), and the identities a
            // CRM created since the last resolve, which have no golden yet and are never a row.
            goldenTotal = await db.Set<GoldenCustomer>().AsNoTracking().CountAsync(ct),
            awaitingResolve = await AwaitingResolveAsync(ct),
            // What this host can serve and this caller can see. `goldens` is on only when the last
            // resolve staged per-golden evidence; without it the page keeps its pair view as the
            // default, so a tenant that never asked for golden rows sees no change.
            features = new
            {
                goldens = reasonsStaged,
                export = false,
                reveal = Pii.Reveal,
                partialSearch = Pii.PartialSearch,
            },
            // The one definition of every plain-language name the page shows; it has none of its own.
            labels = CaseLabels.Wire(),
            sourceLabels = options.SourceLabels,
            indexedCases = catalog.Count,
            registryProfiles = profiles,
            registryIdentities = identities,
            registryRun = run?.RunID,
            corpusCategories = corpus,
            categories = inView,
            clusterCategories = clusterCats,
            sources = sources.ToDictionary(s => s.Key, s => s.N),
            audits = await db.Set<LabelAudit>().AsNoTracking().CountAsync(ct),
            // The gold set is a file in the shared corpus, not registry state. Reported as zero
            // rather than omitted so the page renders a number instead of "undefined".
            goldPairs = 0,
            flags = await db.Set<ReviewFlag>().AsNoTracking().CountAsync(ct),
        });
    }

    // ------------------------------------------------------------------ cases

    [HttpGet("cases")]
    public async Task<IActionResult> Cases(string? cat, string? flag, string? dealer, string? dob,
        string? q, double? minScore, double? maxScore, string? sort, int page = 0, int size = 50,
        CancellationToken ct = default)
    {
        if (Denied(false)) return Forbid();

        var rows = await db.Set<CaseCatalogEntry>().AsNoTracking().ToListAsync(ct);

        long catMask = MaskOf<CaseCat>(cat);
        if (catMask != 0) rows = rows.Where(r => (r.Categories & catMask) != 0).ToList();

        long flagMask = MaskOf<MatchFlags>(flag);
        if (flagMask != 0) rows = rows.Where(r => (r.Flags & flagMask) != 0).ToList();

        if (!string.IsNullOrWhiteSpace(dealer))
            rows = rows.Where(r => r.SourceSystemA == dealer || r.SourceSystemB == dealer).ToList();

        if (minScore is { } lo) rows = rows.Where(r => r.Score >= lo).ToList();
        if (maxScore is { } hi) rows = rows.Where(r => r.Score <= hi).ToList();

        // Load the sides for every surviving row: q and dob filter on record content, and the page
        // renders both sides of every row anyway, so this is the same read either way.
        var sides = await LoadSides(rows.SelectMany(r =>
            new[] { (r.SourceSystemA, r.SourceRecordIdA), (r.SourceSystemB, r.SourceRecordIdB) }), ct);

        if (!string.IsNullOrWhiteSpace(q))
        {
            string needle = q.Trim();
            rows = rows.Where(r =>
                Matches(sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA)), needle) ||
                Matches(sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB)), needle)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(dob))
            rows = rows.Where(r => DobMatches(dob, r,
                sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA)).rec,
                sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB)).rec)).ToList();

        rows = sort switch
        {
            "gap" => rows.OrderByDescending(r => GapOf(r, sides) ?? -1).ToList(),
            "score-asc" => rows.OrderBy(r => r.Score).ToList(),
            _ => rows.OrderByDescending(r => r.Score).ToList(),
        };

        int total = rows.Count;
        var window = rows.Skip(page * size).Take(size).ToList();

        var queued = await QueuedKeys(window.Select(r => r.PairKey), ct);
        var audits = await AuditsByKey(window.Select(r => r.PairKey), ct);

        var items = window.Select(r => new
        {
            a = RecordJson(sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA))),
            b = RecordJson(sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB))),
            score = Math.Round(r.Score, 3),
            flags = NamesOf<MatchFlags>(r.Flags),
            cats = NamesOf<CaseCat>(r.Categories),
            key = r.PairKey,
            dobGap = GapOf(r, sides),
            // The gold set lives in the shared corpus, not the registry.
            gold = (object?)null,
            audit = audits.GetValueOrDefault(r.PairKey) is { } list && list.Count > 0
                ? Last(list)
                : null,
            queued = queued.Contains(r.PairKey),
        }).ToList();

        return new JsonResult(new { total, page, size, items });
    }

    [HttpGet("case")]
    public async Task<IActionResult> Case(int a, int b, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        // The page identifies records by the integer index the resolve assigned them, which is
        // staged on each record's payload and unique across the staged set. That is what lets one
        // page serve both hosts without changing how it keys the DOM.
        var ra = await RecordByIdx(a, ct);
        var rb = await RecordByIdx(b, ct);
        if (ra is null || rb is null) return BadRequest(new { error = "bad record idx" });

        return new JsonResult(await DetailJson(ra.Value.rec, rb.Value.rec, ra.Value.key, rb.Value.key, ct));
    }

    // ------------------------------------------------------------ golden rows

    /// <summary>
    /// One row per golden customer — exactly the rows of the <c>GoldenCustomer</c> view, so the
    /// unfiltered total is that view's <c>COUNT(*)</c> — with the plain-language reasons the last
    /// resolve staged for each (<see cref="GoldenReason"/>).
    ///
    /// <para><paramref name="reason"/> filters by one reason name; <c>reasonCounts</c> is computed
    /// with the same bit test over every golden, so a chip's count and the rows its filter returns
    /// cannot disagree. <paramref name="q"/> matches a golden ID, or a name or phone (by fragment for
    /// callers holding the partial-search action, by whole value otherwise). Identities a CRM created
    /// since the last resolve have no golden yet: they are counted in <c>awaitingResolve</c> and are
    /// never a row.</para>
    ///
    /// <para>A golden with no staged summary (its records' source was not in the last run, or the
    /// run staged no per-golden evidence) still gets a row; its reasons are what can be said
    /// without one — see <see cref="GoldenReasons.ForReader"/>, which this query mirrors in SQL.</para>
    /// </summary>
    [HttpGet("goldens")]
    public async Task<IActionResult> Goldens(string? reason, string? q, string? sort, int page = 0, int size = 50,
        CancellationToken ct = default)
    {
        if (Denied(false)) return Forbid();
        size = Math.Clamp(size, 1, 200);
        page = Math.Max(0, page);

        long filterMask = 0;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            if (!Enum.TryParse<GoldenReason>(reason, ignoreCase: true, out var parsed) || parsed == GoldenReason.None)
                return BadRequest(new { error = "unknown reason" });
            filterMask = (long)parsed;
        }

        if (options.GoldenRowIndex)
            return await GoldensFromIndexAsync(filterMask, q, sort, page, size, ct);

        bool reasonsStaged = await ReasonsStagedAsync(ct);
        var all = GoldenRows(reasonsStaged);

        // Chip counts over every golden, by the same bit test the filter below applies.
        var everyBits = await all.Select(x => x.Bits).ToListAsync(ct);
        var reasonCounts = new Dictionary<string, int>();
        foreach (var r in GoldenReasons.All)
        {
            long bit = (long)r;
            int n = everyBits.Count(b => (b & bit) != 0);
            if (n > 0) reasonCounts[r.ToString()] = n;
        }

        var rows = all;
        long? exactId = null;
        if (filterMask != 0) rows = rows.Where(x => (x.Bits & filterMask) != 0);
        if (!string.IsNullOrWhiteSpace(q)) (rows, exactId) = SearchGoldens(rows, q.Trim());

        int total = await rows.CountAsync(ct);
        // A customer number typed in full comes first, ahead of phones that merely contain it.
        var ordered = rows.OrderBy(x => x.ID == exactId ? 0 : 1);
        rows = sort == "id"
            ? ordered.ThenBy(x => x.ID)
            : ordered.ThenByDescending(x => x.Members).ThenBy(x => x.ID);
        var window = await rows.Skip(page * size).Take(size).ToListAsync(ct);

        var ids = window.Select(w => w.ID).ToList();
        var sourcesOf = await SourceSystemsOfAsync(ids, ct);

        var items = window.Select(w =>
        {
            var reasons = GoldenReasons.ReasonsOf(w.Bits);
            var systems = sourcesOf.GetValueOrDefault(w.ID) ?? [];
            return new
            {
                id = w.ID.ToString(CultureInfo.InvariantCulture),
                name = Shown(PiiKind.Name, w.FullName),
                phone = Shown(PiiKind.Phone, w.Phone),
                city = PiiView.ShowCity(w.City),
                members = w.Members,
                sources = w.Sources ?? systems.Count,
                sourceSystems = systems,
                reasons = NamesOf<GoldenReason>((long)reasons),
                needsReview = (reasons & GoldenReason.NeedsReview) != 0,
            };
        }).ToList();

        return new JsonResult(new
        {
            total,
            page,
            size,
            goldenTotal = everyBits.Count,
            awaitingResolve = await AwaitingResolveAsync(ct),
            reasonsStaged,
            reasonCounts,
            masked = !Pii.Reveal,
            items,
        });
    }

    /// <summary>
    /// <see cref="Goldens"/> from the per-run in-memory index (<see cref="DarlasticApiOptions.GoldenRowIndex"/>):
    /// the same rows, order, totals and chip counts, with only the page's own rows read from the view.
    /// </summary>
    private async Task<IActionResult> GoldensFromIndexAsync(long filterMask, string? q, string? sort, int page, int size,
        CancellationToken ct)
    {
        var index = await runCache.GetAsync(db, GoldenRowIndex.CacheName, GoldenRowIndex.BuildAsync, ct);

        HashSet<long>? found = null;
        long? exactId = null;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var (matches, exact) = SearchGoldens(GoldenSearchRows(), q.Trim());
            found = [.. await matches.Select(x => x.ID).ToListAsync(ct)];
            exactId = exact;
        }

        // The view's order: a customer number typed in full first, then most records first (or by
        // number). Positions walk the snapshot's arrays; nothing is sorted per request.
        IEnumerable<int> positions = sort == "id" ? index.ById : Enumerable.Range(0, index.Count);
        bool Wanted(int i) => (filterMask == 0 || (index.Bits[i] & filterMask) != 0)
                              && (found is null || found.Contains(index.Ids[i]));

        int total = 0;
        int? exactPos = null;
        int skip = page * size;
        var rest = new List<int>(Math.Min(skip + size, 4096));
        foreach (var i in positions)
        {
            if (!Wanted(i)) continue;
            total++;
            // The exact match takes the first slot below; everything else keeps its order.
            if (exactId is { } e && index.Ids[i] == e) { exactPos = i; continue; }
            if (rest.Count < skip + size) rest.Add(i);
        }
        if (exactPos is { } first) rest.Insert(0, first);
        var window = rest.Skip(skip).Take(size).ToList();

        var ids = window.Select(i => index.Ids[i]).ToList();
        var details = await db.Set<GoldenCustomer>().AsNoTracking()
            .Where(g => ids.Contains(g.ID))
            .Select(g => new { g.ID, g.FullName, g.Phone, g.City })
            .ToDictionaryAsync(g => g.ID, ct);
        var sourceCounts = await db.Set<IdentitySummary>().AsNoTracking()
            .Where(s => ids.Contains(s.IdentityID))
            .Select(s => new { s.IdentityID, s.SourceCount })
            .ToDictionaryAsync(s => s.IdentityID, s => s.SourceCount, ct);
        var sourcesOf = await SourceSystemsOfAsync(ids, ct);

        var items = window.Select(i =>
        {
            long id = index.Ids[i];
            var reasons = GoldenReasons.ReasonsOf(index.Bits[i]);
            var systems = sourcesOf.GetValueOrDefault(id) ?? [];
            var g = details.GetValueOrDefault(id);
            return new
            {
                id = id.ToString(CultureInfo.InvariantCulture),
                name = Shown(PiiKind.Name, g?.FullName),
                phone = Shown(PiiKind.Phone, g?.Phone),
                city = PiiView.ShowCity(g?.City),
                members = index.Members[i],
                sources = sourceCounts.TryGetValue(id, out var sc) ? sc : systems.Count,
                sourceSystems = systems,
                reasons = NamesOf<GoldenReason>((long)reasons),
                needsReview = (reasons & GoldenReason.NeedsReview) != 0,
            };
        }).ToList();

        return new JsonResult(new
        {
            total,
            page,
            size,
            goldenTotal = index.Count,
            awaitingResolve = await AwaitingResolveAsync(ct),
            reasonsStaged = index.ReasonsStaged,
            reasonCounts = index.ReasonCounts,
            masked = !Pii.Reveal,
            items,
        });
    }

    /// <summary>The view's rows with only what a golden search reads (no summary join, no reasons).</summary>
    private IQueryable<GoldenRow> GoldenSearchRows() =>
        db.Set<GoldenCustomer>().AsNoTracking()
            .Select(g => new GoldenRow { ID = g.ID, FullName = g.FullName, Phone = g.Phone });

    /// <summary>The source systems of each golden's live records, in ordinal order.</summary>
    private async Task<Dictionary<long, List<string>>> SourceSystemsOfAsync(List<long> ids, CancellationToken ct) =>
        (await db.Set<SourceProfile>().AsNoTracking()
                .Where(p => ids.Contains(p.IdentityID) && !p.Removed)
                .Select(p => new { p.IdentityID, p.SourceSystem })
                .Distinct()
                .ToListAsync(ct))
            .GroupBy(p => p.IdentityID)
            .ToDictionary(g => g.Key, g => g.Select(p => p.SourceSystem).Order(StringComparer.Ordinal).ToList());

    /// <summary>One golden row as the list query sees it.</summary>
    private sealed class GoldenRow
    {
        public long ID { get; init; }
        public string? FullName { get; init; }
        public string? Phone { get; init; }
        public string? City { get; init; }
        public int Members { get; init; }
        public int? Sources { get; init; }
        public long Bits { get; init; }
    }

    /// <summary>
    /// Every golden, LEFT JOINed to its staged summary, with its reason bits computed in SQL so
    /// filtering, counting and paging all run on the same expression. The bits follow
    /// <see cref="GoldenReasons.ForReader"/>: staged reasons when present; the mirrored cluster bits
    /// for a summary staged without reasons; for no summary, single record and (when the run staged
    /// evidence) "not in the latest run".
    /// </summary>
    private IQueryable<GoldenRow> GoldenRows(bool reasonsStaged)
    {
        long allMask = GoldenReasons.AllMask;
        long clusterMask = GoldenReasons.ClusterCatMask;
        long shift = 1L << GoldenReasons.ClusterCatShift;
        long missingSingle = (long)GoldenReasons.ForReader(null, 1, reasonsStaged);
        long missingMany = (long)GoldenReasons.ForReader(null, 2, reasonsStaged);

        return from g in db.Set<GoldenCustomer>().AsNoTracking()
               join s in db.Set<IdentitySummary>().AsNoTracking() on g.ID equals s.IdentityID into staged
               from s in staged.DefaultIfEmpty()
               select new GoldenRow
               {
                   ID = g.ID,
                   FullName = g.FullName,
                   Phone = g.Phone,
                   City = g.City,
                   Members = g.SourceCount,
                   Sources = s == null ? null : (int?)s.SourceCount,
                   Bits = s == null
                       ? (g.SourceCount == 1 ? missingSingle : missingMany)
                       : ((s.Categories & allMask) != 0 ? s.Categories : (s.Categories & clusterMask) * shift),
               };
    }

    /// <summary>
    /// A golden ID, a phone (digits, as the engine stores them) or a name. Fragments only for
    /// callers holding the partial-search action; whole values otherwise.
    /// </summary>
    private (IQueryable<GoldenRow> Rows, long? ExactId) SearchGoldens(IQueryable<GoldenRow> rows, string needle)
    {
        bool partial = Pii.PartialSearch;
        string digits = Digits(needle);
        bool numeric = digits.Length > 0 && needle.All(c => char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')');

        if (numeric)
        {
            long? asId = digits.Length <= 18 && long.TryParse(digits, out var id) ? id : null;
            return (partial
                ? rows.Where(x => x.ID == asId || (x.Phone != null && x.Phone.Contains(digits)))
                : rows.Where(x => x.ID == asId || x.Phone == digits), asId);
        }

        return (partial
            ? rows.Where(x => x.FullName != null && x.FullName.Contains(needle))
            : rows.Where(x => x.FullName == needle), null);
    }

    /// <summary>Did the last resolve stage per-golden evidence? Every identity it summarised then
    /// carries at least one reason bit, and a run without it never sets one.</summary>
    private Task<bool> ReasonsStagedAsync(CancellationToken ct)
    {
        long allMask = GoldenReasons.AllMask;
        return db.Set<IdentitySummary>().AsNoTracking().AnyAsync(s => (s.Categories & allMask) != 0, ct);
    }

    /// <summary>
    /// Live identities with no staged golden: minted by a host (a CRM entry) since the last resolve.
    /// They are real customers without survived attributes yet, so they are reported beside the
    /// golden rows rather than mixed into them.
    /// </summary>
    private async Task<int> AwaitingResolveAsync(CancellationToken ct)
    {
        string sql = $"""
            SELECT COUNT(*) AS [Value]
            FROM {Schema()}.[Identity] i
            WHERE i.Status = {(byte)IdentityStatus.Active}
              AND NOT EXISTS (SELECT 1 FROM {Schema()}.[ProjectionState] p
                              WHERE p.ArtifactType = 'golden'
                                AND p.ArtifactKey = CAST(i.IdentityID AS nvarchar(140)) COLLATE {Data.Extensions.DarlasticModelBuilderExtensions.KeyCollation}
                                AND p.ContentHash <> 'TOMBSTONE'
                                AND p.Payload IS NOT NULL)
            """;
        return await db.Database.SqlQueryRaw<int>(sql).SingleAsync(ct);
    }

    // ------------------------------------------------------------- identities

    /// <summary>
    /// Customers built from several records, largest first. The golden name follows the golden
    /// contract like every other name here, and <paramref name="q"/> matches it by fragment only for
    /// a caller holding the partial-search action; anyone else must give the whole name.
    /// </summary>
    [HttpGet("clusters")]
    public async Task<IActionResult> Clusters(string? cat, string? q, int page = 0, int size = 50,
        CancellationToken ct = default)
    {
        if (Denied(false)) return Forbid();

        var query = db.Set<IdentitySummary>().AsNoTracking().Where(s => s.MemberCount > 1);

        long mask = MaskOf<ClusterCat>(cat);
        if (mask != 0) query = query.Where(s => (s.Categories & mask) != 0);
        if (!string.IsNullOrWhiteSpace(q))
        {
            string needle = q.Trim();
            query = Pii.PartialSearch
                ? query.Where(s => s.GoldenName != null && s.GoldenName.Contains(needle))
                : query.Where(s => s.GoldenName == needle);
        }

        int total = await query.CountAsync(ct);
        // Ties on size are ordered by customer number, so paging neither repeats nor skips a customer.
        var window = await query.OrderByDescending(s => s.MemberCount).ThenBy(s => s.IdentityID)
            .Skip(page * size).Take(size).ToListAsync(ct);

        int run = await LatestRun(ct);
        var status = await StatusOf(window.Select(s => s.IdentityID), ct);

        var items = window.Select(s => new
        {
            root = s.IdentityID,
            size = s.MemberCount,
            sources = s.SourceCount,
            goldenName = Shown(PiiKind.Name, s.GoldenName),
            cats = NamesOf<ClusterCat>(s.Categories),
            // Highlight is a phrase the standalone composes from live survivorship state. The
            // staged summary carries the categories that phrase was derived from, so the page's
            // chips still populate; the sentence does not.
            highlight = (string?)null,
            registry = RegistryJson(s.IdentityID, status.GetValueOrDefault(s.IdentityID), run),
        }).ToList();

        return new JsonResult(new { total, page, size, masked = !Pii.Reveal, items });
    }

    /// <summary>
    /// One golden customer opened: every member record, the golden values and how each was chosen,
    /// and the evidence between records — the auto-merge edges that joined them, the queued pairs
    /// waiting on a person, and catalogued pairs linking them to other customers.
    ///
    /// <para>Any identity opens, not only multi-record ones, and the ID is redirect-chased, so every
    /// row of a golden list opens. A member whose normalized record was not staged (a run without
    /// per-golden evidence stages only queued and catalogued records) still appears by its source and
    /// record id instead of silently dropping out of the member list.</para>
    ///
    /// <para>The shape is what the page's identity view reads: <c>golden[]</c> with <c>attrType</c>,
    /// <c>value</c> and <c>wonBy</c>, <c>nameSurvival</c>, and edges carrying the members' record
    /// indices <c>a</c>/<c>b</c> — none of which this route returned before.</para>
    /// </summary>
    [HttpGet("cluster/{root:long}")]
    public async Task<IActionResult> Cluster(long root, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        long live = await ChaseAsync(root, ct);
        var identity = await db.Set<GoldenIdentity>().AsNoTracking().FirstOrDefaultAsync(i => i.IdentityID == live, ct);
        if (identity is null) return NotFound(new { error = "no such customer" });

        var summary = await db.Set<IdentitySummary>().AsNoTracking().FirstOrDefaultAsync(s => s.IdentityID == live, ct);

        var profiles = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => p.IdentityID == live)
            .OrderBy(p => p.Removed).ThenBy(p => p.SourceSystem).ThenBy(p => p.SourceRecordId)
            .Select(p => new { p.SourceSystem, p.SourceRecordId, p.Removed })
            .ToListAsync(ct);
        var memberKeys = profiles.Select(p => (p.SourceSystem, p.SourceRecordId)).ToHashSet();

        // Bounded: one identity in a measured corpus carries 28,622 edges, and no human reads past the
        // strongest few dozen.
        var edges = await db.Set<IdentityEdge>().AsNoTracking()
            .Where(e => e.IdentityID == live)
            .OrderByDescending(e => e.Score)
            .Take(200)
            .ToListAsync(ct);

        // Pairs that tie a member to someone else: waiting on a person, or catalogued by the resolve.
        var systems = memberKeys.Select(k => k.SourceSystem).Distinct().ToList();
        var recIds = memberKeys.Select(k => k.SourceRecordId).Distinct().ToList();
        bool Touches(string sa, string ia, string sb, string ib) => memberKeys.Contains((sa, ia)) || memberKeys.Contains((sb, ib));
        bool Internal(string sa, string ia, string sb, string ib) => memberKeys.Contains((sa, ia)) && memberKeys.Contains((sb, ib));
        var queued = (await db.Set<StewardQueueEntry>().AsNoTracking()
                .Where(e => (systems.Contains(e.SourceSystemA) && recIds.Contains(e.SourceRecordIdA))
                         || (systems.Contains(e.SourceSystemB) && recIds.Contains(e.SourceRecordIdB)))
                .ToListAsync(ct))
            .Where(e => Touches(e.SourceSystemA, e.SourceRecordIdA, e.SourceSystemB, e.SourceRecordIdB))
            .OrderByDescending(e => e.Score).Take(50).ToList();
        var related = (await db.Set<CaseCatalogEntry>().AsNoTracking()
                .Where(e => (systems.Contains(e.SourceSystemA) && recIds.Contains(e.SourceRecordIdA))
                         || (systems.Contains(e.SourceSystemB) && recIds.Contains(e.SourceRecordIdB)))
                .ToListAsync(ct))
            .Where(e => Touches(e.SourceSystemA, e.SourceRecordIdA, e.SourceSystemB, e.SourceRecordIdB)
                        && !Internal(e.SourceSystemA, e.SourceRecordIdA, e.SourceSystemB, e.SourceRecordIdB))
            .OrderByDescending(e => e.Score).Take(50).ToList();

        var sides = await LoadSides(memberKeys
            .Concat(queued.SelectMany(e => new[] { (e.SourceSystemA, e.SourceRecordIdA), (e.SourceSystemB, e.SourceRecordIdB) }))
            .Concat(related.SelectMany(e => new[] { (e.SourceSystemA, e.SourceRecordIdA), (e.SourceSystemB, e.SourceRecordIdB) })), ct);
        RealRecord? Rec(string sys, string id) => sides.GetValueOrDefault((sys, id)).rec;

        var current = profiles.Where(p => !p.Removed).Select(p => Rec(p.SourceSystem, p.SourceRecordId)).ToList();
        bool allStaged = current.Count > 0 && current.All(r => r is not null);

        // Golden values: recomputed from the members when every one is staged — the same function
        // over the same records the resolve used, which also says WHY each value won and how the name
        // survived. Otherwise the staged golden's own values, without the why.
        var explain = new Merge.NameSurvivalExplain();
        var golden = allStaged
            ? Merge.SurviveGolden([.. current.Select(r => r!)], explain)
            : await StagedGoldenAttrsAsync(live, ct);

        bool reasonsStaged = await ReasonsStagedAsync(ct);
        var reasons = GoldenReasons.ForReader(summary?.Categories, current.Count, reasonsStaged);
        string? goldenName = golden.FirstOrDefault(g => g.AttrType == "full_name")?.Value;

        object PairJson(string sa, string ia, string sb, string ib, float score, long? cats)
        {
            bool aIsMember = memberKeys.Contains((sa, ia));
            var mine = aIsMember ? (sa, ia) : (sb, ib);
            var other = aIsMember ? (sb, ib) : (sa, ia);
            var otherSide = sides.GetValueOrDefault(other);
            return new
            {
                key = $"{sa}:{ia}~{sb}:{ib}",
                a = Rec(sa, ia)?.Idx,
                b = Rec(sb, ib)?.Idx,
                score = Math.Round(score, 3),
                cats = cats is { } c ? NamesOf<CaseCat>(c) : [],
                member = RecordJson(sides.GetValueOrDefault(mine)) ?? StubJson(mine.Item1, mine.Item2, removed: false),
                other = RecordJson(otherSide) ?? StubJson(other.Item1, other.Item2, removed: false),
                otherCustomer = otherSide.identityId?.ToString(CultureInfo.InvariantCulture),
            };
        }

        return new JsonResult(new
        {
            root = live,
            requested = root,
            size = current.Count,
            sources = summary?.SourceCount ?? profiles.Where(p => !p.Removed).Select(p => p.SourceSystem).Distinct().Count(),
            goldenName = Shown(PiiKind.Name, goldenName),
            // Cluster categories (the identity view's chips) and per-golden reasons (the golden list's).
            cats = summary is null ? [] : NamesOf<ClusterCat>(summary.Categories & GoldenReasons.ClusterCatMask),
            reasons = NamesOf<GoldenReason>((long)reasons),
            golden = golden.Select(g => new
            {
                attrType = g.AttrType,
                label = CaseLabels.Attributes.GetValueOrDefault(g.AttrType)?.Label ?? g.AttrType,
                value = ShownAttribute(g.AttrType, g.Value),
                wonBy = g.WonBy,
                how = CaseLabels.Survival.GetValueOrDefault(g.WonBy)?.Label,
            }).ToList(),
            nameSurvival = allStaged && explain.Anchor is not null
                ? new
                {
                    anchor = Shown(PiiKind.Name, explain.Anchor),
                    anchorCount = explain.AnchorCount,
                    chosen = Shown(PiiKind.Name, explain.Chosen),
                    extensions = explain.Extensions.Select(e => new
                    {
                        name = Shown(PiiKind.Name, e.Name),
                        count = e.Count,
                        givenAligns = e.GivenAligns,
                        bridge = Shown(PiiKind.Name, e.Bridge),
                    }).ToList(),
                }
                : null,
            members = profiles.Select(p =>
                    RecordJson(sides.GetValueOrDefault((p.SourceSystem, p.SourceRecordId)))
                    ?? StubJson(p.SourceSystem, p.SourceRecordId, p.Removed))
                .ToList(),
            edges = edges.Select(e =>
            {
                var ra = Rec(e.SourceSystemA, e.SourceRecordIdA);
                var rb = Rec(e.SourceSystemB, e.SourceRecordIdB);
                var flags = (MatchFlags)e.Flags;
                return new
                {
                    key = $"{e.SourceSystemA}:{e.SourceRecordIdA}~{e.SourceSystemB}:{e.SourceRecordIdB}",
                    a = ra?.Idx,
                    b = rb?.Idx,
                    aKey = $"{e.SourceSystemA}:{e.SourceRecordIdA}",
                    bKey = $"{e.SourceSystemB}:{e.SourceRecordIdB}",
                    score = Math.Round(e.Score, 3),
                    flags = NamesOf<MatchFlags>(e.Flags),
                    crossDealer = e.SourceSystemA != e.SourceSystemB,
                    // What the edge scores without the vehicle, when the vehicle is what merged it.
                    noVin = (flags & MatchFlags.VinSoldMerge) != 0 && ra is not null && rb is not null
                        ? Math.Round(RealMatcher.Score(ra, rb, useAddress: true, useVin: false), 3)
                        : (double?)null,
                };
            }).ToList(),
            reviewPairs = queued.Select(e => PairJson(e.SourceSystemA, e.SourceRecordIdA, e.SourceSystemB, e.SourceRecordIdB, e.Score, null)).ToList(),
            relatedPairs = related.Select(e => PairJson(e.SourceSystemA, e.SourceRecordIdA, e.SourceSystemB, e.SourceRecordIdB, e.Score, e.Categories)).ToList(),
            registry = RegistryJson(live, (byte)identity.Status, await LatestRun(ct)),
        });
    }

    /// <summary>A member known only by its key: its normalized record was not staged this run, or it
    /// was removed from its source. Same shape as a staged record, with nothing to show.</summary>
    private static object StubJson(string sys, string id, bool removed) => new
    {
        idx = (int?)null,
        src = sys,
        id,
        staged = false,
        removed,
        rawName = (string?)null,
        normName = (string?)null,
        phones = Array.Empty<string>(),
        weakPhones = Array.Empty<string>(),
        natId = (string?)null,
        dob = (string?)null,
        rawAddress = "",
        normAddress = "",
        city = "",
        mojibake = false,
        arabizi = false,
        vins = (object?)null,
        clusterRoot = (long?)null,
        clusterSize = 0,
    };

    /// <summary>
    /// A golden attribute as this caller may see it — by the protection its DTO declares. The city
    /// is the one attribute the contract leaves open (unless too few customers share it, see
    /// <see cref="DarlasticApiOptions.CityMinimumShare"/>); a type this list does not know is masked
    /// rather than assumed harmless, so a new survived attribute cannot appear unmasked by default.
    /// </summary>
    private string? ShownAttribute(string attrType, string? value) => attrType switch
    {
        "full_name" => Shown(PiiKind.Name, value),
        "phone" => Shown(PiiKind.Phone, value),
        "email" => Shown(PiiKind.Email, value),
        "national_id" => Shown(PiiKind.Identifier, value),
        "city" => PiiView.ShowCity(value),
        _ => Shown(PiiKind.Identifier, value),
    };

    /// <summary>The staged golden's attributes, last-wins per type like the drain and the view.</summary>
    private async Task<List<Merge.GoldenAttr>> StagedGoldenAttrsAsync(long identityId, CancellationToken ct)
    {
        string key = identityId.ToString(CultureInfo.InvariantCulture);
        var payload = await db.Set<ProjectionState>().AsNoTracking()
            .Where(p => p.ArtifactType == "golden" && p.ArtifactKey == key && p.ContentHash != "TOMBSTONE")
            .Select(p => p.Payload)
            .FirstOrDefaultAsync(ct);
        var byType = new Dictionary<string, string>(StringComparer.Ordinal);
        if (payload is not null)
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
                foreach (var a in attrs.EnumerateArray())
                    if (a.TryGetProperty("t", out var t) && a.TryGetProperty("v", out var v) && t.GetString() is { } type)
                        byType[type] = v.GetString() ?? "";
        }
        return [.. byType.Select(kv => new Merge.GoldenAttr(kv.Key, kv.Value, ""))];
    }

    /// <summary>Follows merge redirects to the live identity (bounded, like the golden endpoint).</summary>
    private async Task<long> ChaseAsync(long id, CancellationToken ct)
    {
        long live = id;
        for (int hop = 0; hop < 64; hop++)
        {
            var next = await db.Set<IdentityRedirect>().AsNoTracking()
                .Where(r => r.OldIdentityID == live).Select(r => (long?)r.NewIdentityID).FirstOrDefaultAsync(ct);
            if (next is null) break;
            live = next.Value;
        }
        return live;
    }

    /// <summary>
    /// The page's registry badge. <c>split</c> and <c>unregistered</c> exist because the standalone
    /// compares a LIVE cluster against the last resolve and can find them disagreeing — the engine
    /// moved since. A hosted reader has no live cluster to disagree with: the registry is the only
    /// source, so both are constant here rather than absent. Emitting them keeps the badge rendering
    /// the same in both hosts instead of silently dropping to "undefined".
    /// </summary>
    private static object RegistryJson(long identityId, byte status, int run) => new
    {
        goldenId = (long?)identityId,
        status,
        split = false,
        identities = 1,
        unregistered = 0,
        run,
    };

    private async Task<int> LatestRun(CancellationToken ct) =>
        await db.Set<ResolveRun>().AsNoTracking().OrderByDescending(r => r.RunID)
            .Select(r => r.RunID).FirstOrDefaultAsync(ct);

    private async Task<Dictionary<long, byte>> StatusOf(IEnumerable<long> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        if (list.Count == 0) return [];
        var rows = await db.Set<GoldenIdentity>().AsNoTracking()
            .Where(i => list.Contains(i.IdentityID))
            .Select(i => new { i.IdentityID, i.Status })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.IdentityID, r => (byte)r.Status);
    }

    // ----------------------------------------------------------------- search

    /// <summary>
    /// The page's record search: up to 50 staged records answering <see cref="Matches(RealRecord, string)"/>. Reads
    /// the first 50,000 staged records, unless the host keeps the per-run record index
    /// (<see cref="DarlasticApiOptions.RecordSearchIndex"/>): then every staged record is searched.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(string q, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();
        if (string.IsNullOrWhiteSpace(q)) return new JsonResult(Array.Empty<object>());

        string needle = q.Trim();
        var rows = options.RecordSearchIndex
            ? await SearchByIndexAsync(needle, ct)
            : await db.Set<StewardRecord>().AsNoTracking().Take(50_000).ToListAsync(ct);

        var hits = new List<object>();
        foreach (var row in rows)
        {
            var rec = Revive(row);
            if (rec is null || !Matches(rec, needle)) continue;
            hits.Add(RecordJson((rec, (row.SourceSystem, row.SourceRecordId), null, 1))!);
            if (hits.Count == 50) break;
        }

        var withIdentity = await WithIdentities(hits, ct);
        return new JsonResult(withIdentity);
    }

    /// <summary>
    /// <see cref="Search"/> through the per-run record index: the candidates are the records whose key, names or
    /// phones hold the search text (by fragment for a partial-search caller, whole otherwise, the key always
    /// whole, also after a "source:" prefix); each is kept only if it answers <see cref="Matches(RealRecord, string)"/>.
    /// </summary>
    private async Task<List<StewardRecord>> SearchByIndexAsync(string needle, CancellationToken ct)
    {
        var index = await runCache.GetAsync(db, RecordSearchIndex.CacheName, RecordSearchIndex.BuildAsync, ct);
        const RecordSearchIndex.Fields none = RecordSearchIndex.Fields.None;
        const RecordSearchIndex.Fields phones = RecordSearchIndex.Fields.Phones | RecordSearchIndex.Fields.WeakPhones;
        var searches = new List<(RecordSearchIndex.Fields, RecordSearchIndex.Fields, string?)>
        {
            (RecordSearchIndex.Fields.RecordIds, none, needle),
        };
        // "source:id" names a record too: its ID is whatever follows a colon.
        for (int colon = needle.IndexOf(':'); colon >= 0; colon = needle.IndexOf(':', colon + 1))
            searches.Add((RecordSearchIndex.Fields.RecordIds, none, needle[(colon + 1)..]));
        if (Pii.PartialSearch)
            searches.Add((none, RecordSearchIndex.Fields.Names | phones, needle));
        else
        {
            searches.Add((RecordSearchIndex.Fields.Names, none, needle));
            searches.Add((phones, none, Digits(needle)));
        }
        return await index.RowsAsync(db, index.Find([.. searches]), 50,
            row => Revive(row) is { } rec && Matches(rec, needle), ct);
    }

    /// <summary>
    /// A record's scored candidates. Not available from a hosted reader: candidates come from
    /// blocking over the whole corpus, which is the one thing the prototype's in-memory index buys
    /// and a request-scoped reader cannot afford. Returns the record with an empty candidate list
    /// and says why, rather than 404-ing a route the page calls.
    /// </summary>
    [HttpGet("record/{idx:int}")]
    public async Task<IActionResult> Record(int idx, CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var hit = await RecordByIdx(idx, ct);
        if (hit is null) return BadRequest(new { error = "bad record idx" });

        var sides = await LoadSides([hit.Value.key], ct);
        var rec = hit.Value.rec;
        return new JsonResult(new
        {
            record = RecordJson(sides.GetValueOrDefault(hit.Value.key)),
            // What normalization did and which comparison groups the record falls in — both pure
            // functions of the staged record, so they are recomputed rather than staged. Both quote
            // the record's own values, so a masked caller gets them masked.
            normalize = PiiView.Normalize(rec).Select(s => new { stage = s.Stage, detail = s.Detail }).ToList(),
            blockKeys = RealMatcher.BlockKeysOf(rec).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
                .Select(PiiView.BlockKey).ToList(),
            // Which keys hit the block cap is a property of the run's blocking pass and is not staged.
            cappedKeys = Array.Empty<object>(),
            candidates = Array.Empty<object>(),
            note = "Candidates are compared during a run; they are not listed here.",
        });
    }

    // ------------------------------------------------------------------ flags

    /// <summary>
    /// The notes list. Each target is parsed back into what the page needs to reopen it — a pair's
    /// two record indices, a customer's id, a record's index — which this route used to leave null,
    /// so no note could be reopened from the list.
    /// </summary>
    [HttpGet("flags")]
    public async Task<IActionResult> Flags(CancellationToken ct)
    {
        if (Denied(false)) return Forbid();

        var rows = await db.Set<ReviewFlag>().AsNoTracking().ToListAsync(ct);

        static (string sys, string id)? Side(string s)
        {
            int colon = s.IndexOf(':');
            return colon > 0 && colon < s.Length - 1 ? (s[..colon], s[(colon + 1)..]) : null;
        }
        // A record by the host's name for its source (the slug when the host gives none), as the page
        // names it everywhere else.
        string Named(string side) => Side(side) is { } k ? $"{SourceName(k.sys)} #{k.id}" : side;
        var parsed = rows.Select(f =>
        {
            string t = f.Target;
            if (t.StartsWith("pair:", StringComparison.Ordinal) && t[5..].Split('~') is [var x, var y])
                return (f, kind: "pair", a: Side(x), b: Side(y), root: (long?)null, label: $"Pair {Named(x)} ~ {Named(y)}");
            if (t.StartsWith("cluster:", StringComparison.Ordinal) && long.TryParse(t[8..], out var root))
                return (f, kind: "cluster", a: null, b: null, root: (long?)root, label: $"Customer #{root}");
            if (t.StartsWith("record:", StringComparison.Ordinal) && Side(t[7..]) is { } r)
                return (f, kind: "record", a: ((string, string)?)r, b: null, root: (long?)null, label: $"Record {SourceName(r.sys)} #{r.id}");
            return (f, kind: "other", a: null, b: null, root: (long?)null, label: t);
        }).ToList();

        var sides = await LoadSides(parsed.SelectMany(p => new[] { p.a, p.b }).Where(k => k is not null).Select(k => k!.Value), ct);
        int? IdxOf((string, string)? key) => key is { } k ? sides.GetValueOrDefault(k).rec?.Idx : null;

        return new JsonResult(new
        {
            total = rows.Count,
            byTopic = rows.GroupBy(f => f.Topic ?? "other").ToDictionary(g => g.Key, g => g.Count()),
            open = rows.Count(f => f.Response == null),
            items = parsed
                .OrderBy(p => p.f.Response == null ? 0 : 1)
                .ThenByDescending(p => p.f.UpdatedUtc ?? p.f.CreatedUtc)
                .Select(p => new
                {
                    target = p.f.Target,
                    kind = p.kind,
                    label = p.label,
                    topic = p.f.Topic,
                    comment = p.f.Comment,
                    author = p.f.Author,
                    createdAt = p.f.CreatedUtc.ToString("O"),
                    updatedAt = (p.f.UpdatedUtc ?? p.f.CreatedUtc).ToString("O"),
                    status = p.f.Response == null ? "open" : "addressed",
                    resolution = p.f.Response,
                    resolvedAt = p.f.ResponseUtc?.ToString("O"),
                    resolvedBy = p.f.ResponseBy,
                    snapScore = (double?)null,
                    snapDecision = (string?)null,
                    a = p.kind == "pair" ? IdxOf(p.a) : null,
                    b = p.kind == "pair" ? IdxOf(p.b) : null,
                    root = p.root,
                    idx = p.kind == "record" ? IdxOf(p.a) : null,
                }).ToList(),
        });
    }

    // ----------------------------------------------------------------- writes

    public class FlagBody
    {
        public string? Kind { get; set; }
        public string? Topic { get; set; }
        public string? Comment { get; set; }
        public string? Author { get; set; }
        public int? A { get; set; }
        public int? B { get; set; }
        public long? Root { get; set; }
        public int? Idx { get; set; }
    }

    public class UnflagBody { public string? Target { get; set; } }

    public class AuditBody
    {
        public int A { get; set; }
        public int B { get; set; }
        public string? Verdict { get; set; }
        public string? Rationale { get; set; }
        public string? Judge { get; set; }
    }

    /// <summary>
    /// Raise or edit a review flag. One flag per target — re-flagging edits rather than piling up,
    /// which is what makes the flag list a work list instead of a log.
    /// </summary>
    /// <remarks>
    /// Refusals carry a real status code AND the <c>{ok:false}</c> body the page reads. Returning
    /// 200 with <c>ok:false</c> — which this did at first — keeps the page working while telling
    /// logs, metrics and every other client that the write succeeded.
    /// </remarks>
    [HttpPost("flag")]
    public async Task<IActionResult> Flag([FromBody] FlagBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "not permitted" });
        if (body is null) return BadRequest(new { ok = false, error = "bad body" });

        string kind = (body.Kind ?? "pair").ToLowerInvariant();
        string? target = null;
        string? snapshot = null;

        if (kind == "pair" && body.A is { } a && body.B is { } b)
        {
            var ra = await RecordByIdx(a, ct);
            var rb = await RecordByIdx(b, ct);
            if (ra is null || rb is null) return new JsonResult(new { ok = false, error = "bad pair idx" });
            target = "pair:" + PairKeyOf(ra.Value.key, rb.Value.key);
            // The evidence as it read when flagged, so the flag is self-contained: whoever picks it
            // up later does not have to reconstruct what the engine was saying at the time.
            snapshot = JsonSerializer.Serialize(await DetailJson(ra.Value.rec, rb.Value.rec, ra.Value.key, rb.Value.key, ct));
        }
        else if (kind == "cluster" && body.Root is { } root)
        {
            target = "cluster:" + root;
        }
        else if (kind == "record" && body.Idx is { } idx)
        {
            var r = await RecordByIdx(idx, ct);
            if (r is null) return new JsonResult(new { ok = false, error = "bad record idx" });
            target = $"record:{r.Value.key.sys}:{r.Value.key.id}";
        }

        if (target is null) return new JsonResult(new { ok = false, error = "bad target" });

        var existing = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == target, ct);
        if (existing is null)
        {
            db.Set<ReviewFlag>().Add(new ReviewFlag
            {
                Target = target,
                Topic = body.Topic ?? "other",
                Comment = body.Comment ?? "",
                Author = string.IsNullOrWhiteSpace(body.Author) ? Actor() : body.Author!,
                CreatedUtc = DateTime.UtcNow,
                // The column is NOT NULL; a customer or record flag has no pair evidence to freeze,
                // and writing null there failed every such flag.
                Snapshot = snapshot ?? JsonSerializer.Serialize(new { kind, target }),
            });
        }
        else
        {
            existing.Topic = body.Topic ?? existing.Topic;
            existing.Comment = body.Comment ?? existing.Comment;
            existing.UpdatedUtc = DateTime.UtcNow;
            // The snapshot is NOT refreshed on edit: it records what was on screen when the concern
            // was raised, and rewriting it would quietly erase the evidence for the original note.
        }

        await db.SaveChangesAsync(ct);
        return new JsonResult(new { ok = true, target });
    }

    [HttpPost("unflag")]
    public async Task<IActionResult> Unflag([FromBody] UnflagBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "not permitted" });
        if (string.IsNullOrWhiteSpace(body?.Target)) return BadRequest(new { ok = false, error = "bad target" });

        var row = await db.Set<ReviewFlag>().FirstOrDefaultAsync(f => f.Target == body.Target, ct);
        if (row is not null)
        {
            db.Set<ReviewFlag>().Remove(row);
            await db.SaveChangesAsync(ct);
        }
        return new JsonResult(new { ok = true });
    }

    /// <summary>
    /// Record an adjudication. Appends — a pair can be re-judged and the history is the point, which
    /// is why LabelAudit's index is deliberately not unique.
    /// </summary>
    [HttpPost("audit")]
    public async Task<IActionResult> Audit([FromBody] AuditBody body, CancellationToken ct)
    {
        if (Denied(true)) return StatusCode(StatusCodes.Status403Forbidden, new { error = "not permitted" });
        if (body is null) return BadRequest(new { error = "bad audit body" });

        var ra = await RecordByIdx(body.A, ct);
        var rb = await RecordByIdx(body.B, ct);
        if (ra is null || rb is null) return BadRequest(new { error = "bad audit body" });

        string verdict = (body.Verdict ?? "").ToLowerInvariant() switch
        {
            "same" => "same",
            "different" => "different",
            "ambiguous" or "unsure" => "ambiguous",
            _ => "",
        };

        db.Set<LabelAudit>().Add(new LabelAudit
        {
            PairKey = PairKeyOf(ra.Value.key, rb.Value.key),
            // Empty verdict means "pending an expert call" — recorded, never folded into the labels.
            NewLabel = verdict.Length > 0 ? verdict : null,
            AuditedBy = string.IsNullOrWhiteSpace(body.Judge) ? Actor() : body.Judge!,
            AuditedUtc = DateTime.UtcNow,
            Rationale = body.Rationale,
            Status = "pending",
        });

        await db.SaveChangesAsync(ct);
        return new JsonResult(new { ok = true });
    }

    // ------------------------------------------------------------- shape glue

    private async Task<Dictionary<(string, string), (RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize)>>
        LoadSides(IEnumerable<(string sys, string id)> keys, CancellationToken ct)
    {
        var wanted = keys.Distinct().ToList();
        var result = new Dictionary<(string, string), (RealRecord?, (string, string), long?, int)>();
        if (wanted.Count == 0) return result;

        var systems = wanted.Select(k => k.sys).Distinct().ToList();
        var ids = wanted.Select(k => k.id).Distinct().ToList();

        var rows = await db.Set<StewardRecord>().AsNoTracking()
            .Where(r => systems.Contains(r.SourceSystem) && ids.Contains(r.SourceRecordId))
            .ToListAsync(ct);

        var profiles = await db.Set<SourceProfile>().AsNoTracking()
            .Where(p => systems.Contains(p.SourceSystem) && ids.Contains(p.SourceRecordId))
            .Select(p => new { p.SourceSystem, p.SourceRecordId, p.IdentityID })
            .ToListAsync(ct);

        var identityOf = profiles.ToDictionary(p => (p.SourceSystem, p.SourceRecordId), p => (long?)p.IdentityID);

        var identityIds = identityOf.Values.Where(v => v.HasValue).Select(v => v!.Value).Distinct().ToList();
        var sizeOf = await db.Set<IdentitySummary>().AsNoTracking()
            .Where(s => identityIds.Contains(s.IdentityID))
            .Select(s => new { s.IdentityID, s.MemberCount })
            .ToDictionaryAsync(s => s.IdentityID, s => s.MemberCount, ct);

        foreach (var row in rows)
        {
            var key = (row.SourceSystem, row.SourceRecordId);
            if (!wanted.Contains(key)) continue;
            var rec = Revive(row);
            if (rec is null) continue;
            var id = identityOf.GetValueOrDefault(key);
            result[key] = (rec, key, id, id is { } i ? sizeOf.GetValueOrDefault(i, 1) : 1);
        }

        return result;
    }

    private async Task<(RealRecord rec, (string sys, string id) key)?> RecordByIdx(int idx, CancellationToken ct)
    {
        if (options.RecordSearchIndex)
        {
            // The per-run record index knows which staged record carries the index: one key seek.
            var index = await runCache.GetAsync(db, RecordSearchIndex.CacheName, RecordSearchIndex.BuildAsync, ct);
            int position = index.PositionOfIdx(idx);
            if (position < 0) return null;
            var (sys, id) = index.KeyAt(position);
            var staged = await db.Set<StewardRecord>().AsNoTracking()
                .FirstOrDefaultAsync(r => r.SourceSystem == sys && r.SourceRecordId == id, ct);
            return Revive(staged) is { } found && found.Idx == idx ? (found, (staged!.SourceSystem, staged.SourceRecordId)) : null;
        }

        DbSet<StewardRecord> set = db.Set<StewardRecord>();
        string sql = "SELECT * FROM " + Schema() + ".StewardRecord WHERE JSON_VALUE(Payload,'$.Idx') = {0}";
        var row = await set.FromSqlRaw(sql, idx.ToString()).AsNoTracking().FirstOrDefaultAsync(ct);
        if (row is null) return null;
        var rec = Revive(row);
        return rec is null ? null : (rec, (row.SourceSystem, row.SourceRecordId));
    }

    private async Task<object> DetailJson(RealRecord ra, RealRecord rb,
        (string sys, string id) ka, (string sys, string id) kb, CancellationToken ct)
    {
        var trace = PiiView.NewTrace();
        double score = RealMatcher.Explain(ra, rb, trace);
        RealMatcher.Score(ra, rb, out var flags);

        string key = PairKeyOf(ka, kb);
        var sides = await LoadSides([ka, kb], ct);
        var audits = await AuditsByKey([key], ct);
        var queued = await QueuedKeys([key], ct);

        int? gap = ra.Dob is { } da && rb.Dob is { } dbo && da.Year > 0 && dbo.Year > 0
            ? Math.Abs(da.Year - dbo.Year) : null;

        long? idA = sides.GetValueOrDefault(ka).identityId;
        long? idB = sides.GetValueOrDefault(kb).identityId;

        return new
        {
            key,
            a = RecordJson(sides.GetValueOrDefault(ka)),
            b = RecordJson(sides.GetValueOrDefault(kb)),
            normalizeA = PiiView.Normalize(ra).Select(s => new { stage = s.Stage, detail = s.Detail }).ToList(),
            normalizeB = PiiView.Normalize(rb).Select(s => new { stage = s.Stage, detail = s.Detail }).ToList(),
            blockKeys = RealMatcher.BlockKeysOf(ra).Intersect(RealMatcher.BlockKeysOf(rb), StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(PiiView.BlockKey).ToList(),
            // Which shared keys hit the block cap is a property of the run's blocking pass, not of
            // the pair, and is not staged.
            cappedBlockKeys = Array.Empty<string>(),
            score = Math.Round(score, 3),
            band = RealMatcher.BandLabel(RealMatcher.Band(score)),
            decision = score >= 0.90 ? "auto-merge" : score >= 0.80 ? "steward-queue" : "kept-separate",
            decisionLabel = CaseLabels.Decision(score).Label,
            flags = NamesOf<MatchFlags>((long)flags),
            trace = trace.Steps.Select(s => new
            {
                stage = s.Stage,
                title = s.Title,
                detail = s.Detail,
                conf = s.ConfAfter is { } c ? Math.Round(c, 3) : (double?)null,
            }).ToList(),
            dobGap = gap,
            gold = (object?)null,
            audits = audits.GetValueOrDefault(key)?.Select(r => new
            {
                newLabel = r.NewLabel,
                oldLabel = r.OldLabel,
                auditedBy = r.AuditedBy,
                date = r.AuditedUtc.ToString("O"),
                rationale = r.Rationale,
                status = r.Status,
            }).ToList(),
            sameIdentity = idA is not null && idA == idB,
            queued = queued.Contains(key),
            // The page shows one cluster block when both records resolved into the same identity and
            // two when they did not — that contrast IS the steward's question, so both branches are
            // populated rather than collapsed into one.
            cluster = idA is not null && idA == idB ? await ClusterJson(idA.Value, ct) : null,
            clusterA = idA != idB ? await ClusterJson(idA, ct) : null,
            clusterB = idA != idB ? await ClusterJson(idB, ct) : null,
        };
    }

    private async Task<object?> ClusterJson(long? identityId, CancellationToken ct)
    {
        if (identityId is not { } id) return null;
        var s = await db.Set<IdentitySummary>().AsNoTracking().FirstOrDefaultAsync(x => x.IdentityID == id, ct);
        // IdentitySummary is staged only for identities worth summarising; a single-record identity
        // has no assembly story and the page renders no block for it.
        if (s is null || s.MemberCount <= 1) return null;
        return new
        {
            root = s.IdentityID,
            size = s.MemberCount,
            sources = s.SourceCount,
            goldenName = Shown(PiiKind.Name, s.GoldenName),
            cats = NamesOf<ClusterCat>(s.Categories & GoldenReasons.ClusterCatMask),
        };
    }

    private object? RecordJson((RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize) side)
    {
        if (side.rec is null) return null;
        var r = side.rec;
        return new
        {
            // The resolve's own record index, staged on the payload — the page's DOM key.
            idx = r.Idx,
            src = r.SourceSystem,
            id = r.SourceRecordId,
            staged = true,
            removed = false,
            rawName = Shown(PiiKind.Name, r.RawName),
            normName = Shown(PiiKind.Name, r.NormName),
            phones = r.Phones.Select(p => Shown(PiiKind.Phone, p)!).ToArray(),
            weakPhones = r.WeakPhones.Select(p => Shown(PiiKind.Phone, p)!).ToArray(),
            natId = Shown(PiiKind.Identifier, r.NationalId),
            dob = r.Dob is { } d ? Shown(PiiKind.Identifier, $"{d.P1}/{d.P2}/{d.Year}") : null,
            rawAddress = Shown(PiiKind.Address, r.RawAddress) ?? "",
            normAddress = Shown(PiiKind.Address, r.NormAddress) ?? "",
            city = PiiView.ShowCity(r.NormCity),
            mojibake = r.NameWasMojibake,
            arabizi = r.NameHadArabizi,
            vins = r.VinLinks is { Length: > 0 }
                ? r.VinLinks.Select(l => new
                {
                    vin = l.Vin,
                    src = l.Source == VinSource.Sale ? "sale" : "service",
                    first = l.First?.ToString("yyyy-MM-dd"),
                    last = l.Last?.ToString("yyyy-MM-dd"),
                }).ToList<object>()
                : null,
            // The page passes this straight back to /cluster/{root}; in a hosted registry the
            // identity IS the cluster root, so the round trip stays consistent.
            clusterRoot = side.identityId,
            clusterSize = side.clusterSize,
        };
    }

    private async Task<object> WithIdentities(List<object> hits, CancellationToken ct) => await Task.FromResult<object>(hits);

    private async Task<HashSet<string>> QueuedKeys(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0) return [];
        var found = await db.Set<StewardQueueEntry>().AsNoTracking()
            .Where(e => list.Contains(e.PairKey)).Select(e => e.PairKey).ToListAsync(ct);
        return found.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, List<LabelAudit>>> AuditsByKey(IEnumerable<string> keys, CancellationToken ct)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0) return [];
        var rows = await db.Set<LabelAudit>().AsNoTracking()
            .Where(a => list.Contains(a.PairKey)).ToListAsync(ct);
        return rows.GroupBy(a => a.PairKey).ToDictionary(g => g.Key, g => g.OrderBy(a => a.AuditedUtc).ToList());
    }

    private static object Last(List<LabelAudit> list)
    {
        var a = list[^1];
        return new { newLabel = a.NewLabel, status = a.Status, auditedBy = a.AuditedBy, date = a.AuditedUtc.ToString("O") };
    }

    private string Schema() => options.Schema ?? "Darlastic";

    /// <summary>The host's plain name for a source system, or the slug when it gives none.</summary>
    private string SourceName(string system) => options.SourceLabels.GetValueOrDefault(system) ?? system;

    private static string PairKeyOf((string sys, string id) a, (string sys, string id) b)
    {
        string ka = $"{a.sys}:{a.id}", kb = $"{b.sys}:{b.id}";
        return string.CompareOrdinal(ka, kb) <= 0 ? $"{ka}~{kb}" : $"{kb}~{ka}";
    }

    private static RealRecord? Revive(StewardRecord? row)
    {
        if (row is null) return null;
        try { return JsonSerializer.Deserialize<RealRecord>(row.Payload, PayloadJson); }
        catch { return null; }
    }

    private bool Matches((RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize) side, string needle)
        => side.rec is not null && Matches(side.rec, needle);

    private int? GapOf(CaseCatalogEntry r,
        Dictionary<(string, string), (RealRecord? rec, (string sys, string id) key, long? identityId, int clusterSize)> sides)
    {
        var a = sides.GetValueOrDefault((r.SourceSystemA, r.SourceRecordIdA)).rec;
        var b = sides.GetValueOrDefault((r.SourceSystemB, r.SourceRecordIdB)).rec;
        if (a?.Dob is { } da && b?.Dob is { } dbo && da.Year > 0 && dbo.Year > 0)
            return Math.Abs(da.Year - dbo.Year);
        return null;
    }

    /// <summary>
    /// The page's birth-date filter, with the values the page sends — the same ones the standalone
    /// server understands. This used to bucket by year gap under different names, so every value
    /// the page sent matched nothing here.
    /// </summary>
    private static bool DobMatches(string dob, CaseCatalogEntry row, RealRecord? a, RealRecord? b)
    {
        var flags = (MatchFlags)row.Flags;
        static bool July1(DobInfo? d) => d is { } x && ((x.P1 == 1 && x.P2 == 7) || (x.P1 == 7 && x.P2 == 1));
        int? gap = a?.Dob is { } da && b?.Dob is { } dbo && da.Year > 0 && dbo.Year > 0 ? Math.Abs(da.Year - dbo.Year) : null;
        return dob switch
        {
            "conflict" => (flags & MatchFlags.DobConflict) != 0,
            "equal" => (flags & MatchFlags.DobEqual) != 0,
            "gap10" => gap >= 10,
            "july1" => July1(a?.Dob) || July1(b?.Dob),
            "none" => a?.Dob is null || b?.Dob is null,
            _ => true,
        };
    }

    private static long MaskOf<TEnum>(string? csv) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv)) return 0;
        long mask = 0;
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Enum.TryParse<TEnum>(part, true, out var v)) mask |= Convert.ToInt64(v);
        return mask;
    }

    private static List<string> NamesOf<TEnum>(long mask) where TEnum : struct, Enum =>
        [.. Enum.GetValues<TEnum>()
            .Where(v => Convert.ToInt64(v) != 0 && (mask & Convert.ToInt64(v)) != 0)
            .Select(v => v.ToString()!)];
}

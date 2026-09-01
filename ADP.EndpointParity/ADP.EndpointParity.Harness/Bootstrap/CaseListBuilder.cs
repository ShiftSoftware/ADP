using System.Text.RegularExpressions;
using ShiftSoftware.ADP.EndpointParity.Harness;

namespace ShiftSoftware.ADP.EndpointParity.Harness.Bootstrap;

// ============================================================================================
// WIRING LAYER. The Harness/ purity rule does NOT apply here.
// ============================================================================================

/// <summary>
/// Turns the route catalogue into the case list.
///
/// <para>
/// <b>Driven FROM the catalogue, never from a hand-written URL template.</b> That rule is not
/// bureaucratic — it is load-bearing, and this repo proves it. The plan's own worked template says
/// the print routes are <c>{Entity}/{id}/print</c> and <c>{Entity}/{id}/printtoken</c>; the
/// application actually declares <c>{Entity}/print/{key}</c> and <c>{Entity}/print-token/{key}</c>.
/// It also declares two inherited routes the template never mentions —
/// <c>{Entity}/{key}/attention</c> and <c>{Entity}/{key}/attention/clear</c>. A template-driven
/// harness would have issued four wrong URLs, collected four fallback-HTML 200s, and reported
/// green while testing nothing.
/// </para>
///
/// <para>
/// Enumerating a route is not exercising it, so this class also carries the coverage rule: every
/// catalogue entry must resolve to at least one case, or be listed in <c>parity.psd1</c>'s
/// <c>excludedRoutes</c> with a written reason. <see cref="Uncovered"/> is what the gate reads.
/// </para>
/// </summary>
public sealed class CaseListBuilder
{
    private readonly string routePrefix;
    private readonly IReadOnlyCollection<string> excludedRoutes;
    private readonly bool emitAsOfCases;
    private readonly int listTop;

    /// <param name="emitAsOfCases">
    /// False where the group's tables are not system-versioned. The inherited asOf route emits
    /// FOR SYSTEM_TIME SQL, which 500s on a plain table - a pre-existing condition in at least one
    /// group, where entities carry [TemporalShiftEntity] but nothing ever calls .IsTemporal(true).
    /// Emitting the case anyway would bank a 500 into the baseline and blow the "0 5xx" gate for a
    /// reason that has nothing to do with the upgrade.
    /// </param>
    /// <param name="listTop">
    /// The $top to request on list cases. NOT a constant: the framework caps page size per
    /// principal, and the read-only grant used for the restricted pass rejects $top=25 outright
    /// with "The requested number of records (25) exceeds the maximum allowed limit of 5".
    /// A single hard-coded value makes every restricted list case 400 and the hostile-row gate
    /// then fails for a reason that has nothing to do with the seed.
    /// </param>
    public CaseListBuilder(string routePrefix, IReadOnlyCollection<string> excludedRoutes,
                           bool emitAsOfCases = true, int listTop = 25)
    {
        this.routePrefix = routePrefix.Trim('/');
        this.excludedRoutes = excludedRoutes;
        this.emitAsOfCases = emitAsOfCases;
        this.listTop = listTop;
    }

    /// <summary>Catalogue routes INSIDE this group that produced no case. A gap, not a default.</summary>
    public List<string> Uncovered { get; } = new();

    /// <summary>
    /// Routes outside this group's own route prefix - the ShiftIdentity dashboard surface, the
    /// auth endpoints, the Blazor fallback. They belong to other packages, so this harness makes
    /// no parity claim about them and does not count them against the group's coverage. They are
    /// still REPORTED, so "not covered" is a visible decision rather than a silent omission.
    /// </summary>
    public List<string> OutOfScope { get; } = new();

    /// <summary>
    /// Builds the case list.
    /// </summary>
    /// <param name="routes">Everything the booted app declares.</param>
    /// <param name="seededHashIdsByEntity">
    /// Per controller-route segment, the hash ids of seeded rows. DETAIL and REVISIONS are emitted
    /// once per SEEDED ROW, not once per entity: trap 1 is visible only on the detail body of the
    /// parent that owns a soft-deleted child, so a single id per entity makes it fire by luck.
    /// </param>
    /// <param name="createBodies">Per entity, the hand-authored minimal-valid body plus overlay.</param>
    public IReadOnlyList<ParityCase> Build(
        IReadOnlyList<CatalogueRoute> routes,
        IReadOnlyDictionary<string, IReadOnlyList<string>> seededHashIdsByEntity,
        IReadOnlyDictionary<string, string> createBodies,
        IReadOnlyDictionary<string, string> updateBodies)
    {
        var cases = new List<ParityCase>();

        foreach (var route in routes)
        {
            if (excludedRoutes.Contains(route.Key)) continue;

            var entity = EntityOf(route.Template);
            if (entity is null) { OutOfScope.Add(route.Key); continue; }

            var tail = TailOf(route.Template, entity);
            var ids = seededHashIdsByEntity.TryGetValue(entity, out var e) ? e : Array.Empty<string>();
            var before = cases.Count;

            switch (route.Method, tail)
            {
                // ---- collection routes ---------------------------------------------------
                case ("GET", ""):
                    // Rule 4: an explicit $orderby on EVERY list. Without it OData order is
                    // unspecified and two identical runs diff on ordering alone.
                    cases.Add(New(entity, "LIST", "GET",
                        "/" + routePrefix + "/" + entity + "?$orderby=ID&$top=" + listTop + "&$count=true", route.Key));
                    break;

                case ("POST", "") when createBodies.ContainsKey(entity):
                    cases.Add(New(entity, "CREATE", "POST",
                        "/" + routePrefix + "/" + entity, route.Key, createBodies[entity]));
                    cases.Add(New(entity, "READBACK.afterCreate", "GET",
                        "/" + routePrefix + "/" + entity + "/{newId}", route.Key, needsCreatedId: true));
                    break;

                // ---- item routes, one case per SEEDED ROW --------------------------------
                case ("GET", "/{key}"):
                    foreach (var id in ids)
                    {
                        cases.Add(New(entity, "DETAIL." + id, "GET",
                            "/" + routePrefix + "/" + entity + "/" + id, route.Key));
                        // asOf runs MapToViewGenerated over a temporal snapshot - a genuinely
                        // distinct mapper path, so it is its own case rather than a variant.
                        // Skipped where the group's tables are not system-versioned (see ctor).
                        if (emitAsOfCases)
                            cases.Add(New(entity, "ASOF." + id, "GET",
                                "/" + routePrefix + "/" + entity + "/" + id + "?asOf=2099-01-01T00:00:00Z", route.Key));
                    }
                    break;

                case ("GET", "/{key}/revisions"):
                    foreach (var id in ids)
                        cases.Add(New(entity, "REVISIONS." + id, "GET",
                            "/" + routePrefix + "/" + entity + "/" + id + "/revisions", route.Key));
                    break;

                case ("GET", "/{key}/attention"):
                    foreach (var id in ids)
                        cases.Add(New(entity, "ATTENTION." + id, "GET",
                            "/" + routePrefix + "/" + entity + "/" + id + "/attention", route.Key));
                    break;

                case ("PUT", "/{key}") when updateBodies.ContainsKey(entity):
                    cases.Add(New(entity, "UPDATE", "PUT",
                        "/" + routePrefix + "/" + entity + "/{newId}", route.Key,
                        updateBodies[entity], needsCreatedId: true));
                    cases.Add(New(entity, "READBACK.afterUpdate", "GET",
                        "/" + routePrefix + "/" + entity + "/{newId}", route.Key, needsCreatedId: true));
                    break;

                case ("DELETE", "/{key}"):
                    // DELETE and the GONE check that follows run against the CREATED row, never a
                    // seeded one - deleting a seeded row would change every later list body and
                    // make the run order-dependent.
                    cases.Add(New(entity, "REMOVE", "DELETE",
                        "/" + routePrefix + "/" + entity + "/{newId}", route.Key, needsCreatedId: true));
                    cases.Add(New(entity, "GONE", "GET",
                        "/" + routePrefix + "/" + entity + "/{newId}", route.Key, needsCreatedId: true));
                    // $top is MANDATORY, not decorative: the framework refuses an unbounded query
                    // with "Please specify a page size using the $top query parameter. You do not
                    // have permission to load unrestricted data sets." Omitting it here made this
                    // one case 400 while every other list succeeded.
                    cases.Add(New(entity, "LIST.afterRemove", "GET",
                        "/" + routePrefix + "/" + entity + "?$orderby=ID&$top=" + listTop + "&$count=true", route.Key));
                    break;

                // ---- print: Rule 7, PARTIAL, and note the ACTUAL template shape ----------
                case ("GET", "/print/{key}"):
                    foreach (var id in ids.Take(1))
                        cases.Add(New(entity, "PRINT." + id, "GET",
                            "/" + routePrefix + "/" + entity + "/print/" + id, route.Key));
                    break;

                case ("GET", "/print-token/{key}"):
                    foreach (var id in ids.Take(1))
                        cases.Add(New(entity, "PRINTTOKEN." + id, "GET",
                            "/" + routePrefix + "/" + entity + "/print-token/" + id, route.Key));
                    break;
            }

            // Hand-written actions land here: Darlastic's 31, Surveys' anonymous renderer surface,
            // and so on. They are NOT silently skipped - they surface in the coverage gate so
            // somebody has to either cover them or write down why not.
            if (cases.Count == before)
                Uncovered.Add(route.Key);
        }

        // ---- lifecycle ordering ------------------------------------------------------------
        // Cases are BUILT in catalogue order but must RUN in round-trip order. The catalogue sorts
        // /Entity/{key} by method, which puts DELETE before PUT - so the created row was being
        // deleted before UPDATE could touch it, and UPDATE sat at 0/n while looking like a write
        // regression rather than a harness ordering bug. Reads first (against seeded rows), then
        // the create/update/delete lifecycle against the row this run creates.
        var rank = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["LIST"] = 0, ["DETAIL"] = 1, ["ASOF"] = 2, ["REVISIONS"] = 3, ["ATTENTION"] = 4,
            ["PRINT"] = 5, ["PRINTTOKEN"] = 6,
            ["CREATE"] = 10, ["READBACK"] = 11, ["UPDATE"] = 12,
            ["REMOVE"] = 20, ["GONE"] = 21,
        };

        int RankOf(ParityCase c)
        {
            // READBACK appears twice; the one after UPDATE must follow it, not precede it.
            if (c.Name.EndsWith(".READBACK.afterUpdate", StringComparison.Ordinal)) return 13;
            if (c.Name.EndsWith(".LIST.afterRemove", StringComparison.Ordinal)) return 22;
            return rank.TryGetValue(c.Kind, out var r) ? r : 50;
        }

        return cases
            .OrderBy(c => c.Name.Split('.')[0], StringComparer.Ordinal)   // group by entity
            .ThenBy(RankOf)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
    }

    private ParityCase New(string entity, string kind, string method, string url, string routeKey,
                           string? body = null, bool needsCreatedId = false) =>
        new()
        {
            Name = entity + "." + kind,
            Kind = kind.Split('.')[0],
            Method = method,
            Url = url,
            Body = body,
            RouteKey = routeKey,
            NeedsCreatedId = needsCreatedId,
        };

    /// <summary>The controller segment immediately after the configured route prefix.</summary>
    private string? EntityOf(string template)
    {
        var t = template.Trim('/');
        if (!t.StartsWith(routePrefix + "/", StringComparison.OrdinalIgnoreCase)) return null;

        var rest = t.Substring(routePrefix.Length + 1);
        var segment = rest.Split('/')[0];

        // A parameter in the first position means this is not an entity controller.
        return segment.StartsWith('{') ? null : segment;
    }

    /// <summary>Everything after the entity segment, with route constraints stripped.</summary>
    private string TailOf(string template, string entity)
    {
        var t = template.Trim('/');
        var prefix = routePrefix + "/" + entity;
        var tail = t.Length <= prefix.Length ? "" : t.Substring(prefix.Length);

        // {publicId:guid} -> {key}: constraints are noise for classification, and the framework
        // is inconsistent about naming the id parameter ({key} vs {id}).
        tail = Regex.Replace(tail, @"\{[^}]+\}", "{key}");
        return tail;
    }
}

# Verification — the endpoint-parity harness

How "all endpoints work as before" gets **proven** rather than asserted.

> **Scope note:** this harness is deliberately temporary — it is deleted in **Step 08**. Read §9
> before deciding how much to invest in any part of it.

---

## 1. The requirement, stated precisely

**A harness that checks HTTP 200 is worse than useless here — it is actively misleading.** Every
regression this upgrade can produce returns 200 with a well-formed body of the correct shape:

| Trap | Status | Shape | Values |
|---|---|---|---|
| 1 — soft-deleted children no longer filtered | 200 | same | **child array grows** |
| 2 — link-row PK leaks into a child's `ID` | 200 | **same** | **a real id of the wrong entity** |
| 3-write — ignored member now written from body | 200 | same | **column overwritten by client input** |
| 3-read — forward-map `Ignore()` now convention-matched | 200 | same | **blanked fields become populated** |

Trap 2 is the worst: the wrong value is a well-formed, plausible hash id. Nothing short of comparing
it against a known-good value distinguishes it from correct.

> **The unit of comparison is therefore the full normalized response body, byte for byte after
> normalization.** Not a status code, not a schema, not a field allowlist.

---

## 2. Shape: one harness library, one test project per group, one PowerShell driver

**`ADP.EndpointParity.Harness`** — a class library holding every piece of harness logic, with
**zero** references to any group. **`ADP.EndpointParity.<Group>`** — five thin xUnit v3 projects, one
per group, each referencing only its own `.API` / `.Data` plus the harness. `tools/parity.ps1` is the
operator interface. All logic lives in .NET; the script is a driver, not the harness.

### Why per-group projects and not one project with a `--filter`

**Because a single project cannot compile while any group is mid-migration.** Group selection has to
be a *compile-time* boundary, not a runtime filter.

Under the staged order every step *ends* green — each group step bumps its own package references as
its first commit (work item A) and lands its migrated mappers in the same step — so at step
**boundaries** a single assembly would build fine. The window that matters is **inside** each group
step. The moment item A's bump lands, that group's `: Profile` classes stop compiling; they are gone
only when the migration later in the same step removes them. `Surveys.Data`, `ClaimableItems.Data`
and `WarrantyClaims.Data` are exactly the three projects holding `: Profile` classes, so Steps 03,
04 and 05 each open one such window, in their own group.

A single harness assembly referencing all five groups' `.Data` would be unbuildable for the whole of
each of those windows — which is precisely when the instrument is most needed. Mid-Step-04 you could
run `verify` for **no** group: not `ClaimableItems`, whose migration you are in the middle of and
whose baseline you may need to re-inspect or re-capture; not `Darlastic`, the framework-only control
(§6) your attribution depends on; not `Menus`, the harness's own validation case. Worse, a compile
error in one group's `.Data` would present as *"the parity harness does not build"*, destroying the
ability to attribute a failure to the group that caused it — and a rollback that leaves one group
half-reverted would take the harness down for all five.

The split keeps every red window contained to the one project that owns it. That argument is weaker
than it was under an atomic solution-wide bump — the red windows are now intra-step and short rather
than spanning whole steps — but the conclusion is unchanged, and the cost is one csproj per group.

`parity.ps1 -Group X` targets project X. Baselines, seeds and config stay in one shared tree, so
nothing is duplicated but the csproj.

### Why not pure PowerShell against `dotnet run`

Tempting, because the script never recompiles and so provably cannot change between runs. But:

- **It cannot reach 3 of 6 groups.** `ADP.ClaimableItems`, `ADP.WarrantyClaims` and `ADP.Cases` have
  no process to call. A PowerShell harness would need three new sample hosts written first — strictly
  more work than the mounted host below.
- **It cannot enumerate inherited routes.** Covering base-controller routes rather than a
  hand-written URL list requires asking the running app for
  `IActionDescriptorCollectionProvider`. From outside the process you are back to a hand-maintained
  list — precisely the list that will omit the route that broke.

### Why a compiled harness is acceptable

The valid objection is that it recompiles against the new packages, so the harness itself could
change behaviour. The mitigation is an architectural rule, enforced by review:

> **The capture layer — everything under `Harness/` — touches only `HttpClient`,
> `System.Text.Json` and `string`.** No `ShiftEntityResponse<T>`, no DTO types, no `IMapper`, no
> repositories. It reads bytes off the wire and normalizes text. `Bootstrap/` is explicitly outside
> this rule and is reviewed by reading its diff instead (§3).

Under that rule the observation code is version-independent even though it compiles twice. What
legitimately differs between compilations is the host bootstrap — for Surveys, exactly three lines
(§6) — and that difference is visible in the diff of the harness source, which you review.

### On `ShiftSoftware.ShiftFrameworkTestingTools`

It already exists and is exactly this shape: `ShiftCustomWebApplicationFactory<,>`,
`ShiftCustomWebApplicationBearerAuthSettings` (mints a JWT from the configured issuer/key plus
`TypeAuthActions`), and `BasicTest<,>` with `Get` / `PostOrPut` / `Delete` / `OdataList` /
`RevisionList` / `ParseResponse`.

**But it is published only to `2026.7.28.1`** and pins ShiftEntity `2026.7.28.1`. On the post-upgrade
side NuGet must unify its dependency up by two releases, across the AutoMapper removal. That is
**SPIKE-1**. Spend 30 minutes proving it binds before designing around it. If it does not, its value
is as a *specification* — those six method names are exactly the inherited route set to cover — and
you re-implement the JWT minting yourself. **Do not block the harness on a package you do not
control.**

---

## 3. Layout

```
ADP.EndpointParity/
  ADP.EndpointParity.Harness/          # class library. NO group reference of any kind.
    ADP.EndpointParity.Harness.csproj
    Harness/                           # the CAPTURE LAYER — purity rule applies here
      ParityRunner.cs                  # drives a case list, writes/compares transcripts
      Normalizer.cs                    # ALL normalization rules, one file, heavily commented
      Transcript.cs                    # the on-disk record (request + response), JSON
      TranscriptDiffer.cs              # canonical-JSON structural diff, human-readable output
    Bootstrap/                         # the WIRING LAYER — purity rule does NOT apply here
      RouteCatalog.cs                  # IActionDescriptorCollectionProvider -> canonical route list
      RequestFactory.cs                # reflection over DTO types + seed overlay (§5)
      SampleHostFactory.cs             # WebApplicationFactory<TProgram> over a real sample API
      MountedHostFactory.cs            # synthetic host via Add<Group>ApiServices (§6)
      ParityDb.cs                      # ShiftDbContext; model contributors self-apply
      ParityAuth.cs                    # mints TWO principals: FullAccess and Restricted (§8.7)
  ADP.EndpointParity.Menus/            # xunit.v3 + Mvc.Testing; refs Harness + ADP.Menus.{API,Data}
  ADP.EndpointParity.Surveys/          #   ... + ADP.Surveys.{API,Data}
  ADP.EndpointParity.Darlastic/        #   ... + ADP.Darlastic.{API,Data}
  ADP.EndpointParity.ClaimableItems/   #   ... + ADP.ClaimableItems.{API,Data}
  ADP.EndpointParity.WarrantyClaims/   #   ... + ADP.WarrantyClaims.{API,Data}
  Seed/
    <group>.seed.json                  # deterministic rows, EXPLICIT long IDs (rule 1),
                                       #   hostile rows tagged (§5, §8.2)
    <group>.<Entity>.create.json       # hand-authored MINIMAL VALID create body (§5)
  baselines/                           # COMMITTED, reviewed like source. Shared across the 5 projects.
  reports/                             # gitignored; verify-mode diff output
tools/
  parity.ps1                           # the driver
  parity.psd1                          # per-group config: route prefixes, order-insensitive
                                       #   collections, excludedRoutes, writeUnreachable,
                                       #   restricted-grant action set
```

`baselines/` is committed. **A changed golden in a PR is a claimed behaviour change that must be
explained in the commit message.** That review is the whole control.

**Where the purity rule bites.** §2's rule — HTTP, JSON and `string` only, no framework or DTO types
— applies to `Harness/`, and nothing else. It **cannot** apply to `Bootstrap/`: `RequestFactory` must
reflect over the group's DTO types to fill them, and must recognise
`ShiftSoftware.ShiftEntity.Model.HashIds.JsonHashIdConverterAttribute` to know which members need a
real hash id rather than a string sentinel; the host factories obviously reference the group. Writing
the exit criterion as "no framework type anywhere under the parity project" would be unsatisfiable,
and unsatisfiable criteria get satisfied by weakening the grep. **Grep `Harness/` only.**

---

## 4. Normalization rules

Normalization is where this harness is won or lost. **Over-normalize and you erase the regression;
under-normalize and the diff is unreadable noise that gets ignored.** Bias hard toward
under-normalizing: an unexplained diff you have to look at beats a silent pass.

### Rule 1 — Do not normalize IDs. Make them deterministic instead.

IDs are the payload of trap 2. Replacing them with `<id>` deletes the signal.

- Seed with **explicit long primary keys** from `Seed/<group>.seed.json`. Same longs both runs.
- Pin the hash-id salt and minimum length in the parity host config. Same salt + same long ⇒ same
  hash id, so seeded IDs compare **literally** and a wrong `ID` is a diff.
- IDs the harness *creates* (POST responses) **compare literally too, by default.** §7 gives every
  run its own fresh database and the seed is deterministic, so identity values for created rows are
  deterministic as well. An alias map is normalization this harness does not need — and it is
  precisely the normalization that erases trap 2 on the write path: if a `POST` creates parent `P`
  and child `C`, and under trap 2 `C.ID` comes back carrying `P`'s PK, a bare counter renders both
  the correct and the wrong value as `<new:1>` and **no diff fires**.
- **Only if run-to-run drift is actually observed** — and only after trying to make the value
  deterministic instead — fall back to an alias keyed on the **JSON path of first occurrence**
  (`<new:CREATE.body.ID>`, `<new:CREATE.body.Items[0].ID>`), never a bare counter, so a child slot
  carrying its parent's id normalizes to a *different* token than the parent's own slot. **If the
  alias map's size or key set changes between runs, that is itself a diff.**
- Consequently, **trap-2 coverage comes from seeded hostile link rows, not from created ones**
  (§8.2). Say so out loud rather than letting the write-path round-trip imply coverage it does not
  have.

### Rule 2 — Timestamps: allowlist by name, plus a recency guard

There is **no injectable clock** — `DateTimeOffset.UtcNow` is read directly inside
`ShiftEntity.EFCore`, and no `TimeProvider`/`IClock` seam exists. Audit stamps genuinely differ run
to run and can only be normalized, never frozen.

- **Name allowlist only:** `CreateDate`, `LastSaveDate`, and `ValidFrom`/`ValidTo` **inside a
  `Revisions` array only** → `<ts>`.
- Everything else that is a date is **compared literally**. Business dates are exactly what a mapper
  regression corrupts — and both WarrantyClaims profiles perform a hand-written
  `DateTime → DateTimeOffset` conversion. If the generator does that differently (offset, kind,
  precision), it must show.
- Safety net: any *other* value parsing as a timestamp inside `[runStart − 5min, now]` is **flagged
  as suspected volatile in the report but not normalized**. Classify it once and add it to the
  allowlist deliberately.

### Rule 3 — Identity, revisions, headers

- `CreatedByUserID` / `LastSavedByUserID`: the seeded principal is deterministic → compare literally.
  Normalize only if drift is observed.
- Revision arrays: normalize the timestamps; **keep the count and the ordering**. A revision-count
  change means the write path changed.
- Capture only `status`, `Content-Type` and a per-group header allowlist. Drop `Date`, `Server`,
  `Set-Cookie`, `Content-Length`, `Request-Context`, `traceparent`. `ETag` → `<etag>`.
- `ProblemDetails.traceId` → `<traceid>`. Detailed errors are on in the samples, so truncate
  `.detail`/`.exception` to its first line — a changed exception *type* is signal, a changed stack
  offset is not.

### Rule 4 — Ordering

- **Every list request carries an explicit `$orderby`** (default `$orderby=ID`). Without it OData
  order is unspecified and every run diffs.
- **Child collections preserve source order by default.** Sort order is semantic in several places —
  sorting collections to "stabilize" them would erase an ordering regression.
- Collections whose order is genuinely irrelevant go in an explicit per-group `orderInsensitive`
  list in `parity.psd1`. **Adding an entry there is a deliberate act, reviewed in the PR.**

### Rule 5 — Canonical JSON that preserves the distinctions that matter

- Keys sorted, 2-space indent, UTF-8, LF — so `git diff` on goldens is readable.
- **`null` and absent are NOT collapsed.** The generator may emit a property AutoMapper omitted
  entirely, or vice versa. That is a wire-contract change a consumer can see.
- **Numbers compare as written text, not as parsed values.** `1.0` vs `1.00` vs `1` on a `decimal?`
  money field is a real serialization change worth seeing on financial DTOs.
- **Empty array vs null array: distinct.** Trap 1 turns `[]` into `[{…soft-deleted…}]`; the adjacent
  failure turns `null` into `[]`.

### Rule 6 — Culture pinned, then varied

Send `Accept-Language: en-US` on every request. Then run **one extra pass per group at a second
culture** — number and date formatting differences would otherwise hide inside a single-culture
baseline.

### Rule 7 — Binary responses are a declared gap

Export and print endpoints return binaries. `.xlsx` is a zip with embedded timestamps and is not
byte-reproducible. Record `content-type` + a size band + a SHA-256 of the *sorted extracted sheet
XML* with the core-properties part stripped. If that is still unstable, record content-type and size
band only and **mark the case `PARTIAL` in the report**. Do not let it silently pass as covered.
(SPIKE-10.)

---

## 5. Coverage: inherited routes and write-path round-trips

### Route enumeration is generated, not written

At host startup the harness resolves `IActionDescriptorCollectionProvider` and emits a **route
catalogue** — method, template, controller, action, parameter types — which is itself a golden file.
That gives three things free:

1. Inherited `ShiftEntityControllerAsync` routes (`GET` list, `GET /{id}`, `POST`, `PUT /{id}`,
   `DELETE /{id}`, `GET /{id}/revisions`, `Print`) appear without anyone listing them.
2. A route that **disappears** in the upgrade is caught by the catalogue diff — which a URL-driven
   harness would never notice.
3. Route-prefix conventions are exercised as configured rather than assumed.

### The case list is driven *from* the catalogue, not from a template

Enumerating a route is not exercising it. The templated CRUD list below covers the inherited
`ShiftEntityControllerAsync` surface and nothing else — but the groups carry a large hand-written
surface that the template never touches: counted from `[Http(Get|Post|Put|Delete|Patch)]` attributes
in each group's `API/Controllers`, **Surveys 12, Menus 13, WarrantyClaims 14, ClaimableItems 3,
Darlastic 31** hand-written actions. Among them is the entire anonymous renderer surface —
`ADP.Surveys.API/Controllers/PublicSurveyController.cs`, `[Route("SurveyInstances")]`
`[AllowAnonymous]`, `[HttpGet("{publicId:guid}/schema")]` — in the one group this plan calls "full
HTTP parity".

> **Rule: every entry in the route catalogue must resolve to at least one case, or appear in an
> explicit `excludedRoutes` list in `parity.psd1` with a written reason.** `summary` reports
> `catalogue routes covered: n/n, excluded: k`. An uncovered route is a gap, not a default.

### The round-trip case list, per entity

```
LIST      GET    {prefix}/{Entity}?$orderby=ID&$top=25&$count=true
DETAIL    GET    {prefix}/{Entity}/{id}            <- one per SEEDED ROOT ROW, not one per entity
REVISIONS GET    {prefix}/{Entity}/{id}/revisions  <- likewise
ASOF      GET    {prefix}/{Entity}/{id}?asOf=<fixed instant>   <- distinct mapper path (temporal)
PRINT     GET    {prefix}/{Entity}/{id}/print      <- PARTIAL, rule 7
PRINTTOKEN GET   {prefix}/{Entity}/{id}/printtoken
CREATE    POST   {prefix}/{Entity}            <- minimal-valid body + sentinel overlay
READBACK  GET    {prefix}/{Entity}/{newId}    <- THE ASSERTION
UPDATE    PUT    {prefix}/{Entity}/{newId}    <- same, different sentinels
READBACK  GET    {prefix}/{Entity}/{newId}    <- THE ASSERTION
REMOVE    DELETE {prefix}/{Entity}/{newId}
GONE      GET    {prefix}/{Entity}/{newId}
LIST      GET    {prefix}/{Entity}?$orderby=ID&$count=true   <- does the row count move?
```

Three things about that list are deliberate and were wrong in an earlier draft:

- **`DETAIL` and `REVISIONS` run per seeded root row, not once per entity.** Trap 1 is a *view*-DTO
  phenomenon: the concrete site in this repo is
  `ADP.Menus.Data/Repositories/MenuVariantRepository.cs:72-74`, whose
  `.ForViewChildren(d => d.Items, e => e.Items.Where(mi => !mi.IsDeleted && …))` predicate is visible
  **only** on `GET MenuVariant/{id}` for the variant that owns a soft-deleted item. With a single
  `{seededId}` per entity, trap 1 fires by luck. So: tag hostile rows in the seed
  (`"hostile": ["trap1","trap2"]`) and emit a `DETAIL` case for **every** tagged row — cheapest
  correct version, for every seeded root row.
- **`asOf` and `PrintToken` are inherited base routes** that the earlier list omitted, visible from
  the overrides at `ADP.Surveys.API/Controllers/SurveyInstanceController.cs:54-73`. `asOf` runs
  `MapToViewGenerated` over a temporal snapshot — a genuinely distinct mapper path.
- **The final `LIST` is not a trap-1 detector** and must not be labelled one. A moving row count
  there is the framework's global `IsDeleted` query filter, not the mapper. Trap 1 shows in a child
  array on a `DETAIL` body.

### Write-path reachability is per triple, and must be recorded

Some triples have no reachable HTTP write path at all. `SurveyInstanceController.cs:54-73` overrides
`GetSingle`, `Post`, `Put`, `Delete`, `GetRevisions`, `Print` and `PrintToken` to return **405** —
yet `SurveyInstanceRepository` is a real triple whose `MapToEntityGenerated` is live, driven from the
public submit and trigger-ingest paths. Left as-is, the harness produces four 405 transcripts, passes
`0 5xx`, and covers that write mapper **not at all**.

> **For every triple, record `httpWriteReachable: true | false` in `parity.psd1`. Where it is false,
> the substitute is a mapper-level golden test** — assert `MapToEntityGenerated`'s written member set
> against the old reverse map, the same device `05-warranty-claims.md` already uses for the dealer
> list DTO. **Every triple has either an HTTP write round-trip or a mapper-level write golden.**

### The critical design point — and why the body cannot be pure sentinel

**A request body containing only fields a client would legitimately send cannot detect trap 3-write.**
An ignored member is invisible unless you send it. So the body must carry sentinels in members no
client would set.

**But a body that is *entirely* sentinel never reaches the mapper.** The canonical trap-3-write
instance in this repo is `ADP.Menus.Data/Repositories/MenuRepository.cs:39`
(`.IgnoreEntity(e => e.BrandID)`), whose derivation lives in `UpsertAsync`: it does
`dto.VehicleModel.Value.ToLong()`, looks the model up, and **throws a 404 `ShiftEntityException`**
when it is not found. A `PARITY::VehicleModel.Value` string never gets that far. Separately
`MenuDTO.cs:16-17` decorates `BrandID` with a hash-id converter, so a `PARITY::` string will not even
deserialize, and `MenuDTOValidator` hard-requires `VehicleModel`. The same shape recurs across the
groups (`WarrantyClaimListDTO`, `DistributorFinancialListDTO` carry hash-id members too). **A
baseline in which every `CREATE` 400s satisfies every gate in the earlier draft, replays identically,
and reports green with trap-3-write coverage of exactly zero.**

So the body is built in two layers:

1. **A hand-authored minimal-valid create body**, committed per entity as
   `Seed/<group>.<Entity>.create.json`, satisfying FK resolution, hash-id deserialization and
   FluentValidation. This is slow, careful work and there is no way around it.
2. **A sentinel overlay** applied by `RequestFactory` to every writable member the minimal body does
   *not* need for validity:

| Type | Sentinel |
|---|---|
| `string` | `PARITY::{PropertyPath}` |
| numeric | `900000 + stableHash(path) % 90000`, scaled — collides with nothing real |
| `bool` | `true` |
| enum | second declared value, not the default |
| `DateTimeOffset` | a fixed far-future instant |
| collection | exactly one element, recursively filled, depth-capped |
| **hash-id member** (carries a `JsonHashIdConverterAttribute`) | **a different seeded row's real hash id** — never `PARITY::…`, which cannot decode |

Then the readback tells you everything. Old mapper ignored the member → readback shows the
**repository-derived** value. New mapper writes it by convention → readback shows the **sentinel**. A
sentinel appearing in a readback is unmistakable and self-explaining in a diff.

> **Gate: every `CREATE` and `UPDATE` case in every baseline is 2xx, or its entity appears in an
> explicit `writeUnreachable` list in `parity.psd1` with a reason.** `summary` prints
> `CREATE 2xx: n/m` and **fails below 100%**. Without this, the whole write-path apparatus can be
> dead and every gate still green.

**The generated request goes into the golden alongside the response.** If `RequestFactory` produces
different bytes across the two runs (a DTO changed, reflection order shifted), the *request* diff
fires first and tells you the comparison is invalid — instead of a response diff you misread as a
regression.

---

## 6. Per-group applicability, with the gaps named

| Group | Host today | Triples | Profiles | Mode | Gap / fallback |
|---|---|---|---|---|---|
| `ADP.Menus` | `samples/ADP.Menus.Sample.API` | 10 | migrated | **Sample host** — full HTTP parity | **Baseline is retroactive** — capture from a `git worktree` at `14caf7c9^`. This is the harness's own validation case (Step 01). Also: the sample maps a fallback file, so an unmatched route returns **200 + HTML, not 404** — the harness must treat "response is HTML" as a hard failure, or a deleted route passes silently. |
| `ADP.Surveys` | `samples/ADP.Surveys.Sample.API` | 4 | 1 (151 lines) | **Sample host** — full HTTP parity | Cleanest *mapper* case: host bootstrap differs by exactly three lines between runs (the two `Program.cs` AutoMapper calls plus the API-extensions one), with no response-shape effect. **But it carries the same fallback hazard as Menus** — `Program.cs:200,204` call `UseBlazorFrameworkFiles()` + `MapFallbackToFile("index.html")`, so a deleted or renamed route returns **200 + HTML, not 404**, right beside a `PublicSurveyController` that answers `NotFound()`. The "no response body may be `text/html`" assertion is a **global** rule built in Step 00, not a Menus special case. **And the host seeds itself** (`Program.cs:163-196`) — see §7. |
| `ADP.Darlastic` | `samples/ADP.Darlastic.Sample.API` | **0** | **0** | **Sample host — the framework-only control** | **Not a mapper-risk group** — plain `ControllerBase`, no `ShiftRepository`. Blockers: the host `return 1`s before `app.Run()` on missing config, and needs a populated registry DB the repo does not seed (SPIKE-5). **Its value is not "smoke" — it is the plan's single control** (see below). Still: do not claim *value* parity for it, because there is no mapping behaviour to compare. |
| `ADP.ClaimableItems` | **none** | 5 | 4 | **Mounted host** | No forward-map `Ignore()` ⇒ trap 3-read is low. Heavy JSON round-trips and select-DTO shuttling — **trap 2 territory**. Needs full value diffing. |
| `ADP.WarrantyClaims` | **none** | 7 | 2 | **Mounted host + a dedicated mapper-level golden** | **Highest risk in the repo.** Give it a standalone test asserting the five distributor members are null on the dealer list DTO — which is also the regression test after `IgnoreList` is added. Note the exposure itself **is** visible on the ordinary full-access pass (§8.7): `GET /DealerFinancial` is a separate route with its own DTO, not a filtered projection. |
| `ADP.Cases` | none (library) | **0** | **0** | **None — out of scope** | No controllers, no repository. Endpoint parity is not a meaningful concept here; covered by `ADP.Cases.Shared.Tests`. Its `Certificate` entity *is* mapped by two triples in the two groups above, so it is verified through them. |

### Why Darlastic is the plan's only control, not its throwaway

`ShiftEntityMapperValidation` throws at startup for any triple without a mapper, so **no mapper group
can ever be captured in a "bumped but not migrated" state.** Every Surveys / ClaimableItems /
WarrantyClaims diff therefore confounds two causes: the framework change and the mapper rewrite —
including the six compile-clean behaviour changes in `conventions.md` §10 (`AsNoTracking()` before
projection, `IsDeleted` restored on update, case-insensitive member matching, validation-error
wrapping, and the rest).

Darlastic, at 0 triples and 0 profiles, is the **only** group where a diff is unambiguously
*framework*-caused. That is what makes SPIKE-5 worth real effort rather than a cost-benefit skip: a
Darlastic capture is the baseline against which the mapper groups' diffs get attributed. If SPIKE-5
resolves negative, say plainly in `STATUS.md` that framework-level response changes are then
**inseparable** from mapper changes everywhere in the plan.

### The step order puts that control first

The group steps run **02 `ADP.Darlastic` → 03 `ADP.Surveys` → 04 `ADP.ClaimableItems` →
05 `ADP.WarrantyClaims`**, ascending in mapper risk, with the shared floor (06) after all of them.
Because each group step now carries its own package bump, Darlastic's packages reach `2026.8.30.1`
in Step 02 — **before any mapper group is touched**. The framework-only signal is therefore in hand
*before* Steps 03–05 produce their first confounded diff, instead of arriving alongside it. That is
the single biggest practical gain of the per-group bump for this harness.

Two ordering facts the table above does not show:

- **Step 03 (`ADP.Surveys`) is free-floating.** It references no other ADP group and consumes no
  `ShiftSoftware.ADP.*` package, so nothing downstream of it constrains its position; it sits at 03
  for risk ordering only and could run at any point after Step 01. Its harness project is
  correspondingly independent.
- **Step 05 depends on Step 04 for the shared `Certificate` mapper precedent (SPIKE-8) — a
  *knowledge* dependency, not a build one.** `ADP.Cases`' `Certificate` entity is mapped by a triple
  in each of the two groups; whichever answer Step 04 settles on, Step 05 must match. Nothing in
  `ADP.WarrantyClaims` fails to compile if Step 04 has not run. For the harness this means the two
  groups' baselines are independent and may be captured in either order, but their `Certificate`
  diffs must be read together.

### What the mounted host is, and honestly is not

It boots the module through its own public `Add<Group>ApiServices<TDbContext>(mvcBuilder, configure)`
entry point — the same one a tenant uses. Not a mock, not a reimplementation.

**But it is one notch below a sample host, and here is the notch:** it does not reproduce a
consumer's middleware order, request localization, CORS, fallback routing, dashboard hosting, or JSON
options a real host might override. **A behaviour change hiding in host wiring rather than in the
module will not be caught.** For an upgrade whose risk is concentrated in the mapper that is an
acceptable trade. It would *not* be acceptable for an upgrade touching serialization, routing or
auth. Say so in the step's exit criteria rather than claiming full endpoint parity.

To close that gap for WarrantyClaims specifically, write
`ADP.WarrantyClaims/samples/ADP.WarrantyClaims.Sample.API` mirroring the Surveys sample (~200 lines
plus a disposable DB; skip migrations entirely). Given what that group's Financial profile is hiding,
that is the group where the cost is most obviously justified.

**`ADP.ClaimableItems` gets the same named fallback**, and an earlier draft left it with a dead end —
"if the mounted host does not work this step is `BLOCKED`", and nothing after. That is the group
least able to afford no verification: its trap tally puts it squarely in trap-2 territory
(`ShiftEntitySelectDTO` shuttling). If the mounted host cannot boot it, the fallback is, in order:
(1) a `ADP.ClaimableItems/samples/ADP.ClaimableItems.Sample.API` on the same ~200-line pattern, or
(2) **mapper-level goldens per triple** — assert `MapToViewGenerated` / `__shiftListProjection` /
`MapToEntityGenerated`'s member sets against the old profile maps. Option 2's reduced claim, stated
in those words: *"mapper output verified at the type level; no HTTP surface was exercised."*

---

## 7. Commands

### Prerequisites — checked and reported by the script, not assumed

- SQL Server / SQL Express, integrated security. Required by every group.
- **Cosmos: not required.** The samples gate all Cosmos work on the connection string being
  configured; the parity host sets it empty and the whole replication + provisioning block is
  skipped. Do this — it also removes replication side effects from the write-path cases.
- **Blob emulator: not required** for the endpoints in scope. Keep storage accounts configured
  (registration is lazy) but exercise no blob endpoints.
- Each run creates and drops its own database (`ADP_Parity_<Group>_<runid>`) so runs cannot
  contaminate each other. The existing sample-seeding fixtures are a good source of realistic rows,
  but the parity seed must be **explicit-ID** — do not reuse them verbatim.
- **The parity host must suppress the sample's own seeding.** For the two groups with a real host the
  sample seeds itself unconditionally at startup, before the harness gets a say —
  `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:163-196` runs `EnsureCreatedAsync`,
  `SeedDBAsync(...)`, `SetFullAccessAsync(...)` and `SeedSampleSurveysAsync()`, and the Menus sample
  does the equivalent. Those rows land in the same tables the parity cases list, with
  **identity-generated** PKs the harness does not control, and they are ordinary non-adversarial
  rows. Two consequences, both fatal if ignored:
  - a `> 0 rows in every list case` gate is satisfied by the demo seed **alone**, so it cannot
    distinguish "the adversarial parity seed was applied" from "only the sample's demo data is
    present" — the exact silent failure §8.1 exists to prevent. The gate is therefore strengthened
    to **"every list case's baseline contains every seeded hostile row's id, matched literally"**;
  - Rule 1's "same longs both runs" holds only if the sample's seeders insert in byte-identical order
    on a fresh DB every time, which nobody has verified.

  Add a config flag or a `Parity` environment branch that skips the sample's seeding block, and apply
  the parity seed through a documented **explicit-id** path — EF's identity columns need
  `IDENTITY_INSERT` or `ValueGeneratedNever()`, and this is where it gets fiddly in practice. Decide
  and record which, in Step 00 item B.

### Per group, per step

```powershell
# 1. BEFORE touching the group — capture on the current tree, under BOTH principals
.\tools\parity.ps1 capture -Group WarrantyClaims -Grant FullAccess
.\tools\parity.ps1 capture -Group WarrantyClaims -Grant Restricted

# 2. Review what was captured. A near-empty or all-error baseline is the single most
#    common way this whole exercise silently fails.
.\tools\parity.ps1 summary -Group WarrantyClaims
#    -> cases: 62 | 2xx: 58 | 4xx: 4 | 5xx: 0 | empty bodies: 0 | PARTIAL: 2
#       CREATE 2xx: 6/6 | catalogue routes covered: 14/14, excluded: 0
#       hostile seed rows present in list bodies: 4/4

# 3. Commit the goldens on their own, before any code change
git add ADP.EndpointParity/baselines/warrantyclaims
git commit -m "Capture WarrantyClaims endpoint baseline before framework upgrade"

# 4. Do the upgrade (packages + mapper migration).

# 5. Replay and diff
.\tools\parity.ps1 verify -Group WarrantyClaims

# 6. Read every diff. Each is either a bug you just introduced, or an intended
#    change you record in the commit message.

# 7. Accept intended changes explicitly — never by re-running capture
.\tools\parity.ps1 accept -Group WarrantyClaims -Case <case-name> -Reason "<why>"
```

Raw equivalents if the script is unavailable:

```powershell
$env:PARITY_MODE="capture"; dotnet test ADP.EndpointParity/ADP.EndpointParity.WarrantyClaims
$env:PARITY_MODE="verify";  dotnet test ADP.EndpointParity/ADP.EndpointParity.WarrantyClaims
```

Group selection is the **project**, not a `--filter`. That is the point of the split in §2: while any
group is red, only the projects for the red groups fail to build, and every other group stays
runnable.

Retroactive baseline for the already-migrated Menus group (Step 01):

```powershell
git worktree add ..\ADP-pre-menus 14caf7c9^
Copy-Item -Recurse .\ADP.EndpointParity ..\ADP-pre-menus\ADP.EndpointParity
Push-Location ..\ADP-pre-menus
  $env:PARITY_MODE="capture"; dotnet test ADP.EndpointParity/ADP.EndpointParity.Menus
Pop-Location
Copy-Item -Recurse ..\ADP-pre-menus\ADP.EndpointParity\baselines\menus .\ADP.EndpointParity\baselines\
.\tools\parity.ps1 verify -Group Menus
```

CI (`azure-pipeline.yml`, after the BDD step, gated on a SQL service being available):

```yaml
- script: dotnet test ADP.EndpointParity/ADP.EndpointParity.Menus --logger trx
  env: { PARITY_MODE: verify }
  displayName: Endpoint parity
```

(one step per group project, or a single step over a solution filter containing the five.)

---

## 8. Honest limits — read before trusting a green run

1. **A baseline captured against a database the harness did not control proves nothing.** The naive
   version of this failure is an empty DB: every case returns an empty list, every diff passes, you
   feel safe. The version that actually happens here is subtler — the sample host seeds its own demo
   rows at startup (§7), so `> 0 rows` is satisfied while the adversarial seed was never applied.
   **The gate is therefore "every list case's baseline contains every seeded hostile row's id,
   matched literally", not "> 0 rows".** `parity.ps1 summary` reports it.
2. **Coverage is the seed's coverage.** Trap 1 fires only if the seed contains a soft-deleted child
   **and a `DETAIL` case is issued for the row that owns it** — hence per-hostile-row `DETAIL` cases
   (§5), not one `{seededId}` per entity. Trap 2 fires only if the seed contains a link row whose own
   PK differs from the foreign id it carries; **trap-2 coverage comes from those seeded rows, never
   from rows the harness creates** (Rule 1). **The seed must be authored adversarially — one hostile
   row per known trap, per group, each tagged.** Without that the harness is decoration.
3. **The mounted host is not a deployment** (§6).
4. **Binary endpoints are `PARTIAL`, not covered** (Rule 7).
5. **Darlastic's green is a smoke result, not a *value* parity result** — but it is the plan's only
   framework-only control, and losing it (SPIKE-5 negative) means every mapper group's diff
   permanently confounds framework change with mapper change (§6).
6. **`ADP.Cases` has no endpoints.** Never report it as passing endpoint parity.
7. **Privilege-scoped views need a second pass — but not for the reason an earlier draft gave.**
   That draft said the WarrantyClaims trap 3-read exposure "would show as no diff at all" under a
   full-access token. **That is wrong, and believing it would make you discount the strongest signal
   the harness produces for the highest-risk item in the plan.** The dealer view is *not* a
   privilege-filtered projection of the distributor view: `DealerFinancialController.cs:21-22` is a
   separate controller on its own route with its own DTO; its only gate (`Get` override, lines 35-42)
   is a `CanRead` that a full-access principal **passes**; and `DealerFinancialRepository` is bare
   `base(db)` with no `FilterByTypeAuthValues`, so there is no row scoping either. `GET
   /DealerFinancial` under the full-access token returns the five members `null` in the baseline and
   **populated** post-upgrade — a plain value diff on the ordinary pass, provided the seed has a
   claim with those five entity columns non-null (Step 00 item D requires exactly that).

   The restricted pass is still mandatory, for the surfaces that *are* genuinely row-scoped — e.g.
   `MenuRepository.cs:23-26`'s `FilterByTypeAuthValues` — and as an independent control on the
   dealer/distributor split. **`ParityAuth` mints two principals, `FullAccess` and `Restricted`, and
   the restricted grant set is declared per group in `parity.psd1`** (each group has its own action
   tree, so "restricted" has no group-independent meaning). Both are built in Step 00 item C and both
   baselines are captured in item F — a mandatory gate that no step builds is a gate discovered at
   the riskiest step, after the baselines are committed.
8. **Concurrency and replication triggers are out of scope.** Cosmos replication is deliberately
   disabled during parity runs; it is fire-and-forget and its failures are log lines, so it could not
   be diffed through HTTP anyway. The six replication delegates (`conventions.md` §6b) therefore have
   **no harness coverage at all** and need their own review.
9. **`ADP.Models` has zero executing tests** (SPIKE-6). The most-shared project in the solution is
   unguarded by anything in this plan except the compiler — and by the generated-tree diff (`README.md`
   §7), which is the cheapest available check that its public shape survived.
10. **Hand-written controller actions are covered only if the catalogue rule is enforced** (§5). The
    templated CRUD list reaches the inherited `ShiftEntityControllerAsync` surface and nothing else;
    Surveys' anonymous renderer endpoints and Darlastic's 31 hand-written actions exist only because
    `excludedRoutes` forces someone to write down why they are not covered.

---

## 9. This harness is temporary

**Decision, recorded rather than re-argued: the parity harness is removed in Step 08, once the
upgrade is finished.** The stated reason is that the Shift framework has not had many releases
recently, so a permanent regression harness is not worth its ongoing maintenance. Everything
described in this file — `ADP.EndpointParity/` (the harness library, the five test projects, `Seed/`,
the committed `baselines/`) and `tools/parity.ps1` + `parity.psd1` — is built in order to be deleted.

State the consequence plainly: **after Step 08 there is no automated proof that endpoint behaviour
has not changed.** The next framework upgrade either rebuilds this from nothing or proceeds without
it. That is the accepted trade, and Step 08's job is to make the removal clean rather than to
relitigate it.

### What temporariness changes about how you build it

Build it robust enough to be **trusted for the duration of this migration**, and no further.

- **Do not** invest in CI integration beyond what a step actually needs. The `azure-pipeline.yml`
  snippet in §7 is convenience for the migration window, not a permanent gate — it is removed with
  everything else, and it is not worth hardening for agents, service containers or flake budgets.
- **Do not** write documentation, extensibility points, configuration surface or onboarding material
  aimed at a reader six months out. This file is the documentation.
- **Do not** generalise for groups, hosts or scenarios this migration does not touch.
- **Do** spend freely on anything that makes a *green run* trustworthy inside the window. A harness
  you do not believe is worse than no harness, because it converts "unknown" into "falsely known".

### What temporariness does not licence

These stay mandatory. Each is what makes a green run mean anything, and each is cheap next to the
cost of a false green:

- **The stability gate** (Step 00 item E) — two captures on the unchanged tree must diff to empty,
  and it is fixed by making values deterministic, never by widening normalization.
- **Adversarially authored seeds** (§8.2, Step 00 item D) — one hostile row per known trap, per
  group, each tagged. Without them the harness is decoration.
- **The "no response body may be `text/html`" rule** (§6) — global, not a Menus special case.
- **Value-level diffing of the full normalized body** (§1, §4) — not status codes, not schemas, not
  field allowlists.

Nothing in §4 (normalization), §5 (coverage gates) or §8 (limits) is softened by this section.

`baselines/` stays committed and reviewed like source for the whole migration, and is deleted in
Step 08 with the rest. **Do not start pruning it early to shrink the eventual deletion** — a
baseline set that shrinks during the migration is a loss of coverage wearing the costume of tidiness.

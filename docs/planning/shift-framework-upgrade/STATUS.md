# STATUS — Shift Framework Upgrade

**This file is the ledger.** It answers "which step is done and which is pending". If it disagrees
with anything else in this directory, this file wins.

Last updated: 2026-09-02 (Step 02 CLOSED - Darlastic bumped, 4 lines, zero source changes, NU1903 down to 38 lines / 19 projects; SPIKE-5 RESOLVED POSITIVE so the framework-only control is recovered. Earlier: Step 01 VERIFIED — harness calibrated against the known-answer Menus migration: 17 diffs, all one expected convention change, 0 harness bugs, 0 regressions. SPIKE-9 resolved. Earlier: Step 00 IN PROGRESS — SPIKE-1 and SPIKE-2 resolved, item H probe green, §G baselines recorded, harness skeleton building and group-isolated; seeds/stability-gate/baselines still outstanding. Earlier same day: plan reordered — shared floor moved to the end, the atomic version-bump
step deleted, a harness-removal step added; then a consistency pass over the whole directory —
corrected the per-commit green claims in Step 03, gave Step 04's SPIKE-8 a real item 0, restored
Step 06 to SPIKE-8's `Blocks`, and re-based SPIKE-10 on the repo's actual export formats. No work
started.)

---

## Plan reorder — 2026-09-01, two decisions

Both are settled and are not re-argued anywhere in this directory. They are recorded here because
every step number below changed.

1. **The shared floor moves to the END (now Step 06).** Each group bumps its own `PackageReference`
   lines and the build is green after every step. The old solution-wide "atomic version bump" step
   is **deleted**; its 29 package lines are redistributed to the steps that own the code — see
   §Package-reference ownership. The evidence: the Shift nuspecs declare **minimum-version**
   dependencies (`version="2026.7.31.1"`), *not* exact pins (`[2026.7.31.1]`) — verified in the local
   NuGet cache — so nothing forces a lockstep bump; and the repo already runs the mixed arrangement
   green today, with `ADP.Menus.Shared` pinning `ShiftEntity.Model 2026.8.30.1` while
   `ADP.Menus.Data` `ProjectReference`s `ADP.Models`, which pins `2026.7.31.1`
   (`ADP.Models/Models/Models.csproj:48`). Shared-last also eliminates NU1605 outright:
   `ClaimableItems.Shared:34` and `WarrantyClaims.Shared:33` are already at `8.30.1` by the time the
   floor moves, and `Cases.Shared:32` is bumped in the **same commit** as `ADP.Models`, so no
   downgrade window ever opens. The old core-first argument ("a broken foundation makes later
   failures ambiguous") does not apply: the shared floor is **0 profiles, 0 triples** — a version
   number and nothing else, with no refactor to get wrong.
2. **The parity harness is removed once the upgrade is finished (new Step 08).** The Shift framework
   has not had many releases recently, so a permanent regression harness is not worth its
   maintenance.

Old → new, for anyone holding a stale reference: 00→00, 01→01, old 03 (shared floor)→**06**,
old 04→**02**, old 05→**03**, old 06→**04**, old 07→**05**, old 08→**07**, plus a new **08**.
**Old Step 02 (atomic bump) does not exist.** A reference to "after the atomic bump", "the Step 02
work queue" or "expected to be red out of Step 02" now means: *this step performs its own version
bump as its first commit, and it must end green.*

> **Headline invariant: every step in this plan ends with a green build.**

---

## Status vocabulary

| Status | Means |
|---|---|
| `NOT STARTED` | No work done. |
| `IN PROGRESS` | Work started. The Notes column must say where it stopped. |
| `BLOCKED` | Cannot proceed. The Notes column must name the blocker and the spike that resolves it. |
| `DONE` | **Code changed and it builds.** Tests pass. Endpoint parity NOT yet proven. |
| `VERIFIED` | **Endpoint parity proven by the harness.** Every diff explained or accepted with a recorded reason. |
| `CLOSED` | **Finished, and there is no endpoint surface whose parity could be proven.** Terminal for a step whose deliverable is not an HTTP behaviour: 00 (builds the harness), 02 (0 profiles / 0 triples; smoke, not parity), 06 (libraries only — no controllers, no repository), 08 (removes the harness). Requires the step's own exit criteria all ticked — **including a green build** — and the reason recorded in `Verified by`. |

`DONE` vs `VERIFIED` is the whole point of this exercise. A migration that compiles and passes unit
tests can still be silently serving wrong values — all four regression shapes in `README.md` §4 do
exactly that. **Only the harness moves a step to `VERIFIED`.** Never promote a step on the strength
of a green build.

**`CLOSED` no longer means "ends red."** That meaning died with the deleted atomic-bump step; no step
in this plan ends red any more. `CLOSED` now says one thing only: the step finished, and it had no
endpoint surface whose parity could be diffed.

`CLOSED` exists because four of the nine steps cannot reach `VERIFIED` by construction, and the
earlier vocabulary had no word for them — which deadlocked `README.md`'s resume instruction on the
shared floor (now Step 06) and made release readiness' (now Step 07) precondition unsatisfiable.
**Each step's terminal status is named in the `Terminal status` column below.** "Finished" means *at
its terminal status*, never `VERIFIED` unconditionally.

---

## Ledger

| Step | Group | Projects | Depends on | Terminal status | Status | Verified by | Date | Notes |
|---|---|---|---|---|---|---|---|---|
| 00 | Baseline & parity harness | `ADP.EndpointParity.Harness` + 5 per-group test projects (new), `tools/parity.ps1` (new) | — | `CLOSED` | `IN PROGRESS` | — | 2026-09-01 | **ALL FOUR mapper groups captured — Menus, Surveys, ClaimableItems, WarrantyClaims — each under both grants and each stability-gated to a byte-identical second run. Darlastic skipped by decision. Remaining before `CLOSED`: commit the goldens in their own commit, and re-run the §G solution numbers now that the harness projects are in `ADP.sln`.** Done: items **A** (SPIKE-1 resolved), **H** (`ADP.Models` probe green), **B** (SPIKE-2 resolved both halves — sample host + mounted host both boot; seeding suppression verified), **G** (all baseline numbers recorded below), and the structural half of **C** — six projects build and are in `ADP.sln` (59), capture layer (`Normalizer`/`Transcript`/`TranscriptDiffer`/`ParityRunner`/`ParitySummary`/`Canonical`) and wiring layer (`RouteCatalog`/`RequestFactory`/`ParityAuth`/`ParityDb`/`SampleHostFactory`/`MountedHostFactory`) written, `tools/parity.{ps1,psd1}` written, group-isolation rehearsal passes. **Not yet done: the seeds, the case list that binds the catalogue to cases, the stability gate, and every baseline.** No group is captured, so no later step may rely on this yet. Must run on the pre-bump tree. Two identical capture runs must diff to zero before anything else is trusted. Terminal `CLOSED`: it builds the instrument, it has no endpoints of its own. Also carries the **15-minute throwaway `ADP.Models` compile probe** that de-risks shared-last (see the residual-risk note under the spike table). |
| 01 | Retro-verify `ADP.Menus` | `ADP.Menus.*` (**11 projects**, 8 of them already on 2026.8.30.1) | 00 | `VERIFIED` | **`VERIFIED`** | `parity.ps1 verify -Group Menus` (both grants) against a retroactive baseline captured at `14caf7c9^` — **15 diffs under FullAccess + 2 under Restricted, ALL of one shape, all accepted with a recorded reason; 0 harness bugs, 0 regressions** | 2026-09-02 | Code migration already `DONE` at `14caf7c9` — see the Menus row below. This step only proves it. Retroactive baseline from `14caf7c9^` via `git worktree`. Also resolves SPIKE-9. No package lines: Menus is already at `2026.8.30.1`. |
| 02 | `ADP.Darlastic` | `ADP.Darlastic.{API,Data,Shared,Web}` | 00, 01 | `CLOSED` | **`CLOSED`** | **SMOKE, NOT VALUE PARITY** - `parity.ps1 verify -Group Darlastic`, both grants, 31/31 catalogue routes, 0 5xx, stability-gated. 0 triples and 0 profiles, so there is no mapping behaviour here to prove. | 2026-09-02 | Bumps its own **4** package lines as its first commit and ends green. 0 profiles, 0 triples. Smoke pass only — nothing mapper-shaped to prove, so terminal `CLOSED`. **But it is the plan's only framework-only control** (see SPIKE-5). Do not record as full parity. |
| 03 | `ADP.Surveys` | `ADP.Surveys.{API,Data,Shared,Web}` + 2 samples | 00, 01 | `VERIFIED` | **`VERIFIED`** | `parity.ps1 verify -Group Surveys` **clean under both grants** — 49 cases, 52/52 catalogue routes, 0 5xx. Six `$top` cases accepted with recorded reasons (harness fix, not product); on FullAccess the re-captured golden differs by **exactly one line, the request URL**, response byte-identical. A wholesale Restricted re-capture left **27 of 30 goldens byte-identical**. Plus 2 SPIKE-4 round-trips and the `SurveyInstance` write golden. | 2026-09-02 | Bumps its own **7** package lines. 4 triples, 1 profile (151 lines). **Free-floating** — every `ProjectReference` is intra-group and it consumes no `ShiftSoftware.ADP.*` package, so it is legal anywhere after 01. Ordered here by risk/simplicity, not by the graph. Has a sample host → full HTTP parity available. Carries SPIKE-3 and SPIKE-4. |
| 04 | `ADP.ClaimableItems` | `ADP.ClaimableItems.{API,Data,Shared,Web}` | 00, 01 | `VERIFIED` | **`VERIFIED`** | `parity.ps1 verify -Group ClaimableItems` **clean under both grants** after 8 accepted SPIKE-11 convention diffs (4 cases x 2 grants); a wholesale re-capture left **26 of 30 goldens byte-identical per grant**. Solution builds green, **0 SHENGEN warnings** (all 5 baseline 004s resolved). **MOUNTED-HOST CAVEAT: this is module-level parity, not full endpoint parity** - no sample host exists, so consumer middleware order, request localization, CORS, fallback routing and JSON option overrides are NOT exercised. The 5 Cosmos delegates have **zero** harness coverage and were verified by an 8-agent adversarial line-by-line review instead, which found 3 real defects. | 2026-09-02 | Bumps its own **7** package lines. 5 triples, 4 profiles, 5 Cosmos delegates, 1 `IMapper` site. No host → mounted host (SPIKE-2). First group to generate a `Certificate` mapper, so it now **owns SPIKE-8** (the shared floor no longer runs ahead of it). |
| 05 | `ADP.WarrantyClaims` | `ADP.WarrantyClaims.{API,Data,Shared,Web}` | 00, 01, 04 | `VERIFIED` | **`VERIFIED`** | **`parity.ps1 verify -Group WarrantyClaims` clean under BOTH grants** (14 accepted SPIKE-11 diffs, 7 cases x 2 grants; a wholesale re-capture left **21 of 28 goldens byte-identical per grant**). **THE EXPOSURE IS CLOSED AND PROVEN FOUR WAYS**: the full-access `DealerFinancial.LIST` case shows all five members null against a seed that populates all five; the restricted pass agrees; `IgnoreList` proven baked from emitted `.g.cs`; and `DealerFinancialExposureTests` (3/3) guards it permanently. **MOUNTED HOST, not a sample host** - module-level parity only. 0 SHENGEN warnings, solution green, no NU1605. | 2026-09-02 | Bumps its own **7** package lines. **Highest risk.** 7 triples; dealer/distributor forward-map `Ignore()` exposure. Ordered last of the groups by risk, overriding simplicity (it has fewer profiles than 04). Depends on 04 for the shared `Certificate` mapper precedent (SPIKE-8) — a **knowledge** dependency, not a build one. |
| 06 | Shared floor | `ADP.Models/Models`, `ADP.Cases.Data`, `ADP.Cases.Shared`, `Lookup.Services.DuckDB` | 02, 03, 04, 05 all at terminal | `CLOSED` | **`CLOSED`** | N/A — no endpoints; covered by the unit suites, by the green solution build, by the generated-tree diff, and by Steps 04/05, which exercised the shared `Certificate` entity through real endpoints before this floor moved. All 8 parity runs (4 groups x 2 grants) re-verified CLEAN after the bump | 2026-09-02 | Bumps the last **4** package lines — `ADP.Models` and `Cases.Shared` in the **same commit**, which is what keeps NU1605 from ever appearing. 0 profiles, 0 triples; expected to compile unchanged. Libraries only — no endpoints, so terminal `CLOSED` with the reason in `Verified by`. Carries SPIKE-6. Pulls in `ADP.Menus.Generation` (see the ledger note below). Step 00's compile probe is its early warning. |
| 07 | Release readiness | solution-wide + `GlobalSettings.props` | 00–06 all at terminal (00, 02, 06 `CLOSED`; 01, 03, 04, 05 `VERIFIED`) | `VERIFIED` | **`VERIFIED` (verification complete — RELEASE NOT YET CUT)** | Full sweep on the final tree: solution 0 errors / 543 warnings cold (baseline 580 cold), **0 SHENGEN004/007/008/010**, 0 NU1605/1701/1603/MSB3277, NU1903 20 lines / 5 projects (baseline 42/21); every test suite at baseline with the 2 known SampleDataSeedingTests the only red; web components 114/114; **all 10 parity runs clean** (5 groups x 2 grants); generated trees clean; **package-mode host boot PASSED against the locally-packed 1.16.0 feed — 12 triples, 0 conflicts, VerifyBindings 0 errors** | 2026-09-02 | Package-mode restore smoke check, full baseline comparison, single `ADPVersion` bump. **No package lines left** — all 29 landed in Steps 02–06. |
| 08 | Harness removal & cleanup | `ADP.EndpointParity.Harness` + the 5 per-group test projects, `tools/parity.ps1`, `ADP.sln` | 07 | `CLOSED` | `NOT STARTED` | — | — | **New step, added 2026-09-01.** Deletes the instrument Step 00 built and removes its projects from `ADP.sln`. Decision recorded above: the framework's release cadence does not justify maintaining a permanent regression harness. Terminal `CLOSED` — it removes an instrument; it has no endpoints. Solution must build green and the project count return to its pre-Step-00 figure (53 today). |

**Ledger notes on the dependency column** — these are the edges the graph actually has. Two were
wrong in an earlier draft, and one moved with the 2026-09-01 reorder:

- **Step 03 (`ADP.Surveys`) is free-floating.** It references no ADP project and no
  `ShiftSoftware.ADP.*` package, so nothing in the shared floor (now Step 06) can affect it and
  nothing waits on it. It sits at 03 for risk ordering only; it is legal at any point after Step 01.
- **Step 05 depends on Step 04 for knowledge, not for a build.** The shared `Certificate` mapper
  precedent (SPIKE-8) is the whole of that edge — `ADP.WarrantyClaims` compiles regardless of
  whether `ADP.ClaimableItems` has been migrated.
- **The Menus ↔ LookupServices coupling is bidirectional at group level, and after the reorder it
  belongs to the shared floor at its new number, Step 06.**
  `ADP.LookupServices/Lookup.Services/Lookup.Services.csproj:61` `ProjectReference`s
  `ADP.Menus/ADP.Menus.Generation`, so **Step 06** builds a Menus project; and
  `ADP.Menus/ADP.Menus.Tests:50,58` reference `Lookup.Services` and `Lookup.Services.DuckDB`, so
  **Step 06**'s `Lookup.Services.DuckDB` bump mutates the restore graph of the project whose count
  Step 01 pinned. Consequence, unchanged but for the number: if Step 01 fixes a real regression
  inside `ADP.Menus.Generation`, the `LookupServices.BDD` 452/452 figure Step 06 uses as an exit
  criterion moves under it — and **Step 06 must re-run `ADP.Menus.Tests`**. Shared-last puts four
  more steps between Step 01's pin and that re-run, which makes the re-run more important, not less.
  Note that this note was attached to Step 03 in an earlier draft, when 03 *was* the shared floor;
  Step 03 is now `ADP.Surveys`, which this coupling does not touch at all.

### Already true today — recorded, not scheduled

| Item | Group | Projects | Status | Verified by | Date | Notes |
|---|---|---|---|---|---|---|
| Mapper migration | `ADP.Menus` | 11 projects (8 carry a `2026.8.30.1` reference) | **`VERIFIED`** | Step 01, 2026-09-02 — full HTTP parity, both grants | 2026-08-31 (migrated) / 2026-09-02 (verified) | Commit `14caf7c9`. Already on `2026.8.30.1`, AutoMapper profiles deleted, mappers rewritten. Builds green; `ADP.Menus.Tests` at its known baseline. **Endpoint parity was never proven** — no harness existed. Step 01 closes this. Until then Menus is `DONE`, not `VERIFIED`. |

---

## Open spikes

A spike is a question the survey could not answer. **Do not invent an answer in a step file — resolve
the spike, then record the finding here.**

| ID | Question | Blocks | Status |
|---|---|---|---|
| SPIKE-1 | Does `ShiftSoftware.ShiftFrameworkTestingTools` (published only to `2026.7.28.1`) bind when NuGet unifies its ShiftEntity dependency up to `2026.8.30.1`? | 00 (design choice only — fallback exists) | **RESOLVED — IT BINDS.** Scratch project referencing TestingTools `2026.7.28.1` alongside ShiftEntity.{Model,EFCore,Web} `2026.8.30.1`: restores and builds clean (0 warnings, 0 errors, no NU1605/MSB3277) — its nuspec declares **minimum-version** deps, not exact pins, so NuGet unifies up. Runtime binding proven beyond restore: every public type loads without `ReflectionTypeLoadException`, and **all 17 method bodies of the two generic types that matter — `BasicTest<DTO,ListDTO>` and `ShiftCustomWebApplicationFactory<TStartup,DB>` — JIT-prepare clean** when closed over concrete types (`RuntimeHelpers.PrepareMethod`). **Design decision taken anyway: the harness does NOT consume the package.** `BasicTest` parses responses into typed DTOs, which the capture-layer purity rule forbids, and `ShiftCustomWebApplicationBearerAuthSettings` carries ONE `TypeAuthActions` list per factory instance, so two principals would mean two factories and two databases. Its value is as the **specification** the plan predicted: its method set (`Get`/`PostOrPut`/`Delete`/`OdataList`/`RevisionList`) is the inherited-route checklist, and its `GenerateToken` confirmed the RS256 + `ShiftSoftware/TypeAuth/Claims/AccessTree` shape `ParityAuth` mints. |
| SPIKE-2 | Can a synthetic "mounted host" boot `ADP.ClaimableItems` / `ADP.WarrantyClaims` through their own `Add<Group>ApiServices` entry point? No sample host exists for either, and no sample API declares `public partial class Program`, so `WebApplicationFactory<Program>` will not compile against any of them today. | 00, 04, 05 | **RESOLVED — BOTH HALVES POSITIVE. Steps 04 and 05 are not blocked and no fallback sample API is needed.** (1) *Sample host*: `public partial class Program { }` appended to `ADP.Surveys.Sample.API/Program.cs` (behaviour-free; the implicit top-level-statements class is internal) makes `WebApplicationFactory<Program>` compile and boot. (2) *Mounted host*: `MountedHostFactory` boots `ADP.ClaimableItems` through `AddClaimableItemsApiServices<ParityDb>(mvcBuilder, configure)` against real SQL with `EnsureCreated` and no migrations, exposing **63 catalogue routes** across all its triples. `ParityDb : ShiftDbContext` owns no entities; the group's `IModelBuildingContributor` supplies the tables, exactly as a tenant host gets them. Modelled on the repo's own minimal consumer, `ADP.Darlastic.Sample.API`. (3) *Seeding suppression*: a `Parity:SuppressSampleSeeding` config branch wraps the sample's `SeedDBAsync`/`SetFullAccessAsync`/`SeedSampleSurveysAsync` block — verified to take, list body goes from the demo seed's `Count:8` to `Count:0` on a fresh per-run database. **Explicit-id path still OPEN — see the note below the baselines table.** |
| SPIKE-3 | `BankQuestionListDTO.Type` and `ScreenTemplateListDTO.QuestionCount` are mapped via static JSON-parsing method calls. `ForList` is spliced into the SQL projection. How do these work today and what replaces them? | 03 | **RESOLVED BY EXPERIMENT - carry the method call over into `ForList` verbatim. No fallback needed.** The question was settled by asking the running pre-migration host three things: a plain list returns `Type` correctly ("text"/"nps"); `$filter=Type eq` fails in the OData layer; and **`$orderby=Type` fails with "The LINQ expression `.OrderBy(b => GeneralMappingProfile.ExtractQuestionType(b.QuestionJson))` could not be translated."** That third result is the whole answer: the method call **is** in the expression tree today, and it works only because **EF Core permits client evaluation in the FINAL `Select` projection but not in query operators** like `OrderBy`/`Where`. The generated `MapToListGenerated` ends in `Queryable.Select(queryable, projection)` - also a final projection - so a method call there is client-evaluated exactly as it is today, with identical behaviour and identical limits. **Confirmed by signature:** `ForList(Expression<Func<TEntity,TProp>>)` takes an Expression (SQL projection) while `ForView`/`ForEntity` take a plain `Func` (in-memory) - so the JSON deserializations in items C/D/E are unconditionally safe, and only the two list members were ever in question. **Bonus finding, pre-existing and NOT caused by this upgrade:** `$orderby=Type` and `$orderby=QuestionCount` return **500** on the current tree. Any client that sorts a bank-question or screen-template list by those columns is already broken. Worth a separate fix; out of scope here. |
| SPIKE-4 | AutoMapper's `.Condition(...)` (on `BankQuestion.BankEntryID`) has no documented builder equivalent. Is the existing-aware `ForEntity((dto, entity, ctx) => ...)` overload the correct replacement? | 03 | **RESOLVED - YES. The overload exists with exactly that shape**, confirmed by reflection over `ShiftSoftware.ShiftEntity.Core.ShiftMapperBuilder<TEntity,TListDTO,TViewDTO>` at `2026.8.30.1`: `ForEntity(Expression<Func<TEntity,TProp>> member, Func<TViewDTO,TEntity,MappingContext,TProp> value)`. It sits alongside two simpler overloads (`Func<TViewDTO,TProp>` and `Func<TViewDTO,MappingContext,TProp>`), neither of which can see the EXISTING entity and so neither of which can reproduce a conditional write. The replacement is `.ForEntity(e => e.BankEntryID, (dto, entity, ctx) => dto.BankEntryID != Guid.Empty ? dto.BankEntryID : entity.BankEntryID)` - registered unconditionally with the condition INSIDE the value delegate, which is the shape that avoids `SHENGEN005`. Note `AfterEntity(Action<TViewDTO,TEntity,MappingContext>)` also exists as a fallback seam if a future case needs post-assignment fix-up rather than a per-member conditional. |
| SPIKE-5 | Can the Darlastic sample host be booted headlessly at all? | 02, and the attribution of 03/04/05 diffs | **RESOLVED - POSITIVE. It boots, and the plan KEEPS its framework-only control.** This supersedes the 2026-09-01 "closed by decision" entry: the call then was to skip Darlastic's capture, but once the group was open for its version bump the host turned out to be bootable cheaply, so the control was taken rather than forfeited. Both recorded blockers were real and both were solved. **(1)** Program.cs reads ConnectionStrings:Registry and Sample:AllowDevAuth at the TOP of the file, BEFORE builder.Build() and therefore before WebApplicationFactory's ConfigureAppConfiguration runs - an in-memory override arrives too late, and the first attempt still connected to the appsettings database, failing with "Cannot open database ... login failed". **Environment variables** (ConnectionStrings__Registry, Sample__AllowDevAuth) are read into Configuration from the start and outrank appsettings, so they are what actually redirect this host. **(2)** the registry database: SampleDB deliberately never calls EnsureCreated (a second schema authority against a real registry is the failure the engine's DARLASTIC_SCHEMA_MANAGED switch exists to prevent), so the harness creates a DISPOSABLE schema itself - **from the HOST's service provider**, because the Darlastic tables reach the model through the IModelBuildingContributor that AddDarlasticApiServices registers, and a DbContext built outside DI has an EMPTY model whose EnsureCreated silently creates nothing. Plus DarlasticViews.CreateGoldenCustomerSql(), because GoldenCustomer is mapped ToView and EnsureCreated does not create views. |
| SPIKE-6 | `ADP.Models/Models.Tests` discovers **zero tests** (no test framework referenced; all sources `<Compile Remove>`d) yet exits 0. The most-shared project in the solution is unguarded. Fix, or accept and record? | 06 | **RESOLVED — ACCEPTED, because "fix it" turned out to be impossible.** The plan's preferred option was to add a test framework, drop the `<Compile Remove>`s and make the two existing `[Fact]`s run. Tried it: both files fail to compile with `CS0234` because they target `ShiftSoftware.ADP.Models.DealerData`, **a namespace that no longer exists** — `SOLaborCSV` and `CustomerDataCSV` were deleted outright in the Phase 1-3 refactors, and `CacheableCSVEngine` moved to `ADP.SyncAgent`. The tests are not disabled, they are DEAD, and the `<Compile Remove>` entries are what has been hiding that. Reviving them is not an upgrade task: it would mean writing new tests against types that moved to a different package. **Decision: accept, and make the claim honest.** `CLAUDE.md:22` advertised `dotnet test ADP.Models/Models.Tests` as a build command; it now carries a note stating the command executes nothing, why, and what actually covers `ADP.Models` (the compiler and its consumers' suites). `verification.md` §8.9 is the standing caveat. The dead files were left in place rather than deleted — that is a cleanup decision for the owner, not something to fold into a framework upgrade. |
| SPIKE-7 | Do `IgnoreList` / `IgnoreView` bake correctly for the two Financial triples, and do both triples over the same entity generate distinct list projections? Must be proven from emitted `.g.cs`, not from the build log. | 05 | OPEN |
| SPIKE-8 | Two triples across two different groups map the `ADP.Cases` `Certificate` entity (`ItemClaimCertificateRepository`, `WarrantyCertificateRepository`). Which assembly does each generated mapper land in, and can both coexist in one host? **The old shared-floor probe is gone** — after the reorder the floor runs *last* (Step 06), behind both consumers — so Step 04 answers it from its own emitted `.g.cs` as the first group to generate a `Certificate` mapper, and Step 05 applies the finding to the second triple, and Step 06 confirms the `ADP.Cases` side of it (its item C). | 04 (owns it), 05, 06 (confirms the `ADP.Cases` side) | **PARTLY RESOLVED at Step 04's baseline (Q1-Q3); Q4 deferred to item J by construction.** Probed by `dotnet build ADP.ClaimableItems.Data -p:EmitCompilerGeneratedFiles=true --no-incremental` on the pre-bump tree. **Q1 - which assembly?** The **consumer's**. `Generated_Certificate_CertificateListDTO_ItemClaimCertificateDTO_16cfdeb0.g.cs` is emitted into `ADP.ClaimableItems.Data`; `ADP.Cases` emits **no mapper at all**. The generator follows the `ShiftRepository<>` subclass that declares the triple, not the entity's owning assembly. **Q2 - can both coexist?** **Yes**, and the emitted registration is the proof: `ShiftEntityMapperRegistry.Register(typeof(Cases.Data.Entities.Certificate), typeof(Cases.Shared.DTOs.Certificate.CertificateListDTO), typeof(ClaimableItems.Shared.DTOs.ItemClaimCertificate.ItemClaimCertificateDTO), ...)` keys on the **3-tuple**. Step 05's triple is `Certificate, CertificateListDTO, CertificateDTO` (`WarrantyCertificateRepository.cs:32`) - same entity, same list DTO, **different view DTO** - so it takes a distinct key, a distinct generated type name, and a distinct assembly with its own `[ModuleInitializer]`. No collision. **Q3 - must `ADP.Cases.Data` be a registered data assembly?** **No.** `ClaimableItemsApiExtensions.cs:49-50` registers only `DataMarker` and `SharedMarker`; the mapper lives in `DataMarker`'s assembly, so it resolves without `ADP.Cases.Data` being added. **Q4 - is the generator content with an entity whose owning assembly is still on `2026.7.31.1`?** **Not answerable at baseline** - the analyzer running there IS `7.31.1`, so the floor is uniform, not mixed. It becomes answerable only after item A puts the consumer on `8.30.1` while `ADP.Cases` stays behind. Confirmed in item J. |
| SPIKE-9 | Exact delegate signature required by `Replicate<T>` / `UpdateReference<T>` at `2026.8.30.1`. | resolved by 01; blocks 04, 05 | **RESOLVED — signatures below, read by reflection over `ShiftSoftware.ShiftEntity.CosmosDbReplication 2026.8.30.1` and cross-checked against the ~19 live call sites.** <br><br> **The reference implementation is in `ADP.Menus/ADP.Menus.Sync/`, NOT `ADP.Menus.Data`** — do not hunt for it in the wrong assembly at Step 04. <br><br> **⚠️ THERE ARE TWO API FAMILIES AND THEY DIFFER IN THE DELEGATE'S FIRST ARGUMENT.** This is the trap: copying a call from the wrong family compiles against the wrong lambda parameter and fails in a way that reads like a mapper error. <br><br> **(1) TRIGGER path — `ShiftEntityCosmosDbOptions`, what ADP.Menus.Sync actually uses.** Delegates receive an `EntityWrapper<Entity>`; reach the row with `wrapper.Entity`. <br> `SetUpReplication<DB, Entity>(CosmosClient client, string cosmosDataBaseId, Func<EntityWrapper<Entity>, ValueTask<Entity>> mapper = null)` → `CosmosDbTriggerReplicateOperation<Entity>` <br> `.Replicate<CosmosDbItem>(string cosmosContainerId, Expression<Func<CosmosDbItem, object>> partitionKeyLevel1Expression, [level2], [level3], Func<EntityWrapper<Entity>, CosmosDbItem> mapping)` → `CosmosDbTriggerReferenceOperations<Entity>` <br> `.UpdateReference<CosmosDbItem>(string cosmosContainerId, Func<IQueryable<CosmosDbItem>, EntityWrapper<Entity>, IQueryable<CosmosDbItem>> finder, Func<EntityWrapper<Entity>, CosmosDbItem, CosmosDbItem> mapping)` → chainable <br><br> **(2) DIRECT path — `CosmosDbReplicationOperation<DB, Entity>`.** Delegates receive the **bare `Entity`**, not a wrapper. <br> `.Replicate<CosmosDBItem>(string containerId, Func<Entity, CosmosDBItem> mapping)` <br> `.UpdateReference<CosmosDBItem>(string containerId, Func<IQueryable<CosmosDBItem>, Entity, IQueryable<CosmosDBItem>> finder, Func<Entity, CosmosDBItem, CosmosDBItem> mapping)` <br><br> **Three further facts the call sites make explicit and the signatures alone do not:** (a) **partition-key expressions are over the COSMOS MODEL (`CosmosDbItem`), not the entity** — `document => document.BasicModelCode`, and there are 1/2/3-level overloads; (b) `partitionKeyLevel*Expression` and `mapping` are passed as **NAMED** arguments throughout `MenuReplicationExtensions.cs`, which is what keeps the 2- and 3-level overloads unambiguous at a glance; (c) **register each entity type EXACTLY ONCE** — the framework silently keeps only the last registration per type, which is why a master entity's own document and all its fan-outs chain off a single `SetUpReplication` (`MenuReplicationExtensions.cs:36-38`), and **`UpdateReference` fires on `ChangeType.Modified` ONLY** (`:40-44`), so an inserted master row fans out to nothing and a hard-deleted one leaves embedded copies behind. |
| SPIKE-10 | Are the **binary/print export endpoints** byte-reproducible enough to diff, or must they be recorded `PARTIAL`? Verified against the repo: the only `.xlsx` producers are `ADP.Menus/ADP.Menus.API/Controllers/MenuController.cs:114,248,437` — **Step 01**, not Surveys. `ADP.WarrantyClaims` exports **PDF** (`DistributorFinancialController.cs:108,110`, `WarrantyClaimController.cs:148`), which has no sheet XML to extract and carries `/CreationDate` + `/ID`, so it is the likeliest `PARTIAL`. The `text/csv` exports (`Surveys/SurveyResponsesController.cs:206`, `WarrantyClaims/ManufacturerSettlmentSheetController.cs:69`, `WarrantyClaimController.cs:287`, `Darlastic/CaseBrowserController.cs:363`) are deterministic text and are **covered**, not `PARTIAL` — they are not a Rule-7 case at all. | 01 (`.xlsx`), 05 (PDF) | OPEN |
| SPIKE-11 | **What did `DefaultEntityToDtoAfterMap()` / `DefaultDtoToEntityAfterMap()` do, and what reproduces it?** Both exist in `ShiftSoftware.ShiftEntity.dll` @ `2026.7.31.1` and are **absent** @ `2026.8.30.1` (verified by binary inspection); neither is documented in any XML doc file at either version. **6 call sites** across 2 groups. The calls vanish with the profiles — but so does whatever behaviour they applied. Resolve by reading the implementation at the `2026.7.31.1` tag in the public framework repo. | 04, 05 | **RESOLVED - and it was NOT a no-op. The convention supersedes both, with two behavioural deltas that ride along with the upgrade.** Source read at the exact commit the package declares (`1e22f8de5534818f029a5073cf79cb32b457a11b`, from the nuspec's `<repository commit=...>`), file `ShiftEntity.Core/Extensions/AutoMapperExtensions.cs`. **What they did:** `DefaultEntityToDtoAfterMap` walked every DTO property of type *exactly* `ShiftEntitySelectDTO` (scalar - `List<ShiftEntitySelectDTO>` was skipped) and, when `Value` was blank **and** `Text` null, back-filled it from the entity's `{Name}ID` column. `DefaultDtoToEntityAfterMap` did **two** things: (a) wrote `entity.{Name}ID = long.Parse(selectDTO.Value)` when `Value` was non-blank, and (b) `ForAllMembers(x => x.Condition(...))` suppressing the write of **any** member whose value is a `ShiftEntityBase` - a map-wide navigation-overwrite guard. **Both maps here are affected:** `CampaignDTO.VehicleInspectionType` and `ClaimableItemDTO.Campaign` are both scalar `ShiftEntitySelectDTO?`. **Does the generator reproduce it?** Yes, by convention, and `SHENGEN004` proves it - neither member is reported unmapped. Emitted: `dto.Campaign = MappingHelpers.ToSelectDTO(entity.CampaignID, entity.Campaign != null ? entity.Campaign.Name : null)` and `existing.CampaignID = MappingHelpers.ToNullableForeignKey(dto.Campaign)`; the navigation is never assigned in `MapToEntityGenerated`, which is (b)'s effect. **So nothing must be re-added per triple** - but two deltas are inherited, and neither is caused by anything this step writes: **(1) READ:** `Text` is now populated from the navigation (the old helper always left it null), and a **null FK now yields a null member** where the old helper produced `{Value:""}`. **(2) WRITE:** a blank/null select DTO now **sets the FK to null**, where the old helper **left it unchanged**. That second one is data-loss-shaped and is the one to watch on the UPDATE cases. Also improved: `long.Parse` (→ 500 on bad input) became `TryParse` + a framework `ShiftEntityException` (→ 400). |
| SPIKE-12 | **Does a per-group version bump keep the tree green?** | — (no longer blocks anything) | **RESOLVED — staged per-group bump adopted.** The Shift nuspecs declare **minimum-version** dependencies (`version="2026.7.31.1"`), not exact pins (`[2026.7.31.1]`) — verified in the local NuGet cache — so no lockstep is forced. The repo already runs the mixed arrangement green: `ADP.Menus.Shared` pins `ShiftEntity.Model 2026.8.30.1` while `ADP.Menus.Data` `ProjectReference`s `ADP.Models`, pinned at `2026.7.31.1` (`ADP.Models/Models/Models.csproj:48`) — an upgraded group sitting on a not-yet-upgraded shared project builds today. Ordering the floor **last** also disposes of the 3 NU1605 pins: `ClaimableItems.Shared:34` and `WarrantyClaims.Shared:33` are already at `8.30.1` before the floor moves, and `Cases.Shared:32` moves in the same commit as `ADP.Models`, so no downgrade window opens. **Consequence: the atomic-bump step is deleted and each group step owns its own package lines.** |

**Residual risk carried by shared-last, and its mitigation.** If `ShiftEntity.Model 2026.8.30.1`
contains a breaking change affecting `ADP.Models`, shared-last defers discovery to Step 06 — the end
of the plan. Mitigation, owned by **Step 00** as a work item and referenced from **Step 06**: an
early throwaway compile probe — bump `ADP.Models/Models/Models.csproj:48` on a scratch branch, run
`dotnet build ADP.Models/Models`, record the result in the row below, then **revert**. Timeboxed to
15 minutes and **never committed**. It buys the late ordering without the late surprise.

| Probe | Result | Recorded on |
|---|---|---|
| `ADP.Models` @ `ShiftEntity.Model 2026.8.30.1`, throwaway build (Step 00) | **GREEN.** `dotnet build ADP.Models/Models` on branch `scratch/models-probe` with only `Models.csproj:48` bumped `2026.7.31.1` → `2026.8.30.1`: **0 errors, 9 warnings**. The 9 are pre-existing `CS8632` (nullable annotation outside a `#nullable` context) and the count is **identical before and after the bump**, so the bump is a pure no-op for `ADP.Models`. **Consequence: Step 06 remains the four-line version edit it is written as**, and shared-last carries no late surprise. Branch deleted; `git status` shows no change to `Models.csproj`. | 2026-09-01 |

---

## Corrections to the survey carried into this plan

The survey dossier and the verified repo state disagree in seven places. **The verified numbers are
used throughout this plan.**

| Claim in dossier | Verified | How verified |
|---|---|---|
| 30 `PackageReference` lines to bump | **29** | `grep -c` for `ShiftSoftware.Shift*` at `Version="2026.7.31.1"` across all csproj |
| `ADP.WarrantyClaims` has 5 repository triples | **7** | multi-line class declarations were missed by a single-line grep |
| `ADP.ClaimableItems` has 4 repository triples | **5** | `CampaignVinEntryRepository` uses a primary-constructor declaration spanning two lines |
| `ADP.ClaimableItems` has 4 profile files | **5 files, 4 `Profile` classes** | the fifth, `GeneralMappingHelper.cs`, is a static helper, not a profile |
| `Lookup.Services.Functions` — "either dead or excluded, confirm" | **Deleted**, not merely excluded | its csproj was removed at `67aa8a3e`; only untracked `bin/`+`obj/` remain, including the tree's only `net8.0` `.csproj`-shaped artefact under `obj/Debug/net8.0/WorkerExtensions/` — exclude `**/obj/**` from every project inventory |
| Breaking-change list did not mention `Default*AfterMap` | **A 7th compile break: 6 call sites** | binary grep of `ShiftSoftware.ShiftEntity.dll` at both versions — present @ 7.31.1, absent @ 8.30.1. Now SPIKE-11. |
| `ADP.WarrantyClaims.Data/Services/WarrantyClaimService.cs` listed as an `IMapper` port site | **Dead field — delete, do not port** | no `.Map` call exists anywhere in the file |

### Corrections made after adversarial review of this plan

| Claim in an earlier draft of this plan | Verified | How verified |
|---|---|---|
| "12 projects reference `ADP.Models`, 9 carry their own `ShiftEntity.Model` pin; bumping the hub turns all 9 red" | **14 reference it; 3 of those carry a direct pin.** There are only **7** such pins repo-wide. A Models-only bump yields **3** NU1605s | `grep -rn "Models\.csproj" --include=*.csproj` (14 consumers) and `grep -rn "ShiftSoftware.ShiftEntity.Model" --include=*.csproj` (7 lines) |
| "there is no core-first order that stays green" | **Refuted.** `ADP.Surveys` has no in-repo dependency at all and can be bumped alone today | every `ProjectReference` in `ADP.Surveys/**` is intra-group; no `ShiftSoftware.ADP.*` `PackageReference` in the group |
| "23 csproj files to bump" | **22 files, 29 lines**; three files carry multiple lines (`ClaimableItems.Data` 4, `WarrantyClaims.Data` 4, `Surveys.Data` 2), not four | `grep -rl` / `grep -rn` for `ShiftSoftware\.Shift.*2026\.7\.31\.1` over `*.csproj` |
| "`ImportADPPackagesViaProjectReference=false` forces package mode" | **The property is dead** — declared in `GlobalSettings.props:4`, read by nothing. The real switch is `Condition="Exists(...)"` in 18 reference pairs across 14 csproj, always true in a checkout | `grep -rn ImportADPPackagesViaProjectReference --include=*.csproj --include=*.props --include=*.targets` → 1 hit, the declaration |
| "`ADP.Menus` — 8 projects" | **11 csproj**; 8 is the count *carrying a `2026.8.30.1` reference*. The three omitted are `ADP.Menus.Generation`, `samples/ADP.Menus.Sample.Functions`, `samples/ADP.Menus.Sample.FreeServiceParity` | `find ADP.Menus -name '*.csproj' -not -path '*/obj/*'` |
| "the WarrantyClaims dealer exposure shows no diff under a full-access token" | **False — a full-access run does catch it.** `DealerFinancialController` is a distinct route with a distinct DTO, not a privilege-filtered projection; its only gate is a `CanRead` a full-access principal passes, and `DealerFinancialRepository` applies no row scoping | `DealerFinancialController.cs:21-42`, `DealerFinancialRepository.cs` (bare `base(db)`) |
| "one `ADP.EndpointParity` test project" | **Cannot compile while any group is mid-migration** — one assembly referencing all five groups' `.Data` goes red the moment a group takes its bump and stays red until that group's profiles are gone, which makes that group unverifiable from inside its own step. Split into a group-agnostic `Harness` library plus one test project per group. *(Still true after the reorder: steps now end green, but a monolithic test project is red for the whole of every group step, which is exactly when it is needed.)* | `ClaimableItems.Data`, `Surveys.Data`, `WarrantyClaims.Data` are exactly the three projects holding `: Profile` classes, so each stops compiling the moment its own group takes the bump |

One further correction, to the recorded test baseline: `ADP.Menus.Tests` is **262 passed / 2 failed /
0 skipped (264 total)**, not the remembered `259 / 2 / 1`. The pass count is Cosmos-emulator
sensitive (±1) and the fail count is local-SQL-state sensitive (±2). Compare on the same machine
state, or filter out `SampleDataSeedingTests` and `ServiceMenusProvisioningTests` first.

---

## How to update this file

Keep it accurate or it is worse than nothing. When you finish a piece of work:

1. **Update the row, not the table's prose.** One row per step. Narrative goes in its **named
   section below** (`## Recorded baselines`, `## Package-reference ownership`) — never in a ledger
   cell.
2. **`DONE` requires:** the step's code work complete, its projects building **green**, its tests at
   baseline.
   Fill in `Date`. Leave `Verified by` empty.
3. **`VERIFIED` requires:** the step's Verification section run to completion with every diff either
   explained in the commit message or accepted with a recorded reason. Fill in `Verified by` with
   the concrete artifact — the report path and the run, e.g.
   `parity.ps1 verify -Group Surveys → reports/surveys/diff.md, 0 unexplained`. **"Looked fine" is
   not a value for this column.** Never set `VERIFIED` on the strength of a green build alone.
4. **`BLOCKED` requires:** a spike ID in Notes, and that spike present and `OPEN` in the spike table.
5. **Resolving a spike:** change its Status to `RESOLVED — <one-line finding>` in place. Do not
   delete the row; the finding is the record of why the plan says what it says.
6. **Update the "Last updated" date at the top.**
7. Commit this file **with** the work it describes, in the same commit. A status change in a
   separate commit is how ledgers drift. **Before Step 00, commit the reorder** — the twelve original
   plan files are tracked as of `4c4b3142`, but `08-harness-removal.md` and the 2026-09-01 edits to
   `README.md`, `STATUS.md`, `04`, `06` and `07` are not yet committed, and rule 7 cannot be obeyed
   against an uncommitted plan.

---

## Recorded baselines

Filled in by Step 00 item G, on the pre-bump tree. **Step 07** compares against these. **Record the
machine state alongside them** — several numbers are emulator- and local-SQL-sensitive.

**Machine state for these numbers:** Windows 11, .NET SDK `10.0.400`, SQL Express reachable at
`localhost\sqlexpress` with integrated security, Cosmos emulator **not** running, Azurite **not**
running. Several figures below are emulator- and local-SQL-sensitive; compare on the same state.

| Measure | Baseline | Recorded on |
|---|---|---|
| `dotnet build ADP.sln` | **exit 0, 0 errors.** Project count: **53 pre-harness** (the figure Step 08 returns to after deleting the six parity projects) and **59 post-harness** (the figure **Step 07** compares against — 53 + `Harness` + 5 group projects, exactly as this step predicted). 54 assemblies emitted on a cold build. | 2026-09-01 |
| compiler warnings | **580 cold** (`--no-incremental`), **519 warm**, **482 unique warning lines**. ⚠️ **The plan's expected 535 was not reproduced, and the metric is not "stable warm and cold" as `00`'s §G asserts** — an incremental build skips up-to-date projects and therefore their warnings, so the number is build-state-dependent by construction. **Use 580 cold as the comparison figure at Steps 06/07 and always compare cold-to-cold.** Top codes: CS8632 ×222, CS8618 ×130, NU1903 ×84, CS8602 ×76 (raw line counts, which MSBuild double-reports). | 2026-09-01 |
| `SHENGEN004` | **10** — `ClaimableItems.Data` **5**, `Surveys.Data` **3**, `WarrantyClaims.Data` **2**. **Zero in Menus.** Exactly as the plan predicted. | 2026-09-01 |
| `SHENGEN007` / `008` / `010` | **0 anywhere.** | 2026-09-01 |
| `NU1605` / `NU1701` / `NU1603` / `MSB3277` | **0 anywhere.** (The harness itself briefly introduced an NU1605 by pinning `System.IdentityModel.Tokens.Jwt` **8.14.0** under `EFCore.SqlServer`'s transitive **8.19.2**; pinned up to 8.19.2 so the harness does not perturb this baseline.) | 2026-09-01 |
| `NU1903` (AutoMapper CVE) | **42 unique lines across 21 projects** — exactly as the plan predicted. This is the upgrade's scoreboard and should fall. | 2026-09-01 |
| .NET tests | **1544 total: 1533 passed, 2 failed, 9 skipped** — exactly as the plan predicted. Per assembly: Hawta 502 (493+9 skipped), Menus 264 (**262 + the 2 known `SampleDataSeedingTests` failures**), LookupServices.BDD **452/452**, Surveys.Shared 182, Darlastic.Engine 49, Lookup.Services 47, Cases.Shared 43, Darlastic.Shared 5. | 2026-09-01 |
| web component tests | **114 passed, 4 suites** — exactly as the plan predicted. | 2026-09-01 |
| generated trees clean (`src/global/types/generated`, `ADP.Docs/Docs/docs/generated`, `ADP.TestData/environments`) | **CLEAN** — `git diff --exit-code` over all three returns 0 after a full cold `dotnet build ADP.sln`. ⚠️ **But a FOURTH generated tree the §G check does not name is NOT clean: see the note below.** | 2026-09-01 |

### ⚠️ A fourth generated tree, not covered by §G — `ADP.LookupServices.BDD/Features/*.feature.cs`

A full `dotnet build ADP.sln` rewrites **32 tracked Reqnroll-generated `.feature.cs` files**
(2531 insertions / 2531 deletions). The churn is **semantically empty**: Reqnroll renumbers its
generated local table variables (`table1` → `table31`, …) because the counter depends on
compilation order, not on the feature files. Nothing about the tests changes.

**Why it matters anyway, in two places:**

1. **Step 00's own exit criterion** — *"Nothing outside `ADP.EndpointParity/`, `tools/`, `ADP.sln`,
   `.gitignore` is modified"* — is violated by **merely building the solution**, before anyone
   edits anything. The criterion is satisfiable only if these files are reverted after each build,
   which is what was done here.
2. **Steps 06 and 07** use the generated-tree diff as evidence that the upgrade did not change
   generated output. This tree will produce a large phantom diff every time and, being 32 files of
   real-looking C#, is exactly the kind of noise that gets waved through — or worse, hides a real
   change inside it.

**Recommendation for Steps 06/07:** either add these files to the generated-tree check with the
explicit expectation that they churn (and diff them with whitespace/identifier-renaming ignored),
or `git checkout -- ADP.LookupServices.BDD/Features/` immediately after every full build, as this
step did. Do not silently ignore the path.

### ✅ RESOLVED — the explicit-id insertion path (item B, point 3)

**Decision: `SET IDENTITY_INSERT` around raw-SQL inserts. Proven working, and it needs ZERO
production source change** — which is the deciding factor, since `ValueGeneratedNever()` would mean
editing a module's own `IModelBuildingContributor` for a throwaway harness.

Verified end to end on `[Surveys].[Survey]`: an explicit `ID = 5000001` inserted under
`SET IDENTITY_INSERT … ON/OFF` comes back through the **real HTTP pipeline** as
`{"ID":"KJzYkW", …}` — the hash of that long under the pinned salt. That is the whole of Rule 1's
determinism chain demonstrated on a live endpoint.

Two practical notes for the seeder:
- Tables live under the **module's own SQL schema, not `dbo`** — `[Surveys].[Survey]`, singular,
  because `SurveyModelBuilderExtensions` calls `entityType.SetSchema("Surveys")`. `dbo.Surveys`
  does not exist.
- The identity column is `ID` on every ShiftEntity table.

### 🐞 PRE-EXISTING BUG FOUND — `revisions` and `asOf` are 500 in the Surveys sample

Not caused by this work, and not caused by the upgrade. Recorded here because the harness has to
decide what to do about it, and because it is worth fixing on its own merits.

`GET /api/Surveys/Survey/{id}/revisions` and `GET …/{id}?asOf=…` both return **500**:

> `Temporal FOR SYSTEM_TIME clause can only be used with system-versioned tables.
> 'Surveys.Survey' is not a system-versioned table.`

**Why.** `Survey` (and 40 other entities repo-wide) carry `[TemporalShiftEntity]`, and
`SurveyModelBuilderExtensions.cs:137-148` dutifully calls `entityType.SetHistoryTableSchema("Surveys")`
for them — **but nothing ever calls `.IsTemporal(true)`**. EF's own model confirms it: every Surveys
entity reports `IsTemporal=False`. So the tables are created without system-versioning while the
inherited repository code still emits `FOR SYSTEM_TIME` SQL against them.

**This is schema-creation-independent** — reproduced identically under `EnsureCreated` **and** under
`Database.Migrate()`. It is not an artifact of the harness's disposable database.

**Consequence for the harness.** Two of the plan's case kinds (`REVISIONS`, `ASOF`) cannot be
exercised for Surveys, and capturing them would violate the "0 5xx" gate. They go into
`parity.psd1`'s `excludedRoutes` with this reason. **The temporal mapper path is therefore
UNVERIFIED for this group** — state that plainly rather than letting a green run imply otherwise.

**Also discovered in passing:** the Surveys sample's migrations have drifted from its model —
`Database.Migrate()` throws `PendingModelChangesWarning`. The harness uses `EnsureCreated` anyway
(the plan's original choice), which sidesteps it.

### ✅ Surveys — captured, stability-gated, and PROVEN to detect a regression

**The first group is complete.** `ADP.EndpointParity/baselines/surveys/` holds **76 goldens**
across both grants, plus the route catalogue.

| Gate | FullAccess | Restricted |
|---|---|---|
| cases | 45 | 30 |
| 5xx | **0** | **0** |
| `CREATE 2xx` | **3/3** | 0/3 — a refused CREATE is the correct answer for a read-only principal |
| `UPDATE 2xx` | **3/3** | n/a |
| catalogue routes covered | **52/52**, 23 excluded with written reasons | **52/52** |
| hostile rows in list bodies | **6/6** | 5/6 — `SurveyInstance` is invisible to this principal, which is the point of the pass |
| **stability gate** | **byte-identical over two consecutive captures** | **byte-identical** |

**The instrument was validated, not just run.** Injecting a single changed value into the seed
(`PublishedVersionNumber` 7 → 4242, the exact shape of trap 3-write) produced a diff on three cases
with precise JSON paths — `$.response.body.Entity.PublishedVersionNumber: 7 -> 4242`. Reverting it
returned `verify` to clean. A harness that has never been shown to fail is not evidence of anything.

**What the stability gate caught before it could poison a baseline** — all three fixed by making
values deterministic or by a narrow, named normalization, never by widening a rule:

1. **`BankEntryID`** — minted with `Guid.NewGuid()` inside `BankQuestionRepository` on create, with
   no seam to pin. Sanctioned Rule 1 fallback, scoped as tightly as possible: guids the **seed**
   wrote still compare literally, so a wrong `BankEntryID` on a seeded row is still a diff; only the
   freshly-minted one on the created row is tokenised.
2. **Print-token bodies** — `expires=<stamp>&token=<signature over that stamp>`. Volatile by
   construction. The two fields are tokenised; the body's **shape** is still compared.
3. **`CreateDate` inside the UPDATE request body** — the request side was being stored raw. Request
   bodies are now normalized into the golden with the same Rule 2 name allowlist as responses. The
   bytes actually **sent** are untouched.

**Five real bugs in the harness that a less strict gate would have hidden.** Each was surfaced by a
gate refusing to pass, and each would have produced a confidently green run over nothing:

- Every `UPDATE` returned **409** — the framework enforces optimistic concurrency on `LastSaveDate`,
  and a PUT body omitting it sends `DateTime.MinValue`. The whole update path covered nothing while
  `CREATE` looked perfectly healthy. Fixed by merging the hand-authored body over the row as the
  server last rendered it.
- `DELETE` ran **before** `PUT`, because cases were issued in catalogue order and the catalogue
  sorts methods alphabetically — so the created row was deleted before UPDATE could touch it. Cases
  now run in round-trip lifecycle order.
- Canonical JSON **sorts keys**, which moved a polymorphic DTO's `type` discriminator out of first
  position and 500'd the request. Sorting is a presentation rule for goldens and must never touch a
  body being sent.
- `LIST.afterRemove` was issuing a literal `"LIST"` HTTP verb → 405.
- Stale goldens from a previous case list **survived** a re-capture, where a later `verify` would
  have compared against cases the harness no longer issues.

**A seed defect caught as a 500, not tolerated as one:** `QuestionJson: "{}"` deserializes to null
for a polymorphic `QuestionDto` and made `GET {id}` throw inside the mapper. The seed now carries
real DTO payloads.

**Recorded limits for this group — a green Surveys run does NOT cover:**
- `revisions` and `asOf` (pre-existing 500s; see the bug note above). The temporal mapper path is
  **unverified** here.
- `SurveyInstance` writes — every verb is 405; it needs a mapper-level write golden instead.
- The hand-written controllers (`Preview`, `Publish`, `PublicSurvey`, `SurveyResponses`, `Triggers`)
  — plain `ControllerBase`, no ShiftEntity triple, so a *mapper* upgrade cannot silently change
  them. **If this migration ever touches serialization or routing rather than mapping, they must be
  covered before any parity claim is made.**
- Trap 1 (literal), trap 2 and trap 3-read **do not exist in this group** — verified, then
  adversarially re-checked, and deliberately not faked. Trap-shaped rows are seeded anyway so the
  traps would be caught if the upgrade *introduces* them.

### ✅ Menus — captured, stability-gated, richest trap coverage in the plan

`ADP.EndpointParity/baselines/menus/` — both grants, byte-identical over two consecutive captures.

| Gate | FullAccess | Restricted |
|---|---|---|
| cases | 89 | 64 |
| 5xx | **0** | **0** |
| `CREATE 2xx` | **5/5** | 0/5 (refused — correct) |
| `UPDATE 2xx` | **5/5** | n/a |
| catalogue covered | **112/112**, 47 excluded with reasons | **112/112** |
| hostile rows | **11/11** | 6/11 (5 invisible to a read-only principal) |
| stability gate | **byte-identical** | **byte-identical** |

This is the group the whole exercise is aimed at: **trap 1 ×6, trap 2 ×4, trap 3-write ×3**, every
one confirmed at a cited line and adversarially re-verified. The seed carries soft-deleted children
at two nesting depths, three link rows whose PK differs from every foreign id they carry (including
a two-hop `MenuItem → RIVM → ReplacementItem` case with three wrong answers available), and a
repository-derived `BrandID` set to a value no derivation could produce.

**Findings from the Menus capture:**
- **The temporal bug is REPO-WIDE, not a Surveys quirk.** 24 of the first Menus capture's cases were
  500s — every `revisions` and `asOf` case — with the same "not a system-versioned table" error.
  Same root cause: `[TemporalShiftEntity]` without `.IsTemporal(true)`.
- **`MenuVersionRepository.UpsertAsync` throws `NotImplementedException`.** MenuVersion has no
  working write path at all; it is now in `writeUnreachable`.
- **Unique indexes apply to soft-deleted rows.** A soft-deleted "duplicate" of a live link row is
  rejected by the index, so a trap-1 link row must differ in an indexed column rather than being a
  copy flagged `IsDeleted`.
- **A `_marker` needs a string column to live in.** MenuVersion has none, so it carries none — the
  gate would otherwise hunt for text the row cannot hold.

**Declared gaps, written down rather than glossed:** write-path parity is not captured for
`VehicleModel`, `BrandMapping`, `LabourRateMapping` (their bodies need a ShiftIdentity `Brand` row
the sample never seeds) or `MenuVariant` (its body needs a country-scoped labour rate). All four
keep **full read-path coverage**, which is where their trap 1 and trap 2 sites live.

### ✅ ClaimableItems and WarrantyClaims — captured, stability-gated

Both run on the MOUNTED host (neither group ships a sample API). Both grants captured, both
byte-identical over two consecutive runs.

| Gate | ClaimableItems Full / Restricted | WarrantyClaims Full / Restricted |
|---|---|---|
| cases | 30 / 30 | 28 / 28 |
| 5xx | **0 / 0** | **0 / 0** |
| catalogue covered | **63/63**, 33 excluded | **94/94**, 70 excluded |
| hostile rows | **5/5 / 5/5** | **4/4 / 4/4** |
| stability gate | **byte-identical** | **byte-identical** |

**🎯 The highest-risk baseline in the migration now exists.** `GET /DealerFinancial` returns claim
8000001 with all five distributor-side members — `DistComment1`, `HourTotalDistributor`,
`LaborTotalAmountDistributor`, `SubletTotalAmountDistributor`, `PartsTotalAmountDistributor` —
as **null**, while the same five columns are **non-null in the database**. That is the trap 3-read
captured exactly as `verification.md` §8.7 requires. If the generated convention mapper starts
matching them by name, the diff reads `null -> 11.11` and cannot be missed.

Captured under a **dealer** principal deliberately: `IWarrantyClaimsCapabilityProvider.IsDistributor`
drives the "DTO distributor-field stripping in ViewAsync" its own doc comment describes, and `false`
is the configuration in which those five members must be blank. `GET /DistributorFinancial` correctly
answers **401** to that principal — a live demonstration that the dealer/distributor split works.

#### The blocker that held both groups, and what it actually was

Every entity implementing `IEntityHasCompany` / `IEntityHasCompanyBranch` listed as
`{"Count":0,"Value":[]}` **with a 200** — a healthy-looking baseline proving nothing. Ruled out one
at a time: matching the rows' `CompanyID`/`CompanyBranchID` to the principal's claims; setting them
NULL; registering `RegisterIdentityHashId`. An in-request probe then showed the claims resolve
correctly (`GetCompanyID() = 1`), which eliminated the claim side entirely.

**Cause: the framework's default data-level access is a PERMISSION check, not a column match.**
`DefaultDataLevelAccess.HasDefaultDataLevelAccess` denies unless the principal actually holds
data-level access, and that grant lives on the **identity** action tree — which neither mounted host
was registering or granting. Adding `ShiftIdentityActions` to both the registered trees and the
access-tree claim unblocked both groups at once.

The tell was there the whole time: `ServiceCampaign` and `AdditionalLaborOperationCode` — the only
two entities with **no** `CompanyID` column — were the only two that ever listed.

#### Consumer wiring the mounted host had to supply

Each found by a capture failing, each a genuine difference from a sample host, and together the
clearest evidence for why the plan rates this mode "one notch below":

- an **authentication scheme** — `[Authorize]` controllers 500 with "No authenticationScheme was
  specified" without one (`ParityAuthenticationHandler`, modelled on the repo's own
  `DevAuthenticationHandler`);
- `AddShiftEntityPrint` — the inherited print-token route needs `ShiftEntityPrintOptions`;
- `AddHttpClient`;
- `SharedClaimService`, `WarrantyClaimService`, `DeliveryDateService` — repository constructor
  dependencies the module does not register for itself;
- `IWarrantyClaimsCapabilityProvider` — no default implementation ships outside `.Web`.

#### Seed lessons that generalise

- **`CertificateType` discriminates the shared `ADP.Cases` Certificate** (SPIKE-8 territory):
  `0 = WarrantyClaim`, `1 = ClaimableItemClaim`. Seeding the wrong value yields an empty list with a
  200 — the same silent shape as a scoping mismatch. `WarrantyInvoice` additionally filters
  `InvoiceDate.HasValue`.
- **A non-nullable navigation dereference in a list projection 500s the whole list.**
  `ClaimableItemListDTO` maps `CampaignName` from `src.Campaign!.Name`, so a `ClaimableItem` with no
  campaign produced "Nullable object must have a value". Seed relationships, never leave them dangling.
- **A `_marker` needs a string column the LIST DTO actually exposes.** `ManufacturerSettlmentSheet`
  exposes only ID / InvoiceNumbers / IsDeleted, so it carries none — same as `MenuVersion`.

#### Declared gaps

**Write-path parity is not captured for either group.** The writes are reachable; what is missing is
a hand-authored minimal-valid body per entity, and those are substantial (`ItemClaim` and
`Certificate` each need several resolvable FKs plus validator-satisfying fields). A body that 4xxs
would cover nothing while every gate stayed green — the exact failure the 100% CREATE gate exists to
prevent — so the gap is recorded rather than papered over.

**What that costs, precisely:** ClaimableItems' three trap3-write sites
(`Certificate.CertificateNo`, `Certificate.DisplayDistributorCertificateNo`,
`ItemClaim.ClaimNumber`) are not covered. Their **read-path** traps are: ClaimableItems' trap 2 (the
`ItemClaim` link row, seeded with deliberately divergent ids) and WarrantyClaims' trap 3-read (the
five distributor members) are both fully captured.

### ⚠️ TWO LIMITS ON WHAT THESE BASELINES PROVE — found by an adversarial review, not by the runs

Both were surfaced by a five-angle investigation into the data-level blocker, which decompiled
`ShiftEntity.Web` and executed the real TypeAuth API. Both were then confirmed here directly. They
do not invalidate the baselines; they bound what a green run may be claimed to mean.

**1. The restricted pass is a real control for only two of the four groups.**

Comparing FullAccess against Restricted response-for-response, on cases both passes run:

| Group | shared cases | responses that DIFFER |
|---|---|---|
| Menus | 64 | **25** — a genuine control |
| Surveys | 30 | **7** — a genuine control |
| ClaimableItems | 30 | **0** — a duplicate |
| WarrantyClaims | 28 | **0** — a duplicate |

The two sample-host groups behave as intended: a read-only principal is refused writes (403), and
Menus additionally hides rows (404 on DETAIL for entities it cannot read). **The two mounted groups
produce byte-identical responses under both grants**, because neither module's own gate is armed —
`EnableClaimableItemsActionTreeAuthorization = false`, and WarrantyClaims takes consumer-supplied
actions that are all null. Their restricted baselines are therefore a second identical run, not a
privilege control.

*They are still worth keeping* — they cost nothing, and they will start differing the moment a step
arms either gate. But **do not cite them as evidence that privilege scoping survived the upgrade.**

**2. NO pass on ANY group exercises row-level data scoping.**

The default filter is `WhereIn(GetAccessibleCompanies(), x => x.CompanyID)`
(`DefaultDataLevelAccess.ApplyDefaultDataLevelFilters`). A grant of `[1]` or `[1,2,3,4]` on
`ShiftIdentityActions` resolves to **WildCard**, `ConvertIds` returns **null**, and `WhereIn` then
leaves the query untouched. So no transcript in any baseline contains a `CompanyID IN (...)`
predicate — the filter is present in the code path and inert in the data.

The middle case a real tenant hits — a principal scoped to specific company ids — is **uncovered on
every group**, sample-host and mounted alike. A regression in the default-filter path would be
invisible to all 348 goldens.

Exercising it needs a nested per-id access tree
(`{"ShiftIdentityActions":{"DataLevelAccess":{"Companies":{"1":[1]}}}}`), which
`ParityAuth.BuildAccessTree`'s `Dictionary<string,int[]>` signature cannot express. That is a
deliberate deferral, not an oversight — recorded here so Steps 03-05 do not assume coverage the
baselines do not have.

**Why this is written down rather than fixed:** both are pre-existing properties of the estate, not
regressions, and closing either means either arming module gates that ship disarmed or widening the
access-tree model. Either would make the baseline LESS representative of how these modules actually
run today.

### ✅ Step 01 — the harness graded itself against a known-answer migration, and passed

**This is the calibration result the whole plan depends on.** `ADP.Menus` was migrated at `14caf7c9`
with a reviewed diff, so the set of behaviour changes it *should* produce was knowable in advance.
The harness was run across it blind and found exactly that set — nothing more, nothing less.

**Method.** `git worktree` at `14caf7c9^` (= `9b927a8d`), the **same harness source** copied in
verbatim, captured under both grants, then replayed against migrated `master`.

**The harness compiled in the worktree with ZERO changes**, against `ShiftEntity 2026.7.31.1` with
AutoMapper still referenced. That is Step 00's capture-layer purity rule (HTTP + JSON + string only)
proving itself: the plan said a compile failure there would be the proof the rule had been violated,
and there was none.

Both sides passed every gate identically — 89 cases, **0 5xx**, `CREATE 5/5`, `UPDATE 5/5`,
catalogue **112/112**, hostile rows **11/11**.

**Every diff, classified (item C).** 15 under FullAccess, 2 under Restricted, and **all 17 are a
single shape**:

```
$.response.body.Entity.VehicleModel.Text                  ABSENT in baseline, present now   (×7)
$.response.body.Entity.StandaloneReplacementItemGroup.Text ABSENT in baseline, present now  (×8 + 2)
```

| Bucket | Count |
|---|---|
| Expected — framework convention improvement | **17** |
| Expected — known migration change | 0 |
| **Harness bug** | **0** |
| **Real regression in shipped `master`** | **0** |

`conventions.md:110,116-118` predicted this exact diff before it was observed:
*"the selector now carries `Text` where the old profile left it null. That will show up as a diff in
the harness. It is expected — record it as an accepted change, do not suppress it."*
`MappingHelpers.ToSelectDTO(entity.SomeID)` fills `Text`; the old profile built
`ShiftEntitySelectDTO { Value = … }` and left it null. **Additive only — no value changed and no
member disappeared.** Recorded via `parity.ps1 accept` in `baselines/menus/accepted.md`.

**The three known-decision audits from item C came back clean.** No child collection grew (the two
deliberately-unfiltered-for-soft-deletes collections did not move), no list column came back empty
(which is what a swallowed `SHENGEN007` would look like), and the `MenuVariant.Items` round-trip
produced no unexpected removal.

**Item D — the fallback assertion was DEMONSTRATED, not asserted.** `GET /api/Menu/<deleted route>`
returns **200 `text/html`** from the sample's `MapFallbackToFile`, and the harness converts that into
a hard failure. Guarded permanently by `FallbackAssertionTest`, which fails if the rule is ever
removed. Without it, an endpoint disappearing in the upgrade would read as an ordinary success.

**Item E — the route catalogues are byte-identical** pre and post migration. The Menus work was a
mapper change, not a routing change, and the catalogue confirms it.

**One harness defect found and fixed here** (Step 00 scope, per this step's rollback note): a CLEAN
`verify` left the PREVIOUS run's `diff.md` on disk, so the reports directory still advertised
differences that no longer existed — the same failure shape as a stale golden. `ParityGroupRun` now
deletes the report when a verify produces none.

**Verification figures, pinned for Step 06** — which bumps `Lookup.Services.DuckDB`, referenced by
`ADP.Menus.Tests:58`, and runs LAST, so this number must survive the gap in writing:

| Measure | Figure |
|---|---|
| `ADP.Menus.Tests` | **262 passed / 2 failed / 0 skipped (264 total)** — exactly the plan's expected value. The 2 are the known `SampleDataSeedingTests` duplicate-key failures on the drifted local sample DB. |
| `ADP.Menus.Sample.Functions` | builds, **and starts**: "Worker process started and initialized", all 6 functions registered, so `AddShiftEntityCosmosDbReplication<MenuReplicationDB>()` ran without throwing — the `conventions.md` §6b compile-break concern is clear for this group. |
| `ADP.Menus.Sample.FreeServiceParity` | builds clean. |
| `ADP.Menus.Generation` | **no fix was needed**, so the `LookupServices.BDD` figure is unmoved at **452/452** and Step 06 has nothing extra to re-check from this step. |

**Ten Menus goldens changed in this step, and none of them is a behaviour change.** They are
`LIST.afterRemove` cases picking up a harness fix made during Step 00's mounted-group work: that
case was being issued with no `$top`, which the framework refuses for a page-size-capped principal.

- 5 **FullAccess** goldens: the request URL gained `$top=25`. Response bodies **byte-identical**.
- 5 **Restricted** goldens: went from **400 "Please specify a page size using the $top query
  parameter"** to **200 with a real body**. Five cases that were banking an error are now live
  coverage.

This is a coverage improvement, not a value change, and it is the reason the goldens must be read
rather than waved through: the same diff shape (a 400 becoming a 200) would be alarming if the
harness had not caused it deliberately.

**What this licenses.** From Step 03 onward, a diff the harness reports is evidence about the code,
not about the instrument. That is the entire reason this step ran second.

### ✅ Step 02 — `ADP.Darlastic` bumped, and the framework-only control was RECOVERED

**`CLOSED`. Read the caveat before the numbers: this is a SMOKE result, not value parity.**
0 repository triples and 0 AutoMapper profiles means there is no mapping behaviour here to regress.
A green run proves the routes still exist and still respond. It proves **nothing** about mapper risk,
and must never be cited as if it did.

**The bump — item A, four lines, zero source changes.**

| csproj | package | 7.31.1 → 8.30.1 |
|---|---|---|
| `Darlastic.API:46` | `ShiftEntity.Web` | ✔ |
| `Darlastic.Data:53` | `ShiftEntity.EFCore` | ✔ |
| `Darlastic.Shared:33` | `ShiftEntity.Model` | ✔ |
| `Darlastic.Web:33` | `ShiftBlazor` | ✔ |

`TypeAuth` untouched at `1.6.28`. All **seven** projects in item B — `CaseBrowser` included — build
clean with **no source change at all**, which is the cleanest possible reading of "the framework API
surface did not break this group".

**The headline invariant holds.** `dotnet build ADP.sln` → **exit 0, 0 errors, NU1605 = 0**, with the
shared floor still at `2026.7.31.1`. The mixed arrangement the plan predicted works as predicted.

**The scoreboard moved.** `NU1903` (the AutoMapper CVE) fell from **42 lines across 21 projects** to
**38 across 19** — Darlastic dropped off, because at `8.30.1` the transitive AutoMapper dependency is
gone. First measurable reduction of the thing this upgrade exists to remove.

**Tests unchanged:** `Darlastic.Shared.Tests` **5/5**, `Darlastic.Engine.Tests` **49/49**, identical
before and after the bump.

**Items C and D came back empty**, as predicted: no `AutoMapperProfiles`, no `: Profile`, no
`ShiftRepository<`, no `IMapper`, no `AddAutoMapper`, no `Replicate<` / `UpdateReference<`, and no
direct `TagProjection` / `TaggableProjectionExtensions` / `AddShiftTagging` reference.

**What the capture actually covers, precisely.** 31 catalogue routes, **31/31 accounted for**, of
which **30 are excluded with a written reason** and 2 run live:

- `GoldenCustomer.LIST` — an OData envelope over a `ToView` entity
- `StewardQueue.LIST` — a hand-written action returning framework-shaped JSON
- plus the **route catalogue itself** (40 endpoints) as its own golden

The 28 excluded hand-written actions take query parameters, ids and POST bodies over registry state
the parity database does not hold. **Nothing mapper-shaped is lost** — a mapper upgrade cannot
silently change a hand-written action, because there is no mapper in the path. What this group does
cover is the thing only it can: a framework change to serialization, the OData envelope or
ProblemDetails shape surfaces here, unconfounded by any mapper rewrite.

**One harness bug found and fixed.** The restricted-grant gate required "at least one hostile row
visible", which a group with **no seed at all** can never satisfy — `0 > 0` failed Darlastic forever
for a reason unrelated to its behaviour. Now exempted when `HostileRowsExpected == 0`.

**⚠️ Item E — a product decision is OUTSTANDING, not made.** ShiftBlazor `8.30.1` adds a Find box and
an automatic ID filter to every list grid **by default**, opt-out via new `DisableFind` /
`DisableIdFilter`. This group has exactly **one** affected page —
`ADP.Darlastic.Web/Pages/GoldenCustomerList.razor` — and it currently takes the new defaults, because
nothing opts out. **That is an accepted default, not a reviewed decision.** It needs eyes on the
rendered page; no build or harness can settle it. (Also noted: a ShiftBlazor regression crashing
every page that renders a `ShiftList` was introduced *and fixed* inside the 7.31.1 → 8.30.1 window,
so do not land on an intermediate ShiftBlazor version.)

*Housekeeping, deliberately NOT fixed inside an upgrade commit — the plan says so explicitly:* the
sample's `appsettings.Development.json:10` hard-codes a registry database name carrying a
region-style suffix. Against this repo's client-agnostic rule that is worth a separate look.

### ✅ Step 03 — `ADP.Surveys` migrated, and the harness earned its keep twice

**Result: clean under both grants.** 49 cases, 52/52 catalogue routes covered (exclusions cut 23 → 19),
0 5xx, gates pass. `ADP.Surveys.Shared.Tests` 182/182, `ADP.Surveys.Data.Tests` 2/2. Four triples
rewritten, the 151-line profile deleted, three registration calls removed.

#### The finding that justifies diffing bodies rather than reading profiles

`Tags` on `BankQuestionListDTO` and `ScreenTemplateAdminDTO` went from `[]` to **absent** on 15 cases.

The plan said, correctly quoting the profile source, that `SplitTags` *"yields null, not an empty
list"* — and the old profile called that same helper. Following the instruction faithfully would have
been a **wire-contract change**, because **AutoMapper's `AllowNullCollections` defaults to `false`**
and was silently coercing that null into `[]` on the way out. Every response this endpoint has ever
served carried `"Tags": []`; no reader of the profile could have known.

Preserved deliberately with `?? new List<string>()` and commented at both sites as behaviour we now
own rather than a framework default acting behind us. **This is the case the whole harness exists
for:** it is invisible in the source, invisible in a shape assertion, and visible only in a value
diff against a real recorded body. Rule 5 keeping `null`, `[]` and *absent* distinct is what surfaced
it.

#### SPIKE-3 — confirmed in the emitted code, not just by experiment

`__shiftListProjection` for `SurveyInstance` emits `Status = (int)e.Status` by convention, so the old
profile's `ForMember(Status)` was **deleted rather than restated** — a redundant `ForList` is
indistinguishable, to the next reader, from one doing real work. The two JSON-parsing members
(`BankQuestion.Type`, `ScreenTemplate.QuestionCount`) carried over verbatim into `ForList` and behave
identically, client-evaluated in the final `Select`. The inherited limit is unchanged and
**pre-existing**: `$orderby=Type` / `$orderby=QuestionCount` still 500.

#### SPIKE-4 — the premise was wrong, and the correction is the better test

Both mandated round-trips pass, but the second one asserts the **opposite** of what the plan
anticipated. `BankQuestion.BankEntryID` is **part of a key** (`SurveyAnswer.BankEntryID` carries an FK
to it), so EF refuses to modify it on a tracked entity: *"The property 'BankQuestion.BankEntryID' is
part of a key and so cannot be modified."* That is a **schema** constraint that applied equally to the
AutoMapper profile — so the profile's comment about *"still allow updates from authenticated admin
flows"* was aspirational; the database has never permitted it.

What survives is a sharper probe, because the failure is the discriminator:

| Implementation | A different GUID on update |
|---|---|
| `IgnoreEntity` | write never attempted → request **succeeds**, value silently unchanged |
| `ForEntity` (conditional) | write **is** attempted → EF rejects it → request **fails** |

So `A_different_guid_on_update_is_ATTEMPTED_not_silently_ignored` asserts a *failure* as its pass
condition. Paired with the `Guid.Empty` test proving the skip branch, both directions of the old
`.Condition(...)` are pinned — which a success-asserting test could not have done.

#### `SurveyInstance` — the write mapper no HTTP request can reach

Recorded `writeUnreachable` in `parity.psd1`, with the substitute in place:
`ADP.Surveys/ADP.Surveys.Data.Tests/SurveyInstanceWriteMapperGoldenTests.cs`. It diffs **every scalar
property** across `MapToEntity` and asserts the written set is exactly the five audit members
(`CreateDate`, `LastSaveDate`, `CreatedByUserID`, `LastSavedByUserID`, `IsDeleted`) and **none of the
sixteen domain members** — matching the old bare `.ReverseMap()` over a DTO whose only own member is
`ID`. Writing any domain member would blank live scheduler state with a default.

Two deliberate choices: it **diffs reflectively** rather than listing names, so a member added later
is covered the moment it is declared; and it lives in a **new `ADP.Surveys.Data.Tests` project rather
than under `ADP.EndpointParity`**, because Step 08 deletes the harness and the 405s are permanent, so
the substitute has to outlive it. A companion test asserts the diff really watches all sixteen domain
members — which immediately caught a real bug in the golden itself, where `SurveyInstanceStatus`
being declared in the entities namespace made the navigation filter silently drop `Status`, the most
consequential member of the set.

#### Two harness corrections, both found here

1. **The `$top` omission.** The CRUD template omitted `$top` on the `afterRemove` LIST variant, which
   the framework requires. Six cases accepted with recorded reasons. On FullAccess the change is
   provably inert — `git diff` of each re-captured golden is **exactly one line, the request URL**,
   with status and body byte-identical to the pre-migration recording. Under the read-only grant the
   old golden recorded a 400 *"please specify a page size"* envelope and therefore asserted **nothing**
   about the list projection; it now returns 200 with rows. **Stated limit:** those three response
   halves have no pre-migration counterpart and are not a pre/post control — they become one from
   this baseline forward. The projection under that grant is already controlled by the plain `LIST`
   case, which carried `$top` all along and verifies unchanged.
2. **A misleading report header.** `diff.md` rendered *"Cases compared: 3"* on a run that compared
   **34**, because the count was derived from the differences dictionary, which only holds cases that
   differ — making "compared" and "with differences" identical every time. The honest reading of that
   line is that 31 cases had silently stopped being checked. `TranscriptDiffer.Report` now takes an
   explicit `comparedCount`.

Confidence in the Restricted result is worth stating plainly: a **wholesale re-capture** left 27 of
30 goldens byte-identical to their pre-migration recordings, which is an independent check on the
three that changed.

### ✅ Step 04 — `ADP.ClaimableItems`, where the plan's own item list was the least reliable part

**Result: clean under both grants, solution green, 0 SHENGEN warnings.** 5 triples migrated, 4
profiles deleted, 5 Cosmos delegates hand-written, 1 `IMapper` site ported. But the interesting part
is that **three separate classes of silent regression were found that the step plan did not
mention**, and each was caught by a different instrument.

#### 1. The plan's item B would have broken item G

Item B says "delete the four profiles, keep `GeneralMappingHelper.DeserializeDict`". It does not
mention `ClaimableItemProfile.MappingHelpers.DeserializeModelCosts`, which is a **nested class inside
one of the files being deleted** and is called by the surviving `ClaimableItem -> ServiceItemModel`
Cosmos projection. Deleting the profiles as instructed takes it with them. Both helpers now live in
`ADP.ClaimableItems.Data/Mapping/CosmosProjectionHelpers.cs`.

#### 2. Eleven list members that no diagnostic reports — found by diffing the baseline

`SHENGEN004` reports unmapped members on the **view** mapper only, never on the list projection. The
old repositories never called `UseGeneratedMapper`, so they ran on the **AutoMapper-backed** mapper,
and AutoMapper **flattens** `Campaign.Name` onto `CampaignName` by name convention with no
configuration at all — which is exactly why the deleted profiles contain no `ForMember` for any of
them and why nothing looked missing. The generated projection does not flatten.

Found by diffing every pre-migration baseline LIST body against the emitted `__shiftListProjection`:

| Triple | Members that would have silently gone null |
|---|---|
| `ClaimableItem` | `CampaignName`, `CampaignStartDate`, `CampaignExpireDate`, `CampaignActivationTrigger`, `CampaignActivationType` |
| `ItemClaim` | `CampaignName`, `ClaimableItemName`, `ReimbursementCertificate{CertificateDate,InvoiceDate}`, `ContributionCertificate{CertificateDate,InvoiceDate}` |
| `CampaignVinEntry` | `CampaignName`, `CampaignUniqueReference` — the only two the plan caught (item F) |

The baseline is the proof they were live: it carries
`"ClaimableItemName": "PARITY-CLAIMABLEITEM parity claimable item"` on a row the generated projection
would have returned as null. `Validity`, `ValidityModeText`, `ActivationTriggerText`,
`ActivationTypeText` and `InvoiceID` were checked and need nothing — they are computed getters over
members that ARE projected.

#### 3. `AllowNullCollections` — the same trap as Step 03, missed again, caught by an adversarial pass

The 5 replication delegates have **zero** harness coverage (replication is off during parity runs and
failures are swallowed), so the plan's only stated control is line-by-line review. Run as an 8-agent
workflow — one reviewer per map, then independent refutation attempts per finding — it confirmed
**3 real defects in the first draft of the delegates**, all one root cause:

> AutoMapper's `AllowNullCollections` defaults to **false** and nothing in ShiftEntity or this repo
> overrides it. A resolver returning null for a dictionary- or collection-typed member therefore
> reached Cosmos as an **empty** one. Transcribing the profile's `== null ? null : ...` branch
> *literally* does not reproduce the old document — it writes `null` where production has `{}`.

- `ModelCosts` — the helper returns null for **every Fixed-costing item by design**, so this was the
  common case, not an edge case: `[]` -> `null`.
- `PrintoutTitle`, `PrintoutDescription` — nullable columns: `{}` -> `null`.

The verifiers reproduced it empirically against AutoMapper 14.0.0 rather than arguing from the
source. **This is the identical trap recorded for `ADP.Surveys.Tags` in Step 03** — knowing about it
was not enough to avoid repeating it, which is the case for keeping the adversarial pass.

Crucially it bites **only** the two `ForMember`-based maps. `ConvertUsing` bypasses the member mapper
entirely, so the other three maps' nulls really were nulls and are correct as transcribed. The same
distinction governs which maps needed AutoMapper's **convention** members restored: `ForMember` maps
auto-map same-named properties on top of the profile's list (8 extra on `ServiceItemModel`, 5 on
`ServiceCampaignModel`), `ConvertUsing` maps do not.

#### SPIKE-8 — RESOLVED, all four questions

Q1 the mapper lands in the **consumer's** assembly (`ADP.Cases` emits none); Q2 both `Certificate`
triples coexist because the registry keys on the **3-tuple** and Step 05's view DTO differs; Q3
`ADP.Cases.Data` does **not** need registering; **Q4 the generator is content with a mixed floor** —
`ADP.Cases` is still on `2026.7.31.1` and the `8.30.1` generator emitted a working `Certificate`
mapper with zero errors. **No plan change needed**; Step 06's lines stay where they are.

#### SPIKE-11 — RESOLVED, and its blast radius was 5 triples, not 2

The plan lists 4 call sites in 2 profiles. But `DefaultAutoMapperProfile.CreateEntityMaps` applied
**both helpers to EVERY triple automatically**
(`CreateMap(entity, viewDto).DefaultEntityToDtoAfterMap().ReverseMap().DefaultDtoToEntityAfterMap()`),
so triples with no user profile at all were equally affected. The convention supersedes both, and the
two predicted read deltas showed up in the parity run **exactly as predicted from the framework
source before the run was made**: `Text` now populated from the navigation, and a null FK now
yielding a null member instead of `{"Value":""}`. All 8 accepted with that reasoning.

Two write deltas ride along and are **not** exercised by any parity case here: a blank select DTO now
**nulls** a nullable FK (`ToNullableForeignKey`) where the old helper left it unchanged, and
**throws 400** for a required one (`ToForeignKey`). The first is data-loss-shaped.

#### Smaller findings

- **A framework case-sensitivity asymmetry.** `ItemClaimDTO.CampaignVINEntry` (capital VIN) vs
  entity `CampaignVinEntryID`: the generator's **write** convention matches case-insensitively
  (`ToNullableForeignKey(dto.CampaignVINEntry)` binds) but its **read** convention does not, which is
  why `SHENGEN004` named that member and none of the other select members on the same DTO. Fixed with
  an explicit `ForView`; without it the member would simply have started returning null.
- **A null-reference bug in this step's own item-H port, caught by the baseline.** `ViewAsync`
  includes `ClaimableItem` but not `Campaign`, and the baseline records `"CampaignName": null` inside
  the nested claim list for exactly that reason. Routing those rows through the triple's list
  projection in memory would have dereferenced a null navigation. Both flattenings are now guarded,
  which also reproduces the old null rather than "fixing" it.
- **Item H's first suggested option is the wrong one.** A standalone
  `IShiftObjectMapper<ItemClaim, ItemClaimListDTO>` gets the plain convention and would silently lose
  every rule configured on the triple. Routed through the triple's list mapper instead.
- **Trap 1 and trap 2 both have no vehicle here**, confirmed from emitted code rather than assumed:
  no auto-composed collections exist in any of the 5 mappers, and **no pair mappers were generated at
  all**.
- **`CertificateNo` / `DisplayDistributorCertificateNo` are client-writable** — both are declared on
  `ItemClaimCertificateDTO` and written by the entity map. **Pre-existing**: the framework's
  auto-created reverse map carried no `Ignore()` either, so AutoMapper wrote them too. Flagged, not
  changed, because changing it is not upgrade work.
- **A harness reporting weakness worth knowing before Step 05:** value differences produce
  `reports/<group>/diff.md` but do **not** fail the run's exit code — only hard failures (a missing
  baseline, an uncovered route) do. A green `dotnet test` is therefore NOT sufficient evidence of
  parity; the report must be read. `diff.md` is also per-GROUP, not per-grant, so a second grant's run
  overwrites the first's report. Both bit this step before being noticed.

### ✅ Step 05 — `ADP.WarrantyClaims`: the exposure is closed, and the plan's own remedy for SHENGEN010 was wrong

**Result: clean under both grants, solution green, 0 SHENGEN warnings, 3/3 regression tests.** 7 triples
migrated, 2 profiles deleted, the Cosmos delegate rewritten, all 3 `IMapper` sites resolved.

#### SPIKE-7 — RESOLVED. `IgnoreList` bakes.

Proven from the emitted `.g.cs`, never from the build log, and proven **before** any other mapper work:

- `__shiftBakedIgnored = { "DistComment1", "HourTotalDistributor", "LaborTotalAmountDistributor", "PartsTotalAmountDistributor", "SubletTotalAmountDistributor" }`
- **zero** occurrences of any of the five in `__shiftListProjection`

Useful incidental: the generator emits even when compilation fails, so the proof was available while
the deleted-profile call sites were still red.

#### The hazard — all four controls exercised, and the seed made control #1 real

| # | Control | Result |
|---|---|---|
| 1 | full-access `GET /DealerFinancial` value diff | **PASS** — all five null, and the seed carries `DistComment1: "PARITY-DIST-COMMENT-MUST-NOT-LEAK"`, `11.11`, `2222.22`, `3333.33`, `4444.44`, so null is a real observation and not an empty row |
| 2 | restricted-grant pass | **PASS**, but see the caveat below |
| 3 | emitted-code proof `IgnoreList` baked | **PASS** (SPIKE-7) |
| 4 | standalone regression test | **PASS** — `DealerFinancialExposureTests`, 3 tests |

`DealerFinancial.LIST` shows **zero** differences across the whole migration, and its baseline
re-captured byte-identically. The exit criterion "dealer-vs-distributor differs by exactly the five"
is confirmed twice over: from `__shiftBakedIgnored` (dealer ignores the 5 + the 3 line collections;
distributor ignores only the 3), and by a test that projects one entity through **both** mappers and
diffs every property.

**A limit on control #2 worth recording:** `DistributorFinancial.LIST` returns **401** in the
baseline under both grants — `DistributorFinancialController` gates on
`capabilityProvider.IsDistributor`, which is false in the harness. So the restricted pass is an
independent control on the *dealer* surface, but there is **no value coverage of the distributor
financial list at all**. The dealer/distributor comparison is therefore carried by the emitted-code
diff and the regression test, not by two live response bodies.

#### SHENGEN010 — the plan's prescribed fix would have been a regression

The plan requires it "resolved with `IgnoreEntity` + `AfterEntity` reconciliation **by business
key** — not by suppression". The first half is right; **the business-key half is wrong for this
aggregate**, and following it literally would have broken saving:

- `WarrantyClaimRepository.UpsertAsync` **`RemoveRange`s all three line collections before**
  delegating to the base upsert. Delete-then-insert is this aggregate's established pattern.
- `WarrantyClaimService.WarrantyLinesValidationAndTransformation` snapshots the existing rows **by
  ID** and depends on that ordering.
- The line entities have **no natural business key** to reconcile on.
- The emitted code was `existing.X = dto.X.Select(d => pair.MapBack(d, new Child(), ctx)).ToList()`
  — replace-with-new, which is *exactly* what the AutoMapper reverse map did. The diagnostic
  describes a hazard this repository had already neutralised, invisibly to the generator.

Resolved by taking the write over **explicitly** — `IgnoreEntity` on the three collections plus one
`AfterEntity` calling a shared `WarrantyClaimLineWriter` that reproduces the generated code
byte-for-byte. That satisfies the criterion's mechanism (no suppression, no automatic deep write, the
diagnostic is genuinely gone) while preserving behaviour. **The deviation is the reconciliation
strategy: replace, not business key**, and the reason is documented at the writer.

#### Two more list flattenings AutoMapper supplied silently

Same class of bug as Step 04's eleven, caught here by `SHENGEN007` rather than by baseline diffing:
`CertificateCertificateNo` and `CertificateInvoiceDate` on **both** Financial triples, flattened by
AutoMapper from the `Certificate` navigation and absent from the generated projection.

The baseline pinned a detail that a plain flattening would have got wrong: for a claim with **no**
certificate the old response carried `"CertificateCertificateNo": ""` — an **empty string, not
null** — because AutoMapper renders a null source as `""` when converting to string. Reproduced
literally.

#### Item H — three sites, three different right answers

- `WarrantyCertificateRepository` — needed a **hand-declared** `[ShiftEntityMapper]`
  `IShiftObjectMapper<WarrantyClaim, WarrantyCertificateLineDTO>`. Pair mappers are auto-generated
  only for pairs the generator can *discover* inside a view DTO, and `Certificate` has no claims
  navigation to discover this one through — which is exactly what its `SHENGEN004` was reporting.
  (Note this is the opposite of Step 04's answer, where a triple already existed and a standalone
  object mapper would have been wrong.)
- `WarrantyRatesRepository` — routed through its **own triple's** `MapToView`. **A null guard was
  required**: the method is documented to return null when no rates row exists, `AutoMapper`'s
  `Map<T>(null)` returned null quietly, and `MapToView` would have thrown on the first call against
  a fresh database.
- `WarrantyClaimService` — dead field, parameter and assignment deleted as the plan says.

#### A generator asymmetry worth knowing for later steps

Only the **ignores** are baked into the generated mapper type; the `ForList`/`ForView`
customizations come from the repository's `UseGeneratedMapper` config and are **not** present on a
directly-constructed mapper instance. A first draft of the regression test asserted projected values
against a bare mapper and failed with a local-timezone offset (`+03:00`) instead of the pinned
`TimeSpan.Zero`, because the convention conversion — not the configured one — was running. The
mapper is fine; the test vehicle was wrong. Anything asserting configured list behaviour must go
through the repository, or assert against `__shiftBakedCustom` as this test now does.

### ✅ Step 06 — the floor moved last, and the package surface is closed

**Result: `dotnet build ADP.sln` GREEN with every project in the tree on `2026.8.30.1`, zero
`NU1605`, zero source changes on the floor.** This was the plan's headline criterion and the first
moment the whole tree sat on one version.

| Check | Result |
|---|---|
| 4 floor lines bumped | ✅ 4 files, **one changed line each** |
| `grep` for any `ShiftSoftware.Shift.*2026.7.31.1` in any csproj | ✅ **zero matches** |
| `dotnet restore ADP.sln` `NU1605` | ✅ **0** — the shared-last ordering did what it promised |
| 6 floor projects build clean, no source change | ✅ incl. `ADP.Menus.Generation` |
| generated-tree `git diff --exit-code` | ✅ **clean** — no public-shape change in `ADP.Models` / `Lookup.Services` |
| `ADP.Cases.Shared.Tests` | ✅ 43/43 |
| `Lookup.Services.Tests` | ✅ 47/47 |
| `ADP.Menus.Tests` | ✅ **262 passed / 2 failed** — exactly its Step 01 pin (the 2 are known local sample-DB drift) |
| all 8 parity runs re-verified after the bump | ✅ 4 groups × 2 grants, **all clean** |

#### SPIKE-8 question 3 — CONFIRMED from the `ADP.Cases` side: nothing was needed

`ADP.Cases.Data` does **not** have to be a registered data assembly. Both consumers register only
their own `DataMarker` + `SharedMarker`, and both `Certificate` triples resolve — proven the strong
way rather than by inspection: `ShiftEntityMapperValidation` checks **every** triple at startup, so a
mapper that failed to resolve would stop the host booting. All four ClaimableItems and WarrantyClaims
parity runs pass **after** `ADP.Cases` itself moved to `8.30.1`. **Step 07 does not need to
re-litigate this.**

Recorded once here as the shared context both Steps 04 and 05 needed: on both `Certificate` triples
the unmapped collections (`ReimbursementItemClaims`, `WarrantyClaims`) are populated by the
repositories' `ViewAsync` overrides, not by the mapper, and `Notes` has no source on the entity at
all. Both are `IgnoreView`d deliberately in their respective repositories.

#### Two recorded figures that do NOT match this plan's text — neither caused by this step

1. **`ADP.LookupServices.BDD` is 466/466, not the 452/452 in `STATUS.md` and `06-shared-floor.md`.**
   Investigated rather than accepted: the BDD tree is clean against HEAD (0 modified, 0 untracked),
   326 scenarios expand to 466 tests, and the 16 `.feature.cs` files a build churns are
   **1167 insertions / 1167 deletions with ZERO test methods added or removed** — cosmetic tooling
   churn. Then tested directly: reverting the two floor bumps and re-running gives **466/466 as
   well**. So the count is unaffected by this step and the recorded 452 simply does not reproduce on
   this tree. **Zero failures either way.** Step 07 should use 466.
2. **`$(ADPVersion)` is `1.15.5`, not the `1.15.4` this plan states.** `git diff` confirms this step
   did not touch `GlobalSettings.props`; the figure was already stale. Step 07 still owns the single
   bump, but it should start from `1.15.5`.

#### `NU1903` (the AutoMapper CVE) — final figure

**24 lines across 6 projects**, down from the 42 lines / 21 projects baseline. Fifteen projects left
the CVE surface as Steps 03-05 removed AutoMapper. It will not reach zero, and the remainder is
expected: `ADP.SyncAgent` keeps its own direct `AutoMapper 14.0.0` (deliberate, out of scope), plus
`ADP.Menus.Sync`, `ADP.Menus.Sample.Functions`, `ADP.Menus.Tests`, `ModelDocGen`, and the temporary
`ADP.EndpointParity.Harness` (deleted in Step 08).

#### Two things done beyond the plan's four lines

- **The parity harness carried three `7.31.1` pins of its own** (`ShiftEntity.Model`, `.EFCore`,
  `.Web`) — it is Step 00's own creation, so the plan's "29 lines" never counted it, but it made the
  exit criterion's grep non-zero. Worse, it was a real **version skew**: `project.assets.json` shows
  the harness compiling against `7.31.1` while running against `8.30.1` (the per-group test projects
  unify upward). Bumped, rebuilt, and WarrantyClaims re-verified clean under both grants. The
  instrument should not be pinned to the version it is measuring away from.
- **`CLAUDE.md:22` corrected** — see SPIKE-6.

#### The stray `web/` directory is now actively distorting inventories

The exit-criteria check "all 9 `TypeAuth` lines still `1.6.28`" came back as **two lines not at
1.6.28**, both in `./web/ShiftSoftware.ShiftEntity.Web.csproj`. They are versionless
`<Reference Include=...>` entries in the 41 decompiled framework files committed at `416dc551` — not
package references at all. All 9 real `TypeAuth` `PackageReference`s are at `1.6.28` and untouched.
This is the same class of hazard the plan flags for `**/obj/**`, and it is the second time this
directory has cost time. **It should be deleted in its own commit.**

### ✅ Step 07 — release readiness verified; **the release itself is NOT cut**

Everything this step verifies is done and green. `$(ADPVersion)` is bumped. **No tag has been pushed
and nothing has been published** — that is an outward-facing, irreversible act and is left to the
owner. Items A–G below are complete.

#### A. Full-solution build (`--no-incremental`)

| Measure | Baseline | Now |
|---|---|---|
| errors | 0 | **0** |
| csproj in `ADP.sln` | 59 | **61** — +2 durable test projects added by Steps 03 and 05 (`ADP.Surveys.Data.Tests`, `ADP.WarrantyClaims.Data.Tests`) |
| compiler warnings | **580 cold** (the plan's "535" was already recorded at Step 00 as not reproducible) | **543 cold — DOWN 37** |
| `SHENGEN004` | 10 | **0** |
| `SHENGEN007` / `008` / `010` | 0 | **0** |
| `NU1605` / `NU1701` / `NU1603` / `MSB3277` | 0 | **0** |
| `NU1903` | 42 lines / 21 projects | **20 lines / 5 projects** |
| `NU1504` | 1 project | **1 project** — still only `ADP.Surveys.Sample.API`'s duplicate `EFCore.Design` (4 log lines, 1 project; unchanged, not fixed) |

#### B. Test sweep — every suite at baseline

`Cases.Shared` 43/43 · `Surveys.Shared` 182/182 · `Surveys.Data.Tests` 2/2 · `Darlastic.Shared` 5/5 ·
`Darlastic.Engine` 49/49 · `Hawta` 493/502 (9 skipped) · `Lookup.Services` 47/47 · **`BDD` 466/466**
(the corrected figure — see Step 06) · `Menus` 262/264 · `WarrantyClaims.Data.Tests` 3/3 ·
`Models.Tests` no-op (SPIKE-6, accepted).

**The only red is the sanctioned pair**, confirmed by message:
`SampleDataSeedingTests.SeedsEveryDemoRowTheSampleDatabaseIsMissing` and
`.SeedingTwiceInsertsNothingTheSecondTime`, both
`Cannot insert duplicate key row in object 'Menu.ReplacementItem' with unique index
'IX_ReplacementItem_Name'`.

Web components **114 passed / 4 suites** — baseline. Generated trees `git diff --exit-code` **clean**.

#### C. Full parity sweep on the final tree — ALL CLEAN

Recorded here because **Step 08 deletes the only thing that can reproduce it**:

| Group | FullAccess | Restricted |
|---|---|---|
| `Menus` | clean | clean |
| `Surveys` | clean | clean |
| `ClaimableItems` | clean | clean |
| `WarrantyClaims` | clean | clean |
| `Darlastic` | clean (smoke, not value parity) | clean |

#### D. `$(ADPVersion)` — `1.15.5` → **`1.16.0`**, one bump

A **judgement call, easily changed before release.** The file's convention is patch increments with
occasional minor bumps (`1.14.25` → `1.15.0`); a MINOR bump is the more honest signal here because the
upgrade carries observable wire changes (the SPIKE-11 deltas) and must be released all-or-nothing.
Note the plan's stated starting point (`1.15.4`) was already stale — the file read `1.15.5`.

#### E. Package-mode check — PASSED, but the plan's recipe had to be replaced

**The plan's relocation recipe does not work for `ADP.ClaimableItems`.** It assumes the group's
cross-project references are all conditional `Exists()` pairs. `ADP.Models` and `Lookup.Services` are —
but `ADP.Cases.Shared` and `ADP.Cases.Data` are referenced by **unconditional `ProjectReference`**
(`ADP.ClaimableItems.Data.csproj:43-44`), so relocating the folder leaves them dangling and the build
fails with `CS0234: ShiftSoftware.ADP.Cases` plus a cascading `SHENGEN009` (the mapper builder's type
arguments stop being closed generics once the referenced types fail to resolve). **That is a defect in
the test recipe, not in the package** — the packed nuspec correctly declares
`ShiftSoftware.ADP.Cases.Data 1.16.0` and `ShiftSoftware.ADP.Cases.Shared 1.16.0`, because NuGet
converts a `ProjectReference` to a package dependency at pack time.

Replaced with a **stronger and more faithful test**: a scratch console host **outside the repo** whose
only inputs are `PackageReference`s to `ShiftSoftware.ADP.ClaimableItems.Data` and
`ShiftSoftware.ADP.WarrantyClaims.Data` at `1.16.0`, restored from a local feed with the package
sources `<clear/>`ed. That is literally what a downstream host does, and it exercises the
floor-vs-group unification for real (`ADP.Models` and `ADP.Cases` arrive transitively):

```
ClaimableItems.Data : ShiftSoftware.ADP.ClaimableItems.Data 1.16.0.0
WarrantyClaims.Data : ShiftSoftware.ADP.WarrantyClaims.Data 1.16.0.0
registered triples  : 12
registry conflicts  : 0
Certificate triples : 2
   Certificate / CertificateListDTO / CertificateDTO          -> Generated_..._df5083c3
   Certificate / CertificateListDTO / ItemClaimCertificateDTO -> Generated_..._16cfdeb0
VerifyBindings errs : 0
RESULT: PASS
```

Three things this proves that no in-repo build can:

1. **The package-consumption path resolves and builds** from packages alone.
2. **`VerifyBindings()` returns zero errors** — every generated mapper inside the *packaged*
   assemblies binds against ShiftEntity `2026.8.30.1`. That is the generated-mapper ABI hazard closed
   with the framework's own check.
3. **SPIKE-8 Q2 confirmed at package level**: both `Certificate` triples register side by side from
   **two different packages**, 0 conflicts. Previously proven only from emitted code.

**Packed-nuspec audit (the mixed-published-package check):** all **32** packages at `1.16.0`, and all
**34** `ShiftSoftware.Shift*` dependency references across every nuspec read `2026.8.30.1`. **Zero**
`2026.7.31.1` escaped. This is the check the entire shared-last ordering rests on, and it passes.

#### F / G. Sweeps and CI

Zero functional `AutoMapper` references outside `ADP.SyncAgent` (the only remaining hits are prose in
migration comments and in the temporary harness csprojs). Zero `2026.7.31.1` in any csproj. All
`TypeAuth` references at `1.6.28`. Fixed the last doc leftovers —
`ADP.Docs/Docs/docs/menus/integration.md:10,84,86` and `ADP.Menus/README.md:8`.
**No standing parity job was added**, and **no pipeline file references `ADP.EndpointParity/` or
`tools/parity.*`**, so Step 08's deletion cannot break a build.

#### Caveats that must go into the release notes

- `ADP.ClaimableItems` and `ADP.WarrantyClaims` were verified through a **mounted host**, not a real
  deployment: consumer middleware order, localization, CORS, fallback routing and JSON overrides are
  unverified for those two groups. No sample host was written for either.
- **`DistributorFinancial.LIST` returns 401 in the harness** (the controller gates on `IsDistributor`),
  so there is **no value coverage of the distributor financial list at all**.
- The **six Cosmos replication delegates have no automated coverage.** ClaimableItems' five were
  reviewed by an 8-agent adversarial pass (3 real defects found and fixed); WarrantyClaims' one was
  reviewed line-by-line.
- **Binary export endpoints are `PARTIAL`** (PDF: content-type and size band only).
- **Darlastic's result is smoke, not value parity.**
- **`ADP.Cases` has no endpoints** — not covered by endpoint parity in any sense.
- **`ADP.Models` has no executing tests** (SPIKE-6 accepted; its two `[Fact]`s are dead code).
- **After the release tag, per-group rollback is not available.** Packages release together and hosts
  unify ShiftEntity by max-wins, so a partial revert reproduces the mixed-package state that bricks
  hosts. A post-release problem is fixed by rolling the whole release **forward**.

### Darlastic — the 2026-09-01 decision, now SUPERSEDED by SPIKE-5

> The decision below stood until Step 02 actually tried to boot the host and found it
> cheap. The control was recovered, so the recorded consequence — that framework and
> mapper causes become inseparable — **no longer applies.** Kept for the record.

#### Original note


**SPIKE-5 is closed by decision, not by investigation.** Darlastic has 0 triples and 0 profiles, so
there is no mapper-shaped risk in it; the owner's call is to **skip its parity capture entirely and
simply take the framework upgrade plus whatever refactoring that requires** (Step 02 proceeds as an
ordinary upgrade).

**The cost, recorded so it is not rediscovered later:** Darlastic was the plan's *only* group where
a harness diff would be unambiguously framework-caused. Without it, every Surveys / ClaimableItems /
WarrantyClaims diff **confounds two causes** — the framework change and the mapper rewrite — and
there is no control to separate them. Steps 03-05 must attribute their diffs by reading the code,
not by comparison against a control.

---

## Package-reference ownership — 29 lines, 22 csproj

The deleted atomic-bump step carried all 29 `PackageReference` lines at `2026.7.31.1` in a single
commit. They are now owned by the step that owns the code, as **item A** of that step's work items;
each step file carries the exact csproj + line-number table. Tick a row only when that step's bump
commit is in **and the solution builds green**.

| Step | Group | csproj | Lines | Packages moved to `2026.8.30.1` | Bumped |
|---|---|---|---|---|---|
| 02 | `ADP.Darlastic` | 4 | 4 | `ShiftEntity.Web`, `ShiftEntity.EFCore`, `ShiftEntity.Model`, `ShiftBlazor` | **[x] 2026-09-02** |
| 03 | `ADP.Surveys` | 6 | 7 | the same four, plus `ShiftIdentity.Core`, `ShiftIdentity.Dashboard.AspNetCore`, `ShiftIdentity.Dashboard.Blazor` (the two Dashboard lines are in the samples) | [ ] |
| 04 | `ADP.ClaimableItems` | 4 | 7 | `ShiftEntity.Web`, `ShiftEntity.EFCore`, `ShiftEntity.CosmosDbReplication`, `ShiftEntity.Print`, `ShiftIdentity.Core`, `ShiftEntity.Model`, `ShiftBlazor` | [ ] |
| 05 | `ADP.WarrantyClaims` | 4 | 7 | same seven as 04 | [ ] |
| 06 | Shared floor | 4 | 4 | `ShiftEntity.Model` (`ADP.Models`, `Cases.Shared` — **same commit**), `ShiftEntity.EFCore` (`Cases.Data`), `ShiftEntity` (`Lookup.Services.DuckDB`) | [ ] |
| | **Total** | **22** | **29** | | |

`TypeAuth` stays at `1.6.28` — it is on a separate version line and needs no bump. **9 references**;
leave them alone, and do not count them against the 29.

**There is no cross-step error hand-off any more.** Each step's bump is its own first commit and each
step ends green, so a compiler error raised by a bump is fixed **inside the step that raised it** and
never queued for a later one. The old "Step 02 work queue" table existed only to route a
solution-wide red build's errors to their owning steps; it is deleted with the step that produced it.
Anything a step's bump raises that its own file does not anticipate is a **surprise** — record it in
that step's file and understand it before proceeding.

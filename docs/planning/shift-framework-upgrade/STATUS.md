# STATUS — Shift Framework Upgrade

**This file is the ledger.** It answers "which step is done and which is pending". If it disagrees
with anything else in this directory, this file wins.

Last updated: 2026-09-02 (Step 01 VERIFIED — harness calibrated against the known-answer Menus migration: 17 diffs, all one expected convention change, 0 harness bugs, 0 regressions. SPIKE-9 resolved. Earlier: Step 00 IN PROGRESS — SPIKE-1 and SPIKE-2 resolved, item H probe green, §G baselines recorded, harness skeleton building and group-isolated; seeds/stability-gate/baselines still outstanding. Earlier same day: plan reordered — shared floor moved to the end, the atomic version-bump
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
| 02 | `ADP.Darlastic` | `ADP.Darlastic.{API,Data,Shared,Web}` | 00, 01 | `CLOSED` | `NOT STARTED` | — | — | Bumps its own **4** package lines as its first commit and ends green. 0 profiles, 0 triples. Smoke pass only — nothing mapper-shaped to prove, so terminal `CLOSED`. **But it is the plan's only framework-only control** (see SPIKE-5). Do not record as full parity. |
| 03 | `ADP.Surveys` | `ADP.Surveys.{API,Data,Shared,Web}` + 2 samples | 00, 01 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. 4 triples, 1 profile (151 lines). **Free-floating** — every `ProjectReference` is intra-group and it consumes no `ShiftSoftware.ADP.*` package, so it is legal anywhere after 01. Ordered here by risk/simplicity, not by the graph. Has a sample host → full HTTP parity available. Carries SPIKE-3 and SPIKE-4. |
| 04 | `ADP.ClaimableItems` | `ADP.ClaimableItems.{API,Data,Shared,Web}` | 00, 01 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. 5 triples, 4 profiles, 5 Cosmos delegates, 1 `IMapper` site. No host → mounted host (SPIKE-2). First group to generate a `Certificate` mapper, so it now **owns SPIKE-8** (the shared floor no longer runs ahead of it). |
| 05 | `ADP.WarrantyClaims` | `ADP.WarrantyClaims.{API,Data,Shared,Web}` | 00, 01, 04 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. **Highest risk.** 7 triples; dealer/distributor forward-map `Ignore()` exposure. Ordered last of the groups by risk, overriding simplicity (it has fewer profiles than 04). Depends on 04 for the shared `Certificate` mapper precedent (SPIKE-8) — a **knowledge** dependency, not a build one. |
| 06 | Shared floor | `ADP.Models/Models`, `ADP.Cases.Data`, `ADP.Cases.Shared`, `Lookup.Services.DuckDB` | 02, 03, 04, 05 all at terminal | `CLOSED` | `NOT STARTED` | — | — | Bumps the last **4** package lines — `ADP.Models` and `Cases.Shared` in the **same commit**, which is what keeps NU1605 from ever appearing. 0 profiles, 0 triples; expected to compile unchanged. Libraries only — no endpoints, so terminal `CLOSED` with the reason in `Verified by`. Carries SPIKE-6. Pulls in `ADP.Menus.Generation` (see the ledger note below). Step 00's compile probe is its early warning. |
| 07 | Release readiness | solution-wide + `GlobalSettings.props` | 00–06 all at terminal (00, 02, 06 `CLOSED`; 01, 03, 04, 05 `VERIFIED`) | `VERIFIED` | `NOT STARTED` | — | — | Package-mode restore smoke check, full baseline comparison, single `ADPVersion` bump. **No package lines left** — all 29 landed in Steps 02–06. |
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
| SPIKE-3 | `BankQuestionListDTO.Type` and `ScreenTemplateListDTO.QuestionCount` are mapped in the current profile via **static method calls** over a JSON column. `ForList` requires an EF-translatable expression. How do these list projections work today, and what replaces them? | 03 | OPEN |
| SPIKE-4 | AutoMapper's `.Condition(...)` (used on `BankQuestion.BankEntryID`) has no documented equivalent on `ShiftMapperBuilder`. Is the existing-aware `ForEntity((dto, entity, ctx) => …)` overload the correct replacement? | 03 | OPEN |
| SPIKE-5 | The Darlastic sample host `return 1`s before `app.Run()` on missing config and needs a populated registry DB the repo does not seed. Can it be booted headlessly at all? | 02, and the attribution of 03/04/05 diffs | **CLOSED BY DECISION (2026-09-01), not investigated.** Owner's call: Darlastic has 0 triples and 0 profiles, so there is no mapper risk to prove — take the framework upgrade and any refactoring it forces, and **skip parity capture for this group**. `ADP.EndpointParity.Darlastic` stays in the solution (it builds, and removing it would churn `ADP.sln`) but is never captured. **Accepted cost, recorded so it is not rediscovered as a surprise:** Darlastic was the only framework-only control, so diffs in Steps 03/04/05 now confound framework change with mapper rewrite and must be attributed by reading code rather than by comparison. See the Darlastic section under `## Recorded baselines`. |
| SPIKE-6 | `ADP.Models/Models.Tests` discovers **zero tests** (no test framework referenced; all sources `<Compile Remove>`d) yet exits 0. The most-shared project in the solution is unguarded. Fix, or accept and record? | 06 | OPEN |
| SPIKE-7 | Do `IgnoreList` / `IgnoreView` bake correctly for the two Financial triples, and do both triples over the same entity generate distinct list projections? Must be proven from emitted `.g.cs`, not from the build log. | 05 | OPEN |
| SPIKE-8 | Two triples across two different groups map the `ADP.Cases` `Certificate` entity (`ItemClaimCertificateRepository`, `WarrantyCertificateRepository`). Which assembly does each generated mapper land in, and can both coexist in one host? **The old shared-floor probe is gone** — after the reorder the floor runs *last* (Step 06), behind both consumers — so Step 04 answers it from its own emitted `.g.cs` as the first group to generate a `Certificate` mapper, and Step 05 applies the finding to the second triple, and Step 06 confirms the `ADP.Cases` side of it (its item C). | 04 (owns it), 05, 06 (confirms the `ADP.Cases` side) | OPEN |
| SPIKE-9 | Exact delegate signature required by `Replicate<T>` / `UpdateReference<T>` at `2026.8.30.1`. | resolved by 01; blocks 04, 05 | **RESOLVED — signatures below, read by reflection over `ShiftSoftware.ShiftEntity.CosmosDbReplication 2026.8.30.1` and cross-checked against the ~19 live call sites.** <br><br> **The reference implementation is in `ADP.Menus/ADP.Menus.Sync/`, NOT `ADP.Menus.Data`** — do not hunt for it in the wrong assembly at Step 04. <br><br> **⚠️ THERE ARE TWO API FAMILIES AND THEY DIFFER IN THE DELEGATE'S FIRST ARGUMENT.** This is the trap: copying a call from the wrong family compiles against the wrong lambda parameter and fails in a way that reads like a mapper error. <br><br> **(1) TRIGGER path — `ShiftEntityCosmosDbOptions`, what ADP.Menus.Sync actually uses.** Delegates receive an `EntityWrapper<Entity>`; reach the row with `wrapper.Entity`. <br> `SetUpReplication<DB, Entity>(CosmosClient client, string cosmosDataBaseId, Func<EntityWrapper<Entity>, ValueTask<Entity>> mapper = null)` → `CosmosDbTriggerReplicateOperation<Entity>` <br> `.Replicate<CosmosDbItem>(string cosmosContainerId, Expression<Func<CosmosDbItem, object>> partitionKeyLevel1Expression, [level2], [level3], Func<EntityWrapper<Entity>, CosmosDbItem> mapping)` → `CosmosDbTriggerReferenceOperations<Entity>` <br> `.UpdateReference<CosmosDbItem>(string cosmosContainerId, Func<IQueryable<CosmosDbItem>, EntityWrapper<Entity>, IQueryable<CosmosDbItem>> finder, Func<EntityWrapper<Entity>, CosmosDbItem, CosmosDbItem> mapping)` → chainable <br><br> **(2) DIRECT path — `CosmosDbReplicationOperation<DB, Entity>`.** Delegates receive the **bare `Entity`**, not a wrapper. <br> `.Replicate<CosmosDBItem>(string containerId, Func<Entity, CosmosDBItem> mapping)` <br> `.UpdateReference<CosmosDBItem>(string containerId, Func<IQueryable<CosmosDBItem>, Entity, IQueryable<CosmosDBItem>> finder, Func<Entity, CosmosDBItem, CosmosDBItem> mapping)` <br><br> **Three further facts the call sites make explicit and the signatures alone do not:** (a) **partition-key expressions are over the COSMOS MODEL (`CosmosDbItem`), not the entity** — `document => document.BasicModelCode`, and there are 1/2/3-level overloads; (b) `partitionKeyLevel*Expression` and `mapping` are passed as **NAMED** arguments throughout `MenuReplicationExtensions.cs`, which is what keeps the 2- and 3-level overloads unambiguous at a glance; (c) **register each entity type EXACTLY ONCE** — the framework silently keeps only the last registration per type, which is why a master entity's own document and all its fan-outs chain off a single `SetUpReplication` (`MenuReplicationExtensions.cs:36-38`), and **`UpdateReference` fires on `ChangeType.Modified` ONLY** (`:40-44`), so an inserted master row fans out to nothing and a hard-deleted one leaves embedded copies behind. |
| SPIKE-10 | Are the **binary/print export endpoints** byte-reproducible enough to diff, or must they be recorded `PARTIAL`? Verified against the repo: the only `.xlsx` producers are `ADP.Menus/ADP.Menus.API/Controllers/MenuController.cs:114,248,437` — **Step 01**, not Surveys. `ADP.WarrantyClaims` exports **PDF** (`DistributorFinancialController.cs:108,110`, `WarrantyClaimController.cs:148`), which has no sheet XML to extract and carries `/CreationDate` + `/ID`, so it is the likeliest `PARTIAL`. The `text/csv` exports (`Surveys/SurveyResponsesController.cs:206`, `WarrantyClaims/ManufacturerSettlmentSheetController.cs:69`, `WarrantyClaimController.cs:287`, `Darlastic/CaseBrowserController.cs:363`) are deterministic text and are **covered**, not `PARTIAL` — they are not a Rule-7 case at all. | 01 (`.xlsx`), 05 (PDF) | OPEN |
| SPIKE-11 | **What did `DefaultEntityToDtoAfterMap()` / `DefaultDtoToEntityAfterMap()` do, and what reproduces it?** Both exist in `ShiftSoftware.ShiftEntity.dll` @ `2026.7.31.1` and are **absent** @ `2026.8.30.1` (verified by binary inspection); neither is documented in any XML doc file at either version. **6 call sites** across 2 groups. The calls vanish with the profiles — but so does whatever behaviour they applied. Resolve by reading the implementation at the `2026.7.31.1` tag in the public framework repo. | 04, 05 | OPEN |
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

### Darlastic — decision taken 2026-09-01, not a spike outcome

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
| 02 | `ADP.Darlastic` | 4 | 4 | `ShiftEntity.Web`, `ShiftEntity.EFCore`, `ShiftEntity.Model`, `ShiftBlazor` | [ ] |
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

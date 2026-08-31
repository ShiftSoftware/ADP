# STATUS — Shift Framework Upgrade

**This file is the ledger.** It answers "which step is done and which is pending". If it disagrees
with anything else in this directory, this file wins.

Last updated: 2026-09-01 (plan reordered — shared floor moved to the end, the atomic version-bump
step deleted, a harness-removal step added; no work started)

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
| 00 | Baseline & parity harness | `ADP.EndpointParity.Harness` + 5 per-group test projects (new), `tools/parity.ps1` (new) | — | `CLOSED` | `NOT STARTED` | — | — | Must run on the pre-bump tree. Two identical capture runs must diff to zero before anything else is trusted. Terminal `CLOSED`: it builds the instrument, it has no endpoints of its own. Also carries the **15-minute throwaway `ADP.Models` compile probe** that de-risks shared-last (see the residual-risk note under the spike table). |
| 01 | Retro-verify `ADP.Menus` | `ADP.Menus.*` (**11 projects**, 8 of them already on 2026.8.30.1) | 00 | `VERIFIED` | `NOT STARTED` | — | — | Code migration already `DONE` at `14caf7c9` — see the Menus row below. This step only proves it. Retroactive baseline from `14caf7c9^` via `git worktree`. Also resolves SPIKE-9. No package lines: Menus is already at `2026.8.30.1`. |
| 02 | `ADP.Darlastic` | `ADP.Darlastic.{API,Data,Shared,Web}` | 00, 01 | `CLOSED` | `NOT STARTED` | — | — | Bumps its own **4** package lines as its first commit and ends green. 0 profiles, 0 triples. Smoke pass only — nothing mapper-shaped to prove, so terminal `CLOSED`. **But it is the plan's only framework-only control** (see SPIKE-5). Do not record as full parity. |
| 03 | `ADP.Surveys` | `ADP.Surveys.{API,Data,Shared,Web}` + 2 samples | 00, 01 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. 4 triples, 1 profile (151 lines). **Free-floating** — every `ProjectReference` is intra-group and it consumes no `ShiftSoftware.ADP.*` package, so it is legal anywhere after 01. Ordered here by risk/simplicity, not by the graph. Has a sample host → full HTTP parity available. Carries SPIKE-3 and SPIKE-4. |
| 04 | `ADP.ClaimableItems` | `ADP.ClaimableItems.{API,Data,Shared,Web}` | 00, 01 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. 5 triples, 4 profiles, 5 Cosmos delegates, 1 `IMapper` site. No host → mounted host (SPIKE-2). First group to generate a `Certificate` mapper, so it now **owns SPIKE-8** (the shared floor no longer runs ahead of it). |
| 05 | `ADP.WarrantyClaims` | `ADP.WarrantyClaims.{API,Data,Shared,Web}` | 00, 01, 04 | `VERIFIED` | `NOT STARTED` | — | — | Bumps its own **7** package lines. **Highest risk.** 7 triples; dealer/distributor forward-map `Ignore()` exposure. Ordered last of the groups by risk, overriding simplicity (it has fewer profiles than 04). Depends on 04 for the shared `Certificate` mapper precedent (SPIKE-8) — a **knowledge** dependency, not a build one. |
| 06 | Shared floor | `ADP.Models/Models`, `ADP.Cases.Data`, `ADP.Cases.Shared`, `Lookup.Services.DuckDB` | 02, 03, 04, 05 all at terminal | `CLOSED` | `NOT STARTED` | — | — | Bumps the last **4** package lines — `ADP.Models` and `Cases.Shared` in the **same commit**, which is what keeps NU1605 from ever appearing. 0 profiles, 0 triples; expected to compile unchanged. Libraries only — no endpoints, so terminal `CLOSED` with the reason in `Verified by`. Carries SPIKE-6. Pulls in `ADP.Menus.Generation` (see the ledger note below). Step 00's compile probe is its early warning. |
| 07 | Release readiness | solution-wide + `GlobalSettings.props` | 01–06 all at terminal (00, 02, 06 `CLOSED`; 01, 03, 04, 05 `VERIFIED`) | `VERIFIED` | `NOT STARTED` | — | — | Package-mode restore smoke check, full baseline comparison, single `ADPVersion` bump. **No package lines left** — all 29 landed in Steps 02–06. |
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
  lands on Step 06, not on Step 03.**
  `ADP.LookupServices/Lookup.Services/Lookup.Services.csproj:61` `ProjectReference`s
  `ADP.Menus/ADP.Menus.Generation`, so **Step 06** builds a Menus project; and
  `ADP.Menus/ADP.Menus.Tests:50,58` reference `Lookup.Services` and `Lookup.Services.DuckDB`, so
  **Step 06**'s `Lookup.Services.DuckDB` bump mutates the restore graph of the project whose count
  Step 01 pinned. Consequence, unchanged but for the number: if Step 01 fixes a real regression
  inside `ADP.Menus.Generation`, the `LookupServices.BDD` 452/452 figure Step 06 uses as an exit
  criterion moves under it — and **Step 06 must re-run `ADP.Menus.Tests`**. Shared-last puts four
  more steps between Step 01's pin and that re-run, which makes the re-run more important, not less.

### Already true today — recorded, not scheduled

| Item | Group | Projects | Status | Verified by | Date | Notes |
|---|---|---|---|---|---|---|
| Mapper migration | `ADP.Menus` | 11 projects (8 carry a `2026.8.30.1` reference) | **`DONE`** | — | 2026-08-31 | Commit `14caf7c9`. Already on `2026.8.30.1`, AutoMapper profiles deleted, mappers rewritten. Builds green; `ADP.Menus.Tests` at its known baseline. **Endpoint parity was never proven** — no harness existed. Step 01 closes this. Until then Menus is `DONE`, not `VERIFIED`. |

---

## Open spikes

A spike is a question the survey could not answer. **Do not invent an answer in a step file — resolve
the spike, then record the finding here.**

| ID | Question | Blocks | Status |
|---|---|---|---|
| SPIKE-1 | Does `ShiftSoftware.ShiftFrameworkTestingTools` (published only to `2026.7.28.1`) bind when NuGet unifies its ShiftEntity dependency up to `2026.8.30.1`? | 00 (design choice only — fallback exists) | OPEN |
| SPIKE-2 | Can a synthetic "mounted host" boot `ADP.ClaimableItems` / `ADP.WarrantyClaims` through their own `Add<Group>ApiServices` entry point? No sample host exists for either, and no sample API declares `public partial class Program`, so `WebApplicationFactory<Program>` will not compile against any of them today. | 00, 06, 07 | OPEN |
| SPIKE-3 | `BankQuestionListDTO.Type` and `ScreenTemplateListDTO.QuestionCount` are mapped in the current profile via **static method calls** over a JSON column. `ForList` requires an EF-translatable expression. How do these list projections work today, and what replaces them? | 05 | OPEN |
| SPIKE-4 | AutoMapper's `.Condition(...)` (used on `BankQuestion.BankEntryID`) has no documented equivalent on `ShiftMapperBuilder`. Is the existing-aware `ForEntity((dto, entity, ctx) => …)` overload the correct replacement? | 05 | OPEN |
| SPIKE-5 | The Darlastic sample host `return 1`s before `app.Run()` on missing config and needs a populated registry DB the repo does not seed. Can it be booted headlessly at all? **High priority, not cost-benefit-optional:** with 0 triples and 0 profiles Darlastic is the plan's *only* group where a harness diff is unambiguously framework-caused, so it is the control against which every mapper group's diff is attributed. | 04, and the attribution of 05/06/07 diffs | OPEN |
| SPIKE-6 | `ADP.Models/Models.Tests` discovers **zero tests** (no test framework referenced; all sources `<Compile Remove>`d) yet exits 0. The most-shared project in the solution is unguarded. Fix, or accept and record? | 03 | OPEN |
| SPIKE-7 | Do `IgnoreList` / `IgnoreView` bake correctly for the two Financial triples, and do both triples over the same entity generate distinct list projections? Must be proven from emitted `.g.cs`, not from the build log. | 07 | OPEN |
| SPIKE-8 | Two triples across two different groups map the `ADP.Cases` `Certificate` entity (`ItemClaimCertificateRepository`, `WarrantyCertificateRepository`). Which assembly does each generated mapper land in, and can both coexist in one host? | 03 (probe), 06, 07 | OPEN |
| SPIKE-9 | Exact delegate signature required by `Replicate<T>` / `UpdateReference<T>` at `2026.8.30.1`. The Menus reference implementation lives in `ADP.Menus/ADP.Menus.Sync/`, **not** in `ADP.Menus.Data` as the recipe implies. **Owned by Step 01** — it is answerable today, from the pre-bump tree, against the ~19 already-migrated call sites in `ADP.Menus/ADP.Menus.Sync/Extensions/MenuReplicationExtensions.cs` and `Replication/MenuCatchUpReplicationExtensions.cs`. Step 01 already has the Menus group open; budget ~20 minutes. | resolved by 01; blocks 06, 07 | OPEN |
| SPIKE-10 | Are the `.xlsx` export endpoints byte-reproducible enough to diff, or must they be recorded `PARTIAL`? | 05, 07 | OPEN |
| SPIKE-11 | **What did `DefaultEntityToDtoAfterMap()` / `DefaultDtoToEntityAfterMap()` do, and what reproduces it?** Both exist in `ShiftSoftware.ShiftEntity.dll` @ `2026.7.31.1` and are **absent** @ `2026.8.30.1` (verified by binary inspection); neither is documented in any XML doc file at either version. **6 call sites** across 2 groups. The calls vanish with the profiles — but so does whatever behaviour they applied. Resolve by reading the implementation at the `2026.7.31.1` tag in the public framework repo. | 06, 07 | OPEN |
| SPIKE-12 | **Does a per-group version bump keep the tree green?** Bumping `ADP.Models` alone lifts `ShiftEntity.Model` to 8.30.1 by max-wins inside each of its 14 consumers while those consumers keep a direct `ShiftEntity.EFCore 2026.7.31.1` — a split ShiftEntity family in one compilation, across the AutoMapper removal. Test it cheaply: bump `Models.csproj:48` plus the 3 NU1605 pins (`Cases.Shared:32`, `ClaimableItems.Shared:34`, `WarrantyClaims.Shared:33`) on a scratch branch and build the solution. **If it stays green, fold each group's version edits into Steps 03–07 and delete Step 02** (`README.md` §3, "Escape hatch"). Suggestive prior: the repo is already in a mixed state that builds — `ADP.Menus` at 8.30.1 consuming an `ADP.Models` compiled against 7.31.1. | the shape of 02–07 | OPEN |

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
| "one `ADP.EndpointParity` test project" | **Cannot compile between Steps 02 and 07** — one assembly referencing all five groups' `.Data` is red for as long as any group is red, which makes Steps 04–06 unverifiable. Split into a group-agnostic `Harness` library plus one test project per group | Step 02 item B predicts `ClaimableItems.Data`, `Surveys.Data`, `WarrantyClaims.Data` all fail to compile; those are exactly the three projects holding `: Profile` classes |

One further correction, to the recorded test baseline: `ADP.Menus.Tests` is **262 passed / 2 failed /
0 skipped (264 total)**, not the remembered `259 / 2 / 1`. The pass count is Cosmos-emulator
sensitive (±1) and the fail count is local-SQL-state sensitive (±2). Compare on the same machine
state, or filter out `SampleDataSeedingTests` and `ServiceMenusProvisioningTests` first.

---

## How to update this file

Keep it accurate or it is worse than nothing. When you finish a piece of work:

1. **Update the row, not the table's prose.** One row per step. Narrative goes in its **named
   section below** (`## Recorded baselines`, `## Step 02 work queue`) — never in a ledger cell.
2. **`DONE` requires:** the step's code work complete, its projects building, its tests at baseline.
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
   separate commit is how ledgers drift. **Before Step 00, `git add docs/planning/` — the plan
   directory is currently untracked** (`git status` shows `?? docs/`; it is not gitignored, just
   never added), so rule 7 is impossible to obey until it is.

---

## Recorded baselines

Filled in by Step 00 item G, on the pre-bump tree. Step 08 compares against these. **Record the
machine state alongside them** — several numbers are emulator- and local-SQL-sensitive.

| Measure | Baseline | Recorded on |
|---|---|---|
| `dotnet build ADP.sln` | *(exit code, errors, projects built — pin the project count **after** Step 00 has added its own projects to `ADP.sln`; it is 53 today)* | |
| compiler warnings | | |
| `SHENGEN004` | | |
| `SHENGEN007` / `008` / `010` | | |
| `NU1605` / `NU1701` / `NU1603` / `MSB3277` | | |
| `NU1903` (AutoMapper CVE) | | |
| .NET tests | | |
| web component tests | | |
| generated trees clean (`src/global/types/generated`, `ADP.Docs/Docs/docs/generated`, `ADP.TestData/environments`) | | |

---

## Step 02 work queue

Filled in by Step 02 item B: the **full** compiler error list from the red build, each error assigned
to the step that fixes it. This is the hand-off between Step 02 and Steps 03–07 and it does not fit
in a table cell, which is why it lives here.

| # | Project | Error | Category (`02` item B) | Assigned to step | Fixed |
|---|---|---|---|---|---|
| | | | | | |

Anything that does not fall into one of item B's four categories is a **surprise**; record it here in
its own row and understand it before proceeding.

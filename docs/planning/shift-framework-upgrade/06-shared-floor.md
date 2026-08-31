# Step 06 — Shared floor: `ADP.Models`, `ADP.Cases`, `ADP.LookupServices`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `CLOSED` — libraries only; there are no endpoints here to prove.

**Goal:** move the dependency root to `2026.8.30.1` **last**, confirm it needs no mapper work, and
close the plan's package surface with the whole solution green.

> **This is the shared-LAST step.** The floor moves *after* all four group steps, not before. It is
> simultaneously the group everything else depends on *and* the cheapest work in the plan — and
> taking it last is what lets every step in this plan end green. The argument is in
> §Why-the-floor-moves-last; it is settled, not open.

---

## Projects touched

| Path | Bumped line | Profiles | Triples |
|---|---|---|---|
| `ADP.Models/Models/Models.csproj` | `ShiftEntity.Model` (48) | 0 | 0 |
| `ADP.Cases/ADP.Cases.Data/ADP.Cases.Data.csproj` | `ShiftEntity.EFCore` (31) | 0 | 0 |
| `ADP.Cases/ADP.Cases.Shared/ADP.Cases.Shared.csproj` | `ShiftEntity.Model` (32) | 0 | 0 |
| `ADP.LookupServices/Lookup.Services.DuckDB/Lookup.Services.DuckDB.csproj` | `ShiftEntity` (21) | 0 | 0 |

Also validated here (no Shift package reference of their own, but they sit on this floor):
`ADP.LookupServices/Lookup.Services`, `ADP.Models/Models.Tests`, `ADP.Cases/ADP.Cases.Shared.Tests`,
`ADP.LookupServices/Lookup.Services.Tests`, `ADP.LookupServices.BDD`, **and
`ADP.Menus/ADP.Menus.Generation`**.

**`ADP.Menus.Generation` is in this step whether you want it or not.**
`ADP.LookupServices/Lookup.Services/Lookup.Services.csproj:61` `ProjectReference`s it, so "the
dependency root" pulls a Menus project into every build and test command below. The coupling is
bidirectional at group level: `ADP.Menus/ADP.Menus.Tests:50,58` reference `Lookup.Services` and
`Lookup.Services.DuckDB`, so **this step's `Lookup.Services.DuckDB` bump mutates the restore graph of
the project whose test count Step 01 pinned.**

Because the floor now moves **last**, that disturbance lands at the end of the plan instead of at its
start. Re-running `ADP.Menus.Tests` and the `LookupServices.BDD` suite here is therefore the plan's
**final confirmation** that the already-migrated Menus group survived the floor moving underneath it
— not an early tremor that four later steps would each have to re-check. Two consequences, both
handled in the exit criteria: this step re-runs `ADP.Menus.Tests`, and if Step 01 fixed a real
regression inside `ADP.Menus.Generation`, the BDD figure to compare against is the one Step 01 left
in `STATUS.md`, not the one printed in this file.

**Not in scope:** `ADP.LookupServices/Lookup.Services.Functions` **no longer exists** — its csproj
was deleted at `67aa8a3e` and only untracked `bin/` and `obj/` remain, so nothing can build it. The
live hazard is the reverse of "leave it alone": its stale
`obj/Debug/net8.0/WorkerExtensions/WorkerExtensions.csproj` is the only `net8.0` `.csproj`-shaped
artefact in the tree, so **exclude `**/obj/**` and `**/bin/**` from any `find`-based inventory** or
the project count comes out wrong. Consider deleting the directory outright, in its own commit.

---

## Preconditions

- **Steps 02, 03, 04 and 05 are all at their terminal status** (`CLOSED`, `VERIFIED`, `VERIFIED`,
  `VERIFIED`). Each of those steps bumped its own package lines and ended green, so 25 of the 29
  `ShiftSoftware.Shift*` lines are already at `2026.8.30.1` and exactly the **4** in §Projects-touched
  remain.
- Step 00 `CLOSED` and Step 01 `VERIFIED` — the baselines this step compares against exist.
- **The Step 00 `ADP.Models` compile probe result is on record.** Step 00 bumps `Models.csproj:48` on
  a scratch branch, runs `dotnet build ADP.Models/Models`, records the outcome and **reverts**
  (15-minute timebox, never committed). That throwaway probe is the only thing standing between
  shared-last and a late surprise. **If it found a breaking change in `ShiftEntity.Model 2026.8.30.1`
  against `ADP.Models`, that work lands here and this step's scope grows** — read the recorded result
  before estimating. If the probe was skipped, run it before touching item A, not after.
- **SPIKE-8 `RESOLVED` in `STATUS.md`.** Step 04 probes it against a *green* consumer and Step 05
  reuses the answer; both are at terminal status before this step starts. This step confirms the
  `ADP.Cases` side of it (item C) rather than discovering it.
- Working tree clean.
- **NuGet cache warm for the four packages bumped here.** Two private feeds registered at machine
  level are unreachable, which produces ~202 `NU1900` warnings on any real restore; restore succeeds
  only because packages are already cached. Steps 02–05 will have pulled `ShiftEntity.Model`,
  `ShiftEntity.EFCore` and — transitively, via `ShiftEntity.EFCore` — `ShiftEntity` at `2026.8.30.1`,
  so the cache should already hold everything this step needs. Confirm that rather than assume it,
  before committing.

---

## Why the floor moves last — and why the step still exists

**Nothing forces a lockstep bump.** The Shift nuspecs declare **minimum-version** dependencies
(`version="2026.7.31.1"`), *not* exact pins (`[2026.7.31.1]`) — verified in the local NuGet cache. A
consumer at `8.30.1` sitting on a shared project still at `7.31.1` is an ordinary resolution, not a
violation.

**The repo already proves it, today.** `ADP.Menus.Shared:32` pins `ShiftEntity.Model 2026.8.30.1`
while `ADP.Menus.Data` `ProjectReference`s `ADP.Models`, which pins `2026.7.31.1`
(`ADP.Models/Models/Models.csproj:48`). An upgraded group sitting on a not-yet-upgraded floor
**builds green right now**. That is exactly the arrangement Steps 02–05 run in.

**Shared-last eliminates `NU1605` entirely — this is the strongest argument for the order.** There
are **7** direct `ShiftEntity.Model` pins in the repo, and this is when each reaches `8.30.1`:

| direct `ShiftEntity.Model` pin | reaches `8.30.1` at |
|---|---|
| `ADP.Menus.Shared:32` | already there, before this plan started |
| `ADP.Darlastic.Shared:33` | Step 02 |
| `ADP.Surveys.Shared:32` | Step 03 |
| `ADP.ClaimableItems.Shared:34` | Step 04 |
| `ADP.WarrantyClaims.Shared:33` | Step 05 |
| `ADP.Cases.Shared:32` | **this step — same commit as the hub** |
| `ADP.Models/Models/Models.csproj:48` | **this step — the hub itself** |

Under a shared-**first** order, the hub's bump lifts `ShiftEntity.Model` to `8.30.1` by max-wins
inside consumers still holding their own `7.31.1` pin, and the **3** projects that carry both an
`ADP.Models` reference *and* a direct pin — `ADP.Cases.Shared:32`, `ADP.ClaimableItems.Shared:34`,
`ADP.WarrantyClaims.Shared:33` — produce package-downgrade errors. Under shared-**last**, two of
those three are already at `8.30.1` when the hub moves, and the third moves in the same commit.
**No downgrade window ever opens: the count is 0, not 3.** (It was never 9 — an earlier draft claimed
that; the repo says 3.)

**The old core-first argument does not apply here.** "A broken foundation makes later failures
ambiguous" is a real concern where the foundation gets *refactored*. This floor carries **0
AutoMapper profiles and 0 repository triples**: it is a version number and nothing else. There is no
refactor here to get wrong, so there is nothing downstream for it to make ambiguous.

**SPIKE-12 is `RESOLVED — staged per-group bump adopted`**, on the nuspec minimum-range finding plus
the live `ADP.Menus`/`ADP.Models` counterexample above. It is not re-opened here.

**Staged commits, atomic release.** Slicing the bump per group is safe *in-repo*, for the reasons
above. It is **not** safe to *publish* a slice: mixed published packages hard-brick a downstream
host — max-wins unification leaves the older group with dead AutoMapper profiles and no registered
mapper, and `ShiftEntityMapperValidation` throws at startup. Hence one release, not five. Step 07
owns it, and `$(ADPVersion)` moves exactly once, there.

**Why "expected to compile unchanged" is safe, concretely:** `ADP.Models/Models` targets
**netstandard2.0** (one of only three netstandard2.0 projects in the repo, alongside
`Lookup.Services` and `ADP.Menus.Generation`; every other project is net10.0, and nothing
multi-targets). `ShiftSoftware.ShiftEntity.Model 2026.8.30.1` still ships `lib/netstandard2.0`,
verified in the local NuGet cache alongside `2026.7.31.1`. There is therefore no question of running
an analyzer-bearing package against an unsupported target here — the source generator arrives via
`ShiftEntity.EFCore`, which this project does not reference.

**The one thing shared-last defers.** If `ShiftEntity.Model 2026.8.30.1` *does* contain a breaking
change affecting `ADP.Models`, shared-first would have found it on day one and this order finds it at
the end. That is the honest cost of the ordering, and it is bought off by the Step 00 compile probe
named in the Preconditions: fifteen minutes, thrown away, never committed, and its result must be on
record before this step starts.

`ADP.Cases` is the interesting one. It has **no controllers and no repository** — one entity
(`Certificate`) consumed as a library. But that entity is the subject of **two repository triples in
two different groups**:

- `ADP.ClaimableItems/.../Repositories/ItemClaimCertificateRepository.cs` —
  `ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, ItemClaimCertificateDTO>`
- `ADP.WarrantyClaims/.../Repositories/WarrantyCertificateRepository.cs` —
  `ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, CertificateDTO>`

So `ADP.Cases` owns the entity but **neither owns nor configures its mappers** — they are generated
in the two consumer assemblies, from two different triples over the same entity, with different view
DTOs. That is **SPIKE-8**. In this order it is probed and resolved in **Step 04**, against a *green*
consumer, and reused as precedent by Step 05 — strictly better than the old arrangement, where this
step had to probe a project that the (now deleted) atomic bump had left red, and was explicitly
allowed to give up and defer. This step does not probe; it **confirms from the `ADP.Cases` side**
(item C). **What is not acceptable is guessing.**

---

## Work items

### A. Bump this group's package references

The last **4** of the plan's 29 lines. `2026.7.31.1` → `2026.8.30.1`, on `ShiftSoftware.Shift*`
references only.

| csproj | line | package |
|---|---|---|
| `ADP.Models/Models/Models.csproj` | 48 | `ShiftEntity.Model` |
| `ADP.Cases/ADP.Cases.Data/ADP.Cases.Data.csproj` | 31 | `ShiftEntity.EFCore` |
| `ADP.Cases/ADP.Cases.Shared/ADP.Cases.Shared.csproj` | 32 | `ShiftEntity.Model` |
| `ADP.LookupServices/Lookup.Services.DuckDB/Lookup.Services.DuckDB.csproj` | 21 | `ShiftEntity` |

**Guards:**

- **`ADP.Models/Models` and `ADP.Cases.Shared` must move in the SAME commit.** That is what holds the
  `NU1605` count at zero (see the pin table above). Do not split them to make the diff smaller.
- **Do not touch `ShiftSoftware.TypeAuth.*`.** All 9 lines stay at `1.6.28` — already the newest
  release, and what ShiftEntity `2026.8.30.1` itself depends on. Its version line has poisoned SemVer
  ordering (`2024.2.22.2` sorts above `1.6.28` but is 2.5 years older), so any tool resolving "latest"
  will silently install the ancient package.
- **Do not touch `$(ADPVersion)`** (`1.15.4` in `GlobalSettings.props`). That is the ADP package line,
  it is orthogonal, and it moves once — in Step 07.
- **Do not add or remove any package.** No new reference is needed: the generator ships as an analyzer
  inside `ShiftSoftware.ShiftEntity`, pulled transitively by `ShiftEntity.EFCore`.
- **No BOMs.** The Menus migration gained a UTF-8 BOM on almost every touched file, inflating the diff
  of csprojs that changed one line each. `git diff --stat` for the bump alone must read **4 files,
  one changed line each**.
- **Four `AfterTargets="Build"` self-runners will dirty tracked files the moment you build.**
  `WebComponentModelGenerator`, `ADP.Docs/ModelDocGen`, `ADP.Docs/FeatureDocGen` and
  `ADP.TestData/Generator` each run themselves after build and rewrite 247 tracked files between them
  (`README.md` §7). Before committing:
  `git checkout -- ADP.WebComponents/adp-web-components/src/global/types/generated ADP.Docs/Docs/docs/generated ADP.TestData/environments`
  — and **read what changed first.** A non-empty diff there is a public-shape change in `ADP.Models`
  or `Lookup.Services`, which is a finding, not noise. (The `git diff --exit-code` in §Verification is
  the same check, stated as a criterion.)
- **Record the final `NU1903` figure.** The AutoMapper CVE warning is at **42 lines across 21
  projects** in the baseline. This is the last commit in the plan that can move it. It will **not**
  reach zero: AutoMapper still arrives through `ADP.SyncAgent`'s own direct `AutoMapper 14.0.0`, which
  is deliberate and out of scope. Write the final number into `STATUS.md` for Step 07 to report.

### B. Confirm the floor compiles unchanged

```bash
dotnet build ADP.Models/Models
dotnet build ADP.Cases/ADP.Cases.Shared
dotnet build ADP.Cases/ADP.Cases.Data
dotnet build ADP.LookupServices/Lookup.Services      # transitively builds ADP.Menus.Generation
dotnet build ADP.LookupServices/Lookup.Services.DuckDB
dotnet build ADP.Menus/ADP.Menus.Generation          # explicitly, so a failure is not mistaken for a Lookup one

dotnet build ADP.sln                                 # and the whole tree, which must be GREEN
```

Expected: all green, **no source change**. If any fails, classify the error and expand scope here —
this is the last step that can absorb a floor surprise, and every other group is already green and
independently revertible behind it.

The solution build is not decoration. This is the first moment in the plan at which **every** project
in the tree is on `2026.8.30.1` at once, and the step is not finished until that build is green.

### C. Confirm SPIKE-8 from the `ADP.Cases` side

SPIKE-8's three questions are answered in Step 04 — and reused by Step 05 — from **emitted code**,
not from reasoning:

1. Which assembly does each generated `Certificate` mapper land in — the consumer's `.Data`, or
   `ADP.Cases.Data`?
2. Can both triples coexist in one host process? A downstream host may install **both**
   ClaimableItems and WarrantyClaims, so two mappers over the same entity with different view DTOs
   must register side by side in `ShiftEntityMapperRegistry` without collision.
3. Does `ADP.Cases.Data` need to be a registered data assembly for either mapper to resolve at
   startup?

**What this step owes** is question (3) specifically, re-checked against `ADP.Cases` *after* its own
bump: if the recorded answer is "yes, `ADP.Cases.Data` must be registered", that is a change to a
shared library and it belongs **here** — implement it and record it. If the answer is "no", record
that `ADP.Cases` needed nothing, so Step 07 does not re-litigate it.

If — contrary to the preconditions — SPIKE-8 is still `OPEN`, the probe is now cheap and unblocked,
because every consumer is green:

```bash
rm -rf ADP.ClaimableItems/ADP.ClaimableItems.Data/obj
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data -p:EmitCompilerGeneratedFiles=true
```

Read what appears in the emitted tree. Do not answer from reasoning.

The **baseline diagnostics already tell you these triples generate**, and what they cannot map:

```
SHENGEN004: Generated_Certificate_CertificateListDTO_ItemClaimCertificateDTO_16cfdeb0
            does not map: ReimbursementItemClaims, Notes
SHENGEN004: Generated_Certificate_CertificateListDTO_CertificateDTO_df5083c3
            does not map: WarrantyClaims, Notes
```

Both unmapped collections (`ReimbursementItemClaims`, `WarrantyClaims`) are populated today by
`ViewAsync` overrides in the respective repositories, not by the mapper. `Notes` is unmapped in both.
**Record that here** — it is the shared context Steps 04 and 05 both needed; if they did not write it
down when they ran, write it down now, because Step 07 will otherwise discover it a third time.

### D. Resolve SPIKE-6 — `ADP.Models` has zero test coverage

`dotnet test ADP.Models/Models.Tests` reports **"No test is available"** and **exits 0**, so it looks
green in any scripted loop. Cause is structural: the csproj references `Microsoft.NET.Test.Sdk` and
`coverlet` but **no xunit/NUnit/MSTest**, and it `<Compile Remove>`s all three of its own source
files — including the two containing `[Fact]`s.

This matters here specifically: `ADP.Models` is the most-shared project in the solution and has
**zero** executing tests guarding it. `CLAUDE.md` advertises `dotnet test ADP.Models/Models.Tests` as
a build command; that command is a no-op today.

**Decide and record one of:**

- **Fix it** — add a test framework reference, drop the `<Compile Remove>` entries, make the existing
  `[Fact]`s run. Cheap, and it closes a real hole. Preferred.
- **Accept it** — record explicitly in `STATUS.md` that `ADP.Models` is verified by the compiler
  alone in this upgrade, and that `verification.md` §8.9 is the standing caveat.

**Do not leave it silently exiting 0.** Whatever is decided, `CLAUDE.md`'s claim should match reality.

### E. Confirm the floor's own tests

These have real coverage and must stay at baseline:

| Suite | Baseline |
|---|---|
| `ADP.Cases/ADP.Cases.Shared.Tests` | 43 / 43 |
| `ADP.LookupServices/Lookup.Services.Tests` | 47 / 47 |
| `ADP.LookupServices.BDD` | 452 / 452 |
| `ADP.Menus/ADP.Menus.Tests` | 262 / 2 / 0 — **re-run here, as the plan's final Menus confirmation.** Step 01 pinned this number *before* `Lookup.Services.DuckDB` moved, and `ADP.Menus.Tests:58` references it. |

---

## Verification

```bash
# the plan's package surface is now closed — this must return nothing
grep -rn 'ShiftSoftware\.Shift.*2026\.7\.31\.1' --include=*.csproj . | grep -v '/obj/\|/bin/'

dotnet restore ADP.sln          # must succeed; zero NU1605
dotnet build ADP.sln            # must be GREEN

dotnet build ADP.Models/Models
dotnet build ADP.Cases/ADP.Cases.Shared
dotnet build ADP.Cases/ADP.Cases.Data
dotnet build ADP.LookupServices/Lookup.Services
dotnet build ADP.LookupServices/Lookup.Services.DuckDB

dotnet test ADP.Cases/ADP.Cases.Shared.Tests
dotnet test ADP.LookupServices/Lookup.Services.Tests
dotnet test ADP.LookupServices.BDD
dotnet test ADP.Menus/ADP.Menus.Tests          # re-run: this step moved Lookup.Services.DuckDB
dotnet test ADP.Models/Models.Tests            # see item D — currently a no-op

# the generated trees must be unchanged by the bump — cheapest possible check that
# ADP.Models' and Lookup.Services' public shape survived (README.md §7)
git diff --exit-code ADP.WebComponents/adp-web-components/src/global/types/generated \
                     ADP.Docs/Docs/docs/generated ADP.TestData/environments
```

**Group-specific caveats.**

- **There is no endpoint harness run in this step.** `ADP.Cases` has no controllers and no repository;
  `ADP.Models` and `Lookup.Services` are libraries. **Endpoint parity is not a meaningful concept
  here and must never be reported as passing for these projects** (`verification.md` §8.6).
- Consequently this step's terminal status is **`CLOSED`**, not `VERIFIED`. Record it as `CLOSED`
  with `Verified by` = "N/A — no endpoints; covered by the unit suites, by the green solution build,
  by the generated-tree diff, and by Steps 04/05, which exercised the shared `Certificate` entity
  through real endpoints before this floor moved". Steps 00, 02 and 08 are the other three `CLOSED`
  steps; see `STATUS.md`'s vocabulary.
- **`CLOSED` does not mean "ends red".** That meaning belonged to the deleted atomic-bump step and
  died with it. This step ends with a green solution build, like every step in this plan; `CLOSED`
  means only *finished, with no endpoint surface whose parity could be proven*.

---

## Exit criteria

- [ ] All **4** lines in item A read `Version="2026.8.30.1"`, and `ADP.Models/Models` and
      `ADP.Cases.Shared` moved in the **same commit**.
- [ ] `grep` for `ShiftSoftware.Shift.*2026\.7\.31\.1` across every csproj (excluding `**/bin/**` and
      `**/obj/**`) returns **zero** matches — all 29 lines are now done: 25 by Steps 02–05, 4 here.
- [ ] All 9 `ShiftSoftware.TypeAuth.*` lines still read `Version="1.6.28"`; `$(ADPVersion)` is still
      `1.15.4`; no package was added or removed anywhere.
- [ ] `dotnet restore ADP.sln` succeeds with **zero `NU1605`** — in this subtree and solution-wide.
- [ ] **`dotnet build ADP.sln` is green.** This is the step's headline criterion, and the plan's: the
      whole tree, every project on `2026.8.30.1`, building clean.
- [ ] All six projects in §Work-item-B build clean with **zero source changes**, or every change is
      listed and justified. `ADP.Menus.Generation` is one of them.
- [ ] `ADP.Cases.Shared.Tests` = 43/43; `Lookup.Services.Tests` = 47/47; `LookupServices.BDD` =
      452/452 — **or the figure Step 01 left behind, if it fixed a regression inside
      `ADP.Menus.Generation`.** Compare against `STATUS.md`, not against this file.
- [ ] `ADP.Menus.Tests` re-run and still at its Step 01 figure. Step 01's pin was taken before
      `Lookup.Services.DuckDB` moved.
- [ ] `git diff --exit-code` over the three generated trees is **clean** after a build. A diff there
      means `ADP.Models`' or `Lookup.Services`' public shape changed under the bump — a finding, and
      the only signal this plan has for it.
- [ ] SPIKE-8's question (3) is confirmed against the bumped `ADP.Cases`: either the registration
      change is implemented here, or `STATUS.md` records that `ADP.Cases` needed nothing.
- [ ] SPIKE-6 is `RESOLVED` in `STATUS.md` with the decision (fix or accept) and, if accepted, the
      caveat written into `STATUS.md` notes.
- [ ] The final `NU1903` count is recorded in `STATUS.md` (baseline 42 lines / 21 projects; it will
      not be zero while `ADP.SyncAgent` keeps its own `AutoMapper 14.0.0`).
- [ ] Every project inventory run in this step excluded `**/obj/**` and `**/bin/**`, so the deleted
      `Lookup.Services.Functions`' stale `WorkerExtensions` artefacts did not enter the count.
- [ ] `STATUS.md` records this step **`CLOSED`** with the "N/A — no endpoints" note in `Verified by`.

---

## Rollback

`git revert` this step's bump commit. It touches 4 `PackageReference` lines plus whatever items C and
D required — a registration change in `ADP.Cases`, or the `ADP.Models/Models.Tests` fix — so keep
those in separate commits if they turn out to be non-trivial.

Because Steps 02–05 bumped their own lines in their own commits, reverting this one lands the tree
back in exactly the configuration those steps ended in: **groups at `8.30.1`, floor at `7.31.1`** — a
state the repo already demonstrates is green (`ADP.Menus` sits in it today). **That is the practical
payoff of shared-last: the plan's most central revert lands on a known-good arrangement rather than
on a half-migrated tree.**

---

## Effort & risk

**Effort:** the smallest step in the plan, if the floor compiles as expected — and the Step 00 probe
has already told you whether it does. Items C and D are confirmation and investigation, not
construction. If the probe found a breaking change, take the estimate from that record, not from
here.

**Risks:**

| Risk | Mitigation |
|---|---|
| **A breaking change in `ShiftEntity.Model 2026.8.30.1` against `ADP.Models` is discovered only at the end** — the one real cost of shared-last | The Step 00 throwaway compile probe: bump `Models.csproj:48` on a scratch branch, build, record, revert. Fifteen minutes, never committed, and its result is a precondition of this step. If it was skipped, run it before item A. |
| **The floor does not compile clean** and the blast radius is wider than the survey found | The Step 00 probe answers this before four groups are built on top of it; item B checks it for real here. Scope expands at the end of the plan, with every other group already green and independently revertible. |
| A package-downgrade (`NU1605`) window opens between the hub and its consumers | Structurally impossible in this order: every other direct `ShiftEntity.Model` pin is already at `8.30.1` by Step 05, and `Cases.Shared` moves in the same commit as the hub. The exit criteria check for zero anyway. |
| **The `ADP.Menus.Generation` edge is missed** and a Menus-side fix silently moves this step's BDD baseline | Named in §Projects touched, built explicitly in item B, and `ADP.Menus.Tests` re-run in the exit criteria — here, at the end, where the re-run is a final confirmation rather than a check later steps would invalidate |
| **The step feels trivial and gets skipped**, because the tree already builds without it | Skipping it publishes `ADP.Models`, `ADP.Cases` and `Lookup.Services` still compiled against `7.31.1` underneath consumer groups that require `8.30.1`. Mixed published packages hard-brick a downstream host (see §Staged commits, atomic release). Step 07 releases; this step is what makes that release coherent. |
| `ADP.Models`'s zero-coverage hole is quietly carried forward | Item D forces an explicit, recorded decision |

# Step 03 — Shared floor: `ADP.Models`, `ADP.Cases`, `ADP.LookupServices`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `CLOSED` — libraries only; there are no endpoints here to prove.

**Goal:** restore the dependency root to green and confirm it needs no mapper work.

> **This is the core-first step.** Both orderings agree here: it is simultaneously the group
> everything else depends on *and* the cheapest work in the plan.

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
the project whose test count Step 01 pinned.** Two consequences, both handled in the exit criteria:
Step 03 must re-run `ADP.Menus.Tests`, and if Step 01 fixed a real regression inside
`ADP.Menus.Generation`, this step's `LookupServices.BDD` baseline moved under it.

**Not in scope:** `ADP.LookupServices/Lookup.Services.Functions` **no longer exists** — its csproj
was deleted at `67aa8a3e` and only untracked `bin/` and `obj/` remain, so nothing can build it. The
live hazard is the reverse of "leave it alone": its stale
`obj/Debug/net8.0/WorkerExtensions/WorkerExtensions.csproj` is the only `net8.0` `.csproj`-shaped
artefact in the tree, so **exclude `**/obj/**` and `**/bin/**` from any `find`-based inventory** or
the project count comes out wrong. Consider deleting the directory outright, in its own commit.

---

## Preconditions

- Step 02 `CLOSED` — the atomic bump is committed and the error list classified into
  `STATUS.md`'s `## Step 02 work queue`. (`CLOSED`, not `DONE`: Step 02 ends red by design and can
  never satisfy `DONE`'s "it builds".)
- These four projects were expected to compile clean straight out of Step 02. If they did not, that
  surprise is this step's problem and the scope below grows.

---

## Why this step is thin, and why it still exists

`ADP.Models` is the hub: **14 csproj `ProjectReference` it**, and **3** of those carry their own
direct `ShiftEntity.Model` pin (`ADP.Cases.Shared:32`, `ADP.ClaimableItems.Shared:34`,
`ADP.WarrantyClaims.Shared:33`) — so a hub-only bump would have produced 3 NU1605s, not the 9 an
earlier draft claimed. It carries zero AutoMapper profiles and zero repository triples, so the
expected work is *nothing* — but "expected nothing" has to be **checked**, because everything
downstream assumes it.

**Why "expected to compile unchanged" is safe, concretely:** `ADP.Models/Models` targets
**netstandard2.0** (one of only three netstandard2.0 projects in the repo, alongside
`Lookup.Services` and `ADP.Menus.Generation`; every other project is net10.0, and nothing
multi-targets). `ShiftSoftware.ShiftEntity.Model 2026.8.30.1` still ships `lib/netstandard2.0`,
verified in the local NuGet cache alongside `2026.7.31.1`. There is therefore no question of running
an analyzer-bearing package against an unsupported target here — the source generator arrives via
`ShiftEntity.EFCore`, which this project does not reference.

`ADP.Cases` is the interesting one. It has **no controllers and no repository** — one entity
(`Certificate`) consumed as a library. But that entity is the subject of **two repository triples in
two different groups**:

- `ADP.ClaimableItems/.../Repositories/ItemClaimCertificateRepository.cs` —
  `ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, ItemClaimCertificateDTO>`
- `ADP.WarrantyClaims/.../Repositories/WarrantyCertificateRepository.cs` —
  `ShiftRepository<ShiftDbContext, Certificate, CertificateListDTO, CertificateDTO>`

So `ADP.Cases` owns the entity but **neither owns nor configures its mappers** — they are generated
in the two consumer assemblies, from two different triples over the same entity, with different view
DTOs. That is **SPIKE-8**, and this step is where it gets *probed*. The probe may legitimately fail
(item B says why), in which case SPIKE-8 is recorded `BLOCKED` and resolved as item 0 of Step 06 —
Step 06's preconditions accept both outcomes explicitly. **What is not acceptable is guessing.**

---

## Work items

### A. Confirm the floor compiles unchanged

```bash
dotnet build ADP.Models/Models
dotnet build ADP.Cases/ADP.Cases.Shared
dotnet build ADP.Cases/ADP.Cases.Data
dotnet build ADP.LookupServices/Lookup.Services      # transitively builds ADP.Menus.Generation
dotnet build ADP.LookupServices/Lookup.Services.DuckDB
dotnet build ADP.Menus/ADP.Menus.Generation          # explicitly, so a failure is not mistaken for a Lookup one
```

Expected: all green, **no source change**. If any fails, classify the error and expand scope here.

### B. Resolve SPIKE-8 — the shared `Certificate` entity

Answer these three, from **emitted code**, not from reasoning:

1. Which assembly does each generated `Certificate` mapper land in — the consumer's `.Data`, or
   `ADP.Cases.Data`?
2. Can both triples coexist in one host process? A downstream host may install **both**
   ClaimableItems and WarrantyClaims, so two mappers over the same entity with different view DTOs
   must register side by side in `ShiftEntityMapperRegistry` without collision.
3. Does `ADP.Cases.Data` need to be a registered data assembly for either mapper to resolve at
   startup?

Method — build one consumer with generated files emitted and read what appears:

```bash
rm -rf ADP.ClaimableItems/ADP.ClaimableItems.Data/obj
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data -p:EmitCompilerGeneratedFiles=true
```

(This is a read-only probe of the current red state; expect compile errors from the profiles — Step
02 guarantees `ADP.ClaimableItems.Data` is red. If the generator does not run far enough to emit,
defer the probe to Step 06 and mark SPIKE-8 `BLOCKED`, recording that Step 06 must resolve it as its
item 0 — but say so, do not guess.)

The **baseline diagnostics already tell you these triples generate**, and what they cannot map:

```
SHENGEN004: Generated_Certificate_CertificateListDTO_ItemClaimCertificateDTO_16cfdeb0
            does not map: ReimbursementItemClaims, Notes
SHENGEN004: Generated_Certificate_CertificateListDTO_CertificateDTO_df5083c3
            does not map: WarrantyClaims, Notes
```

Both unmapped collections (`ReimbursementItemClaims`, `WarrantyClaims`) are populated today by
`ViewAsync` overrides in the respective repositories, not by the mapper. `Notes` is unmapped in both.
**Record that here** — it is the shared context Steps 06 and 07 both need, and discovering it twice
is waste.

### C. Resolve SPIKE-6 — `ADP.Models` has zero test coverage

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

### D. Confirm the floor's own tests

These have real coverage and must stay at baseline:

| Suite | Baseline |
|---|---|
| `ADP.Cases/ADP.Cases.Shared.Tests` | 43 / 43 |
| `ADP.LookupServices/Lookup.Services.Tests` | 47 / 47 |
| `ADP.LookupServices.BDD` | 452 / 452 |
| `ADP.Menus/ADP.Menus.Tests` | 262 / 2 / 0 — **re-run here.** Step 01 pinned this number *before* `Lookup.Services.DuckDB` moved, and `ADP.Menus.Tests:58` references it. |

---

## Verification

```bash
dotnet build ADP.Models/Models
dotnet build ADP.Cases/ADP.Cases.Shared
dotnet build ADP.Cases/ADP.Cases.Data
dotnet build ADP.LookupServices/Lookup.Services
dotnet build ADP.LookupServices/Lookup.Services.DuckDB

dotnet test ADP.Cases/ADP.Cases.Shared.Tests
dotnet test ADP.LookupServices/Lookup.Services.Tests
dotnet test ADP.LookupServices.BDD
dotnet test ADP.Menus/ADP.Menus.Tests          # re-run: this step moved Lookup.Services.DuckDB
dotnet test ADP.Models/Models.Tests            # see item C — currently a no-op

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
  with `Verified by` = "N/A — no endpoints; covered by unit suites, by the generated-tree diff, and
  by Steps 06/07 for the shared `Certificate` entity". Steps 00, 02 and 04 are the other three
  `CLOSED` steps; see `STATUS.md`'s vocabulary.

---

## Exit criteria

- [ ] All six projects in §Work-item-A build clean with **zero source changes**, or every change is
      listed and justified. `ADP.Menus.Generation` is one of them.
- [ ] Zero `NU1605` in this subtree.
- [ ] `ADP.Cases.Shared.Tests` = 43/43; `Lookup.Services.Tests` = 47/47; `LookupServices.BDD` =
      452/452 — **or the figure Step 01 left behind, if it fixed a regression inside
      `ADP.Menus.Generation`.** Compare against `STATUS.md`, not against this file.
- [ ] `ADP.Menus.Tests` re-run and still at its Step 01 figure. Step 01's pin was taken before
      `Lookup.Services.DuckDB` moved.
- [ ] `git diff --exit-code` over the three generated trees is **clean** after a build. A diff there
      means `ADP.Models`' or `Lookup.Services`' public shape changed under the bump — a finding, and
      the only signal this plan has for it.
- [ ] SPIKE-8 is `RESOLVED` in `STATUS.md` with all three questions answered from emitted code — or
      explicitly `BLOCKED` and deferred to Step 06, with the reason recorded.
- [ ] SPIKE-6 is `RESOLVED` in `STATUS.md` with the decision (fix or accept) and, if accepted, the
      caveat written into `STATUS.md` notes.
- [ ] Every project inventory run in this step excluded `**/obj/**` and `**/bin/**`, so the deleted
      `Lookup.Services.Functions`' stale `WorkerExtensions` artefacts did not enter the count.
- [ ] `STATUS.md` records this step **`CLOSED`** with the "N/A — no endpoints" note in `Verified by`.

---

## Rollback

If item A required source changes, revert them; the version bump itself belongs to Step 02 and is
reverted there. Nothing in this step alters behaviour, so rollback is limited to whatever item C
changed in `ADP.Models/Models.Tests`.

---

## Effort & risk

**Effort:** the smallest step in the plan, if the floor compiles as expected. Items B and C are
investigation, not construction.

**Risks:**

| Risk | Mitigation |
|---|---|
| **The floor does not compile clean** and the blast radius is wider than the survey found | Item A checks it first thing; scope expands here rather than surfacing mid-migration in a harder group |
| SPIKE-8 cannot be probed while the consumers are red | Explicitly allowed to defer to Step 06 as that step's item 0, provided it is recorded as `BLOCKED` rather than assumed away. Step 06's preconditions accept both outcomes, so the deferral cannot deadlock it. |
| **The `ADP.Menus.Generation` edge is missed** and a Menus-side fix silently moves this step's BDD baseline | Named in §Projects touched, built explicitly in item A, and `ADP.Menus.Tests` re-run in the exit criteria |
| **The step feels trivial and gets skipped** | Its entire value is being the checkpoint that the dependency root is sound before four groups build on it. Skipping it means a Step 06 error could originate two layers down. |
| `ADP.Models`'s zero-coverage hole is quietly carried forward | Item C forces an explicit, recorded decision |

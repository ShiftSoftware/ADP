# Step 00 — Baseline capture & parity harness

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `CLOSED` — this step builds the instrument; it has no endpoint surface of its own
whose parity could be proven, which is all `CLOSED` means. **It ends green**, like every step in this
plan.

**Goal:** build the endpoint-parity harness and capture every baseline **while the tree is still
pre-upgrade**. Nothing after this step can be verified if this step is skipped or done sloppily.

**The harness is deliberately temporary.** Step 08 deletes it once Step 07 closes — the decision is
recorded in `README.md` and is not re-argued here. Build it for the duration of this migration and no
longer: no NuGet packaging, no CI pipeline wiring, no operator documentation beyond `verification.md`
and this file, no abstraction for a sixth group that does not exist. **That is a ceiling on polish,
not on rigour.** The stability gate (item E) and the adversarial seeds (item D) are the entire reason
a green run at Steps 01–07 means anything; neither is negotiable, and neither gets trimmed because the
code is short-lived.

---

## Projects touched

New, both created by this step:

| Path | What |
|---|---|
| `ADP.EndpointParity/ADP.EndpointParity.Harness/` | class library: `Harness/` (capture layer) + `Bootstrap/` (wiring). **No group reference of any kind.** |
| `ADP.EndpointParity/ADP.EndpointParity.{Menus,Darlastic,Surveys,ClaimableItems,WarrantyClaims}/` | five thin xUnit v3 projects — listed in the order their steps run (01–05) — each referencing the harness plus **only its own** `.API` / `.Data` |
| `tools/parity.ps1` | operator driver: prerequisites, mode, `-Group`, `-Grant`, run, render diff |
| `tools/parity.psd1` | per-group config: route prefixes, order-insensitive collections, `excludedRoutes`, `writeUnreachable`, per-triple `httpWriteReachable`, restricted-grant action set |

**Six projects, not one — this is a structural decision and it has to be made here, before the
harness is written.** Group selection has to be a *compile-time* boundary, not a runtime `--filter`.
Every step in this plan ends green, so there is no longer a multi-step red window between steps — but
there is still a red window **inside** each group step, and it lasts as long as that group's refactor:
its `: Profile` classes are deleted before their replacements are wired, and its repository triples
are collapsed one at a time. A single test project referencing all five groups' `.Data` would fail to
build for the whole of whichever group step is in flight, so the group finished last week could not be
re-verified and the group starting next could not be baselined. The same coupling bites on any group
that stalls: one unbuildable or `BLOCKED` `.Data` — Darlastic under SPIKE-5, say — would take the
other four groups' parity runs down with it and collapse the staged verification into a big-bang check
at the end. `verification.md` §2 argues the same shape at more length.

**Almost no existing project is modified in this step.** The two permitted exceptions, both
behaviour-free and both forced by items B and C below, are: adding `public partial class Program` to
a sample's `Program.cs` (SPIKE-2), and adding the parity branch that suppresses a sample's own
seeding. Anything else outside `ADP.EndpointParity/`, `tools/`, `ADP.sln` and `.gitignore` gets
backed out.

---

## Preconditions

- Working tree clean at `14caf7c9` or later on `master`, with **no** package versions changed. (Item
  H bumps one line on a throwaway branch and reverts it; nothing it touches is committed.)
- SQL Server / SQL Express reachable with integrated security.
- `dotnet --version` reports `10.0.400`.
- Read `verification.md` end to end first. This step is that document's implementation and the
  normalization rules are not negotiable defaults — they are the design.

---

## Work items

### A. Resolve SPIKE-1 before building anything (timebox: 30 minutes)

`ShiftSoftware.ShiftFrameworkTestingTools` is published only to `2026.7.28.1`. Add it to a scratch
project, reference `ShiftEntity 2026.8.30.1` alongside, restore and build.

- **If it binds:** use `ShiftCustomWebApplicationFactory<,>` and
  `ShiftCustomWebApplicationBearerAuthSettings` for host + token minting, and treat `BasicTest<,>`'s
  method set as the inherited-route checklist.
- **If it does not:** re-implement JWT minting in `Bootstrap/ParityAuth.cs` and keep its method names as
  the specification for which routes to cover.

Record the finding in `STATUS.md` against SPIKE-1 either way. **Do not block on this** — the fallback
is known and bounded.

### B. Resolve SPIKE-2 — can a host be booted at all?

Two separate problems, both real today:

1. **No sample API declares `public partial class Program`**, and none has an `InternalsVisibleTo`
   for a test project. `WebApplicationFactory<Program>` will not compile against any of them as
   things stand. Decide and record: add the partial-class declaration to each sample's `Program.cs`
   (a one-line, behaviour-free change), or use a different entry point.
2. **`ADP.ClaimableItems` and `ADP.WarrantyClaims` have no host at all.** Prove the mounted host
   (`Bootstrap/MountedHostFactory.cs`) can boot one of them through its own
   `Add<Group>ApiServices<ParityDb>(mvcBuilder, configure)` entry point, with a disposable DB via
   `EnsureCreated` and no migrations.
3. **The sample hosts seed themselves before the harness gets a say.**
   `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:163-196` unconditionally runs
   `EnsureCreatedAsync`, `SeedDBAsync(...)`, `SetFullAccessAsync(...)` and `SeedSampleSurveysAsync()`;
   the Menus sample does the equivalent. Those rows are identity-keyed and non-adversarial, and they
   are enough to satisfy a naive "> 0 rows" gate on their own (`verification.md` §7, §8.1). **Add a
   config flag or a `Parity` environment branch that skips the sample's seeding block**, and decide
   and record how the parity seed's explicit long PKs get past EF's identity columns —
   `IDENTITY_INSERT` or `ValueGeneratedNever()`. This is the fiddly part; do not leave it implicit.

If the mounted host cannot boot, Steps 04 and 05 are `BLOCKED` and the fallbacks are the ones named
in `verification.md` §6 — a ~200-line sample API for **either** group (WarrantyClaims first, but
ClaimableItems gets the same option), or per-triple mapper-level goldens with the reduced claim
written down. **"BLOCKED with no alternative" is not an acceptable outcome for ClaimableItems.**

### C. Build the harness

Per the layout in `verification.md` §3. In dependency order:

1. `Harness/Normalizer.cs` — **all** rules from `verification.md` §4 in one file, each with a comment
   naming the rule it implements. This file is the one a reviewer reads to decide whether to trust a
   green run. Created IDs compare **literally** by default (Rule 1) — no alias map unless drift is
   actually observed, and then keyed by JSON path, never by a counter.
2. `Harness/Transcript.cs`, `Harness/TranscriptDiffer.cs`.
3. `Bootstrap/ParityDb.cs`, `Bootstrap/ParityAuth.cs`, `Bootstrap/SampleHostFactory.cs`.
   **`ParityAuth` mints two principals — `FullAccess` and `Restricted`** — with the restricted grant
   set declared per group in `tools/parity.psd1`, because each group has its own action tree and
   "restricted" has no group-independent meaning. Four later step files make the restricted pass
   mandatory; it has to be built here or it is discovered missing at Step 05.
4. `Bootstrap/RouteCatalog.cs` — emits the route catalogue as a golden, **and drives the case list**:
   every catalogue entry resolves to ≥1 case or to an `excludedRoutes` entry with a reason.
5. `Bootstrap/RequestFactory.cs` — minimal-valid create body plus sentinel overlay
   (`verification.md` §5), with hash-id members filled from a seeded row's real hash id.
6. `Harness/ParityRunner.cs`. **It carries the global HTML assertion:** no response body in any group
   may be `text/html`. Both the Menus and the Surveys samples map a fallback file
   (`MapFallbackToFile("index.html")`), so a deleted or renamed route returns 200 + HTML rather than
   404 and would pass silently. This is a global rule from the start, not a Menus special case
   retro-fitted at Step 01.
7. `tools/parity.ps1` with `capture` / `verify` / `summary` / `accept` verbs and a `-Grant` parameter
   (`FullAccess` | `Restricted`), `-Group` selecting the **project**.

**Enforce the capture-layer rule while writing, not afterwards:** no `ShiftEntityResponse<T>`, no DTO
types, no repository types anywhere under `Harness/`. Only `HttpClient`, `System.Text.Json`, `string`.
Grep `Harness/` for it before committing. **`Bootstrap/` is deliberately outside the rule** — it
cannot satisfy it, since `RequestFactory` must reflect over DTO types and recognise
`JsonHashIdConverterAttribute` — so it is reviewed by reading its diff instead.

### D. Author adversarial seeds

`Seed/<group>.seed.json`, explicit long primary keys. Per `verification.md` §8.2, **one hostile row
per known trap, per group**:

- a parent with at least one **soft-deleted child** (trap 1),
- a **link row whose own PK differs from the foreign id it carries** (trap 2),
- a row where a **repository-derived column** differs from anything a client would send (trap 3-write),
- for WarrantyClaims specifically, a claim with **non-null values in all five distributor-side
  members** (trap 3-read) — without this the exposure is invisible.

**Tag them.** Each hostile row carries `"hostile": ["trap1","trap2", …]`, and `ParityRunner` emits a
`DETAIL` + `REVISIONS` case for **every tagged row** (cheapest correct version: for every seeded root
row). A single `{seededId}` per entity makes trap 1 fire only by luck — the concrete site,
`MenuVariantRepository.cs:72-74`, is visible only on the detail body of the variant that owns a
soft-deleted item.

**Then author the create bodies.** Per entity, `Seed/<group>.<Entity>.create.json`: a hand-written
**minimal valid** request body that satisfies FK resolution, hash-id deserialization and
FluentValidation. `RequestFactory` overlays sentinels only onto members the minimal body does not
need for validity. Skipping this is how the write path silently covers nothing: a body of pure
`PARITY::` strings 400s before it reaches the mapper (`verification.md` §5 has the worked example
from `MenuRepository.UpsertAsync`).

A seed without hostile rows is a seed that cannot fail; a create body that 4xxs is a write path that
was never tested. This is the highest-leverage work item in the step.

### E. Prove stability — the gate

Run `capture` twice in a row on the **unchanged** tree and diff the two baselines.

> **If two identical runs diff, the normalization is wrong. Fix it before trusting anything.**

Iterate on `Normalizer.cs` until the diff is empty — but fix it by making values *deterministic*
(seeding, salt pinning, explicit `$orderby`), not by widening normalization. Every rule you loosen is
a regression you can no longer see.

### F. Capture and commit baselines

For every group with a working host, in step order: `Menus` (current tree, post-migration — the
*pre*-migration Menus baseline is Step 01's job), `Darlastic`, and `Surveys`. For `ClaimableItems` and
`WarrantyClaims`, capture via the mounted host once item B is resolved.

**Capture each group under both grants** — `-Grant FullAccess` and `-Grant Restricted`. A restricted
baseline captured after the code changes is not a baseline.

Run `summary` on each and check it before committing — in particular `CREATE 2xx: n/m`, the
catalogue-coverage line, and the hostile-row presence line. Commit goldens in their own commit,
separate from harness code.

### G. Record the solution baseline

Pin these numbers in `STATUS.md` notes so later steps compare against something concrete:

Pin the **project count after this step has added its own projects to `ADP.sln`**, not before:
`ADP.sln` contains exactly 53 `.csproj` today and this step adds six, so the figure Step 07 compares
against is **59/59** unless the harness layout changed. Recording "53/53" here and again at Step 07
guarantees a false red. Record **both** numbers — 53 pre-harness, 59 post-harness — because Step 08
deletes the six parity projects again and its own check is against the pre-harness figure.

| Measure | Baseline |
|---|---|
| `dotnet build ADP.sln` | exit 0, **0 errors**, 53/53 projects *pre-harness*; record the post-harness figure as the number Step 07 uses, and keep the pre-harness figure as the number Step 08 returns to |
| compiler warnings | **535** (stable warm and cold) |
| `SHENGEN004` | **10** — `ClaimableItems.Data` 5, `Surveys.Data` 3, `WarrantyClaims.Data` 2. **Zero in Menus.** |
| `SHENGEN007` / `008` / `010` | **0 anywhere** |
| `NU1605` / `NU1701` / `NU1603` / `MSB3277` | **0 anywhere** |
| `NU1903` (AutoMapper CVE) | 42 lines across **21 projects** — this is the upgrade's scoreboard and should fall |
| .NET tests | **1544 total: 1533 passed, 2 failed, 9 skipped** |
| web component tests | 114 passed, 4 suites |
| generated trees | `git diff --exit-code ADP.WebComponents/adp-web-components/src/global/types/generated ADP.Docs/Docs/docs/generated ADP.TestData/environments` **clean after a full solution build** — four `AfterTargets="Build"` self-runners rewrite 247 tracked files on every `dotnet build ADP.sln` (`README.md` §7). Establishing that they are a no-op on the pre-bump tree is what makes the same check meaningful at Steps 06 and 07. |

Known-acceptable red: the 2 `SampleDataSeedingTests` failures (drifted local sample DB, duplicate-key
on a unique name index). Everything else must stay green.

### H. Probe `ADP.Models` against `ShiftEntity.Model 2026.8.30.1` (timebox: 15 minutes)

**Do this first.** It is independent of every other item in this step — it touches nothing the harness
touches — and it is the one thing that makes the shared-last ordering safe to commit to.

This plan moves the shared floor (`ADP.Models`, `ADP.Cases`, `Lookup.Services.DuckDB`) to **Step 06**,
after all five group steps. That ordering is what keeps every step green and removes NU1605 from the
plan entirely. Its one cost: if `ShiftEntity.Model 2026.8.30.1` carries a breaking change that lands
on `ADP.Models`, shared-last defers discovering it to the very last build of the migration. Buy the
ordering without the late surprise by finding out on day one:

```powershell
git switch -c scratch/models-probe
# bump ONLY ADP.Models/Models/Models.csproj:48 — ShiftEntity.Model 2026.7.31.1 -> 2026.8.30.1
dotnet build ADP.Models/Models
# record the result, then throw the branch away
git checkout -- ADP.Models/Models/Models.csproj
git switch -
git branch -D scratch/models-probe
```

**Do not commit the bump and do not carry the branch forward.** Step 06 owns that edit, in its own
commit, alongside `Cases.Shared:32`. The probe's only product is a line in `STATUS.md`: green, or red
with the error codes and the files they land in.

**Step 06 consumes this result and its preconditions name it.** Green means Step 06 is the four-line
version edit it is written as. Red means Step 06 carries real code work, and the plan learns that at
the start instead of at the end — raise it as a spike in `STATUS.md` immediately rather than absorbing
it into Step 06's estimate, because a breaking change in `ADP.Models` reaches all 14 of its consumers
and could be grounds for revisiting the ordering while there is still time to.

---

## Verification

```powershell
# harness builds, and each group project builds independently of the others (step order)
dotnet build ADP.EndpointParity/ADP.EndpointParity.Harness
dotnet build ADP.EndpointParity/ADP.EndpointParity.Menus            # Step 01
dotnet build ADP.EndpointParity/ADP.EndpointParity.Darlastic        # Step 02
dotnet build ADP.EndpointParity/ADP.EndpointParity.Surveys          # Step 03
dotnet build ADP.EndpointParity/ADP.EndpointParity.ClaimableItems   # Step 04
dotnet build ADP.EndpointParity/ADP.EndpointParity.WarrantyClaims   # Step 05

# the stability gate — must produce an EMPTY diff
.\tools\parity.ps1 capture -Group Surveys
Copy-Item -Recurse ADP.EndpointParity\baselines\surveys ADP.EndpointParity\baselines\surveys-run1
.\tools\parity.ps1 capture -Group Surveys
# compare surveys-run1 against surveys: must be identical

# sanity-check every captured baseline, both grants
.\tools\parity.ps1 summary -Group Menus
.\tools\parity.ps1 summary -Group Darlastic
.\tools\parity.ps1 summary -Group Surveys
```

**Prove the split works before trusting it.** The whole point of six projects is that a red group
does not take the others down. Rehearse it: temporarily break a file in `ADP.Surveys.Data`, confirm
`dotnet build ADP.EndpointParity/ADP.EndpointParity.Menus` still succeeds, then revert.

**Group-specific caveats.** Every group needs SQL. Cosmos and the blob emulator must be *off* /
unconfigured so replication and provisioning are skipped. Darlastic additionally needs SPIKE-5
resolved before it can be captured at all — if it cannot boot, record it and move on; it is a smoke
target only.

---

## Exit criteria

- [ ] Each of the six parity projects builds independently; breaking one group's `.Data` leaves the
      other four group projects building.
- [ ] Nothing outside `ADP.EndpointParity/`, `tools/`, `ADP.sln`, `.gitignore` is modified, except
      the two permitted sample edits (`public partial class Program`; the parity seeding branch),
      each listed in the commit message.
- [ ] No type from `ShiftSoftware.ShiftEntity.*` or any group's `Shared`/`Data` assembly appears
      under `ADP.EndpointParity/ADP.EndpointParity.Harness/Harness/` (grep proves it). **`Bootstrap/`
      is out of scope for this grep by design** — do not widen the grep and do not weaken it.
- [ ] Two consecutive `capture` runs on the unchanged tree produce a **byte-identical** baseline tree.
- [ ] `summary` for every captured group reports **0 5xx**, and **every list case's baseline contains
      every seeded hostile row's id, matched literally** — not merely "> 0 rows", which the sample
      host's own demo seed satisfies on its own.
- [ ] `summary` reports **`CREATE 2xx: n/n` and `UPDATE 2xx: n/n` at 100%**, or every shortfall is an
      entity listed in `parity.psd1`'s `writeUnreachable` with a written reason.
- [ ] `summary` reports **catalogue routes covered: n/n**, with every exclusion in
      `parity.psd1`'s `excludedRoutes` carrying a reason.
- [ ] Every triple has `httpWriteReachable` recorded; each `false` has a mapper-level write golden
      instead (`verification.md` §5).
- [ ] Each group's seed contains at least one soft-deleted child, one link row whose PK differs from
      its carried foreign id, and one repository-derived column that differs from client input —
      **each tagged, and each with its own `DETAIL` case whose baseline body is non-empty.**
- [ ] The WarrantyClaims seed has a claim with **all five distributor-side members non-null**.
- [ ] A **restricted-grant baseline exists for every group with an action tree**, captured here and
      not later.
- [ ] The global "no response body is `text/html`" assertion is in `ParityRunner` and passing for
      **every** group, Surveys included.
- [ ] The sample hosts' own seeding is suppressed under the parity branch, and the explicit-id
      insertion path is documented in `STATUS.md`.
- [ ] Route catalogue goldens exist for every group that boots.
- [ ] Baselines are committed in a commit that contains **no** harness source changes.
- [ ] SPIKE-1 and SPIKE-2 are `RESOLVED` in `STATUS.md`, with their findings recorded.
- [ ] The item H `ADP.Models` probe has been run and its outcome — green, or red with the error codes
      and files — is recorded in `STATUS.md`. The scratch branch is deleted and `git status` shows no
      change to `ADP.Models/Models/Models.csproj`.
- [ ] The §G baseline numbers are recorded in `STATUS.md`'s **`## Recorded baselines`** section,
      including **both** `ADP.sln` project counts — the post-harness figure Step 07 compares against,
      and the pre-harness figure Step 08 returns to after deleting the parity projects.
- [ ] The plan directory is **fully committed** — the twelve original files are tracked as of
      `4c4b3142`, but `08-harness-removal.md` and the 2026-09-01 reorder edits to `README.md`,
      `STATUS.md`, `04`, `06` and `07` are not. Commit them before this step's first status change,
      or `STATUS.md` rule 7 cannot be obeyed.

---

## Rollback

Delete `ADP.EndpointParity/` and `tools/parity.ps1` / `tools/parity.psd1`, revert the six `ADP.sln`
entries and the `.gitignore` line, and revert the two permitted sample edits. No production behaviour
was touched, so rollback is total and carries no risk to the rest of the repo. Item H leaves nothing
to roll back — the branch is deleted as part of the item, and its only durable output is a `STATUS.md`
note. Step 08 performs this same deletion deliberately at the end of the migration; the two are the
same operation, run for different reasons.

---

## Effort & risk

**Effort:** the largest step in the plan. The harness is a real piece of software, and the seed
authoring in item D is slow, careful work that cannot be automated.

**Risks:**

| Risk | Mitigation |
|---|---|
| **Normalization tuned until it passes.** The failure mode is invisible: you loosen a rule to kill a noisy diff and delete a regression signal with it. | Item E's rule — fix by making values deterministic, not by widening normalization. Every loosened rule gets a comment in `Normalizer.cs` saying what it gives up. |
| A seed with no hostile rows makes the whole exercise decoration | Item D is an explicit exit criterion, not a nice-to-have |
| Mounted host cannot boot the two hostless groups | SPIKE-2, resolved here rather than discovered at Step 04. **Both** groups have a named fallback (`verification.md` §6) — neither is allowed to end at `BLOCKED` with nothing after it. |
| **Every `CREATE` 4xxs and the whole write path covers nothing**, while every gate stays green | Hand-authored minimal-valid bodies (item D) plus the 100% `CREATE 2xx` gate. This is the single most likely way trap 3-write goes undetected everywhere. |
| **A mandatory capability is discovered missing at Step 05** — the restricted grant | Built here, in item C, and captured here, in item F |
| `ShiftFrameworkTestingTools` does not bind post-upgrade | SPIKE-1; bounded fallback already specified |
| **Shared-last defers a breaking change in `ADP.Models` to the last build of the migration** | Item H — a 15-minute throwaway compile probe run on day one. It converts a late unknown into an early recorded fact without committing anything. |
| **The harness is temporary, so it gets built cheaply — including the parts that carry the proof** | The stability gate (item E) and the adversarial seeds (item D) are exit criteria, not polish. Temporary licenses skipping packaging and CI wiring; it does not license skipping either of those. |
| Time pressure tempts skipping straight to the first group step | Steps 01–07 have no meaning without this, and Step 08 has nothing to remove. A version bump with no baseline is unverifiable and unrollbackable-with-confidence — and under the staged ordering there are now five separate bumps that each need one. |

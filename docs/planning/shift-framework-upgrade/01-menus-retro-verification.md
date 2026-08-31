# Step 01 — Retro-verify `ADP.Menus` (the harness's proving ground)

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** grade the harness against a migration whose answer is already known, and close the repo's
one `DONE`-but-not-`VERIFIED` gap.

---

## Why this step exists, and why it is second

`ADP.Menus` is already on `2026.8.30.1` and already migrated off AutoMapper (commit `14caf7c9`). It
builds, its tests are at baseline, and it owns **none** of the 29 package-reference lines the group
steps carry — so, unlike Steps 02–05, this step opens with no version bump of its own.
**But its endpoint parity was never proven** — no harness existed when it was done.

That makes it uniquely valuable *now*: it is a completed migration with a reviewed diff, so the set of
behaviour changes it should have produced is knowable in advance. Running the harness across it turns
an unknown into a graded test:

> **A diff the harness reports here is either a change the migration knowingly made, or a harness
> bug.** Both outcomes are useful, and you can tell them apart — which is exactly what you cannot do
> on a group you are migrating for the first time.

Running this *before* any new migration means that at Step 03, when the harness reports a diff on
Surveys, you already know the harness itself is sound. Skip this step and every later diff is
ambiguous.

---

## Projects touched

**None.** No source file changes in this step. The only artifacts produced are baselines and a report.

The Menus group is **11 csproj**, of which 8 carry a `2026.8.30.1` reference. The projects *exercised*
here are the migrated ones, principally through `ADP.Menus/samples/ADP.Menus.Sample.API`. Three
Menus projects are outside that path and were missing from an earlier draft of this file — name them,
because two of them touch machinery this upgrade breaks:

| Project | Why it matters here |
|---|---|
| `ADP.Menus.Generation` | netstandard2.0, carries the conditional `ADP.Models` reference pair (`csproj:54-57`), and is `ProjectReference`d by `Lookup.Services` — so **Step 06 builds it.** A regression fixed here inside it moves Step 06's `LookupServices.BDD` figure — but Step 06 is now **last**, so that re-check lands at the very end of the plan rather than immediately after this step. The lag is the risk: write the fix and the figure it produces down here (item C) and in `STATUS.md`, because Step 06 has nothing to compare against otherwise. |
| `samples/ADP.Menus.Sample.Functions` | a **second Menus host**. References `ADP.Menus.Data` + `ADP.Menus.Sync`, and `Program.cs:38` calls `AddShiftEntityCosmosDbReplication<MenuReplicationDB>()` — the exact replication path `conventions.md` §6b calls a compile break. It boots mapper validation and nothing in this plan exercises it; **at minimum, build it and confirm it starts.** |
| `samples/ADP.Menus.Sample.FreeServiceParity` | references `Lookup.Services` and `Lookup.Services.DuckDB` (both Step 06 projects) and `Program.cs:119` calls `AddShiftEntityHashId(...)` — the salt whose pinning `verification.md` Rule 1 depends on. Read its salt/min-length configuration when pinning the parity host's. |

---

## Preconditions

- Step 00 `CLOSED` — in particular, its stability gate passed (two identical captures diffed to
  zero), its `CREATE 2xx` gate is at 100%, and both grants' baselines exist. **Without that, a diff
  here cannot be attributed.** (`CLOSED`, not `VERIFIED`: Step 00 has no endpoints of its own —
  see `STATUS.md`'s vocabulary.)
- The Menus adversarial seed from Step 00 item D exists and includes soft-deleted children and a link
  row whose PK differs from the foreign id it carries. Menus is the group where trap 2 was actually
  found, so the seed must contain a row that would have exposed it.
- `git worktree` usable; enough disk for a second checkout.

---

## Work items

### 0. Resolve SPIKE-9 — the `Replicate<T>` delegate signature (timebox: 30 minutes)

SPIKE-9 gates Steps 04 and 05 and, in an earlier draft, no step owned it. It is answerable **today**,
from the pre-bump tree, because Menus is already migrated: roughly 19 `Replicate<T>` /
`UpdateReference<T>` call sites already exist at `2026.8.30.1` in
`ADP.Menus/ADP.Menus.Sync/Extensions/MenuReplicationExtensions.cs` (lines 79, 101, 118, 143, 163,
167, 187, 191, …) and `Replication/MenuCatchUpReplicationExtensions.cs`.

- [ ] Read them and write down the exact required delegate shape — parameter list, return type,
      whether the `mapping` argument is positional or named, and how a nested collection projection
      is expressed.
- [ ] Record it against SPIKE-9 in `STATUS.md` as `RESOLVED — <the signature>`.
- [ ] Note explicitly that the reference implementation lives in `.Sync`, **not** `.Data`, so nobody
      hunts for it in the wrong assembly at Step 04.

This step already has the Menus group open; doing it anywhere else costs more.

### A. Capture the retroactive pre-migration baseline

The pre-upgrade tree is `14caf7c9^`. Copy the harness into a worktree at that commit so the *same*
harness source runs on both sides.

```powershell
git worktree add ..\ADP-pre-menus 14caf7c9^
Copy-Item -Recurse .\ADP.EndpointParity ..\ADP-pre-menus\ADP.EndpointParity
Copy-Item -Recurse .\tools           ..\ADP-pre-menus\tools
Push-Location ..\ADP-pre-menus
  $env:PARITY_MODE="capture"; dotnet test ADP.EndpointParity/ADP.EndpointParity.Menus
Pop-Location
```

Only the Menus project needs to build in the worktree — another reason the harness is split per
group rather than filtered at runtime.

**The harness will compile against `2026.7.31.1` there.** That is the point — and it is why the
capture-layer rule from Step 00 (HTTP + JSON + string only) matters. If the harness fails to compile
in the worktree because it references a framework type, that is a Step 00 defect: fix it there, not
here.

Run `summary` inside the worktree before trusting the capture. An empty or all-error baseline is the
usual silent failure.

### B. Copy the baseline forward and replay

```powershell
Copy-Item -Recurse ..\ADP-pre-menus\ADP.EndpointParity\baselines\menus .\ADP.EndpointParity\baselines\
.\tools\parity.ps1 verify -Group Menus
```

### C. Classify every diff — this is the actual work

Take each diff and put it in exactly one bucket. **Every diff must land in a bucket; "probably fine"
is not a bucket.**

| Bucket | Meaning | Action |
|---|---|---|
| **Expected — known migration change** | The `14caf7c9` diff deliberately caused it | `accept` with a reason quoting the repository file and line that caused it |
| **Expected — framework convention improvement** | e.g. a select DTO's `Text` is now populated where the old profile left it null (`conventions.md` §3) | `accept` with a reason naming the convention |
| **Harness bug** | Non-determinism, wrong normalization, missing `$orderby`, alias-map instability | Fix `Normalizer.cs` / the case list, re-run **both** sides |
| **Real regression in the shipped Menus migration** | A trap that was missed | **Stop.** File it, fix the Menus repository, re-verify. This is a live bug in `master`. |

The fourth bucket is not hypothetical. Audit it against the specific things the Menus migration is
known to have decided:

- **Two collections were deliberately left unfiltered for soft-deletes**, because the old profile did
  not filter them either. If the harness shows those collections *growing*, the reasoning was wrong.
- **`MenuVariant.Items` is never removed, only added/updated** — an item dropped from the DTO is not
  soft-deleted by the mapper; a separate path cascades that. A round-trip case that drops an item and
  reads back is the direct test of whether that separate path actually fires.
- **Two files carry file-scoped `SHENGEN` suppressions** covering `004`/`007`/`008`. A list column
  coming back empty in the diff is exactly what a swallowed `SHENGEN007` looks like.

### D. Confirm the fallback-route assertion is in place and firing

The Menus sample maps a fallback file, so **an unmatched route returns 200 with HTML, not 404**. A
deleted or renamed route would therefore pass silently. So does the Surveys sample
(`Program.cs:200,204`), which is why the assertion belongs to Step 00 item C as a **global** rule and
not to this step as a Menus special case.

- [ ] Confirm `ParityRunner`'s "no response body is `text/html`" assertion exists and is applied to
      Menus, and demonstrate that it fires: request a route that does not exist and confirm the
      harness reports a hard failure rather than a 200.
- [ ] If it is missing, that is a **Step 00 defect** — fix it there, and re-capture **both** sides.

### E. Diff the route catalogues

The pre- and post-migration route catalogues should be **identical** — the Menus migration was a
mapper change, not a routing change. Any route appearing or disappearing is a finding.

---

## Verification

```powershell
.\tools\parity.ps1 summary -Group Menus      # sanity-check the retroactive baseline first
.\tools\parity.ps1 verify  -Group Menus
.\tools\parity.ps1 verify  -Group Menus -Grant Restricted   # MenuRepository is row-scoped

# the group's own suite, for the record
dotnet test ADP.Menus/ADP.Menus.Tests

# the two hosts nothing else in this plan exercises
dotnet build ADP.Menus/samples/ADP.Menus.Sample.Functions
dotnet build ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity
```

The restricted pass matters here specifically: `MenuRepository.cs:23-26` applies
`FilterByTypeAuthValues` on `BrandID`, which is the plan's clearest genuinely row-scoped surface and
the thing the restricted principal actually exists to exercise (`verification.md` §8.7).

**Group-specific caveats.**

- **Test baseline is `262 passed / 2 failed / 0 skipped (264 total)`** — not the older remembered
  `259 / 2 / 1`. The pass count is Cosmos-emulator sensitive (±1: one provisioning test skips when
  the emulator is down) and the fail count is local-SQL sensitive (±2: the drifted sample DB). Either
  compare on identical machine state or filter out `SampleDataSeedingTests` and
  `ServiceMenusProvisioningTests` first.
- Menus is the only group here with a real sample host, so this is **full HTTP parity** — the
  strongest evidence any step in this plan produces.
- `.xlsx` export endpoints are `PARTIAL` (SPIKE-10). Do not count them as covered.

---

## Exit criteria

- [ ] A retroactive baseline exists at `ADP.EndpointParity/baselines/menus/`, captured from
      `14caf7c9^` with the same harness source.
- [ ] `summary` on that baseline shows **> 0 rows in every list case** and **0 5xx**.
- [ ] `verify -Group Menus` completes, and **every** diff is classified into one of the four buckets
      in item C.
- [ ] **Zero** diffs remain in the "harness bug" bucket.
- [ ] **Zero** diffs remain in the "real regression" bucket, or each has a filed issue and a fix
      landed and re-verified.
- [ ] Every accepted diff has a recorded reason naming the repository file/line or the framework
      convention responsible.
- [ ] The route catalogues from both sides are identical, or each difference is explained.
- [ ] No Menus response body is HTML (the fallback-route assertion is in place and passing).
- [ ] `ADP.Menus.Tests` is at `262 / 2 / 0` on this machine state, with the 2 failures being the
      known drifted-sample-DB duplicate key. **Record the exact figure in `STATUS.md`, not only in the
      report** — Step 06 bumps `Lookup.Services.DuckDB`, which `ADP.Menus.Tests:58` references, so this
      number gets re-checked there, and Step 06 is now the **last** step in the plan. A figure that is
      only remembered will not survive that gap.
- [ ] `ADP.Menus.Sample.Functions` and `ADP.Menus.Sample.FreeServiceParity` build; the Functions host
      starts far enough to run `AddShiftEntityCosmosDbReplication<MenuReplicationDB>()` without
      throwing (or the reason it cannot be started here is recorded).
- [ ] **SPIKE-9 is `RESOLVED` in `STATUS.md`** with the exact `Replicate<T>` / `UpdateReference<T>`
      delegate signature written out, and the note that the reference implementation is in
      `ADP.Menus.Sync`, not `ADP.Menus.Data`.
- [ ] If item C forced a fix inside `ADP.Menus.Generation`, the fix and the `LookupServices.BDD`
      figure it produces are recorded in `STATUS.md` — Step 06 builds that project and does not
      re-check it until the end of the plan.
- [ ] The restricted-grant Menus pass ran and its diffs are classified like the rest.
- [ ] `STATUS.md`: the Menus row moves from `DONE` to `VERIFIED`, with `Verified by` naming the
      report path and the run.
- [ ] The worktree is removed: `git worktree remove ..\ADP-pre-menus`.

---

## Rollback

Nothing to roll back — no source changed. If the baseline is bad, delete
`ADP.EndpointParity/baselines/menus/` and re-capture. If item C or D forced a harness fix, that
change belongs to Step 00's scope and both sides must be re-captured with the corrected harness.

---

## Effort & risk

**Effort:** small in code, moderate in judgement. Item C is the whole step and it is careful reading,
not typing.

**Risks:**

| Risk | Mitigation |
|---|---|
| **The retroactive baseline is not truly comparable** — the worktree restores different transitive packages, or the harness compiles differently there | Same harness source copied in verbatim; the capture-layer rule keeps it framework-independent. If it will not compile in the worktree, that is the proof the rule was violated. |
| Diffs get accepted in bulk to "get through it" | Item C forces a bucket and a written reason per diff. Bulk-accepting here destroys the one calibration opportunity in the plan. |
| A real regression is found in shipped `master` code | Genuinely possible and a **success** for this step, not a failure. Budget for it: it means stopping and fixing Menus in `master` before the first group step (Step 02, `ADP.Darlastic`) starts its own package bump. |
| **A fix landed here inside `ADP.Menus.Generation` is not re-checked until the end**, because Step 06 now runs last | Record the fix and the `LookupServices.BDD` figure it produces in `STATUS.md` when the fix lands; Step 06's exit criteria compare against `STATUS.md`, not against memory |
| The fallback route masks a missing endpoint | Item D, verifying the **global** Step 00 assertion actually fires — and it applies to Surveys too, not just Menus |
| SPIKE-9 goes unowned and surfaces mid-Step-04 | Item 0 resolves it here, from the already-migrated call sites, in half an hour |
| Cosmos emulator state shifts the test count under you | Documented ±1 / ±2 tolerance and the filter workaround |

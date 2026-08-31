# Step 08 — Harness removal & cleanup

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `CLOSED` — this step removes the instrument; it has no endpoint surface of its
own whose parity could be proven, which is all `CLOSED` means. **It ends green**, like every step in
this plan.

**Goal:** delete the endpoint-parity harness and every artefact Step 00 created for it, revert the two
edits Step 00 made to real projects, and return the solution to a 53-project tree that builds green.

**The decision is recorded, not re-argued.** The parity harness is removed once the upgrade is
finished, because the Shift framework has not had many releases recently and a permanent regression
harness is not worth its ongoing maintenance (`STATUS.md` §Plan reorder, decision 2; `README.md` §7;
`verification.md` §9). Step 00 was written knowing this. This step's job is to make the removal
**clean and complete**, not to relitigate it.

**Why this is a written step and not a mental note.** Deleting `ADP.EndpointParity/` is the obvious
part and nobody forgets it. The part that gets forgotten is item D: Step 00 was permitted to modify
exactly **two things outside its own footprint**, both inside real sample hosts, and one of them
changes how a sample seeds itself. Left behind, that edit is a silent behaviour change sitting in a
project nobody associates with a migration that finished months earlier.

---

## Projects touched

Everything here is a **deletion** or a **revert**. No product code is written.

| Path | Change |
|---|---|
| `ADP.EndpointParity/` | **deleted** — the harness library, the five per-group test projects, `Seed/`, `baselines/`, `reports/` |
| `tools/parity.ps1` | **deleted** |
| `tools/parity.psd1` | **deleted** |
| `ADP.sln` | six project entries removed |
| `.gitignore` | the `ADP.EndpointParity/reports/` entry removed |
| a sample's `Program.cs` — parity seeding branch | **reverted (mandatory)** |
| a sample's `Program.cs` — `public partial class Program` | decision recorded; **recommended: leave** |
| `azure-pipeline.yml` | only if Step 07 item G wired a temporary parity job |

**Nothing else.** If a diff in this step touches a file outside that list, it is not part of this step.

---

## Preconditions

- **Step 07 at `VERIFIED`, and the release out.** This ordering is one-way. Step 07 item C's full
  parity sweep *is* the harness; a release whose caveats rest on a harness run cannot be cut after the
  harness is gone.
- **Step 07's parity results are already written into `STATUS.md`** — per group, both grants
  (`07-release-readiness.md` §Hand-off to Step 08). This is an exit criterion of Step 07 and a
  precondition here for the obvious reason: after this step there is no way to re-run them, so an
  unrecorded green run becomes an unverifiable claim.
- Working tree clean, on `master`, at or after the Step 07 release commit.
- Every step at its terminal status: 00, 02, 06 `CLOSED`; 01, 03, 04, 05, 07 `VERIFIED`.

---

## Work items

### A. Create the recovery point **before** deleting anything

The harness is recoverable from history either way, but only if someone can find it. Tag it
deliberately rather than relying on a future reader to bisect for it:

```bash
git tag -a parity-harness-final -m "Last commit containing the endpoint-parity harness (Step 08 removes it)"
git push origin parity-harness-final
```

- [ ] Tag created **on the commit before this step's first deletion commit**, and pushed.
- [ ] Confirm `STATUS.md`'s Step 07 row carries the item C parity results — per group, both grants.
      If it does not, **stop and fill it in first.** That record is the durable evidence; the
      `baselines/` tree is not.

The Step 07 release tag (`release-nuget-<yy-mm-dd>-<nn>`) also predates this step and therefore also
contains the harness. Name both in the commit message: the purpose-made tag is the one to reach for,
the release tag is the one that exists whether or not anyone remembered.

### B. Delete the harness projects, the driver, and their solution and gitignore entries

Remove from the solution **first**, while the files still exist:

```powershell
dotnet sln ADP.sln remove `
  ADP.EndpointParity/ADP.EndpointParity.Harness/ADP.EndpointParity.Harness.csproj `
  ADP.EndpointParity/ADP.EndpointParity.Menus/ADP.EndpointParity.Menus.csproj `
  ADP.EndpointParity/ADP.EndpointParity.Darlastic/ADP.EndpointParity.Darlastic.csproj `
  ADP.EndpointParity/ADP.EndpointParity.Surveys/ADP.EndpointParity.Surveys.csproj `
  ADP.EndpointParity/ADP.EndpointParity.ClaimableItems/ADP.EndpointParity.ClaimableItems.csproj `
  ADP.EndpointParity/ADP.EndpointParity.WarrantyClaims/ADP.EndpointParity.WarrantyClaims.csproj
```

Then delete the trees:

```bash
git rm -r ADP.EndpointParity
git rm tools/parity.ps1 tools/parity.psd1
```

- [ ] **`dotnet sln remove` does not clean up solution folders.** If Step 00 nested the six projects
      under a solution folder, `ADP.sln` is left holding an orphan
      `Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}")` entry and possibly stale `NestedProjects`
      lines. Grep `ADP.sln` for `EndpointParity` afterwards and hand-remove whatever survives.
- [ ] **Remove the `.gitignore` entry Step 00 added** for `ADP.EndpointParity/reports/`
      (`verification.md` §3 — `reports/` is gitignored, `baselines/` is not). One line; it is the
      easiest thing in the whole step to leave behind, and a gitignore rule for a path that no longer
      exists is a small permanent puzzle for the next reader.
- [ ] `tools/` may hold nothing else. If `parity.ps1` and `parity.psd1` were its only contents,
      delete the empty directory too — git will not track it, but a stray empty folder on disk
      confuses `ls`.
- [ ] **Do not delete anything else under `tools/`.** Only the two `parity.*` files are Step 00's.

### C. Delete the captured baselines and seeds

They live inside `ADP.EndpointParity/` and go with item B's `git rm -r`. They get their own item
because they are the part someone will hesitate over, and the hesitation should be resolved in writing
rather than in the moment:

| Path | What it is | Fate |
|---|---|---|
| `ADP.EndpointParity/baselines/` | the committed goldens — per group, per grant, plus the route-catalogue goldens (`verification.md` §3, §5) | deleted |
| `ADP.EndpointParity/Seed/<group>.seed.json` | the adversarially authored seeds, one hostile row per known trap, per group (Step 00 item D) | deleted |
| `ADP.EndpointParity/Seed/<group>.<Entity>.create.json` | the hand-authored minimal-valid create bodies (`verification.md` §5) | deleted |
| `ADP.EndpointParity/reports/` | gitignored verify-mode diff output; untracked, so `git rm` will not touch it — delete it from disk | deleted |
| `ADP.EndpointParity/baselines/surveys-run1` (or similar) | the stability-gate copy from Step 00 item E's verification block, if it was never cleaned up | deleted |

**A baseline is a recording of a tree that no longer exists.** Every golden in `baselines/` was
captured against the pre-upgrade code; Steps 01–05 then changed that code deliberately and accepted
the resulting diffs with recorded reasons. Keeping the goldens after the upgrade preserves nothing
useful — they cannot be replayed without the harness, they no longer describe current behaviour, and
they would be re-captured from scratch by any future harness anyway. Delete them with the rest.

- [ ] Do **not** prune `baselines/` gradually across earlier steps to shrink this deletion.
      `verification.md` §9 already forbids it: a baseline set that shrinks during the migration is a
      loss of coverage wearing the costume of tidiness. It all goes here, at once, or not at all.

### D. Revert the two edits Step 00 made to real projects

**This is the item that justifies the step existing.** Step 00's exit criteria permitted exactly two
modifications outside `ADP.EndpointParity/`, `tools/`, `ADP.sln` and `.gitignore`, and required both
to be **listed in the commit message** (`00-baseline-and-harness.md`, Projects touched + exit
criteria). Find them from that commit, not from memory:

```bash
# the Step 00 commits, and every non-harness file they touched
git log --oneline parity-harness-final -- ADP.EndpointParity tools/parity.ps1
git show --stat <step-00-commit>
```

Two useful anchors, both verified on the pre-Step-00 tree:

- **`git grep -n "partial class Program" -- '*.cs'` returns nothing today.** Every match after the
  migration is a Step 00 edit.
- **The sample seeding blocks are unconditional today.** In
  `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs` the block runs at **163-196** —
  `EnsureCreatedAsync` :167, `SeedDBAsync` :172, `SetFullAccessAsync` :190, `SeedSampleSurveysAsync`
  :196, with no condition around any of them. In `ADP.Menus/samples/ADP.Menus.Sample.API/Program.cs`
  it is narrower — `EnsureCreatedAsync` :244, `SeedDBAsync` :326, `SetFullAccessAsync` :344; the
  demo-row seeding there was **already removed deliberately** before this plan started, with the
  reason written out at :246-258. So the Menus parity branch wraps the identity seed and
  `EnsureCreatedAsync` only, and there is less to look for than in Surveys.

Those line numbers are the **pre-edit** state, i.e. the target of the revert. They will have drifted
by the time you read them; locate the code by the Step 00 diff and confirm against these anchors.

#### D1 — `public partial class Program` (SPIKE-2): **recommended LEAVE, decision required**

Step 00 item B.1 added `public partial class Program` to a sample's `Program.cs` because
`WebApplicationFactory<Program>` will not compile against a top-level-statements host without it.

- It is **behaviour-free**. It changes the generated `Program` class's accessibility and nothing else:
  no code runs differently, no route changes, no DI registration moves.
- It is the **conventional** shape for an ASP.NET Core sample that anyone might later want to test,
  and it is what the next person writing an integration test against that sample would have to add
  back.

**Recommendation: leave it.** But *record the decision either way* in `STATUS.md`'s Step 08 row —
"left in place, inert, conventional" or "reverted, sample restored to top-level statements". An
undocumented leftover in a `Program.cs` is a small mystery; a documented one is a decision.

- [ ] Decision recorded in `STATUS.md`, naming every file that carries the declaration.
- [ ] If left: it is the **only** Step 00 edit that survives this step. Say so explicitly in the
      commit message, so a later `git log` on that file explains itself.

#### D2 — the parity seeding branch: **MANDATORY revert**

Step 00 item B.3 added a config flag or a `Parity` environment branch that **skips the sample's own
seeding block**. This one is not optional and is not a judgement call.

Left in place it silently changes how the sample host seeds itself — a sample whose entire purpose is
to boot with realistic data now has a code path that boots it with none, keyed on an environment name
that no longer means anything to anybody. The next person to hit an empty sample database will have
no reason to suspect a migration that ended months ago. Worse, the branch is invisible in the ordinary
case: the sample behaves normally until something sets the flag, so this is a defect that waits.

- [ ] `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs` — the branch removed, the block at
      163-196 unconditional again: `EnsureCreatedAsync`, `SeedDBAsync(...)`, `SetFullAccessAsync(...)`
      and `SeedSampleSurveysAsync()` all run on every startup, as they do today.
- [ ] `ADP.Menus/samples/ADP.Menus.Sample.API/Program.cs` — same, for its narrower block
      (`EnsureCreatedAsync` :244, `SeedDBAsync` :326, `SetFullAccessAsync` :344). **Leave the
      deliberate no-seeding comment at :246-258 exactly as it is** — that is pre-existing design, not
      a parity artefact, and it must not be swept up in the revert.
- [ ] Any **other** sample the branch reached, per the Step 00 commit message.
- [ ] **Also revert the explicit-id insertion path** if Step 00 implemented it inside a real project.
      Step 00 item B.3 required deciding between `IDENTITY_INSERT` and `ValueGeneratedNever()` for the
      parity seed's explicit long PKs. If that landed in the harness, it goes with item B. **If it
      landed in a sample's `Program.cs` or in a group's `Data` project — a `ValueGeneratedNever()` on
      a real entity configuration — it is a live model change and must come out.** Check; do not
      assume.
- [ ] **Boot each reverted sample once** and confirm it seeds. This is the only runtime check in the
      step and it is cheap: `dotnet run` the sample, watch the identity seed complete, stop it. A
      revert that compiles but leaves the branch half-removed is exactly the failure this item exists
      to prevent.

### E. Remove any temporary CI wiring

Step 07 item G forbids a standing parity job and requires any temporary one to be listed here.

- [ ] Grep every pipeline file for the harness:
      `grep -rn "EndpointParity\|PARITY_MODE\|parity" azure-pipeline.yml ADP.WebComponents/adp-web-components/azure-pipelines.yml .github/workflows/`
- [ ] Remove whatever it finds. If it finds nothing, that is the expected result — say so in the
      commit message rather than leaving the check unrecorded.

### F. Residue sweep

Small, cheap, and each one is a thing that outlives a careless deletion:

- [ ] **The Step 01 worktree.** `git worktree list` must not show `../ADP-pre-menus`. Step 01's own
      exit criteria removes it (`git worktree remove ..\ADP-pre-menus`); this is the backstop.
- [ ] **The Step 00 probe branch.** `git branch --list "scratch/*"` — `scratch/models-probe` is
      deleted as part of Step 00 item H, but confirm.
- [ ] **Leftover parity databases.** Each run creates and drops its own `ADP_Parity_<Group>_<runid>`
      (`verification.md` §7), but a crashed or interrupted run leaves one behind. List the local
      instance's databases and drop any `ADP_Parity_*` that survive. They are disposable by design;
      nothing references them.
- [ ] **Step 07's own untracked residue**, if it is still on disk: `./localfeed`, any
      `./tmp-packages`, and the relocated group copy made outside the repo for item E's package-mode
      check. Not harness, but this is the step titled "cleanup" and they are the only other thing this
      migration leaves lying around.

---

## Verification

Run all four. The first three are the step; the fourth is what proves nothing was missed.

```bash
# 1. the solution builds green, at the pre-harness project count
dotnet build ADP.sln --no-incremental

# 2. the solution's project count is back to its pre-Step-00 value
grep -oE '[^"\\]*\.csproj' ADP.sln | sort -u | wc -l          # expect 53

# 3. the tree's project count agrees — EXCLUDING obj/ and bin/
find . -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*' \
                        -not -path '*/node_modules/*' | wc -l  # expect 53

# 4. no dangling reference to the harness survives outside this planning directory
git grep -nE "EndpointParity|parity\.ps1|parity\.psd1" -- ':!docs/planning/**'   # expect: nothing
```

**On the project count, and why check 3 carries those exclusions.** An unfiltered
`find . -name '*.csproj'` returns **56** today against a true count of **53**. The three extras are
`.csproj`-shaped build artefacts under `obj/`: two `WorkerExtensions.csproj` beneath the **deleted**
`ADP.LookupServices/Lookup.Services.Functions/obj/Debug/{net8.0,net9.0}/`, whose csproj was removed at
`67aa8a3e` and which therefore cannot build at all, and one beneath
`ADP.Menus/samples/ADP.Menus.Sample.Functions/obj/Debug/net10.0/`. **Every project inventory in this
plan excludes `**/obj/**` and `**/bin/**`** (`README.md` §7, `06-shared-floor.md`, `STATUS.md`
§Corrections) — and this step is the one most likely to be tripped by it, because it is checking a
count *downward* and three phantom projects would read as "the deletion did not work".

The moving figure, in full: **53 pre-harness → 59 at Step 07's release-time build → 53 here.** Step 00
item G recorded both 53 and 59 for exactly this moment.

**Two caveats on the `git grep`:**

- It reads tracked files only. `ADP.EndpointParity/reports/` is gitignored, so grep will not see it
  and will not tell you it is still on disk. Check the filesystem separately: `ls ADP.EndpointParity`
  must fail.
- `':!docs/planning/**'` is not a convenience — the planning directory is **supposed** to keep every
  reference. It is the record of what was built and why. Excluding it is the correct scope, not a
  weakened check.

---

## Exit criteria

- [ ] `parity-harness-final` tagged on the last commit containing the harness, and pushed.
- [ ] `STATUS.md` carries Step 07 item C's parity results — per group, both grants — recorded
      **before** the deletion commit.
- [ ] `ADP.EndpointParity/` does not exist: not in git, not on disk.
- [ ] `tools/parity.ps1` and `tools/parity.psd1` deleted; nothing else under `tools/` touched.
- [ ] `ADP.sln` holds no `EndpointParity` entry, including no orphan solution-folder or
      `NestedProjects` line.
- [ ] The `ADP.EndpointParity/reports/` entry is gone from `.gitignore`.
- [ ] **The parity seeding branch is reverted in every sample it reached** (D2), and each reverted
      sample has been booted once and observed to seed. `ADP.Surveys.Sample.API`'s block at 163-196
      runs unconditionally again; `ADP.Menus.Sample.API`'s equivalent likewise, with the pre-existing
      no-seeding comment at :246-258 untouched.
- [ ] Any explicit-id insertion path (`IDENTITY_INSERT` / `ValueGeneratedNever()`) that landed in a
      real project rather than in the harness has been removed.
- [ ] **`public partial class Program`: an explicit decision is recorded in `STATUS.md`** — leave
      (recommended) or revert — naming every file that carries it.
- [ ] No pipeline file references the harness. If Step 07 item G wired a temporary job, it is gone.
- [ ] `dotnet build ADP.sln --no-incremental` — **0 errors**, and the project count reads **53/53**.
- [ ] Both project-count checks return **53**, with `**/obj/**` and `**/bin/**` excluded.
- [ ] `git grep -nE "EndpointParity|parity\.ps1|parity\.psd1" -- ':!docs/planning/**'` returns
      **nothing**.
- [ ] `git worktree list` shows no `ADP-pre-menus`; no `scratch/*` branch survives; no
      `ADP_Parity_*` database survives.
- [ ] `STATUS.md` updated: Step 08 `CLOSED`, `Verified by` recording the green build and the two
      count checks, and the D1 decision written down. All nine steps at their terminal statuses.
- [ ] The deletion is **one commit** (or a small, clearly separated set) touching only the paths in
      §Projects touched — no product code, no package versions, no `$(ADPVersion)`.

---

## What survives on purpose

**The plan documents are the durable artefact. The harness was the disposable one.**

Everything in `docs/planning/shift-framework-upgrade/` stays, unedited by this step:

| File | Why it survives |
|---|---|
| `STATUS.md` | **the record of what was done and what proved it** — the ledger, the recorded baselines, the resolved spikes, the per-group parity results from Step 07 item C. After this step it is the only evidence that endpoint parity was ever proven. |
| `verification.md` | the harness's design. If a future upgrade rebuilds it, this is the specification — normalization rules, the trap taxonomy, the coverage gates, the honest limits. |
| `conventions.md` | the migration recipe and the coding conventions, useful well beyond this upgrade |
| `README.md`, `00`–`08` | the plan as executed, including the reasoning behind the ordering |

They still reference `ADP.EndpointParity/` and `tools/parity.ps1` in the present tense, and **that is
correct** — they describe what was built and run at the time. Do not rewrite them into the past tense
and do not strip the paths out. The `git grep` in Verification excludes this directory for that
reason.

**State the consequence plainly, as `verification.md` §9 already does: after this step there is no
automated proof that endpoint behaviour has not changed.** The next framework upgrade either rebuilds
the harness from this directory's design or proceeds without one.

---

## Rollback

The harness is fully recoverable, and this step's own rollback is trivial because it touches no
product code and no package version.

**Recover the harness** (item A's tag is the anchor; the Step 07 release tag works identically):

```bash
git checkout parity-harness-final -- ADP.EndpointParity tools/parity.ps1 tools/parity.psd1
dotnet sln ADP.sln add ADP.EndpointParity/ADP.EndpointParity.Harness/ADP.EndpointParity.Harness.csproj \
                       ADP.EndpointParity/ADP.EndpointParity.Menus/ADP.EndpointParity.Menus.csproj \
                       ADP.EndpointParity/ADP.EndpointParity.Darlastic/ADP.EndpointParity.Darlastic.csproj \
                       ADP.EndpointParity/ADP.EndpointParity.Surveys/ADP.EndpointParity.Surveys.csproj \
                       ADP.EndpointParity/ADP.EndpointParity.ClaimableItems/ADP.EndpointParity.ClaimableItems.csproj \
                       ADP.EndpointParity/ADP.EndpointParity.WarrantyClaims/ADP.EndpointParity.WarrantyClaims.csproj
```

If the tag is missing, the release tag from Step 07 predates this step and carries the same tree:
`git tag --list "release-nuget-*" | tail -1`, then the same `git checkout <tag> -- …`.

**Undo this step wholesale:** `git revert` the deletion commit. Because the step is a single commit
over a closed path list, the revert is total and carries no risk to product code.

**A recovered harness is not a working harness.** Its baselines were captured against the pre-upgrade
tree and will diff against everything on the post-upgrade tree; a recovery is a recovery of the
*instrument*, and any future use starts by re-capturing baselines on whatever tree is current then.
Say so to anyone who reaches for the tag.

This step is **not** part of Step 07's release rollback surface. It lands after the release, touches
no packaged code, and reverts independently (`07-release-readiness.md` §Rollback).

---

## Effort & risk

**Effort:** the smallest step in the plan. A deletion, a solution edit, two file reverts and four
checks — well under a day, most of it item D and the boot check that goes with it.

**Recorded trade-off:** removing the harness means the next Shift framework upgrade rebuilds it from
scratch, using `verification.md` as its specification. That cost was accepted when the decision was
made and is noted here only so the next reader knows it was priced in, not overlooked.

**Risks:**

| Risk | Mitigation |
|---|---|
| **The parity seeding branch is left in a sample**, silently changing how it seeds and surfacing months later as an unexplained empty database | Item D2 is mandatory, names the files and the pre-edit line numbers, and requires each reverted sample to be **booted once** — not merely compiled |
| **An explicit-id path (`ValueGeneratedNever()` / `IDENTITY_INSERT`) landed in a real project** rather than the harness, and survives as a live model change | Item D2's last checklist entry makes it a thing to *check*, not to assume; Step 00 item B.3 required the choice to be documented in `STATUS.md`, so there is a record to check against |
| **The project-count check reads red because of phantom `obj/` projects** and the deletion is wrongly believed incomplete | Verification excludes `**/obj/**` and `**/bin/**`, with the three known artefacts and the 56-vs-53 discrepancy named outright |
| **The evidence dies with the instrument** — a green parity sweep that was never written down | Recording Step 07 item C's results is an exit criterion *there* and a precondition *here*; item A stops the step if the record is missing |
| **The harness is deleted before the release is out**, taking the sweep the release notes rest on with it | The ordering is one-way and stated in the preconditions and in Step 07's hand-off |
| A stale `.gitignore` rule or an orphan solution folder survives the deletion | Both are explicit checklist entries in item B; the `git grep` catches the solution entry, and the gitignore line is called out separately because grep for `EndpointParity` **does** find it and it is easy to skim past |
| **The plan documents get "cleaned up" too**, on the theory that they reference deleted paths | §What survives on purpose: they are the durable artefact, they are correct in the present tense, and the verification grep excludes them by design |

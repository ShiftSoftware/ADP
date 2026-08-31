# Step 07 — Release readiness

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** prove the solution is green as a whole, prove the **package-consumption path** works (which
local development never exercises), bump `$(ADPVersion)` once, and release once.

Cleanup is **not** here. Removing the parity harness is Step 08, after the release is out.

---

## Projects touched

| Path | Change |
|---|---|
| `GlobalSettings.props` | `$(ADPVersion)` `1.15.4` → next |
| — | no source changes expected |

Everything in the solution is *validated* here; nothing is migrated.

---

## Preconditions

- **Every earlier step at its own terminal status**, as named in `STATUS.md`'s `Terminal status`
  column: Steps 00, 02 and 06 at `CLOSED`; Steps 01, 03, 04 and 05 at `VERIFIED`. **For the four
  groups that have endpoints — Menus, Surveys, ClaimableItems, WarrantyClaims — `DONE` is not
  enough.** A group whose endpoint parity was never proven must not ride out in a release. (Steps 02
  and 06 cannot reach `VERIFIED` by construction: 02 `ADP.Darlastic` has no mapping behaviour, 06
  the shared floor is libraries only. An earlier draft demanded `VERIFIED` from all five and so had
  an unsatisfiable precondition.)
- **`CLOSED` does not mean "ended red."** That meaning left the plan with the deleted atomic-bump
  step. Every step now ends with a green build, each group having bumped its own package references
  as its first commit; `CLOSED` means only *finished, with no endpoint surface whose parity could be
  proven*. So this step inherits a tree that has been green continuously — there is no outstanding
  bump to absorb here, and anything red is a regression owned by the last step to touch it.
- **Step 06 `CLOSED` matters more than it looks.** Under this ordering the shared floor is the
  *last* thing bumped, which makes it the easiest thing to leave out of the release. Item E is where
  that is caught; see §Why this step exists, point 3.
- Every spike either `RESOLVED` or explicitly accepted with a recorded decision. SPIKE-12 is
  `RESOLVED` — the staged per-group bump this plan uses *is* its answer.

---

## Why this step exists

Three failure modes are invisible until now:

1. **The package-consumption path — the one downstream hosts actually use — is never exercised by
   `dotnet build` in this repo.** True, and the reason for this step. But note the mechanism, because
   an earlier draft named the wrong one and the wrong one produces a **false pass**:
   `ImportADPPackagesViaProjectReference` is declared in `GlobalSettings.props:4` and **read by
   nothing** — no csproj, props or targets file references it. The real switch is
   `Condition="Exists('..\..\ADP.Models\Models\Models.csproj')"` (and its negation), repeated in
   **18 reference pairs across 14 csproj**. The sibling csproj always exists in a checkout —
   including in a scratch clone — so local dev is *always* `ProjectReference` and `$(ADPVersion)` is
   a pack-output value only. **Setting the property to `false` changes nothing and exercises nothing.** Item E
   forces package mode by making `Exists()` false instead.
2. **Generated mappers carry an ABI to the framework.** A generated mapper is code frozen at its own
   build day; `ShiftEntityMapperRegistry.VerifyBindings()` JIT-prepares each one at startup and fails
   if a member it was compiled against is gone. **Every `ADP.*.Data` package that emits mappers must
   be rebuilt and republished whenever ShiftEntity moves.** You can no longer leave a group's package
   at an older `ADPVersion` and let hosts float the framework forward.
3. **A mixed *published* set — the publish-time hazard the shared-last ordering leaves standing.**
   Inside this
   repo a mixed floor is harmless and that is deliberate: the Shift nuspecs declare **minimum-version**
   dependencies (`version="2026.7.31.1"`), not exact pins, so an upgraded group sitting on a
   not-yet-upgraded shared project builds green — which is exactly what lets Steps 02–05 run before
   Step 06. **The same property is a trap at publish time**, because nothing in the package graph
   forces the floor forward and the build cannot tell you it was forgotten. If `ADP.Models` were
   published still compiled against `ShiftEntity.Model 2026.7.31.1` while the group packages above it
   declare `2026.8.30.1`, a downstream host unifies ShiftEntity by **max-wins** — so the floor
   assembly it loads is one compiled against a framework that is no longer the one running. What the
   registry expects from that assembly is then not there, `ShiftEntityMapperValidation` throws at
   startup out of `RegisterShiftRepositories`, and the host does not boot. **Step 06 bumping the
   floor, and item D releasing it inside the same single bump, is what prevents this — and item E is
   the only place in the plan where it is actually proven.** In-repo the floor is consumed by
   `ProjectReference`, so no `dotnet build` in this repository can see the defect.

---

## Work items

### A. Full-solution build against the recorded baseline

```bash
dotnet build ADP.sln --no-incremental 2>&1 | tee /tmp/step07-build.log
```

Compare against the Step 00 baseline:

| Measure | Baseline | Expected now |
|---|---|---|
| errors | 0 | **0** |
| projects | the **post-harness** count recorded in `STATUS.md` at Step 00 — `ADP.sln` holds 53 `.csproj` today and Step 00 adds six, so expect **59/59** unless the harness layout changed. This is the *release-time* figure; Step 08 takes it back to 53 | same |
| compiler warnings | 535 | **≤ 535.** Should fall — the 10 `SHENGEN004` are resolved. Any *increase* must be explained. |
| `SHENGEN004` | 10 (ClaimableItems 5, Surveys 3, WarrantyClaims 2) | **0**, or only justified-and-documented ones |
| `SHENGEN007` / `008` / `010` | 0 | **0** |
| `NU1605` / `NU1701` / `NU1603` / `MSB3277` | 0 | **0** |
| `NU1903` (AutoMapper CVE) | 42 lines / 21 projects | **sharply reduced** — AutoMapper now reaches only via `ADP.SyncAgent`'s direct reference. **It will not be zero, and should not be.** |
| `NU1504` | 1 (`ADP.Surveys.Sample.API`, duplicate `EFCore.Design`) | 0 if fixed opportunistically, else still 1 |

- [ ] Record the new numbers in `STATUS.md`.
- [ ] Any warning code that is *new* since baseline gets explained, not waved through.

### B. Full test sweep against the recorded baseline

```bash
dotnet test ADP.Cases/ADP.Cases.Shared.Tests          # 43 / 43
dotnet test ADP.Surveys/ADP.Surveys.Shared.Tests      # 182 / 182
dotnet test ADP.Darlastic/ADP.Darlastic.Shared.Tests  # 5 / 5
dotnet test ADP.Darlastic/ADP.Darlastic.Engine.Tests  # 49 / 49
dotnet test ADP.Hawta/Hawta.Tests                     # 493 / 502 (9 skipped, blob emulator down)
dotnet test ADP.LookupServices/Lookup.Services.Tests  # 47 / 47
dotnet test ADP.LookupServices.BDD                    # 452 / 452
dotnet test ADP.Menus/ADP.Menus.Tests                 # 262 / 264 (2 known failures)
dotnet test ADP.Models/Models.Tests                   # see SPIKE-6
```

```bash
# the generated trees, after the full build above — see README.md §7
git diff --exit-code ADP.WebComponents/adp-web-components/src/global/types/generated \
                     ADP.Docs/Docs/docs/generated ADP.TestData/environments
```

Plus the web components: `npm test` in `ADP.WebComponents/adp-web-components/` — baseline 114 passed,
4 suites. Untouched by this upgrade, but it is part of "green".

**And the generated trees must be byte-identical to the baseline.** Four `AfterTargets="Build"`
self-runners rewrite 247 tracked files on any `dotnet build ADP.sln`
(`WebComponentModelGenerator` → the 44 TypeScript types, from `Lookup.Services`;
`ADP.Docs/ModelDocGen` → the docs tree, from `ADP.Models`; `ADP.Docs/FeatureDocGen`;
`ADP.TestData/Generator`). A clean `git diff --exit-code` over those three trees is the **cheapest
available proof that `ADP.Models`' and `Lookup.Services`' public shape survived the framework
bump** — and a dirty one is a wire-contract change for the web components, which no other check in
this plan would see.

- [ ] **The only acceptable red is the same 2 `SampleDataSeedingTests` failures** with the same
      duplicate-key message on a unique name index. Everything else must be green.
- [ ] Remember the environment sensitivities: `ADP.Menus.Tests` pass count moves ±1 with the Cosmos
      emulator, fail count ±2 with local SQL state; `ADP.Hawta.Tests` skips 9 when no blob emulator
      is listening. **Verify on the same machine state as the baseline, or filter and compare.**

### C. Full parity sweep

```powershell
.\tools\parity.ps1 verify -Group Menus
.\tools\parity.ps1 verify -Group Surveys
.\tools\parity.ps1 verify -Group ClaimableItems
.\tools\parity.ps1 verify -Group WarrantyClaims
.\tools\parity.ps1 verify -Group Darlastic          # framework-only control, not value parity
# and the restricted-grant passes
.\tools\parity.ps1 verify -Group Menus          -Grant Restricted
.\tools\parity.ps1 verify -Group Surveys        -Grant Restricted
.\tools\parity.ps1 verify -Group ClaimableItems -Grant Restricted
.\tools\parity.ps1 verify -Group WarrantyClaims -Grant Restricted
```

- [ ] All groups re-verified **together**, on the final tree. A group verified at its own step and
      never re-run can have been broken by a later step.

### D. Bump `$(ADPVersion)` — once, and **before** the package-mode check

- [ ] `GlobalSettings.props:5` — `1.15.4` → the next version.
- [ ] **One bump, one release.** Every ADP package releases from a single pipeline tag, and every
      `.Data` package emitting generated mappers must be rebuilt (§Why this step exists, point 2).
      **Do not release a subset.**

**This used to be item E, after the package check. It has been moved ahead of it**, because packing
at the old `1.15.4` collides with the already-published and already-cached `1.15.4`: roughly 25
`shiftsoftware.adp.*` packages sit in `~/.nuget/packages`, the global cache is consulted before any
feed, and the restore would quietly resolve the **cached old** 1.15.4 — compiled against
`2026.7.31.1`. The boot check would then fail for a tooling reason, and item E's old "expect failures
on the first attempt" note would read that failure as *confirmation of the diagnosis*. The step was
set up to mis-read its own result.

If for any reason the version must stay at `1.15.4` while the check runs, pack at a throwaway
prerelease version (`-p:Version=$(ADPVersion)-parity1`) **and** restore into a private package
directory (`--packages ./tmp-packages`) so the global cache cannot answer.

### E. Package-mode restore smoke check — **the point of this step**

Local development never exercises this path — and **the property does not switch it**
(§Why this step exists, point 1). `ImportADPPackagesViaProjectReference` is read by nothing; the real
switch is `Condition="Exists(...)"` on 18 reference pairs across 14 csproj, and the sibling csproj
exists in every checkout, scratch clones included. Force package mode by making `Exists()` **false**:

- [ ] `dotnet pack` every ADP project to `./localfeed`, at the version set in item D.
- [ ] Copy **one group's folder alone** — e.g. `ADP.ClaimableItems/`, which has conditional pairs on
      both `ADP.Models` and `Lookup.Services` — into an empty directory *outside* the repo. The
      relative paths `..\..\ADP.Models\Models\Models.csproj` and
      `..\..\ADP.LookupServices\Lookup.Services\Lookup.Services.csproj` no longer resolve, so
      every conditional pair flips to its `PackageReference` branch.
- [ ] Add `./localfeed` as the only extra source, then `dotnet restore` and `dotnet build` there.
      **This, and not a property edit, is the package-consumption path.**
- [ ] **Check the packed nuspecs, not just the build** — this is the mixed-published-package check
      (§Why this step exists, point 3), and it is the check the whole shared-last ordering rests on.
      A `.nupkg` is a zip: read the `.nuspec` out of each one and confirm **every**
      `ShiftSoftware.Shift*` dependency across the packed set reads `2026.8.30.1`, the floor packages
      (`ShiftSoftware.ADP.Models`, `ShiftSoftware.ADP.Lookup.Services`) included.

      ```bash
      for f in localfeed/*.nupkg; do echo "== $f"; unzip -p "$f" '*.nuspec' | grep -i 'ShiftSoftware.Shift'; done
      ```

      One `2026.7.31.1` surviving here is the defect escaping the repo, and **no `dotnet build` in
      this repository can see it** — in-repo the floor arrives by `ProjectReference`.
- [ ] **Boot-check at least one host** with the locally-packed set, and confirm
      `ShiftEntityMapperValidation` passes at startup. It runs unconditionally from
      `RegisterShiftRepositories`, so a missing mapper is a hard startup failure listing every
      uncovered triple — this is the single best end-to-end proof that the migration is complete.
      Pick a host graph that pulls **a group package *and* the floor** (`ADP.ClaimableItems` does),
      so the floor-vs-group unification in point 3 is actually exercised rather than assumed.
- [ ] **Failures here are real.** With item D done first, every ADP package in the feed is the new
      one, so a restore or boot failure is a genuine defect — not the expected mixed-version artefact
      the old wording invited you to wave through.

*Optional, one-time, and out of the upgrade commit:* if you want the property to be real, rewrite the
18 condition pairs to
`Condition="'$(ImportADPPackagesViaProjectReference)' != 'true' Or !Exists(...)"`. Then a property
flip does what this step's earlier draft claimed. Do it in its own commit, or not at all.

### F. Final sweeps

- [ ] `grep -rn "AutoMapper" --include=*.cs --include=*.csproj . | grep -v ADP.SyncAgent` — should
      return **nothing** outside `ADP.SyncAgent`, which keeps its own deliberate reference.
- [ ] `grep -rn "ShiftSoftware\.Shift.*2026\.7\.31\.1" --include=*.csproj .` — zero.
      **This is now the only aggregate check that all 29 package lines were bumped.** There is no
      atomic-bump commit whose diff could be reviewed as a set; the 29 are redistributed across the
      steps that own them — 02 `ADP.Darlastic` 4, 03 `ADP.Surveys` 7, 04 `ADP.ClaimableItems` 7,
      05 `ADP.WarrantyClaims` 7, 06 the shared floor 4. A line missed in any of those steps surfaces
      here and nowhere else, because a stale-but-lower minimum version still builds green.
- [ ] All 9 `ShiftSoftware.TypeAuth.*` references still pinned at `1.6.28`. **Re-check at release
      time** — this is the last chance to catch a tool having floated it to the 2.5-year-old
      `2024.2.22.2`.
- [ ] `ADP.Docs/Docs/docs/menus/integration.md:10,84,86` and `ADP.Menus/README.md:8` still describe
      AutoMapper profiles — leftovers from the Menus migration (`conventions.md` §7). Fix them here
      if no earlier step did.
- [ ] Leave `ADP.Docs/Docs/docs/integrations/sync-agent/getting-started.md` alone — it documents
      `ADP.SyncAgent`'s own independent AutoMapper use.

### G. CI

**The parity harness does not become standing CI.** Step 08 deletes it, by recorded decision — a job
wired here would be removed one step later. This is a change from an earlier draft, which added a
permanent parity job; the note stays so a later reader does not "fix" the omission.

- [ ] **Do not add a standing parity job.** If you wire one anyway to get item C's sweep onto a build
      agent for the duration of the release, treat it as temporary and **add its removal to Step 08's
      checklist**. The shape, kept here for the record and gated on a SQL service being available:

```yaml
- script: dotnet test ADP.EndpointParity/ADP.EndpointParity.Menus --logger trx
  env: { PARITY_MODE: verify }
  displayName: Endpoint parity — Menus
```

one step per group project (or a single step over a solution filter containing the five), because
group selection is the project, not a `--filter` (`verification.md` §2).

- [ ] Confirm the NuGet pipeline still runs BDD tests and packs correctly on a `release-nuget-*` tag.
- [ ] Confirm no pipeline file references `ADP.EndpointParity/` or `tools/parity.ps1`, so Step 08's
      deletion cannot break a build. If one does (see the bullet above), it is Step 08's to remove.

---

## Verification

Everything above **is** the verification. The step has no separate check.

**Caveats that survive into the release** — carry them into the release notes rather than letting a
green run imply more than it proves:

- `ADP.ClaimableItems` and `ADP.WarrantyClaims` were verified through a **mounted host**, not a real
  deployment: consumer middleware order, localization, CORS, fallback routing and JSON overrides are
  unverified for those two groups (unless a real sample host was written — that fallback is open to
  **both** groups, at Step 04 or Step 05; see `verification.md` §6).
- The **six Cosmos replication delegates have no automated coverage at all.**
- **Binary export endpoints are `PARTIAL`.**
- **Darlastic's result is smoke, not parity.**
- **`ADP.Cases` has no endpoints** and is not covered by endpoint parity in any sense.
- **`ADP.Models` has no executing tests** unless SPIKE-6 was resolved by fixing it.

---

## Exit criteria

- [ ] `dotnet build ADP.sln --no-incremental` — **0 errors**, project count matching the
      post-harness figure recorded in `STATUS.md` at Step 00 (53 today + Step 00's six = **59**
      unless the harness layout changed), warning count ≤ baseline with any increase explained.
- [ ] Zero `SHENGEN007` / `008` / `010`; `SHENGEN004` at zero or fully justified.
- [ ] Zero `NU1605` / `NU1701` / `NU1603` / `MSB3277`.
- [ ] `NU1903` reduced to `ADP.SyncAgent`'s reference only.
- [ ] Full test sweep at baseline; **the only red is the 2 known `SampleDataSeedingTests` failures.**
- [ ] Web component tests: 114 passed.
- [ ] All five parity groups re-verified on the final tree, **plus** the restricted-grant pass for
      every group that has one.
- [ ] `$(ADPVersion)` bumped exactly once — **and before** the package-mode check ran.
- [ ] Package-mode restore exercised **by relocating a group outside the repo so `Exists()` is
      false**, not by editing `ImportADPPackagesViaProjectReference` (which does nothing). At least
      one host booted against the locally-packed ADP packages with `ShiftEntityMapperValidation`
      passing at startup, on a graph carrying **both** a group package and the floor.
- [ ] **Every `ShiftSoftware.Shift*` dependency in every packed `.nuspec` reads `2026.8.30.1`** —
      the floor packages included. This is the mixed-published-package check and the one thing no
      in-repo build can prove.
- [ ] `git diff --exit-code` over the three generated trees is clean after the full build.
- [ ] No `AutoMapper` reference anywhere outside `ADP.SyncAgent`.
- [ ] No `ShiftSoftware.Shift*` reference left at `2026.7.31.1`.
- [ ] All 9 TypeAuth references still at `1.6.28`.
- [ ] **No standing parity job added to CI** — and if one was wired temporarily, it is listed in
      Step 08's removal checklist. No pipeline file otherwise references `ADP.EndpointParity/` or
      `tools/parity.ps1`.
- [ ] `STATUS.md` fully updated; **every earlier step at its documented terminal status** (00, 02,
      06 `CLOSED`; 01, 03, 04, 05 `VERIFIED`), and this step at `VERIFIED`; every spike `RESOLVED`
      or explicitly accepted; the `## Recorded baselines` section filled in.
- [ ] Release notes carry the surviving caveats listed above.
- [ ] The item C parity results are **recorded in `STATUS.md` before the release closes**, because
      Step 08 deletes the only thing that could reproduce them.
- [ ] Step 08 is unblocked: the release is out, and nothing outside `ADP.EndpointParity/`,
      `tools/parity.*` and the six `ADP.sln` entries depends on the harness.

---

## Hand-off to Step 08

This step ends the **upgrade**. It does not end the **cleanup**.

Step 08 removes the parity harness — `ADP.EndpointParity/` (the `Harness` library plus the five group
test projects), `tools/parity.ps1` / `tools/parity.psd1`, the committed baselines, the six `ADP.sln`
entries, and any temporary CI wiring from item G. That is a **recorded decision**, not an oversight:
the Shift framework has not moved often enough recently to justify maintaining a standing regression
harness. Step 00 was written knowing this — the harness is deliberately temporary.

Ordering is one-way and matters:

- **Step 08 runs after this step, never before.** Item C's sweep *is* the harness, and every caveat
  in the release notes rests on a harness run.
- **Evidence outlives the harness only if it is written down.** Record item C's results — per group,
  both grants — in `STATUS.md` before handing off. After Step 08 there is no way to re-run them.
- **The project count is a moving figure.** 53 pre-harness, **59** at this step's build check,
  back to 53 after Step 08. The 59 here is the release-time number, not the end state.

---

## Rollback

Before the release tag: revert the `$(ADPVersion)` bump; everything else is per-step rollback.

**After the release tag, per-group rollback is no longer available.** The packages release together
and hosts unify ShiftEntity by max-wins, so a partial revert reproduces exactly the mixed-package
state that bricks hosts (`README.md` §3). A post-release problem is fixed by **rolling the whole
release forward**, not by reverting one group.

State that in the release notes.

Step 08's harness removal is not part of this rollback surface: it is a separate commit touching only
`ADP.EndpointParity/`, `tools/parity.*` and the `ADP.sln` entries — no product code, no package —
so it reverts independently and after the release.

---

## Effort & risk

**Effort:** small in edits, moderate in verification time — the full sweep is the longest-running part
of the plan.

**Risks:**

| Risk | Mitigation |
|---|---|
| **A group is released on `DONE` rather than `VERIFIED`** | Preconditions make it explicit, per terminal status; the ledger distinguishes `DONE` / `VERIFIED` / `CLOSED` for exactly this moment |
| **The package-mode check passes without testing anything**, because the property it toggles is dead | Item E forces package mode by relocating a group so `Exists()` is false — the only mechanism that actually works |
| **The package check is run before the version bump** and a cached 1.15.4 is mistaken for evidence | D and E are ordered deliberately, with the reason written down |
| **The package path fails in a host after release**, because local dev never tested it | Item E is the whole reason this step exists; boot-check with `ShiftEntityMapperValidation` is the end-to-end proof |
| **The shared floor is bumped in the repo but left out of the release**, or published still compiled against `2026.7.31.1` — the residual hazard of bumping the floor last, and invisible to every in-repo build because minimum-version ranges keep the mixed state green | Item D's single `$(ADPVersion)` bump releases the whole set at once; item E reads the packed nuspecs and boots a host on a graph carrying both a group package and the floor (§Why this step exists, point 3) |
| **One of the 29 redistributed package lines was never bumped** — no atomic-bump diff exists to review as a set, and a stale-but-lower minimum still builds green | Item F's `2026.7.31.1` grep over all csproj is the aggregate check, with the per-step counts (4/7/7/7/4) written down beside it |
| A tool floated TypeAuth to the ancient `2024.2.22.2` at some point during the plan | Item F re-checks all 9 at release time |
| Test-count comparison is confounded by emulator/SQL state | Item B's sensitivities are stated with the filter workaround |
| A later step broke an earlier group | Item C re-verifies every group together on the final tree |
| Release notes overstate what was proven | The surviving-caveats list is an exit criterion |

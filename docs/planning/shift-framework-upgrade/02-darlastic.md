# Step 02 — `ADP.Darlastic`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `CLOSED` — 0 triples, 0 profiles; smoke, not value parity.

> **`CLOSED` does not mean "ends red".** In this plan `CLOSED` means only: *finished, with no mapper
> surface whose parity could be proven.* This step performs its own version bump as its first commit
> and **ends with a green build** — like every other step in this plan.

**Goal:** move this group to `2026.8.30.1` and confirm it survives the bump. There is **no mapper
migration here** — this group has zero AutoMapper profiles and zero `ShiftRepository` triples.

---

## Projects touched

| Path | Bumped line | Profiles | Triples |
|---|---|---|---|
| `ADP.Darlastic/ADP.Darlastic.API/ADP.Darlastic.API.csproj` | `ShiftEntity.Web` (46) | 0 | 0 |
| `ADP.Darlastic/ADP.Darlastic.Data/ADP.Darlastic.Data.csproj` | `ShiftEntity.EFCore` (53) | 0 | 0 |
| `ADP.Darlastic/ADP.Darlastic.Shared/ADP.Darlastic.Shared.csproj` | `ShiftEntity.Model` (33) | 0 | 0 |
| `ADP.Darlastic/ADP.Darlastic.Web/ADP.Darlastic.Web.csproj` | `ShiftBlazor` (33) | 0 | 0 |

Also exercised: `ADP.Darlastic/ADP.Darlastic.Engine`, `ADP.Darlastic.Engine.Core`,
**`ADP.Darlastic.CaseBrowser`**, `ADP.Darlastic.Shared.Tests`, `ADP.Darlastic.Engine.Tests`,
`ADP.Darlastic/samples/ADP.Darlastic.Sample.API`. That is the whole group — 10 csproj.

`ADP.Darlastic.CaseBrowser` was missing from an earlier draft. It `ProjectReference`s
`ADP.Darlastic.Engine`, which `ProjectReference`s `ADP.Models` (`Darlastic.Engine.csproj:39`,
conditional). Under the shared-last order `ADP.Models` is still on the `2026.7.31.1` floor while this
group moves to `2026.8.30.1` — that mixed arrangement builds green today (`ADP.Menus.Shared` at
8.30.1 over an `ADP.Models` at 7.31.1 is the live proof already in this repo), and it means **the
floor bump at Step 06 reaches CaseBrowser a second time.** Build it here, and build it again at
Step 06.

**No NU1605 is reachable from this group's bump.** `Darlastic.Engine.csproj:38-39` is the only
`ADP.Models` edge in Darlastic, and that project carries no direct `ShiftSoftware.Shift*` pin of its
own. The group's four pins all sit in projects that do not reference `ADP.Models`. The three projects
that *can* downgrade under a staged bump — `ADP.Cases.Shared:32`, `ADP.ClaimableItems.Shared:34`,
`ADP.WarrantyClaims.Shared:33` — are all elsewhere.

---

## Preconditions

- Step 00 `CLOSED` — the harness exists, its stability gate passed (two identical captures diffed to
  zero) and the baselines are captured.
- Step 01 `VERIFIED` — the harness has been graded against `ADP.Menus`, a migration whose answer was
  already known. Without that grading, a diff here cannot confidently be attributed to the framework.

**There is no dependency on the shared floor.** Under the shared-last order the floor (`ADP.Models`,
`ADP.Cases`, `Lookup.Services.DuckDB`) moves at Step 06, *after* this step. That is sound because the
Shift nuspecs declare **minimum-version** dependencies (`version="2026.7.31.1"`), not exact pins
(`[2026.7.31.1]`) — verified in the local NuGet cache — so nothing forces a lockstep bump, and an
upgraded group sitting on a not-yet-upgraded shared project is an arrangement this repo already
ships.

**Why this position in the order:** Darlastic sits **above** the Step 06 floor and nothing outside the
group depends on it, so its position among 02–05 is free. It goes **first among the group steps**
because it is the simplest (0 profiles, 0 triples) *and* because doing it first turns up any
**non-mapper** fallout of the version bump — the `ShiftEntity.EFCore` / `.Web` / ShiftBlazor API
surface — in the group where nothing else is happening, so the signal is unambiguous.

Going first makes it a **better** control, not a worse one. At this point in the plan not one mapper
has been rewritten anywhere in the tree, so nothing can be confused with mapper fallout: every
framework API break the bump can cause surfaces here, in isolation, *before* Steps 03–05 begin mixing
framework change with mapper rewrite. That second reason is the important one; see below.

---

## What this group is *for* — the plan's only framework-only control

An earlier draft treated this step as a cheap confirmation whose harness pass was optional. That
undersells it.

`ShiftEntityMapperValidation` throws at startup for any triple without a mapper, so **no mapper group
can ever be captured in a "bumped but not migrated" state.** Every Surveys, ClaimableItems and
WarrantyClaims diff therefore confounds two causes — the framework change and the mapper rewrite —
including the six compile-clean behaviour changes in `conventions.md` §10 (`AsNoTracking()` before
projection, `IsDeleted` restored on update, case-insensitive member matching, validation-error
wrapping, and the rest).

**Darlastic, at 0 triples and 0 profiles, is the only group in the repo where a harness diff is
unambiguously framework-caused.** It is the control against which the mapper groups' diffs get
attributed. That is why SPIKE-5 is high priority rather than a cost-benefit skip — and why, if it
resolves negative, `STATUS.md` must record plainly that framework-level response changes are then
**inseparable** from mapper changes everywhere else in the plan.

## What this group actually risks

Verified: Darlastic's controllers are plain `ControllerBase` (`CaseBrowserController`,
`GoldenCustomerController`, `StewardQueueController`, `CaseBrowserCompatController`, and others), it
declares no `ShiftRepository<...>`, and it has no `AutoMapperProfiles/` directory. Its exposure is
**the `ShiftEntity.EFCore` / `.Web` / `ShiftBlazor` API surface only — which the compiler catches.**

Two behaviour changes could still reach it silently:

- **`OdataList` now applies `.AsNoTracking()`** before projection. Darlastic has no `ShiftRepository`,
  so this should not apply — confirm by grep rather than assumption.
- **ShiftBlazor list grids gained a Find box and an automatic ID filter by default**, opt-out via new
  `DisableFind` / `DisableIdFilter` parameters. This changes what every `ADP.Darlastic.Web` list page
  renders. It needs a **visual pass**, not a code fix.

---

## Work items

### A. Bump this group's package references

Four lines, `2026.7.31.1` → `2026.8.30.1`. **This is the step's first commit.**

| csproj | line | package |
|---|---|---|
| `ADP.Darlastic/ADP.Darlastic.API/ADP.Darlastic.API.csproj` | 46 | `ShiftEntity.Web` |
| `ADP.Darlastic/ADP.Darlastic.Data/ADP.Darlastic.Data.csproj` | 53 | `ShiftEntity.EFCore` |
| `ADP.Darlastic/ADP.Darlastic.Shared/ADP.Darlastic.Shared.csproj` | 33 | `ShiftEntity.Model` |
| `ADP.Darlastic/ADP.Darlastic.Web/ADP.Darlastic.Web.csproj` | 33 | `ShiftBlazor` |

All four move together, so this group's ShiftEntity family is never split across two versions.

`ShiftSoftware.TypeAuth.*` stays at `1.6.28` — separate version line, no bump required (here:
`Darlastic.API.csproj:47`, `Darlastic.Shared.csproj:34`).

Commit those four lines alone, then run item B. If item B turns out to need source changes, they go
in a **second** commit, so the bump and the adaptation stay separable in `git log`.

### B. Build the group and confirm nothing broke

```bash
dotnet build ADP.Darlastic/ADP.Darlastic.Shared
dotnet build ADP.Darlastic/ADP.Darlastic.Data
dotnet build ADP.Darlastic/ADP.Darlastic.API
dotnet build ADP.Darlastic/ADP.Darlastic.Engine
dotnet build ADP.Darlastic/ADP.Darlastic.CaseBrowser
dotnet build ADP.Darlastic/ADP.Darlastic.Web
dotnet build ADP.Darlastic/samples/ADP.Darlastic.Sample.API
```

Expected: green with **no source change**. Any error here is a genuine framework API change, since
there is no mapper work to confuse it with.

Then prove the plan's headline invariant — the rest of the solution is still on the old floor and
must be unaffected by this group's bump:

```bash
dotnet build
```

Expected: green, with **no NU1605** (see "Projects touched" for why none is reachable from these four
lines). A downgrade warning here means a csproj edge exists that this plan has not recorded — stop
and add it to `STATUS.md` before continuing.

### C. Confirm the negative findings still hold

Cheap, and it prevents a later "we assumed Darlastic was clean":

```bash
grep -rn "AutoMapperProfiles\|: Profile\b\|ShiftRepository<\|IMapper" --include=*.cs ADP.Darlastic
grep -rn "AddAutoMapper" --include=*.cs ADP.Darlastic
grep -rn "Replicate<\|UpdateReference<" --include=*.cs ADP.Darlastic
```

All must return nothing. (Note: `SourceProfile` in `ADP.Darlastic.Data/Entities/RegistryEntities.cs`
is a domain entity, not an AutoMapper `Profile` — do not let the name trip the grep review.)

### D. Check for tagging-namespace fallout

`TagProjection` / `TaggableProjectionExtensions` moved namespace. A compat shim covers
extension-method call sites, but a direct type reference needs a `using` change.

```bash
grep -rn "TagProjection\|TaggableProjectionExtensions\|AddShiftTagging" --include=*.cs ADP.Darlastic
```

Expected: nothing. Run it across the whole repo once while you are here.

### E. Visual pass on the Blazor list pages

The new Find box and ID filter appear on every list grid by default. Walk the `ADP.Darlastic.Web`
list pages and decide, per page, whether to keep them or opt out via `DisableFind` /
`DisableIdFilter`.

**This is a product decision, not a defect** — record the decision rather than silently accepting the
new default. Also note a ShiftBlazor regression ("ShiftList crashing every page that renders one")
was introduced *and fixed* inside the 7.31.1 → 8.30.1 window, so do not land on an intermediate
ShiftBlazor version.

### F. Resolve SPIKE-5 — can the sample host boot headlessly?

Two known blockers in `ADP.Darlastic/samples/ADP.Darlastic.Sample.API`:

1. `Program.cs` `return 1`s before `app.Run()` when required config is missing, so
   `WebApplicationFactory` must inject config.
2. It needs a populated registry database that the repo does not seed.

**Treat this as high priority, not optional.** Per §"What this group is for", a Darlastic capture is
the plan's only framework-only control. Both blockers are tractable: `WebApplicationFactory` can
inject the missing config, and the registry DB can be seeded from the parity seed like any other
group.

- [ ] Capture a **route-catalogue smoke pass plus a full value capture** of whatever endpoints boot.
      The value capture is not "parity" — there is no mapping behaviour — but a framework-caused
      response change (serialization, ProblemDetails shape, OData envelope) shows here and only here.
- [ ] If it genuinely cannot boot, record SPIKE-5 resolved-negative **and record the consequence**:
      the mapper groups' diffs can no longer be attributed between framework and mapper causes.
      That sentence goes in `STATUS.md`, not just in someone's head.

*Housekeeping noted while surveying, not part of this upgrade:* the sample's development
`appsettings` hard-codes a database name carrying a region-style suffix. Given this repo's
client-agnostic rule, that is worth a separate look. Do not fix it inside an upgrade commit.

---

## Verification

```bash
dotnet build                                            # whole solution: green, no NU1605
dotnet build ADP.Darlastic/ADP.Darlastic.API
dotnet build ADP.Darlastic/ADP.Darlastic.Web

dotnet test ADP.Darlastic/ADP.Darlastic.Shared.Tests    # baseline 5 / 5
dotnet test ADP.Darlastic/ADP.Darlastic.Engine.Tests    # baseline 49 / 49

# only if SPIKE-5 resolved positively:
.\tools\parity.ps1 verify -Group Darlastic
```

**Group-specific caveats — read before recording a status.**

> **Darlastic's green is a smoke result, not a parity result.** There is no mapper here to regress.
> A passing harness run proves the routes still exist and still respond; it proves nothing about
> mapping behaviour, because there is no mapping behaviour.

Record it as such. `verification.md` §8.5 is the standing caveat. Do **not** let a green Darlastic
run create the impression that the upgrade's mapper risk has been tested.

---

## Exit criteria

- [ ] All **four** package lines in item A are at `2026.8.30.1`, committed on their own, with
      `TypeAuth` untouched at `1.6.28`.
- [ ] **The solution builds green** (`dotnet build`) with no NU1605, while the shared floor is still
      at `2026.7.31.1`.
- [ ] All **seven** projects in item B build clean with **zero source changes**, or each change is
      listed and justified as a framework API adaptation. `ADP.Darlastic.CaseBrowser` is one of them.
- [ ] Items C and D greps all return empty — no profiles, no triples, no `IMapper`, no
      `AddAutoMapper`, no replication call sites, no direct tagging-type references.
- [ ] `ADP.Darlastic.Shared.Tests` = 5/5 and `ADP.Darlastic.Engine.Tests` = 49/49.
- [ ] The Blazor list-page Find/ID-filter change has had a visual pass, and the keep-or-opt-out
      decision is recorded per page.
- [ ] SPIKE-5 is `RESOLVED` in `STATUS.md` — either with a capture taken, or with a recorded
      resolution **plus the recorded consequence** that framework and mapper causes are no longer
      separable in Steps 03–05.
- [ ] `STATUS.md` notes explicitly say **smoke, not value parity**, and the step is recorded
      `CLOSED`, not `VERIFIED` — `CLOSED` here meaning *nothing mapper-shaped to prove*, not
      *ends red*.

---

## Rollback

Revert the four package lines from item A, plus whatever items B or E changed in `ADP.Darlastic/`.
Nothing outside the group depends on Darlastic and no other step's bump is entangled with this one,
so the rollback is contained to `ADP.Darlastic/` and leaves the solution green.

---

## Effort & risk

**Effort:** small. A four-line bump plus confirmation. Item E's visual pass is the only slow part, and
item F is timeboxed by its own cost-benefit.

**Risks:**

| Risk | Mitigation |
|---|---|
| **A green run here is mistaken for evidence the upgrade is safe** | Stated twice above and made an exit criterion. This is the main risk of the step. |
| The ShiftBlazor grid default changes UI without anyone noticing | Item E's explicit visual pass and recorded decision |
| Sample host cannot boot, and the plan loses its only framework-only control | SPIKE-5 may resolve negative, but the *consequence* is recorded rather than shrugged off: every later diff then confounds framework and mapper causes |
| Framework API break hides among mapper errors | Precisely why this group goes before the three mapper groups |
| This group bumps while the shared floor is still at `2026.7.31.1` | Nuspec dependencies are minimum ranges, not exact pins; no Darlastic project mixes an `ADP.Models` edge with a direct Shift pin, so no NU1605 is reachable; `ADP.Menus` over `ADP.Models` is the live precedent in this repo. Step 00's throwaway compile probe of `Models.csproj:48` de-risks the floor itself ahead of Step 06. |

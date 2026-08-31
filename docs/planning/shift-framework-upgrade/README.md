# Shift Framework Upgrade — 2026.7.31.1 → 2026.8.30.1

Staged plan for moving every `ShiftSoftware.Shift*` package in this repo to `2026.8.30.1`,
migrating off AutoMapper onto the ShiftEntity source-generated mappers, and **proving** — not
assuming — that the HTTP surface behaves exactly as it did before.

> This repo is public, generic and multi-tenant. Nothing in this plan names a client, a client
> URL/hostname, a warehouse or branch code, or an action-tree namespace. Keep it that way when you
> fill in status and notes.

---

## 1. Why this needs a plan at all

`2026.8.30.1` **removed AutoMapper from the framework entirely** and replaced it with a Roslyn
source generator. The compatibility package `ShiftSoftware.ShiftEntity.EFCore.AutoMapper` was never
published, and the two intermediate releases (`2026.8.6.1`, `2026.8.24.1`) still depend on
AutoMapper 14.0.0. There is no stepping stone and no fallback:

**Taking the version bump *is* the mapper migration.**

Three things make that harder than a normal package bump:

1. **The version bump is taken solution-wide, in one commit.** See §3 — a staged bump is *possible*
   but leaves several assemblies compiled against a split ShiftEntity family, which nothing in this
   repo has verified.
2. **The dangerous regressions are silent.** They return HTTP 200 with a well-formed body and the
   right shape, and produce *zero* compiler diagnostics. A harness that checks status codes would
   pass every one of them. See `verification.md` §2.
3. **One group already went through this** (`ADP.Menus`, commit `14caf7c9`) — but its endpoint
   parity was never proven. That makes it the ideal proving ground for the harness, not a step to
   skip.

---

## 2. How the steps are ordered

The request asked for two orderings that can conflict:

- **(a) core-first** — groups others depend on go first.
- **(b) simplest-first** — cheapest and lowest-risk go first.

**Resolution rule, applied throughout this plan — three levels, in this order:**

> **1. Dependency order is a hard constraint.**
> **2. Among groups at the same dependency level, higher risk goes later.**
> **3. Simplicity breaks ties among groups of equal risk.**

The middle level is not decoration: it is what actually orders Steps 06 and 07. By profile count
WarrantyClaims (2 profiles) is *simpler* than ClaimableItems (4), and simplicity alone would put it
first — but it carries the trap 3-read data-exposure hazard, so risk overrides and it goes last. Each
step file names which level put it where.

The conflict between (a) and (b) is mild at the root, because the dependency root of this migration
(`ADP.Models`, `ADP.Cases`, `ADP.LookupServices`) is *also* the simplest work — it carries zero
AutoMapper profiles and zero repository triples.

The genuine ordering insight is at the front, not the middle: **the harness is built and graded
before any code changes.** Step 01 replays an already-completed migration whose answer is known, so
that a later diff can be attributed to a migration bug rather than a harness bug. A harness proven
only after the fact proves nothing.

### Resulting order

| # | Step | Why here |
|---|---|---|
| 00 | Baseline & parity harness | Nothing can be verified before a baseline exists. Must run on the pre-bump tree. |
| 01 | Retro-verify `ADP.Menus` | Grades the harness against a migration whose answer is already known. Also closes the one DONE-but-unVERIFIED gap in the repo. |
| 02 | Atomic solution-wide version bump | A deliberate choice, not a forced one (§3): staging is possible but leaves assemblies compiled against a split ShiftEntity family. Expected to leave the build red. **Deleted if SPIKE-12 resolves positive.** |
| 03 | Shared floor — `ADP.Models`, `ADP.Cases`, `ADP.LookupServices` | **Core-first.** The dependency root; **14 csproj `ProjectReference` `ADP.Models`**. Also the simplest — 0 profiles, 0 triples. Both orderings agree. |
| 04 | `ADP.Darlastic` | **Simplest-first** among the groups above the floor: 0 profiles, 0 triples, plain `ControllerBase`; nothing outside the group depends on it. Also the plan's **only framework-only control** — with no mapper here, a diff can only be framework-caused. |
| 05 | `ADP.Surveys` | Cleanest real mapper migration: 4 triples, 1 profile, and the only remaining group with a working sample host. **Has no in-repo dependency of any kind** (verified: every `ProjectReference` in the group is intra-group, and it references no `ShiftSoftware.ADP.*` package), so nothing constrains its position but risk and simplicity. |
| 06 | `ADP.ClaimableItems` | 5 triples, 4 profiles, 5 Cosmos delegates, no host. Harder than Surveys; **more** profiles than WarrantyClaims but no data-exposure hazard, so risk puts it first of the two. |
| 07 | `ADP.WarrantyClaims` | **Last by risk** (level 2 of the rule, overriding simplicity — it has *fewer* profiles than 06). 7 triples, and a dealer/distributor data-exposure hazard that only value-level diffing can catch (§4). |
| 08 | Release readiness | Package-mode restore check + single `ADPVersion` bump + one release. |

**The dependency levels among 04–07 are not identical**, and the earlier draft of this file was
wrong to say they were. Verified from the csprojs:

- `ADP.Surveys` sits *beside* the Step 03 floor, not above it — it references nothing in this repo.
- `ADP.Darlastic` (`Darlastic.Engine:39`), `ADP.ClaimableItems` (`.Data:58`, `.Shared:40`, `.Web:38`,
  and `.API:38 → Lookup.Services`) and `ADP.WarrantyClaims` (`.Shared:37,43,51`, `.Web:36`) all sit
  **above** the Step 03 floor and genuinely depend on it.
- No build edge exists *among* Darlastic / Surveys / ClaimableItems / WarrantyClaims, so any relative
  order of 04–07 is legal. Risk then simplicity picks the one used here.

---

## 3. Lockstep verdict — how atomic the bump really has to be

Dependencies inside the Shift family are minimum-version floors, not exact pins (no bracketed
ranges appear in any nuspec — verified across `Model` / `EFCore` / `ShiftEntity` / `Web` /
`ShiftBlazor` at 8.30.1), so mixing is *technically* legal.

### What is actually true about a staged bump — corrected

An earlier draft of this file claimed "bumping the hub alone turns 9 projects red; there is no core
first, then leaves order that stays green." **That is false and has been removed.** The verified
numbers:

- There are exactly **7** direct `ShiftSoftware.ShiftEntity.Model` `PackageReference` lines in the
  repo. One is the hub itself; `Menus.Shared` is already at 8.30.1.
- **14** csproj `ProjectReference` `ADP.Models/Models/Models.csproj`. Of those, **3** carry their own
  direct `ShiftEntity.Model` pin: `ADP.Cases.Shared:32`, `ADP.ClaimableItems.Shared:34`,
  `ADP.WarrantyClaims.Shared:33`. **A Models-only bump therefore produces 3 NU1605s, not 9.** The
  other pins (`Darlastic.Shared:33`, `Surveys.Shared:32`) sit in projects that do not reference
  `ADP.Models` at all, and the `EFCore` / `.Web` / `.Print` / `ShiftBlazor` / `ShiftIdentity.Core`
  pins are different package IDs that nothing floors, so they never NU1605 either way.
- **`ADP.Surveys` is a live counterexample.** Every `ProjectReference` in the group is intra-group
  and it consumes no `ShiftSoftware.ADP.*` package, so it could be bumped and migrated on a green
  tree today, alone.

So the practical atomic unit is the **group**, not the solution.

### Why this plan still bumps solution-wide

- **The real cost of staging is a split ShiftEntity family, not NU1605.** Bumping `ADP.Models` alone
  lifts `ShiftEntity.Model` to 8.30.1 by max-wins inside every consumer, while those consumers keep a
  direct `ShiftEntity.EFCore 2026.7.31.1`. Each such assembly then *compiles* against 8.30.1 `Model`
  and 7.31.1 `EFCore` — across the AutoMapper removal. Nothing in this repo has proven that state
  compiles and behaves; **that is SPIKE-12.** A fortnight of interleaved mixed-version restores is a
  worse trade than one deliberately red commit.
- **Across published packages**, a downstream host installing two ADP groups gets one unified
  ShiftEntity by max-wins. A group still compiled against 7.31.1 then has dead AutoMapper profiles,
  no registered mapper, and `ShiftEntityMapperValidation` **throws at startup** — the host does not
  boot. This argues for an atomic *release*, which Step 08 guarantees however the commits were
  sliced; it does not by itself require a single red commit.

**Consequence for this plan:** Step 02 bumps all 29 lines in one commit and the repo will not build
afterwards. That is expected and is the step's exit criterion, not a failure. Steps 03–07 restore
green group by group. There is exactly one release, at Step 08.

**Escape hatch.** If SPIKE-12 resolves positive — a per-group bump keeps the tree green — fold each
group's version edits into its own step (03–07), delete Step 02, and every commit ends green. That
restores per-group `git revert` and costs the plan nothing else, because Steps 03–07 already
partition the work along exactly those boundaries. Note the repo is *already* in a mixed state that
builds: `ADP.Menus` is at 8.30.1 while `ADP.Models` — which Menus consumes by `ProjectReference` —
is itself compiled against 7.31.1. That is suggestive evidence, not proof; resolve the spike, do not
assume.

### The package-consumption path is never exercised locally — but not for the reason once given

The *conclusion* is right; the mechanism this file used to state was wrong.
`ImportADPPackagesViaProjectReference` in `GlobalSettings.props` is **declared once and read
nowhere** — no csproj, props or targets file references it. The real switch is a filesystem probe,
repeated in **18 `PackageReference`/`ProjectReference` pairs across 14 csproj**, e.g.
`ADP.ClaimableItems.Shared.csproj:39-40`:

```xml
<PackageReference Include="ShiftSoftware.ADP.Models" Version="$(ADPVersion)"
                  Condition="!Exists('..\..\ADP.Models\Models\Models.csproj')" />
<ProjectReference Include="..\..\ADP.Models\Models\Models.csproj"
                  Condition="Exists('..\..\ADP.Models\Models\Models.csproj')" />
```

The sibling csproj always exists in a checkout, so **local development is always `ProjectReference`
and never consumes an ADP NuGet version**; `$(ADPVersion)` is a pack-output value only. Flipping the
property does nothing, in this repo or in a scratch clone of it. Step 08 item D forces package mode
by making `Exists()` false instead.

---

## 4. The four regression shapes this plan is built around

All four compile clean, return HTTP 200, and emit no diagnostic.

| # | Shape | Where it bites |
|---|---|---|
| 1 | Auto-composed child collections no longer filter soft-deleted rows | any `.Where(x => !x.IsDeleted)` that lived in an old profile |
| 2 | Pair mappers apply name conventions to a child's own `ID`, so a link-row DTO carries the link row's PK instead of the foreign id | link/join collections |
| 3-write | A member the old profile `Ignore()`d on the reverse map is now written from the request body | repository-derived columns overwritten by client input |
| 3-read | A member the old profile `Ignore()`d on a **forward** map is now populated by convention | **data exposure** — see `07-warranty-claims.md` |

Trap 3-read is not in the original Menus trap taxonomy and was found during this survey. It is the
highest-severity item in the plan.

Full detection recipe and per-map audit checklist: `conventions.md` §5.

---

## 5. How to use this plan

**Start here:** `STATUS.md` is the single source of truth for what is done. Read it first, every time.

1. Open `STATUS.md`, find the lowest step that has not reached **its own documented terminal
   status** — the `Terminal status` column names it per step. Four steps can never be `VERIFIED`
   (00, 02, 03, 04); their terminal status is `CLOSED`.
2. Open that step file. Check its **Preconditions** actually hold.
3. Do the work items in order. They name real files; none of them are generic advice.
4. Run the **Verification** section verbatim.
5. Check every box in **Exit criteria**. None of them is a judgement call.
6. Update `STATUS.md` — see the note at the bottom of that file.

**Resuming mid-way.** Every step is written to be resumable from a clean tree at any point.
`STATUS.md` records what is `DONE` (code changed, builds) separately from `VERIFIED` (endpoint parity
proven) and `CLOSED` (finished, with no endpoint surface to prove — the terminal status for Steps 00,
02, 03 and 04). If a step is `DONE` where the ledger says its terminal status is `VERIFIED`, the code
work is finished and only the harness run remains — re-run that step's Verification section, nothing
else. If a step is `IN PROGRESS` or `BLOCKED`, its Notes column says exactly where it stopped.

**Never skip forward past a red build.** Steps 03–07 each restore a slice of the solution to green
after Step 02 deliberately breaks it. Skipping one leaves the next step unable to distinguish its
own errors from the previous step's.

---

## 6. Files

| File | What it is |
|---|---|
| `README.md` | this — orientation, ordering rule, lockstep verdict |
| `STATUS.md` | **the ledger.** Which step is done, which is pending, what proved it |
| `conventions.md` | the migration recipe, the per-map audit checklist, coding conventions for the rewrite |
| `verification.md` | the endpoint-parity harness: design, capture, replay, normalization, per-group applicability, honest gaps |
| `00-baseline-and-harness.md` … `08-release-readiness.md` | one file per step, in execution order |

---

## 7. Scope

**In scope:** every `ShiftSoftware.Shift*` package reference (29 lines across 22 csproj at
`2026.7.31.1`), the AutoMapper profile removal in the three groups that still have profiles, the six
Cosmos replication delegates that relied on the removed AutoMapper fallback, and the four
`AutoMapper.IMapper` injection sites outside `ADP.SyncAgent`.

**Explicitly out of scope:**

- **`ShiftSoftware.TypeAuth.*` stays at `1.6.28`.** It is already the newest release and is what
  ShiftEntity 2026.8.30.1 itself depends on. It is on a separate, non-date version line with
  **poisoned SemVer ordering** — `2024.2.22.2` sorts higher than `1.6.28` but is 2.5 years older.
  Anything that resolves "latest" (the NuGet UI, `dotnet add package` without `--version`, a
  floating `*`, Dependabot) will silently install the ancient one. **Keep all 9 TypeAuth references
  explicitly pinned and never let them float.**
- **`ADP.SyncAgent` keeps its own direct `AutoMapper 14.0.0` reference.** It has no ShiftEntity
  coupling; its AutoMapper use is independent and unaffected.
- `ADP.Hawta`, `ADP.Rastgo`, `ADP.LookupServices.BDD`, `ADP.TestData`, `ADP.Docs` and
  `ADP.WebComponents` carry no `ShiftSoftware.Shift*` reference and need no *edit*. They must still
  be green in the Step 08 full-solution build — **and they are not inert.** Four projects declare
  `<Target Name="RunSelfAfterBuild" AfterTargets="Build">` and rewrite tracked files on any
  `dotnet build ADP.sln`:

  | Generator | Scans | Rewrites (tracked) |
  |---|---|---|
  | `ADP.WebComponents/WebComponentModelGenerator` | `ADP.LookupServices/Lookup.Services/**/*.cs` (the 44 `[TypeScriptModel]` types live there, **not** in `ADP.Models`) | `adp-web-components/src/global/types/generated/` — 44 files, deleted and regenerated wholesale |
  | `ADP.Docs/ModelDocGen` | `ADP.Models` | `ADP.Docs/Docs/docs/generated/` |
  | `ADP.Docs/FeatureDocGen` | `ADP.LookupServices.BDD/Features` | `ADP.Docs/Docs/docs/generated/` |
  | `ADP.TestData/Generator` | its own inputs | `ADP.TestData/environments/*.json` |

  Together they own **247 tracked files**. This cuts two ways: it threatens Step 02's "csproj files
  only" commit (guard in `02` item A), and it is the **cheapest available check that
  `Lookup.Services`' and `ADP.Models`' public shape survived the bump** — a `git diff --exit-code`
  over those three trees after a full build, made an exit criterion in Step 03 and Step 08.
- `ADP.LookupServices/Lookup.Services.Functions` **no longer exists.** Its csproj was deleted at
  `67aa8a3e`; the directory now holds only untracked `bin/` and `obj/`. Nothing can build it. The one
  live hazard is the opposite of "leave it alone": its stale `obj/Debug/net8.0/WorkerExtensions/`
  contains the only `net8.0` `.csproj`-shaped artefact in the tree, so **every project inventory in
  this plan must exclude `**/obj/**` and `**/bin/**`** or it will over-count.

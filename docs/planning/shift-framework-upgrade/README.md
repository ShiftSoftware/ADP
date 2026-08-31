# Shift Framework Upgrade — 2026.7.31.1 → 2026.8.30.1

Staged plan for moving every `ShiftSoftware.Shift*` package in this repo to `2026.8.30.1`,
migrating off AutoMapper onto the ShiftEntity source-generated mappers, and **proving** — not
assuming — that the HTTP surface behaves exactly as it did before.

> This repo is public, generic and multi-tenant. Nothing in this plan names a client, a client
> URL/hostname, a warehouse or branch code, or an action-tree namespace. Keep it that way when you
> fill in status and notes.

> **The invariant this plan is built on:**
> **every step ends with a green build, and every step is revertible on its own — a revert of that
> step's commits, touching only that group's files.**
>
> **Green is a *step* boundary, not a per-commit one.** There is no interval *between* steps where
> the solution is broken, and no step that needs a later step to compile. Inside the three groups
> that still hold AutoMapper profiles (03, 04, 05) the item A bump commit **does not compile** —
> `2026.8.30.1` drops AutoMapper out of the reference graph and the `: Profile` classes go red until
> that step's rewrite lands. Those intermediate commits stay on the step branch and are never merged
> or handed off. Each group step bumps its own package-version lines *and* rewrites its own
> mappers, inside one slice that touches only that group's files. The shared floor moves **last**,
> because last is where it keeps every earlier step green (§3).

---

## 1. Why this needs a plan at all

`2026.8.30.1` **removed AutoMapper from the framework entirely** and replaced it with a Roslyn
source generator. The compatibility package `ShiftSoftware.ShiftEntity.EFCore.AutoMapper` was never
published, and the two intermediate releases (`2026.8.6.1`, `2026.8.24.1`) still depend on
AutoMapper 14.0.0. There is no stepping stone and no fallback:

**Taking the version bump *is* the mapper migration.**

Three things make that harder than a normal package bump:

1. **A group's version bump and its mapper rewrite are the same piece of work.** They cannot be
   split into separate steps without leaving the tree red in between. So there is no "bump
   everything first" step: each group step takes its own 4–7 package lines as its opening commit and
   lands its refactor behind them, ending green. See §3.
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

Dependency order used to be treated as the hard constraint that settled the conflict. **It is not
one here.** The dependency root of this migration — `ADP.Models`, `ADP.Cases`,
`ADP.LookupServices` — carries **0 AutoMapper profiles and 0 repository triples**. There is no
refactor in it at all; it is four package-version lines. Nothing downstream is waiting on a design
decision taken there, so nothing is gained by taking it first — and, as §3 shows, taking it first is
the only way to open an NU1605 downgrade window.

**Resolution rule, applied throughout this plan — three levels, in this order:**

> **1. Every step ends green, and each group is self-contained.** A group step bumps its own package
>    lines and rewrites its own mappers in one revertible slice. **The shared floor goes last,
>    because that is what keeps every earlier step green.**
> **2. Among the group steps, higher risk goes later.**
> **3. Simplicity breaks ties among groups of equal risk.**

The middle level is not decoration: it is what actually orders Steps 04 and 05. By profile count
WarrantyClaims (2 profiles) is *simpler* than ClaimableItems (4), and simplicity alone would put it
first — but it carries the trap 3-read data-exposure hazard, so risk overrides and it goes last. Each
step file names which level put it where.

The genuine ordering insight is at the front, not the middle: **the harness is built and graded
before any code changes.** Step 01 replays an already-completed migration whose answer is known, so
that a later diff can be attributed to a migration bug rather than a harness bug. A harness proven
only after the fact proves nothing.

### Resulting order

| # | Step | Why here |
|---|---|---|
| 00 | Baseline & parity harness | Nothing can be verified before a baseline exists. Must run on the pre-bump tree. Also carries the 15-minute throwaway `ADP.Models` compile probe that buys off the one cost of moving the floor last (§3). |
| 01 | Retro-verify `ADP.Menus` | Grades the harness against a migration whose answer is already known. Also closes the one DONE-but-unVERIFIED gap in the repo. |
| 02 | `ADP.Darlastic` | **Simplest-first** among the groups: 4 package lines, 0 profiles, 0 triples, plain `ControllerBase`; nothing outside the group depends on it. Also the plan's **only framework-only control** — with no mapper here, a diff can only be framework-caused. |
| 03 | `ADP.Surveys` | Cleanest real mapper migration: 7 package lines, 4 triples, 1 profile, and the only remaining group with a working sample host. **Free-floating** — it references no other ADP group and consumes no `ShiftSoftware.ADP.*` package (verified: every `ProjectReference` in the group is intra-group), so it could legally run at any point after 01. It sits here on risk ordering alone. |
| 04 | `ADP.ClaimableItems` | 7 package lines, 5 triples, 4 profiles, 5 Cosmos delegates, no host. Harder than Surveys; **more** profiles than WarrantyClaims but no data-exposure hazard, so risk puts it first of the two. |
| 05 | `ADP.WarrantyClaims` | **Last by risk** (level 2 of the rule, overriding simplicity — it has *fewer* profiles than 04). 7 package lines, 7 triples, and a dealer/distributor data-exposure hazard that only value-level diffing can catch (§4). Its dependency on Step 04 is a **knowledge** dependency, not a build one: it reuses the shared `Certificate` mapper precedent settled there (SPIKE-8). |
| 06 | Shared floor — `ADP.Models`, `ADP.Cases`, `ADP.LookupServices` | **Last on purpose, and this is the load-bearing choice.** 4 package lines, 0 profiles, 0 triples — a version number and nothing else. By the time it moves, `ClaimableItems.Shared` and `WarrantyClaims.Shared` are already at 8.30.1, and `Cases.Shared` is bumped in the *same commit* as `ADP.Models`, so no NU1605 downgrade window ever opens (§3). |
| 07 | Release readiness | Package-mode restore check + single `ADPVersion` bump + one release. **The bump was staged; the release is still atomic** (§3). |
| 08 | Harness removal & cleanup | The parity harness is **deliberately temporary**: it exists to prove this one upgrade, not to be carried forever. Once Step 07 is `VERIFIED` it has done its job and is removed, along with its scaffolding. |

**The dependency levels among 02–05 are not identical**, and the earlier draft of this file was
wrong to say they were. Verified from the csprojs:

- `ADP.Surveys` sits *beside* the shared floor, not above it — it references nothing in this repo.
- `ADP.Darlastic` (`Darlastic.Engine:39`), `ADP.ClaimableItems` (`.Data:58`, `.Shared:40`, `.Web:38`,
  and `.API:38 → Lookup.Services`) and `ADP.WarrantyClaims` (`.Shared:37,43,51`, `.Web:36`) all sit
  **above** the shared floor and genuinely depend on it. They are still upgraded before it, because a
  `ProjectReference` consumer compiles happily against a shared project still pinned to the older
  framework — that is not an assumption, it is the state this repo is in today (§3).
- No build edge exists *among* Darlastic / Surveys / ClaimableItems / WarrantyClaims, so any relative
  order of 02–05 is legal. Risk then simplicity picks the one used here.

---

## 3. Lockstep verdict — why the bump is staged, and the floor moves last

**The bump does not have to be atomic, and this plan does not make it atomic.**

Dependencies inside the Shift family are **minimum-version floors, not exact pins**. Every nuspec
declares a lower bound (`version="2026.7.31.1"`), never a bracketed exact range (`[2026.7.31.1]`) —
verified in the local NuGet cache across `Model` / `EFCore` / `ShiftEntity` / `Web` / `ShiftBlazor`
at 8.30.1. Nothing in the framework forces a lockstep bump.

### The live proof is already in this repo

`ADP.Menus.Shared` pins `ShiftSoftware.ShiftEntity.Model 2026.8.30.1`. `ADP.Menus.Data`
`ProjectReference`s `ADP.Models`, which pins `ShiftEntity.Model 2026.7.31.1`
(`ADP.Models/Models/Models.csproj:48`). **That tree builds green today**, and has since `14caf7c9`.

An upgraded group sitting on a not-yet-upgraded shared project is therefore not a hypothesis here —
it is the repo's current state. That is exactly the arrangement the shared-last order relies on.

### Shared-last eliminates NU1605 entirely

The verified numbers:

- There are exactly **7** direct `ShiftSoftware.ShiftEntity.Model` `PackageReference` lines in the
  repo. One is the hub itself; `Menus.Shared` is already at 8.30.1.
- **14** csproj `ProjectReference` `ADP.Models/Models/Models.csproj`. Of those, **3** carry their own
  direct `ShiftEntity.Model` pin: `ADP.Cases.Shared:32`, `ADP.ClaimableItems.Shared:34`,
  `ADP.WarrantyClaims.Shared:33`. **Those three are the only NU1605 candidates in the repo.**
- Under a **shared-first** order they are exactly the failure: bumping `ADP.Models` lifts
  `ShiftEntity.Model` to 8.30.1 by max-wins inside all 14 consumers while those three still pin
  7.31.1 directly — **3 package-downgrade errors**.
- Under **shared-last** they cannot fire. `ClaimableItems.Shared` is at 8.30.1 from Step 04 and
  `WarrantyClaims.Shared` from Step 05, both before the floor moves; and `Cases.Shared:32` is bumped
  in the **same commit** as `Models.csproj:48` in Step 06. **No downgrade window ever opens.**
- The other pins (`Darlastic.Shared:33`, `Surveys.Shared:32`) sit in projects that do not reference
  `ADP.Models` at all, and the `EFCore` / `.Web` / `.Print` / `ShiftBlazor` / `ShiftIdentity.Core`
  pins are different package IDs that nothing floors, so they never NU1605 either way.

**And no assembly ever compiles against two ShiftEntity versions.** Within each group the
`ShiftEntity.Model` and `ShiftEntity.EFCore` lines move in the **same commit** — Darlastic
`.Shared:33` + `.Data:53`, Surveys `.Shared:32` + `.Data:35`, ClaimableItems `.Shared:34` +
`.Data:48`, WarrantyClaims `.Shared:33` + `.Data:48`, the floor `Cases.Shared:32` + `Cases.Data:31`
— so a group's ShiftEntity family is never split across two versions *across the AutoMapper
removal*. The nuspec ranges say a split family would resolve anyway; this line pairing is what makes
the question moot. Each group step's item A states it.

**SPIKE-12 is `RESOLVED — staged per-group bump adopted`**, on the nuspec minimum-range finding plus
the live Menus / `ADP.Models` counterexample above. Nothing in this plan is gated on it any more, and
no step is a precondition for resolving it.

### Why "core-first" does not apply to this floor

The usual case for core-first is that a broken foundation makes every later failure ambiguous.
**That argument does not reach this floor.** `ADP.Models`, `ADP.Cases` and `ADP.LookupServices`
carry **0 AutoMapper profiles and 0 repository triples** between them. Step 06 is four package
lines and nothing else — there is no refactor there to get wrong, and so no decision taken there
that the group steps need settled before they start.

### The one residual risk, and how it is bought off

Moving the floor last defers, to the very end, the discovery of any breaking change in
`ShiftEntity.Model 2026.8.30.1` that affects `ADP.Models` itself. That is the real cost of this
ordering, and it is paid down at the front instead:

> **Step 00 carries a throwaway compile probe, timeboxed to 15 minutes.** On a scratch branch, bump
> `ADP.Models/Models/Models.csproj:48` to `2026.8.30.1`, run `dotnet build ADP.Models/Models`,
> record the result in `STATUS.md`, then **revert**. **Do not commit it.** Step 06 reads the
> recorded result as a precondition.

That buys the late ordering with no late surprise.

### The release is still atomic, even though the bump is not

Across *published* packages, a downstream host installing two ADP groups gets one unified
ShiftEntity by max-wins. A group still compiled against 7.31.1 would then have dead AutoMapper
profiles, no registered mapper, and `ShiftEntityMapperValidation` **throws at startup** — the host
does not boot. That argues for an atomic **release**, not an atomic commit. **Step 07 ships exactly
one release**, after every group is at 8.30.1. Nothing is published between Steps 02 and 06.

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
property does nothing, in this repo or in a scratch clone of it. Step 07 item E forces package mode
by making `Exists()` false instead.

---

## 4. The four regression shapes this plan is built around

All four compile clean, return HTTP 200, and emit no diagnostic.

| # | Shape | Where it bites |
|---|---|---|
| 1 | Auto-composed child collections no longer filter soft-deleted rows | any `.Where(x => !x.IsDeleted)` that lived in an old profile |
| 2 | Pair mappers apply name conventions to a child's own `ID`, so a link-row DTO carries the link row's PK instead of the foreign id | link/join collections |
| 3-write | A member the old profile `Ignore()`d on the reverse map is now written from the request body | repository-derived columns overwritten by client input |
| 3-read | A member the old profile `Ignore()`d on a **forward** map is now populated by convention | **data exposure** — see `05-warranty-claims.md` |

Trap 3-read is not in the original Menus trap taxonomy and was found during this survey. It is the
highest-severity item in the plan.

Full detection recipe and per-map audit checklist: `conventions.md` §5.

---

## 5. How to use this plan

**Start here:** `STATUS.md` is the single source of truth for what is done. Read it first, every time.

1. Open `STATUS.md`, find the lowest step that has not reached **its own documented terminal
   status** — the `Terminal status` column names it per step. Four steps can never be `VERIFIED`
   (00, 02, 06, 08); their terminal status is `CLOSED`.
2. Open that step file. Check its **Preconditions** actually hold.
3. Do the work items in order. They name real files; none of them are generic advice.
4. Run the **Verification** section verbatim.
5. Check every box in **Exit criteria**. None of them is a judgement call.
6. Update `STATUS.md` — see the note at the bottom of that file.

**Resuming mid-way.** Every step is written to be resumable from a clean tree at any point.
`STATUS.md` records what is `DONE` (code changed, builds) separately from `VERIFIED` (endpoint parity
proven) and `CLOSED` (finished, and it has no endpoint surface whose parity could be proven — the
terminal status for Steps 00, 02, 06 and 08). **`CLOSED` never means "ended red"** — no step in this
plan ends red. If a step is `DONE` where the ledger says its terminal status is `VERIFIED`, the code
work is finished and only the harness run remains — re-run that step's Verification section, nothing
else. If a step is `IN PROGRESS` or `BLOCKED`, its Notes column says exactly where it stopped.

**Every step ends green, and every step reverts on its own** — a revert of that step's commits,
top-down; a group step is three or more commits, so it is not a single `git revert`. No step hands
the next one a broken tree, so a red build at the *start* of a step is a genuine failure, not an expected hand-off — stop
and find it before doing anything else. And when a step turns out to be wrong after the fact, the
remedy is `git revert` on that step's commits, not a forward patch: because a group's package bump,
mapper rewrite and verification all live inside one step and touch only that group's files, the
revert is local and total. Preserving that property is the whole reason the shared floor moves last
(§3).

---

## 6. Files

| File | What it is |
|---|---|
| `README.md` | this — orientation, ordering rule, lockstep verdict |
| `STATUS.md` | **the ledger.** Which step is done, which is pending, what proved it |
| `conventions.md` | the migration recipe, the per-map audit checklist, coding conventions for the rewrite |
| `verification.md` | the endpoint-parity harness: design, capture, replay, normalization, per-group applicability, honest gaps |
| `00-baseline-and-harness.md` … `08-harness-removal.md` | one file per step, in execution order. Nine steps, and **no separate version-bump step** — each group step bumps the package lines it owns (§7) |

---

## 7. Scope

**In scope:** every `ShiftSoftware.Shift*` package reference (29 lines across 22 csproj at
`2026.7.31.1`), the AutoMapper profile removal in the three groups that still have profiles, the six
Cosmos replication delegates that relied on the removed AutoMapper fallback, and the four
`AutoMapper.IMapper` injection sites outside `ADP.SyncAgent`.

The 29 package lines are **partitioned across the steps that own them** — Darlastic 4, Surveys 7,
ClaimableItems 7, WarrantyClaims 7, shared floor 4 — and each step file lists its own lines by csproj
and line number. No step edits a line it does not own.

The parity harness is in scope twice: **built in Step 00, removed in Step 08.** It is deliberately
temporary — an instrument for proving this upgrade, not a permanent fixture to maintain.

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
  be green in the Step 07 full-solution build — **and they are not inert.** Four projects declare
  `<Target Name="RunSelfAfterBuild" AfterTargets="Build">` and rewrite tracked files on any
  `dotnet build ADP.sln`:

  | Generator | Scans | Rewrites (tracked) |
  |---|---|---|
  | `ADP.WebComponents/WebComponentModelGenerator` | `ADP.LookupServices/Lookup.Services/**/*.cs` (the 44 `[TypeScriptModel]` types live there, **not** in `ADP.Models`) | `adp-web-components/src/global/types/generated/` — 44 files, deleted and regenerated wholesale |
  | `ADP.Docs/ModelDocGen` | `ADP.Models` | `ADP.Docs/Docs/docs/generated/` |
  | `ADP.Docs/FeatureDocGen` | `ADP.LookupServices.BDD/Features` | `ADP.Docs/Docs/docs/generated/` |
  | `ADP.TestData/Generator` | its own inputs | `ADP.TestData/environments/*.json` |

  Together they own **247 tracked files**. This cuts two ways: it threatens the "csproj files only"
  opening commit of **every** group step (each step's work item A carries the guard), and it is the
  **cheapest available check that `Lookup.Services`' and `ADP.Models`' public shape survived the
  bump** — a `git diff --exit-code` over those three trees after a full build, made an exit criterion
  in Step 06 and Step 07.
- `ADP.LookupServices/Lookup.Services.Functions` **no longer exists.** Its csproj was deleted at
  `67aa8a3e`; the directory now holds only untracked `bin/` and `obj/`. Nothing can build it. The one
  live hazard is the opposite of "leave it alone": its stale `obj/Debug/net8.0/WorkerExtensions/`
  contains the only `net8.0` `.csproj`-shaped artefact in the tree, so **every project inventory in
  this plan must exclude `**/obj/**` and `**/bin/**`** or it will over-count.

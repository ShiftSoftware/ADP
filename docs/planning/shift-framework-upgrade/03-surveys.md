# Step 03 — `ADP.Surveys`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** first real mapper migration. 4 triples, 1 profile, and the **only remaining group with a
working sample host** — so this is the first step that produces full HTTP endpoint parity evidence
for a migration done under this plan.

---

## Projects touched

| Path | Bumped line (item A performs these edits) |
|---|---|
| `ADP.Surveys/ADP.Surveys.API/ADP.Surveys.API.csproj` | `ShiftEntity.Web` (33) |
| `ADP.Surveys/ADP.Surveys.Data/ADP.Surveys.Data.csproj` | `ShiftEntity.EFCore` (35), `ShiftIdentity.Core` (36) |
| `ADP.Surveys/ADP.Surveys.Shared/ADP.Surveys.Shared.csproj` | `ShiftEntity.Model` (32) |
| `ADP.Surveys/ADP.Surveys.Web/ADP.Surveys.Web.csproj` | `ShiftBlazor` (34) |
| `ADP.Surveys/samples/ADP.Surveys.Sample.API/ADP.Surveys.Sample.API.csproj` | `ShiftIdentity.Dashboard.AspNetCore` (15) |
| `ADP.Surveys/samples/ADP.Surveys.Sample.Web/ADP.Surveys.Sample.Web.csproj` | `ShiftIdentity.Dashboard.Blazor` (15) |

Also: `ADP.Surveys/ADP.Surveys.Shared.Tests` (182 tests).
`ADP.Surveys/renderer/` is a separate JS/SDK tree and is untouched.

**Coverage note — the endpoints this group has beyond the CRUD template.** `ADP.Surveys.API`
declares 12 hand-written `[Http*]` actions, and the templated case list reaches none of them. Two
matter enough to name:

- `Controllers/PublicSurveyController.cs` — `[Route("SurveyInstances")]` `[AllowAnonymous]`, with
  `[HttpGet("{publicId:guid}/schema")]` and the submit path. This is the **entire anonymous renderer
  surface**, in the one group this plan calls full HTTP parity, and it is also where
  `SurveyInstanceRepository`'s write mapper is actually driven from.
- `Controllers/TriggerIngestController.cs` — the other driver of that write path.

Per `verification.md` §5, every catalogue entry resolves to a case or to an `excludedRoutes` entry
with a reason. **These two do not get excluded.**

---

## Preconditions

- Step 00 `CLOSED` — a Surveys baseline exists, under **both grants**, and its `summary` showed the
  seeded hostile rows present, `CREATE 2xx` at 100%, and full catalogue coverage.
- Step 01 `VERIFIED` — the harness is graded. A diff in this step must be attributable to *this*
  migration.

That is the whole dependency list. **This step performs its own package bump** (item A) as its first
commit and ends green. It does not inherit a version bump, and it does not inherit a red tree from
anywhere — there is no solution-wide bump step in this plan.

**Not a precondition, but worth having first:** Step 02 (`ADP.Darlastic`) is the one group with 0
profiles and 0 triples, so it flushes out non-mapper framework fallout on its own. If it is already
done, every error here is mapper-shaped, and the Darlastic capture (if SPIKE-5 resolved positive) is
available as the framework-only control against which to attribute this group's diffs. If it is not,
expect to attribute framework-caused diffs by hand.

**This step is free-floating.** `ADP.Surveys` references no other ADP group and consumes no
`ShiftSoftware.ADP.*` package — every `ProjectReference` in the group is intra-group (verified) — so
it waits on nothing but the harness, and in particular not on the shared floor (Step 06). It could
run at any point after Step 01; it sits at 03 for risk ordering only, and an earlier draft's
`Depends on: <the shared floor>` was fictional.

**Why Surveys before ClaimableItems and WarrantyClaims:** all three are free to move, so simplicity
breaks the tie between 03 and 04 — one profile of 151 lines against four profiles / 224 lines — and
Surveys is the only one of the three with a real host. Doing the group where verification is
strongest *first* means the mapper recipe is proven under full HTTP parity before it is applied where
only a mounted host is available. (05 goes last on **risk**, not simplicity; it has fewer profiles
than 04.)

### Resolve first — before any rewriting

**SPIKE-3 and SPIKE-4 are not preconditions of this step; they are its own first work.** A
precondition satisfied by the step's own body is not a precondition, and writing them as one made
this step look permanently unstartable. Resolve them after the item A bump and before any rewriting,
in this order, recording both findings in `STATUS.md` before touching a repository:

- [ ] **SPIKE-3** — how `BankQuestionListDTO.Type` and `ScreenTemplateListDTO.QuestionCount` work
      today, and what replaces a static JSON-parsing method call in an EF-translatable list
      projection. Detail in item D. **Blocking for items D and E.**
- [ ] **SPIKE-4** — whether the existing-aware `ForEntity((dto, entity, ctx) => …)` overload
      reproduces AutoMapper's `.Condition(...)`. Detail in item F. **Blocking for item D.**

---

## The survey — what is actually in this group

**One profile:** `ADP.Surveys/ADP.Surveys.Data/AutoMapperProfiles/GeneralMappingProfile.cs`
(151 lines, 8 `CreateMap` calls, 4 private static helpers).

**Four triples**, all in `ADP.Surveys/ADP.Surveys.Data/Repositories/`, and **all four currently call
`base(db)` with no options lambda** — so every repository that needs customization gains one:

| Repository | Triple |
|---|---|
| `SurveyRepository.cs` | `Survey, SurveyListDTO, SurveyAdminDTO` |
| `BankQuestionRepository.cs` | `BankQuestion, BankQuestionListDTO, BankQuestionAdminDTO` |
| `ScreenTemplateRepository.cs` | `ScreenTemplate, ScreenTemplateListDTO, ScreenTemplateAdminDTO` |
| `SurveyInstanceRepository.cs` | `SurveyInstance, SurveyInstanceListDTO, SurveyInstanceAdminDTO` |

`BankQuestionRepository` and `ScreenTemplateRepository` also override `UpsertAsync`.

**Baseline diagnostics — 3 `SHENGEN004`, and they name exactly the three view members to write:**

```
Generated_Survey_SurveyListDTO_SurveyAdminDTO_1b10fe89                  does not map: Draft
Generated_BankQuestion_BankQuestionListDTO_BankQuestionAdminDTO_13a57671 does not map: Question
Generated_ScreenTemplate_ScreenTemplateListDTO_ScreenTemplateAdminDTO_b2178bf1 does not map: Template
```

All three are JSON-column deserializations. None is convention-derivable. Each becomes a `ForView`.

**Trap tally for this group:**

| Trap | Count | Where |
|---|---|---|
| 1 — soft-delete filtering | **2** | `SurveyInstanceListDTO.ResponseCount`, `.CompletedAt` |
| 2 — link-row PK leak | **0** | no link/join collections in this group's DTOs |
| 3-write — reverse-map `Ignore()` | **2** | `Survey.PublishedVersionNumber`, `BankQuestion.Locked` |
| 3-read — forward-map `Ignore()` | **0** | verified: no `Ignore()` on any forward map here |
| **`.Condition(...)`** | **1** | `BankQuestion.BankEntryID` — **not in the standard taxonomy**, see item F |

---

## Work items

Every item below names a real file and a real member. Work them in order.

### A. Bump this group's package references

This group owns its bump — there is no solution-wide version-bump step. Do this **first**, in its own
commit, so a bisect can separate "the version moved" from "the mappers changed".

> **That commit does not compile, and it cannot.** `2026.8.30.1` takes AutoMapper **out of the
> reference graph** — `ShiftSoftware.ShiftEntity`'s nuspec declares `AutoMapper 14.0.0` at
> `2026.7.31.1` and declares no AutoMapper dependency at `2026.8.30.1` (verified in the local NuGet
> cache), and `ADP.Surveys.Data` carries **no direct `AutoMapper` `PackageReference`** of its own.
> So the moment `.Data:35` moves, `AutoMapperProfiles/GeneralMappingProfile.cs:22` (`: Profile`,
> nine `CreateMap`s) loses `Profile`, `CreateMap` and `IMapper` as hard `CS0246`/`CS0103` **errors,
> not warnings**. Items B–E end it green. **Green is a *step* boundary, not a per-commit one**:
> keep these intermediate commits on the step branch and do not merge or hand off mid-step.

| csproj | Line | Package |
|---|---|---|
| `ADP.Surveys/ADP.Surveys.API/ADP.Surveys.API.csproj` | 33 | `ShiftEntity.Web` |
| `ADP.Surveys/ADP.Surveys.Data/ADP.Surveys.Data.csproj` | 35 | `ShiftEntity.EFCore` |
| `ADP.Surveys/ADP.Surveys.Data/ADP.Surveys.Data.csproj` | 36 | `ShiftIdentity.Core` |
| `ADP.Surveys/ADP.Surveys.Shared/ADP.Surveys.Shared.csproj` | 32 | `ShiftEntity.Model` |
| `ADP.Surveys/ADP.Surveys.Web/ADP.Surveys.Web.csproj` | 34 | `ShiftBlazor` |
| `ADP.Surveys/samples/ADP.Surveys.Sample.API/ADP.Surveys.Sample.API.csproj` | 15 | `ShiftIdentity.Dashboard.AspNetCore` |
| `ADP.Surveys/samples/ADP.Surveys.Sample.Web/ADP.Surveys.Sample.Web.csproj` | 15 | `ShiftIdentity.Dashboard.Blazor` |

Seven lines, `2026.7.31.1` → `2026.8.30.1`, **two of them under `samples/`**. The samples are not
optional here: the sample host is what produces this group's HTTP parity evidence, so a sample left
on the old version means the post-upgrade capture is not a capture of the upgrade.

- [ ] Bump all seven lines.
- [ ] `TypeAuth` stays at `1.6.28` (`TypeAuth.AspNetCore` in `.API:34`, `TypeAuth.Core` in
      `.Shared:33`) — separate version line, no bump.
- [ ] `dotnet build` and capture the error list. It must move **inside `ADP.Surveys/` only** and it
      must be the AutoMapper break above and nothing else; anything outside this group is a surprise
      — record it in `STATUS.md` before continuing. (The 3 baseline `SHENGEN004`s recorded below are
      warnings; they reappear once the group compiles again, and items C–E resolve them.)
- [ ] `.Shared:32` (`ShiftEntity.Model`) and `.Data:35` (`ShiftEntity.EFCore`) move in this same
      commit, so this group's ShiftEntity family is never split across two versions.
- [ ] **Commit csproj files only.** Four `AfterTargets="Build"` self-runners rewrite 247 tracked
      files on any `dotnet build ADP.sln` (`README.md` §7), so before committing this bump run
      `git checkout -- ADP.WebComponents/adp-web-components/src/global/types/generated ADP.Docs/Docs/docs/generated ADP.TestData/environments`.
- [ ] Nothing outside `ADP.Surveys/` changes in this commit. The shared floor (`ADP.Models`,
      `ADP.Cases`, `ADP.LookupServices`) moves last, in Step 06, and this group references none of it
      — no downgrade window, nothing to coordinate. The Shift nuspecs declare **minimum-version**
      dependencies, not exact pins, so a group running ahead of the floor is a supported arrangement.

### B. Delete the profile and the registration calls

- [ ] Delete `ADP.Surveys/ADP.Surveys.Data/AutoMapperProfiles/` whole (the directory holds only
      `GeneralMappingProfile.cs`).
- [ ] `ADP.Surveys/ADP.Surveys.API/Extensions/SurveyApiExtensions.cs:48` — delete
      `o.AddAutoMapper(typeof(Data.Marker).Assembly);`. **Keep `o.AddDataAssembly(...)`.** Rewrite
      the surrounding comment to say mappers are source-generated and self-registering, and that
      `RegisterShiftRepositories` validates every triple at startup.
- [ ] `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:38` — delete
      `x.AddAutoMapper(typeof(DB).Assembly);`.
- [ ] `ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:46` — delete
      `x.AddShiftIdentityAutoMapper();`.
- [ ] Update the comment above that block if it lists AutoMapper among the things being configured.

These three deletions are **the entire host-bootstrap difference** between the pre- and post-upgrade
harness runs. Known, reviewable, and with no response-shape effect (`verification.md` §6).

### C. `SurveyRepository` — `Survey / SurveyListDTO / SurveyAdminDTO`

Old profile (`MapSurvey()`):

```csharp
CreateMap<Survey, SurveyListDTO>();                       // bare — delete, no replacement
CreateMap<Survey, SurveyAdminDTO>()
    .ForMember(d => d.Draft, opt => opt.MapFrom(src => DeserializeDraft(src)))
    .ReverseMap()
    .ForMember(e => e.DraftJson, opt => opt.MapFrom(src => ...JsonSerializer.Serialize(src.Draft, ...)))
    .ForMember(e => e.PublishedVersionNumber, opt => opt.Ignore());
```

- [ ] **`Draft` → `ForView`.** Carry `DeserializeDraft` over as a private static helper on the
      repository. It is not a plain deserialize: it also **stamps `SurveyDto.SurveyId` with the
      entity's long ID**. Losing that stamp is a silent regression the compiler will not catch —
      the field simply comes back null. `ForView` runs in memory, so a method call is fine here.
- [ ] **`DraftJson` → `ForEntity`.** Preserve the `Draft == null ? "" : Serialize(...)` shape
      exactly — note it produces empty string, **not null**, on a null draft.
- [ ] **`PublishedVersionNumber` → `IgnoreEntity(e => e.PublishedVersionNumber)`. TRAP 3-WRITE.**
      Server-owned. Without this, a client can set the published version number through the request
      body. **Verify from the emitted `MapToEntityGenerated` that no
      `existing.PublishedVersionNumber = …` line remains.**
- [ ] Confirm the canonical serializer options are still used on both directions — the wire format
      must stay consistent with what the renderer and SDK expect.

### D. `BankQuestionRepository` — and **SPIKE-3**

Old profile (`MapBankQuestion()`):

```csharp
CreateMap<BankQuestion, BankQuestionListDTO>()
    .ForMember(d => d.Type, opt => opt.MapFrom(src => ExtractQuestionType(src.QuestionJson)));   // ← SPIKE-3
CreateMap<BankQuestion, BankQuestionAdminDTO>()
    .ForMember(d => d.Question, ...Deserialize...)
    .ForMember(d => d.Tags,     ...SplitTags...)
    .ReverseMap()
    .ForMember(e => e.QuestionJson, ...Serialize...)
    .ForMember(e => e.Tags,         ...string.Join(",")...)
    .ForMember(e => e.Locked,       opt => opt.Ignore())                                          // ← TRAP 3-WRITE
    .ForMember(e => e.BankEntryID,  opt => opt.Condition(src => src.BankEntryID != Guid.Empty));  // ← SPIKE-4
```

- [ ] **Resolve SPIKE-3 first — this is blocking.** `BankQuestionListDTO.Type` is mapped by a
      **static method call** (`ExtractQuestionType`) that parses a JSON column. `ForList` is spliced
      into the generated **SQL projection** and must be EF-translatable; a method call is not.
      Determine how this list projection works today (is it `ProjectTo`'d at all, or materialized
      first?) and what the replacement is. Candidate outcomes: an EF-translatable JSON path
      expression, an `IgnoreList` plus post-processing, or a `MapToList` override. **Record the
      finding in `STATUS.md` — do not guess.**
- [ ] **`Question` → `ForView`** (named by `SHENGEN004`). In-memory, so the deserialize is fine.
- [ ] **`Tags` → `ForView`** — `string?` → `List<string>?` via the comma split. Preserve the exact
      null/empty semantics: `null` or empty input yields **null**, not an empty list.
- [ ] **`QuestionJson` → `ForEntity`** — serialize with the explicit `typeof(QuestionDto)` overload
      the profile used, so polymorphic question types serialize identically. Preserve
      `null → ""`.
- [ ] **`Tags` (reverse) → `ForEntity`** — `null` or empty list yields **null**, not `""`.
- [ ] **`Locked` → `IgnoreEntity(e => e.Locked)`. TRAP 3-WRITE.** Server-owned; flipped true
      automatically on first publish reference. Without this a client can unlock a locked bank
      question through a PUT body. **Verify from emitted code.**
- [ ] **`BankEntryID` → SPIKE-4.** See item F.
- [ ] Re-check `BankQuestionRepository.UpsertAsync`'s override against the new write path — it may
      have relied on profile behaviour that has moved.

### E. `ScreenTemplateRepository` — the second SPIKE-3 instance

```csharp
CreateMap<ScreenTemplate, ScreenTemplateListDTO>()
    .ForMember(d => d.QuestionCount, opt => opt.MapFrom(src => CountTemplateQuestions(src.TemplateJson)));  // ← SPIKE-3
CreateMap<ScreenTemplate, ScreenTemplateAdminDTO>()
    .ForMember(d => d.Template, ...Deserialize...)
    .ForMember(d => d.Tags,     ...SplitTags...)
    .ReverseMap()
    .ForMember(e => e.TemplateJson, ...Serialize...)
    .ForMember(e => e.Tags,         ...string.Join(",")...);
```

- [ ] **`QuestionCount`** — same SPIKE-3 problem as `BankQuestionListDTO.Type`: a static method
      counting a JSON array. Same resolution applies to both; resolve once, apply twice.
- [ ] **`Template` → `ForView`** (named by `SHENGEN004`).
- [ ] **`Tags` → `ForView` and `ForEntity`**, identical null/empty semantics to item D.
- [ ] **`TemplateJson` → `ForEntity`**, `null → ""`.
- [ ] **No `Ignore()` on this map** — no trap 3 here. Confirm from the emitted `MapToEntityGenerated`
      that the members written match the old reverse map exactly, with nothing extra.
- [ ] Re-check `ScreenTemplateRepository.UpsertAsync`.

### F. Resolve SPIKE-4 — AutoMapper `.Condition(...)` has no builder equivalent

```csharp
// BankEntryID is server-owned on create (the entity's `= Guid.NewGuid()` default).
// Skip mapping when the client sends Guid.Empty so the default survives; still allow
// updates from authenticated admin flows that explicitly carry the value.
.ForMember(e => e.BankEntryID, opt => opt.Condition(src => src.BankEntryID != Guid.Empty));
```

This is a **conditional write**, not an ignore: write when the incoming value is non-empty, leave the
entity's value alone when it is empty. Neither `IgnoreEntity` nor a plain `ForEntity` reproduces it —
`IgnoreEntity` would break admin updates that legitimately carry the value; a plain `ForEntity` would
overwrite the generated default with `Guid.Empty` on create.

- [ ] Confirm the **existing-aware `ForEntity((dto, entity, ctx) => …)` overload** is the correct
      replacement, i.e.:
      `.ForEntity(e => e.BankEntryID, (dto, entity, ctx) => dto.BankEntryID != Guid.Empty ? dto.BankEntryID : entity.BankEntryID)`
- [ ] Watch for `SHENGEN005` ("conditional mapper configuration"). The rule is: **register
      unconditionally and put the condition inside the value delegate** — which the shape above does.
      If the diagnostic fires anyway, the call is not being baked and needs restructuring.
- [ ] Add a **round-trip harness case** that POSTs with `BankEntryID = Guid.Empty` and asserts the
      readback shows a generated non-empty GUID, plus one that PUTs an explicit GUID and asserts it
      is honoured. Both behaviours must be pinned; this is the one member where the sentinel-fill
      strategy alone is not enough, because `Guid.Empty` is the meaningful input.
- [ ] Record the resolution in `STATUS.md`.

### G. `SurveyInstanceRepository` — **the two trap-1 candidates**

```csharp
// List projection runs through ProjectTo — every member below must stay EF-translatable.
CreateMap<SurveyInstance, SurveyInstanceListDTO>()
    .ForMember(d => d.IsTest,        MapFrom(s => s.TriggeredBy == <a constant>))
    .ForMember(d => d.Status,        MapFrom(s => (int)s.Status))
    .ForMember(d => d.SchemaVersion, MapFrom(s => s.SurveyVersion.Version))
    .ForMember(d => d.ResponseCount, MapFrom(s => s.Responses.Count(r => !r.IsDeleted)))          // ← TRAP 1
    .ForMember(d => d.CompletedAt,   MapFrom(s => s.Responses.Where(r => !r.IsDeleted).Max(r => r.CompletedAt)));  // ← TRAP 1

CreateMap<SurveyInstance, SurveyInstanceAdminDTO>().ReverseMap();
```

- [ ] **`ResponseCount` → `ForList`, keeping `.Where(r => !r.IsDeleted)`. TRAP 1.** Without the
      predicate the count includes soft-deleted responses. Returns 200 with a plausible number — no
      diagnostic fires. Only a value diff catches it, and only if the seed contains a soft-deleted
      response (`verification.md` §8.2).
- [ ] **`CompletedAt` → `ForList`, keeping `.Where(r => !r.IsDeleted)`. TRAP 1.** Same shape; a
      soft-deleted response could otherwise supply the `Max`.
- [ ] `IsTest`, `Status`, `SchemaVersion` → `ForList`. All three are EF-translatable (constant
      comparison, enum→int cast, nav property). Check whether the enum→int cast is now conventional
      before writing it — if the convention handles it, delete rather than restate, per
      `conventions.md` §3.
- [ ] All five must stay **EF-translatable** — they are spliced into one SQL projection. No method
      calls.
- [ ] **`SurveyInstance / SurveyInstanceAdminDTO` still needs a mapper even though the CRUD routes
      405.** `SurveyInstanceController.cs:54-73` overrides `GetSingle`, `Post`, `Put`, `Delete`,
      `GetRevisions`, `Print` and `PrintToken` to return 405, and the profile comment calls the pair
      "framework-required for the `ViewAndUpsert` generic". `ShiftEntityMapperValidation` now checks
      **every triple at startup** — so this triple must resolve a mapper or **the app will not
      boot**, regardless of the 405s. Do not delete it as dead.
- [ ] **Its write mapper is live but HTTP-unreachable — so it needs a mapper-level write golden.**
      `MapToEntityGenerated` on this triple is driven from the public submit / trigger-ingest paths,
      not from `PUT`/`POST`. Left alone, the harness produces four 405 transcripts, passes `0 5xx`,
      and covers the write mapper not at all — trap 3-write on this triple would be structurally
      invisible. Record `httpWriteReachable: false` for it in `parity.psd1` and add a golden test
      asserting `MapToEntityGenerated`'s written member set against the old reverse map
      (`verification.md` §5). Then cover the *public* submit path with real HTTP cases as well.

### H. Sweep the prose

- [ ] `ADP.Surveys/ADP.Surveys.Data/ADP.Surveys.Data.csproj:16` — `<Description>` still says
      "AutoMapper profiles". Replace with "source-generated entity/DTO mappers".
- [ ] `ADP.Surveys/README.md:8` — same phrase in the package table.
- [ ] `grep -rn "AutoMapper\|AfterMap\|mapping profile\|MappingProfile" ADP.Surveys --include=*.cs --include=*.md --include=*.csproj`
      and fix every remaining hit. The Menus migration skipped this and left contradictory comments
      (`conventions.md` §7) — do not repeat it.

### I. Emit and audit

```bash
rm -rf ADP.Surveys/ADP.Surveys.Data/obj
dotnet build ADP.Surveys/ADP.Surveys.Data -p:EmitCompilerGeneratedFiles=true
```

Read the **newest-timestamped** file per triple (`conventions.md` §4 — the orphan trap is real) and
check:

- [ ] `MapToEntityGenerated` has **no** `existing.PublishedVersionNumber = …` and **no**
      `existing.Locked = …`.
- [ ] `__shiftBakedIgnored` contains `PublishedVersionNumber` on the Survey triple and `Locked` on
      the BankQuestion triple — proving the calls were seen at build time.
- [ ] `__shiftListProjection` on the SurveyInstance triple contains the `!IsDeleted` predicate in
      **both** `ResponseCount` and `CompletedAt`.
- [ ] `__shiftBakedCustom` lists every member you configured. **Anything you wrote that appears in
      neither baked array did not take effect.**
- [ ] Zero `SHENGEN007`, `008` or `010`. The three baseline `SHENGEN004`s should be **gone**, since
      `Draft`, `Question` and `Template` are now explicitly mapped.

---

## Verification

```bash
dotnet build ADP.Surveys/ADP.Surveys.Data
dotnet build ADP.Surveys/ADP.Surveys.API
dotnet build ADP.Surveys/ADP.Surveys.Web
dotnet build ADP.Surveys/samples/ADP.Surveys.Sample.API
dotnet build ADP.Surveys/samples/ADP.Surveys.Sample.Web

dotnet build                                          # whole solution — green at the END of the step (item A's
                                                      # bump commit is red; items B-E close it)

dotnet test ADP.Surveys/ADP.Surveys.Shared.Tests      # baseline 182 / 182
```

```powershell
.\tools\parity.ps1 verify -Group Surveys
# then, per verification.md §8.7 — privilege-scoped second pass
.\tools\parity.ps1 verify -Group Surveys -Grant Restricted
```

**Group-specific caveats.**

- **Needs SQL.** Cosmos and the blob emulator must stay unconfigured so replication and provisioning
  are skipped.
- **Has a real sample host** → this is genuine full HTTP parity, the strongest evidence available
  short of Menus — *provided* two things Step 00 set up are actually in force here:
  - **the sample's own seeding is suppressed.** `samples/ADP.Surveys.Sample.API/Program.cs:163-196`
    unconditionally runs `EnsureCreatedAsync`, `SeedDBAsync`, `SetFullAccessAsync` and
    `SeedSampleSurveysAsync`. Those identity-keyed demo rows satisfy a naive "> 0 rows" check on
    their own, so a capture that never applied the adversarial seed can still look healthy. Confirm
    the hostile-row-presence line in `summary`, not the row count.
  - **the HTML-fallback assertion is on.** `Program.cs:200,204` call `UseBlazorFrameworkFiles()` and
    `MapFallbackToFile("index.html")` — identical to the Menus sample. A deleted or renamed route
    here returns **200 + HTML, not 404**, right beside a `PublicSurveyController` that legitimately
    answers `NotFound()`. `verification.md` §6's earlier "no response-shape effect" note for this
    group was about the three deleted bootstrap lines and did **not** mean the fallback hazard was
    absent.
- The host bootstrap differs by exactly the **three lines** deleted in item B. No response-shape
  effect, but note it when reviewing the harness diff.
- `ADP.Surveys/samples/ADP.Surveys.Sample.API` carries a pre-existing `NU1504` (duplicate
  `Microsoft.EntityFrameworkCore.Design` `PackageReference`). Unrelated and pre-existing — **safe to
  fix opportunistically, but in its own commit**, not folded into the migration.
- Expect a diff where a select DTO's `Text` is now populated where the old profile left it null —
  that is the documented convention improvement (`conventions.md` §3), accept it with a reason.

---

## Exit criteria

- [ ] `AutoMapperProfiles/` is gone from `ADP.Surveys.Data`; `grep -rn "AutoMapper" ADP.Surveys`
      returns **zero** hits in `.cs`, `.csproj` and `.md`.
- [ ] All six projects build clean.
- [ ] **The solution builds green** — `dotnet build` at the repo root succeeds **at the end of the
      step**. Item A's bump commit is red by construction (the AutoMapper break); the step is not
      finished until the rewrite has closed it. This step ends green; it hands no red tree to any
      later step.
- [ ] **Zero** `SHENGEN` warnings in `ADP.Surveys.Data` — including the 3 baseline `SHENGEN004`s,
      which must now be resolved rather than suppressed.
- [ ] No `#pragma warning disable SHENGEN…` was added. (If one is genuinely needed, it carries a
      justification block naming every member — `conventions.md` §7.)
- [ ] Emitted-code audit (item I) complete, all six checks passing.
- [ ] `ADP.Surveys.Shared.Tests` = 182/182.
- [ ] `parity.ps1 verify -Group Surveys` — every diff explained or accepted with a recorded reason.
- [ ] The **restricted-grant** pass also run against the Step 00 restricted baseline, with its diffs
      explained.
- [ ] **`PublicSurveyController` and `TriggerIngestController` routes are covered by cases**, not
      sitting in `excludedRoutes`. They are the anonymous renderer surface and the only HTTP driver
      of `SurveyInstanceRepository`'s write mapper.
- [ ] **`SurveyInstance` has a mapper-level write golden** and `httpWriteReachable: false` recorded
      — its CRUD writes 405, so the harness alone covers that write mapper not at all.
- [ ] No Surveys response body is HTML (the global fallback assertion is on and passing for this
      group too, not just Menus).
- [ ] The two SPIKE-4 round-trip cases (`Guid.Empty` and explicit GUID) pass.
- [ ] SPIKE-3 and SPIKE-4 marked `RESOLVED` in `STATUS.md` with their findings — **recorded before
      the rewrite began**, per §"Resolve first".
- [ ] Prose sweep (item H) done — csproj `<Description>` and `README.md:8` both updated.
- [ ] `git diff --stat` shows no BOM churn.
- [ ] `STATUS.md` updated to `VERIFIED` with the report path in `Verified by`.

---

## Rollback

Revert this step's commits. The group is a leaf — nothing else in the solution references
`ADP.Surveys.*` — so reverting restores this group to its baseline state, **green on the old package
versions**, without affecting any other group or the shared floor (Step 06).

Commit shape: the item A bump alone; then the profile deletion and the repository rewrites in **one
commit** so the revert is single-shot; then the prose sweep and the `NU1504` fix separately.
Revert **from the top**: reverting only the rewrite commit leaves the group **red** on the new
packages, because the bump alone does not compile. This step's commits are reverted as a set.

---

## Effort & risk

**Effort:** moderate. A seven-line version bump, then eight `CreateMap`s across four repositories and
roughly 20 members to place. The two blocking spikes are the schedule risk, not the rewrite.

**Risks:**

| Risk | Mitigation |
|---|---|
| **SPIKE-3 has no clean answer** — a JSON-parsing method call cannot become an EF-translatable list projection | Resolve *before* starting the rewrite. Fallbacks: EF JSON path expression, `IgnoreList` + post-processing, or a `MapToList` override. If none works, the step is `BLOCKED`, not fudged. |
| **SPIKE-4's conditional write is reproduced as an unconditional one**, overwriting a generated default with `Guid.Empty` on create | Item F's two explicit round-trip cases pin both directions |
| **The two trap-1 soft-delete filters are dropped** — counts silently include deleted responses | Named as explicit work items; verified in emitted `__shiftListProjection`; only caught by the harness if the seed has a soft-deleted response |
| `DeserializeDraft`'s `SurveyId` stamp is lost when the helper is carried over | Called out explicitly in item C |
| Tags null/empty semantics drift (`null` vs `""` vs `[]`) | Stated per direction in items D and E; `verification.md` Rule 5 keeps null/absent/empty distinct in the diff |
| The unused `SurveyInstanceAdminDTO` triple is deleted as dead code, and the app stops booting | Item G states the startup-validation consequence |

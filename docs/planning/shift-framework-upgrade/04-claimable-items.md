# Step 04 — `ADP.ClaimableItems`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** raise this group's own package floor to `2026.8.30.1`, then migrate 5 triples off 4
AutoMapper profiles, hand-write 5 Cosmos replication delegates, and port 1 `IMapper` call site — all
without a runnable host, **ending with a green solution build.**

---

## Projects touched

| Path | Bumped line |
|---|---|
| `ADP.ClaimableItems/ADP.ClaimableItems.API/ADP.ClaimableItems.API.csproj` | `ShiftEntity.Web` (32) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | `ShiftEntity.EFCore` (48), `CosmosDbReplication` (49), `ShiftEntity.Print` (52), `ShiftIdentity.Core` (53) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Shared/ADP.ClaimableItems.Shared.csproj` | `ShiftEntity.Model` (34) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Web/ADP.ClaimableItems.Web.csproj` | `ShiftBlazor` (34) |

**This step performs those seven bumps itself** — see item A. There is no solution-wide version-bump
step any more; each group raises its own floor and ends green.

---

## Preconditions

- **Step 00 `CLOSED`** and **Step 01 `VERIFIED`**. Those are the only hard dependencies.
- **No shared-floor precondition.** The shared floor (`ADP.Models`, `ADP.Cases`,
  `ADP.LookupServices`) is now **Step 06** and runs *after* this one. This group does
  `ProjectReference` all three — `.Data:58`, `.Shared:40`, `.Web:38` → `ADP.Models`; `.Data:43-44` →
  `ADP.Cases.{Shared,Data}`; `.API:38` → `Lookup.Services` — and they are still on `2026.7.31.1`
  while this step runs. That is fine, and it is not a novel arrangement: the Shift nuspecs declare
  **minimum-version** dependencies (`version="2026.7.31.1"`), not exact pins, and `ADP.Menus.Shared`
  already pins `ShiftEntity.Model 2026.8.30.1` while `ADP.Menus.Data` `ProjectReference`s
  `ADP.Models`, which pins `2026.7.31.1` (`ADP.Models/Models/Models.csproj:48`). That builds green
  today. An upgraded group sitting on a not-yet-upgraded floor is the proven case, not the risky one.
- **No NU1605 window opens here.** `ADP.ClaimableItems.Shared:34` is one of the three projects that
  carry both an `ADP.Models` reference *and* their own direct `ShiftEntity.Model` pin. Item A raises
  that pin to `2026.8.30.1` while `ADP.Models` is still at `2026.7.31.1`, so the direct pin sits
  **above** the transitive floor — and a package *downgrade*, which is what NU1605 reports, never
  occurs. It stays above it until Step 06 raises the floor to meet it.
- **Step 03 (`ADP.Surveys`) is a preference, not a dependency.** Nothing in this group compiles
  against Surveys. Surveys is the only group with a sample host and therefore full HTTP parity, so
  running it first proves the mapper recipe under a real endpoint surface before it is applied here,
  where only a mounted host exists. If scheduling forces the other order this step may still start —
  record in `STATUS.md` that the recipe was unproven at full-HTTP level when it did.
- **SPIKE-2 resolved** — the mounted host must actually boot this group. There is **no sample host**
  for ClaimableItems. If the mounted host does not work, take the named fallback rather than stopping:
  either write `ADP.ClaimableItems/samples/ADP.ClaimableItems.Sample.API` on the ~200-line Surveys
  pattern, or fall back to **per-triple mapper-level goldens** with the reduced claim recorded
  verbatim — *"mapper output verified at the type level; no HTTP surface was exercised"*
  (`verification.md` §6). This group's trap tally puts it in trap-2 territory; it is the group least
  able to afford no verification at all, so `BLOCKED` with nothing after it is not an outcome.
- **SPIKE-8 is now owned by this step — resolve it as item 0.** It used to be probed in the
  shared-floor step; with the shared floor moved to Step 06 there is no earlier owner, and this is
  the first step to migrate a `Certificate` triple (`ItemClaimCertificateRepository`, item H). The
  probe is reachable at this step's **baseline** — `ADP.ClaimableItems.Data` compiles before item A,
  which is where the survey's 5 `SHENGEN004`s came from — and again at its green end. Nothing has to
  be deferred to reach emitted code. Read the assembly-placement answer early; confirm it against the
  `2026.8.30.1` emit in item J.
  **The reorder adds one question to it.** The `Certificate` entity lives in
  `ADP.Cases/ADP.Cases.Data/Entities/Certificate.cs` and `CertificateListDTO` in `ADP.Cases.Shared`,
  and both are still on `2026.7.31.1` while this step runs. The probe must therefore also answer
  whether the generator emits a usable mapper for an entity whose **owning assembly is still on the
  old floor**. The `ADP.Menus` / `ADP.Models` pairing above says a mixed floor compiles; it does not
  say the generator is happy. If it is not, that is a real finding — record it in `STATUS.md` and
  raise it, because the fix would be to pull `ADP.Cases`'s two lines forward out of Step 06's table.
  That is a plan change, not a local decision: do **not** silently move them.
- **SPIKE-9 resolved** — the exact `Replicate<T>` delegate signature at `2026.8.30.1`. **Answered in
  Step 01** from the ~19 already-migrated call sites in `ADP.Menus.Sync`; if that did not happen, do
  it now before item G, not during it.
- **SPIKE-11 resolved** — see item C; blocking for two of the four profiles.

**Why here:** this group and WarrantyClaims are both free to move relative to each other, so the tie
goes to **risk, then simplicity**. ClaimableItems is *harder* than Surveys (4 profiles vs 1, no host,
5 replication delegates) but carries no known data-**exposure** hazard, while WarrantyClaims does —
so ClaimableItems goes first even though it has more profiles. Simplicity would have ordered these
two the other way round; risk overrides it (`README.md` §2).

**Step 05 depends on this step**, but only for the shared `Certificate` mapper precedent (SPIKE-8) —
a **knowledge** dependency, not a build one. Nothing in `ADP.WarrantyClaims` compiles against
`ADP.ClaimableItems`; its `WarrantyCertificateRepository` is simply the *second* triple over the same
`ADP.Cases` entity, and it should follow the answer this step records.

---

## The survey

**Five files in `ADP.ClaimableItems/ADP.ClaimableItems.Data/AutoMapperProfiles/`** — 4 `Profile`
classes plus 1 static helper (224 lines total):

| File | Lines | Contains |
|---|---|---|
| `CampaignProfile.cs` | 55 | `Campaign → ServiceCampaignModel` (Cosmos), `Campaign → ServiceItemModel` (Cosmos), `Campaign ↔ CampaignDTO` |
| `ClaimableItemProfile.cs` | 88 | `ClaimableItem → ServiceItemModel` (Cosmos, ~25 members), `ClaimableItem ↔ ClaimableItemDTO` |
| `CampaignVinEntryProfile.cs` | 27 | `CampaignVinEntry → CampaignVinEntryListDTO`, `CampaignVinEntry → CampaignVinEntryModel` (Cosmos) |
| `ItemClaimProfile.cs` | 44 | `ItemClaim → ItemClaimListDTO`, `ItemClaim → ItemClaimModel` (Cosmos, `ConvertUsing`) |
| `GeneralMappingHelper.cs` | 10 | static `DeserializeDict` — **not a profile.** Its `DeserializeDict` is used by the Cosmos maps, so it **survives**; move it out of the deleted folder. |

**Five triples**, all in `ADP.ClaimableItems/ADP.ClaimableItems.Data/Repositories/`:

| Repository | Triple |
|---|---|
| `CampaignRepository.cs` | `Campaign, CampaignListDTO, CampaignDTO` |
| `CampaignVinEntryRepository.cs` | `CampaignVinEntry, CampaignVinEntryListDTO, CampaignVinEntryDTO` |
| `ClaimableItemRepository.cs` | `ClaimableItem, ClaimableItemListDTO, ClaimableItemDTO` |
| `ItemClaimRepository.cs` | `ItemClaim, ItemClaimListDTO, ItemClaimDTO` |
| `ItemClaimCertificateRepository.cs` | `Certificate, CertificateListDTO, ItemClaimCertificateDTO` — **entity owned by `ADP.Cases`** |

**Baseline diagnostics — 5 `SHENGEN004`, naming every member that needs a `ForView`:**

```
Generated_Campaign_CampaignListDTO_CampaignDTO_d3cdbe09              does not map: Brands, Companies, Countries
Generated_ClaimableItem_ClaimableItemListDTO_ClaimableItemDTO_6ad9dc16 does not map: Costs
Generated_ItemClaim_ItemClaimListDTO_ItemClaimDTO_892cc74c           does not map: CampaignVINEntry, ReSubmitForDistributorReview
Generated_CampaignVinEntry_..._CampaignVinEntryDTO_ed731e4e          does not map: DisableVinValidation
Generated_Certificate_CertificateListDTO_ItemClaimCertificateDTO_16cfdeb0 does not map: ReimbursementItemClaims, Notes
```

**Trap tally:**

| Trap | Count | Notes |
|---|---|---|
| 1 — soft-delete filtering | **0 found** | no `IsDeleted` predicate appears in any profile map here. **Confirm against the emitted code anyway** — a collection auto-composed for the first time is exactly where this appears. |
| 2 — link-row PK leak | **suspected** | heavy `ShiftEntitySelectDTO` shuttling (`Brands`/`Companies`/`Countries` built from `long` lists). This is the group's main risk — see item D. |
| 3-write — reverse-map `Ignore()` | **0** | verified: no `Ignore()` on any reverse map in this group |
| 3-read — forward-map `Ignore()` | **0** | verified |

The absence of `Ignore()` anywhere makes trap 3 a non-issue here — which is why this group ranks
below WarrantyClaims in risk despite having more profiles.

---

## Work items

### 0. Resolve SPIKE-8 — where the shared `Certificate` mapper lands

**Do this at this step's baseline, before item A.** `ADP.ClaimableItems.Data` compiles today — that
is where the survey's 5 `SHENGEN004`s came from — so the emitted code is reachable without any of
this step's edits, and nothing has to be deferred. It is reachable again at the step's green end;
item J confirms the answer against the `2026.8.30.1` emit.

```bash
rm -rf ADP.ClaimableItems/ADP.ClaimableItems.Data/obj
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data -p:EmitCompilerGeneratedFiles=true
```

Read what appears in the emitted tree. **Do not answer any of these from reasoning.**

- [ ] Which assembly does each generated `Certificate` mapper land in — the consumer's `.Data`, or
      `ADP.Cases.Data`?
- [ ] Can both `Certificate` triples coexist in one host process? A downstream host may install
      **both** ClaimableItems (`ItemClaimCertificateRepository`, item H) and WarrantyClaims
      (`WarrantyCertificateRepository`, Step 05), so two mappers over the same entity with different
      view DTOs must register side by side in `ShiftEntityMapperRegistry` without collision.
- [ ] Does `ADP.Cases.Data` need to be a registered data assembly for either mapper to resolve at
      startup? Step 06 item C re-checks this one against `ADP.Cases` after its own bump.
- [ ] **Added by the reorder:** does the generator emit a usable mapper for an entity whose *owning
      assembly is still on `2026.7.31.1`*? `Certificate` lives in
      `ADP.Cases/ADP.Cases.Data/Entities/Certificate.cs` and `CertificateListDTO` in
      `ADP.Cases.Shared`, and both stay on the old floor until Step 06. A mixed floor *compiles*
      (`ADP.Menus.Shared` over `ADP.Models`); that does not say the generator is happy. If it is not,
      record it in `STATUS.md` and raise it — the fix would be to pull `ADP.Cases`'s two lines
      forward out of Step 06's table, which is a **plan change, not a local decision**.
- [ ] Record the finding against SPIKE-8 in `STATUS.md` before item A starts. Step 05 reads it as
      precedent; Step 06 confirms the `ADP.Cases` side of it.

### A. Bump this group's package references

**The first commit of this step's code changes** (item 0 is a read-only probe). Seven lines, all `2026.7.31.1` → `2026.8.30.1`:

| csproj | Line | Package |
|---|---|---|
| `ADP.ClaimableItems/ADP.ClaimableItems.API/ADP.ClaimableItems.API.csproj` | 32 | `ShiftEntity.Web` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | 48 | `ShiftEntity.EFCore` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | 49 | `ShiftEntity.CosmosDbReplication` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | 52 | `ShiftEntity.Print` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | 53 | `ShiftIdentity.Core` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Shared/ADP.ClaimableItems.Shared.csproj` | 34 | `ShiftEntity.Model` |
| `ADP.ClaimableItems/ADP.ClaimableItems.Web/ADP.ClaimableItems.Web.csproj` | 34 | `ShiftBlazor` |

- [ ] Edit those seven lines and nothing else. **`ShiftSoftware.TypeAuth.*` stays at `1.6.28`** — it
      is on a separate version line and needs no bump; this group holds several of the 9 references.
- [ ] Leave `ADP.Models`, `ADP.Cases` and `Lookup.Services.DuckDB` alone. Their four lines belong to
      Step 06, and moving them early re-opens the NU1605 question the Preconditions just closed.
- [ ] `.Shared:34` (`ShiftEntity.Model`) and `.Data:48` (`ShiftEntity.EFCore`) move in this same
      commit, so this group's ShiftEntity family is never split across two versions.
- [ ] **Commit csproj files only.** Four `AfterTargets="Build"` self-runners rewrite 247 tracked
      files on any `dotnet build ADP.sln` (`README.md` §7), so before committing this bump run
      `git checkout -- ADP.WebComponents/adp-web-components/src/global/types/generated ADP.Docs/Docs/docs/generated ADP.TestData/environments`.
- [ ] `dotnet build` and capture the error list. It must move **inside `ADP.ClaimableItems` only**.
      The compile breaks are already known and already scoped: items G and H (the 5 replication
      delegates and the `IMapper` call site), plus whatever the generator reports once the profiles
      go in item B. Anything outside this group is a surprise — record it in `STATUS.md` before
      continuing.

> **Green is a *step* boundary, not a per-commit one.** The bump on its own leaves this group red
> until items B–H land, because the removed AutoMapper fallback is a compile break, not a warning.
> Keep those intermediate commits on the step branch; do not merge or hand off mid-step. The step is
> not finished until Verification's solution build is clean.

### B. Delete the profiles and the registration call

- [ ] Delete `CampaignProfile.cs`, `ClaimableItemProfile.cs`, `CampaignVinEntryProfile.cs`,
      `ItemClaimProfile.cs`.
- [ ] **Keep `GeneralMappingHelper.DeserializeDict`** — move it to a non-profile namespace (it is
      used by the Cosmos projections in item G, which survive). Do not delete the folder blindly.
- [ ] `ADP.ClaimableItems/ADP.ClaimableItems.API/Extensions/ClaimableItemsApiExtensions.cs:51` —
      delete `o.AddAutoMapper(typeof(DataMarker).Assembly);`, keep `o.AddDataAssembly(...)`, rewrite
      the surrounding comment.

### C. Resolve SPIKE-11 — `DefaultEntityToDtoAfterMap` / `DefaultDtoToEntityAfterMap` are gone

**A finding not in the original survey.** Verified by binary inspection: both extension methods exist
in `ShiftSoftware.ShiftEntity.dll` @ `2026.7.31.1` and are **absent** @ `2026.8.30.1`. They are
undocumented — they appear in no XML doc file at either version.

Four call sites in this group:

```
ADP.ClaimableItems.Data/AutoMapperProfiles/CampaignProfile.cs:48       .DefaultEntityToDtoAfterMap()
ADP.ClaimableItems.Data/AutoMapperProfiles/CampaignProfile.cs:53       .DefaultDtoToEntityAfterMap()
ADP.ClaimableItems.Data/AutoMapperProfiles/ClaimableItemProfile.cs:57  .DefaultEntityToDtoAfterMap()
ADP.ClaimableItems.Data/AutoMapperProfiles/ClaimableItemProfile.cs:60  .DefaultDtoToEntityAfterMap()
```

(Two more in `ADP.WarrantyClaims` — Step 05.)

The call sites disappear with the profiles, **but whatever behaviour they applied disappears with
them.** That behaviour is currently unknown and is applied to both `Campaign ↔ CampaignDTO` and
`ClaimableItem ↔ ClaimableItemDTO` in both directions.

- [ ] Read the implementation at the `2026.7.31.1` tag in the public framework repo
      (`github.com/ShiftSoftware/ShiftEntity`) and record exactly what each did.
- [ ] Determine whether the generator now applies the same behaviour by convention, or whether it
      must be re-added per triple.
- [ ] Record the finding in `STATUS.md`. **Do not assume "it was a no-op".** If it normalized
      hash-ids, stamped a member, or reconciled a collection, dropping it is a silent regression on
      four maps.

**Blocking for items D and E.**

### D. `CampaignRepository` — `Brands` / `Companies` / `Countries`

Old profile (`CampaignProfile.cs:44–53`):

```csharp
CreateMap<Campaign, CampaignDTO>()
    .ForMember(x => x.Brands,    MapFrom(y => y.Brands.Select(v => new ShiftEntitySelectDTO { Value = v.ToString() }).ToList()))
    .ForMember(x => x.Companies, ...same shape...)
    .ForMember(x => x.Countries, ...same shape...)
    .DefaultEntityToDtoAfterMap()
    .ReverseMap()
    .ForMember(x => x.Brands,    MapFrom(y => y.Brands.Select(s => s.Value.ToLong()).ToList()))
    .ForMember(x => x.Companies, ...same shape...)
    .ForMember(x => x.Countries, ...same shape...)
    .DefaultDtoToEntityAfterMap();
```

- [ ] **Forward: three `ForView` calls.** These are the three members `SHENGEN004` names. The source
      is a `List<long>` on the entity — **not a navigation** — so `MappingHelpers.ToSelectDTO` does
      not apply and there is no convention. Each becomes an explicit `ForView` building
      `ShiftEntitySelectDTO { Value = v.ToString() }`.
- [ ] **Check whether `Text` should now be populated.** The convention fills `Text` from a navigation
      where one exists; here there is none, so `Value`-only is correct — but state that in a comment
      so the next reader does not "fix" it (`conventions.md` §8).
- [ ] **Reverse: three `ForEntity` calls**, `s.Value.ToLong()` back into `List<long>`.
- [ ] **TRAP 2 check.** These are `long` lists, not link rows, so the classic PK-leak shape does not
      apply — **but verify from the emitted code** that nothing auto-composes a pair mapper over them
      and substitutes an `ID`.
- [ ] Apply the SPIKE-11 finding for both `DefaultEntityToDtoAfterMap` and
      `DefaultDtoToEntityAfterMap`.

### E. `ClaimableItemRepository` — `Costs` JSON round-trip

Old profile (`ClaimableItemProfile.cs:55–60`):

```csharp
CreateMap<ClaimableItem, ClaimableItemDTO>()
    .ForMember(x => x.Costs, MapFrom(y => JsonSerializer.Deserialize<List<ClaimableItemCostDTO>>(y.Costs, new JsonSerializerOptions { })))
    .DefaultEntityToDtoAfterMap()
    .ReverseMap()
    .ForMember(x => x.Costs, MapFrom(y => JsonSerializer.Serialize(y.Costs, new JsonSerializerOptions { })))
    .DefaultDtoToEntityAfterMap();
```

- [ ] **`Costs` → `ForView`** (the member `SHENGEN004` names) and **`ForEntity`** for the reverse.
- [ ] **Preserve the serializer options exactly.** Both directions use a bare
      `new JsonSerializerOptions { }` — i.e. **default** options, not the framework's configured ones.
      Substituting a different options instance changes property naming on the stored JSON and
      silently corrupts the column. Carry the default-options behaviour over verbatim.
- [ ] Note the asymmetry: the entity's `Costs` is a `string`, the DTO's is a `List<ClaimableItemCostDTO>`.
      `SHENGEN008` may fire on the name pairing; only suppress after proving it against this profile.
- [ ] Apply the SPIKE-11 finding.

### F. `CampaignVinEntryRepository` and `ItemClaimRepository`

- [ ] **`CampaignVinEntryListDTO.CampaignName` / `.CampaignUniqueReference` → two `ForList` calls.**
      Both reach through the `Campaign` navigation
      (`s.Campaign != null ? s.Campaign.Name : null`) — a flattening the convention does not derive.
      Must stay EF-translatable; both are.
- [ ] **`CampaignVinEntryDTO.DisableVinValidation` → `ForView` or `IgnoreView`.** Named by
      `SHENGEN004`. Determine whether it has an entity source at all; if it is a pure client-side
      flag, `IgnoreView` plus `ForEntity` may be right. **Check whether it is currently written on
      the reverse path** — if the old profile never wrote it and the convention now does, that is
      trap 3-write appearing without an `Ignore()` to warn you.
- [ ] **`ItemClaimListDTO.HasAttachment` → `ForList`**, preserving
      `Attachments == null || Attachments == "[]" ? No : Yes` exactly. Note the `"[]"` literal — an
      empty JSON array counts as *no* attachment. Dropping that comparison flips the value for every
      row with an empty array.
- [ ] **`ItemClaimDTO.CampaignVINEntry` and `.ReSubmitForDistributorReview` → `ForView` /
      `IgnoreView`.** Both named by `SHENGEN004`, and **neither has a `CreateMap` entry in the old
      profile** — so they are populated today by something else (a repository override, or they are
      genuinely unpopulated). Find out which before writing anything. This is the case
      `conventions.md` §2 warns about: triples with no `CreateMap` still need mappers.

### G. Rewrite the 5 Cosmos replication delegates — **compile break**

`ADP.ClaimableItems/ADP.ClaimableItems.Data/Extensions/ClaimableItemsReplicationExtensions.cs`:

| Line | Call |
|---|---|
| 30 | `Replicate<ServiceItemModel>` |
| 41 | `Replicate<ServiceCampaignModel>` |
| 45 | `UpdateReference<ServiceItemModel>` |
| 55 | `Replicate<CampaignVinEntryModel>` |
| 83 | `Replicate<ItemClaimModel>` |

All five pass **no** `mapping` delegate today and rely on the removed AutoMapper fallback. The
parameter is now **required**.

- [ ] Transcribe each delegate from the corresponding profile map, which is its specification:
      - `Campaign → ServiceCampaignModel` (`CampaignProfile.cs:13–22`)
      - `Campaign → ServiceItemModel` (`CampaignProfile.cs:25–43`)
      - `ClaimableItem → ServiceItemModel` (`ClaimableItemProfile.cs:16–52`, ~25 members — the
        largest)
      - `CampaignVinEntry → CampaignVinEntryModel` (`CampaignVinEntryProfile.cs:15`)
      - `ItemClaim → ItemClaimModel` (`ItemClaimProfile.cs:25`, a `ConvertUsing`)
- [ ] **`ItemClaimModel.id` is a frozen production document-identity contract.** The profile's own
      doc comment says it is byte-frozen and, critically, that it **includes `CampaignVinEntryID`
      while the SQL unique hash does not**. Transcribe the composite id **character for character**,
      including field order and separators. Changing it re-keys live Cosmos documents.
- [ ] Preserve the null-conditional `.ToString()` shapes exactly
      (`x.VehicleInspectionResultID == null ? null : x.VehicleInspectionResultID.ToString()`), and
      `Cost = x.Cost ?? 0m`.
- [ ] `DeserializeDict` is used by the `Name` / `CampaignName` / `PrintoutTitle` /
      `PrintoutDescription` projections — hence keeping the helper in item B.
- [ ] Copy the delegate *shape* from the reference implementations in
      `ADP.Menus/ADP.Menus.Sync/Extensions/MenuReplicationExtensions.cs` and
      `Replication/MenuCatchUpReplicationExtensions.cs`. **Note these live in `.Sync`, not `.Data`**
      — the recipe's implication otherwise is wrong.

> **These delegates have zero harness coverage.** Replication is disabled during parity runs, and
> failures on this path are swallowed by a catch — they surface as permanently-dirty rows under a
> clean watermark, never as an exception or an HTTP error. **Review them line-by-line against the old
> profile; the harness will not save you here.** (`verification.md` §8.8)

### H. Port the `IMapper` call site — **compile break**

`ADP.ClaimableItems/ADP.ClaimableItems.Data/Repositories/ItemClaimCertificateRepository.cs`:

- Field at line 33, constructor parameter at line 38, and one call at line 74:
  `dto.ReimbursementItemClaims = mapper.Map<List<ItemClaimListDTO>>(claims);`

- [ ] Replace with a `[ShiftEntityMapper] partial class : IShiftObjectMapper<ItemClaim, ItemClaimListDTO>`,
      or route through the `ItemClaim` triple's generated list mapper.
- [ ] Remove the `AutoMapper.IMapper` field and constructor parameter, and check every construction
      site and DI registration for that repository.
- [ ] This is the `ViewAsync` override that populates `ReimbursementItemClaims` — one of the two
      members `SHENGEN004` reports as unmapped on the `Certificate` triple. **The unmapped warning is
      expected and correct here**: the mapper does not fill it, the repository does. Do not "fix" the
      warning by adding a `ForView` that duplicates the repository's work.

### I. Prose sweep

- [ ] `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj:16` —
      `<Description>` says "AutoMapper profiles".
- [ ] `grep -rn "AutoMapper\|AfterMap\|mapping profile" ADP.ClaimableItems --include=*.cs --include=*.csproj --include=*.md`
      and fix each hit, including the profile doc comments' references now that the profiles are gone.

### J. Emit and audit

```bash
rm -rf ADP.ClaimableItems/ADP.ClaimableItems.Data/obj
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data -p:EmitCompilerGeneratedFiles=true
```

- [ ] Read the **newest-timestamped** file per triple and per pair (`conventions.md` §4).
- [ ] **Trap 1:** check every auto-composed collection in `MapToViewGenerated` and
      `__shiftListProjection` for a missing `IsDeleted` predicate — even though the profiles had none,
      because a collection composed for the first time is exactly where this appears.
- [ ] **Trap 2:** check every pair mapper for `dto.ID = source.ID.ToString();` where the DTO's `ID`
      should carry a foreign id.
- [ ] **Trap 3-write:** diff `MapToEntityGenerated` against each old reverse map. The profiles had no
      `Ignore()`, so any `existing.X = …` line whose member the old reverse map did not write is new
      behaviour — `DisableVinValidation` is the specific one to check (item F).
- [ ] Confirm `__shiftBakedCustom` lists every member you configured.
- [ ] Resolve the 5 baseline `SHENGEN004`s — except the `Certificate` triple's, where
      `ReimbursementItemClaims` is repository-populated by design (item H) and `Notes` needs its own
      determination.

---

## Verification

```bash
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.API
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Web
dotnet build                                  # the whole solution — this step ends green
```

```powershell
.\tools\parity.ps1 verify -Group ClaimableItems
.\tools\parity.ps1 verify -Group ClaimableItems -Grant Restricted
```

**Group-specific caveats — these limit what a green run means.**

- **No sample host exists**, and if the mounted host cannot boot this group the fallback is a real
  sample API or per-triple mapper goldens — not a dead end. See Preconditions.
- **No sample host exists.** This runs on the **mounted host**, which boots the module through its own
  `AddClaimableItemsApiServices<ParityDb>(...)` entry point. That is the module's real mounting API,
  **but it does not reproduce a consumer's middleware order, request localization, CORS, fallback
  routing or JSON option overrides.** A behaviour change hiding in host wiring rather than in the
  module **will not be caught**. Record this step's result as *module-level* parity, not full
  endpoint parity (`verification.md` §6).
- **Needs SQL.** Cosmos must stay unconfigured — which also removes replication side effects from the
  write-path cases.
- **The 5 replication delegates are not covered at all.** Their verification is item G's line-by-line
  review plus, if available, a manual Cosmos round-trip in a scratch environment.
- `ADP.ClaimableItems.Web` has 14 pages inheriting the obsolete `ShiftForm<,>` base across this group
  and Menus. They keep compiling with the same warning they already emit — **not upgrade work**, but
  worth a backlog item.

---

## Exit criteria

- [ ] `AutoMapperProfiles/` contains no `Profile` class; `DeserializeDict` survives in a sensible
      namespace and still compiles.
- [ ] `grep -rn "AutoMapper" ADP.ClaimableItems` returns zero hits in `.cs`, `.csproj`, `.md`.
- [ ] This group's **seven** package lines all read `2026.8.30.1`; `TypeAuth` untouched at `1.6.28`;
      `ADP.Models`, `ADP.Cases` and `Lookup.Services.DuckDB` untouched (Step 06 owns those four).
- [ ] All four projects build clean **and the solution builds green** — `dotnet build` at the repo
      root, zero errors. Nothing is left red for a later step to pick up.
- [ ] **Zero** `SHENGEN007`, `008`, `010`. Every baseline `SHENGEN004` is either resolved or carries a
      justification block naming each member (`Certificate.ReimbursementItemClaims` is the known
      legitimate case).
- [ ] Emitted-code audit (item J) complete: traps 1, 2 and 3-write each explicitly checked and the
      finding recorded, not merely "no warnings".
- [ ] All 5 replication delegates written and **reviewed line-by-line against the old profile maps**,
      with the `ItemClaimModel.id` composite verified character-for-character.
- [ ] `ItemClaimCertificateRepository` no longer references `AutoMapper.IMapper`; the repository's
      construction sites and DI registration are updated.
- [ ] SPIKE-11 `RESOLVED` with the recorded behaviour of both `Default*AfterMap` helpers, and that
      behaviour either reproduced or explicitly determined unnecessary.
- [ ] SPIKE-8 `RESOLVED` — the `Certificate` mapper's assembly and coexistence answered from emitted
      code, **plus** the added question: whether the generator is content with an entity whose owning
      assembly (`ADP.Cases`) is still on `2026.7.31.1`. Step 05 reads this answer as its precedent.
- [ ] `parity.ps1 verify -Group ClaimableItems` — every diff explained or accepted with a reason.
- [ ] Restricted-grant pass also run, against the restricted baseline captured in Step 00.
- [ ] Every triple has `httpWriteReachable` recorded; each `false` carries a mapper-level write
      golden instead (`verification.md` §5).
- [ ] `CREATE 2xx` at 100% for this group, or each shortfall listed in `writeUnreachable` with a
      reason. A group whose creates all 4xx has tested trap 3-write nowhere.
- [ ] `STATUS.md` `VERIFIED`, with `Verified by` naming the report **and** the mounted-host caveat.
- [ ] No BOM churn.

---

## Rollback

Revert this step's commits. The group is a leaf **and the shared floor is untouched**, so nothing
outside `ADP.ClaimableItems` moves when you do.

Commit shape, the same as Step 05's: **the item A bump alone first** — it does not compile, but an
isolated bump commit is what lets a bisect separate "the version moved" from "the mappers changed" —
then the profile deletion and the repository rewrites in a second commit, the replication delegates
in a third (they are separately reviewable and separately risky), and the prose sweep in a fourth.
Because the bump commit is red on its own, **no single commit here is independently revertible**:
revert **from the top**, as a set. Reverting the whole step returns this group to `2026.7.31.1` on a
floor that is still `2026.7.31.1`.

---

## Effort & risk

**Effort:** the largest migration step. The 5 replication delegates are roughly half of it, and the
`ClaimableItem → ServiceItemModel` map alone is ~25 members. The seven-line bump (item A) is minutes;
it is the work it uncovers that costs.

**Risks:**

| Risk | Mitigation |
|---|---|
| **The frozen Cosmos document id is altered**, re-keying live documents | Item G: transcribe character-for-character; the profile's own comment flags it as byte-frozen and notes the `CampaignVinEntryID` asymmetry |
| **Replication delegates are wrong and nothing detects it** — failures are swallowed, surfacing as dirty rows under a clean watermark | Line-by-line review is the only control. Stated twice. Consider a scratch-environment Cosmos round-trip. |
| **SPIKE-11's dropped `Default*AfterMap` behaviour is silently lost** on four maps | Blocking spike; must be resolved before items D and E |
| `Costs` JSON serializer options substituted, corrupting the stored column | Item E: default options carried over verbatim |
| **Trap 2 in the select-DTO shuttling** | Emitted-pair audit in item J; this is the group's main mapper risk |
| Mounted host cannot boot → no verification at all | SPIKE-2, resolved at Step 00. Two named fallbacks (a ~200-line sample API, or per-triple mapper goldens with the reduced claim written down) — this group is **not** allowed to end at `BLOCKED` with nothing after it |
| SPIKE-8 has no earlier owner now that the shared floor moved to Step 06 | This step owns it outright as item 0. The probe needs only `ADP.ClaimableItems.Data` to compile, which it does at this step's baseline and again at its green end — no window where it is unreachable |
| The `Certificate` triple is generated while its owning assembly `ADP.Cases` is still on `2026.7.31.1` | Folded into SPIKE-8's probe. `ADP.Menus.Shared` on 8.30.1 over `ADP.Models` on 7.31.1 already shows a mixed floor compiling; if the *generator* objects, that is a plan-level finding for `STATUS.md`, not a quiet early bump of Step 06's lines |
| The bump leaves the group red mid-step and someone merges it | Item A's callout: green is a step boundary, not a per-commit one. Intermediate commits stay on the step branch; the step is unfinished until the solution build is clean |
| A green mounted-host run is read as full endpoint parity | Caveat stated in Verification and required in the exit criteria wording |

# Step 06 — `ADP.ClaimableItems`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** migrate 5 triples off 4 AutoMapper profiles, hand-write 5 Cosmos replication delegates, and
port 1 `IMapper` call site — all without a runnable host.

---

## Projects touched

| Path | Bumped line |
|---|---|
| `ADP.ClaimableItems/ADP.ClaimableItems.API/ADP.ClaimableItems.API.csproj` | `ShiftEntity.Web` (32) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj` | `ShiftEntity.EFCore` (48), `CosmosDbReplication` (49), `ShiftEntity.Print` (52), `ShiftIdentity.Core` (53) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Shared/ADP.ClaimableItems.Shared.csproj` | `ShiftEntity.Model` (34) |
| `ADP.ClaimableItems/ADP.ClaimableItems.Web/ADP.ClaimableItems.Web.csproj` | `ShiftBlazor` (34) |

---

## Preconditions

- Step 03 `CLOSED` (this group genuinely depends on it: `.Data:58`, `.Shared:40`, `.Web:38` →
  `ADP.Models`; `.Data:43-44` → `ADP.Cases.{Shared,Data}`; `.API:38` → `Lookup.Services`),
  Step 04 `CLOSED`, **Step 05 `VERIFIED`.** The mapper recipe must be proven under full HTTP parity
  (Surveys) before being applied where only a mounted host exists.
- **SPIKE-2 resolved** — the mounted host must actually boot this group. There is **no sample host**
  for ClaimableItems. If the mounted host does not work, take the named fallback rather than stopping:
  either write `ADP.ClaimableItems/samples/ADP.ClaimableItems.Sample.API` on the ~200-line Surveys
  pattern, or fall back to **per-triple mapper-level goldens** with the reduced claim recorded
  verbatim — *"mapper output verified at the type level; no HTTP surface was exercised"*
  (`verification.md` §6). This group's trap tally puts it in trap-2 territory; it is the group least
  able to afford no verification at all, so `BLOCKED` with nothing after it is not an outcome.
- **SPIKE-8 `RESOLVED`, or `BLOCKED`-and-deferred from Step 03** — in which case **resolve it as item
  0 of this step**, using the same emitted-code probe now that `ADP.ClaimableItems.Data` compiles.
  Step 03 is explicitly permitted to defer it (the probe builds a project Step 02 leaves red), so
  writing "SPIKE-8 resolved" as a hard precondition made this step unstartable on the permitted path.
- **SPIKE-9 resolved** — the exact `Replicate<T>` delegate signature at `2026.8.30.1`. **Answered in
  Step 01** from the ~19 already-migrated call sites in `ADP.Menus.Sync`; if that did not happen, do
  it now before item F, not during it.
- **SPIKE-11 resolved** — see item B; blocking for two of the four profiles.

**Why here:** this group and WarrantyClaims are both free to move relative to each other, so the tie
goes to **risk, then simplicity**. ClaimableItems is *harder* than Surveys (4 profiles vs 1, no host,
5 replication delegates) but carries no known data-**exposure** hazard, while WarrantyClaims does —
so ClaimableItems goes first even though it has more profiles. Simplicity would have ordered these
two the other way round; risk overrides it (`README.md` §2).

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
| 2 — link-row PK leak | **suspected** | heavy `ShiftEntitySelectDTO` shuttling (`Brands`/`Companies`/`Countries` built from `long` lists). This is the group's main risk — see item C. |
| 3-write — reverse-map `Ignore()` | **0** | verified: no `Ignore()` on any reverse map in this group |
| 3-read — forward-map `Ignore()` | **0** | verified |

The absence of `Ignore()` anywhere makes trap 3 a non-issue here — which is why this group ranks
below WarrantyClaims in risk despite having more profiles.

---

## Work items

### A. Delete the profiles and the registration call

- [ ] Delete `CampaignProfile.cs`, `ClaimableItemProfile.cs`, `CampaignVinEntryProfile.cs`,
      `ItemClaimProfile.cs`.
- [ ] **Keep `GeneralMappingHelper.DeserializeDict`** — move it to a non-profile namespace (it is
      used by the Cosmos projections in item F, which survive). Do not delete the folder blindly.
- [ ] `ADP.ClaimableItems/ADP.ClaimableItems.API/Extensions/ClaimableItemsApiExtensions.cs:51` —
      delete `o.AddAutoMapper(typeof(DataMarker).Assembly);`, keep `o.AddDataAssembly(...)`, rewrite
      the surrounding comment.

### B. Resolve SPIKE-11 — `DefaultEntityToDtoAfterMap` / `DefaultDtoToEntityAfterMap` are gone

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

(Two more in `ADP.WarrantyClaims` — Step 07.)

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

**Blocking for items C and D.**

### C. `CampaignRepository` — `Brands` / `Companies` / `Countries`

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

### D. `ClaimableItemRepository` — `Costs` JSON round-trip

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

### E. `CampaignVinEntryRepository` and `ItemClaimRepository`

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

### F. Rewrite the 5 Cosmos replication delegates — **compile break**

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
      `PrintoutDescription` projections — hence keeping the helper in item A.
- [ ] Copy the delegate *shape* from the reference implementations in
      `ADP.Menus/ADP.Menus.Sync/Extensions/MenuReplicationExtensions.cs` and
      `Replication/MenuCatchUpReplicationExtensions.cs`. **Note these live in `.Sync`, not `.Data`**
      — the recipe's implication otherwise is wrong.

> **These delegates have zero harness coverage.** Replication is disabled during parity runs, and
> failures on this path are swallowed by a catch — they surface as permanently-dirty rows under a
> clean watermark, never as an exception or an HTTP error. **Review them line-by-line against the old
> profile; the harness will not save you here.** (`verification.md` §8.8)

### G. Port the `IMapper` call site — **compile break**

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

### H. Prose sweep

- [ ] `ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj:16` —
      `<Description>` says "AutoMapper profiles".
- [ ] `grep -rn "AutoMapper\|AfterMap\|mapping profile" ADP.ClaimableItems --include=*.cs --include=*.csproj --include=*.md`
      and fix each hit, including the profile doc comments' references now that the profiles are gone.

### I. Emit and audit

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
      behaviour — `DisableVinValidation` is the specific one to check (item E).
- [ ] Confirm `__shiftBakedCustom` lists every member you configured.
- [ ] Resolve the 5 baseline `SHENGEN004`s — except the `Certificate` triple's, where
      `ReimbursementItemClaims` is repository-populated by design (item G) and `Notes` needs its own
      determination.

---

## Verification

```bash
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Data
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.API
dotnet build ADP.ClaimableItems/ADP.ClaimableItems.Web
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
- **The 5 replication delegates are not covered at all.** Their verification is item F's line-by-line
  review plus, if available, a manual Cosmos round-trip in a scratch environment.
- `ADP.ClaimableItems.Web` has 14 pages inheriting the obsolete `ShiftForm<,>` base across this group
  and Menus. They keep compiling with the same warning they already emit — **not upgrade work**, but
  worth a backlog item.

---

## Exit criteria

- [ ] `AutoMapperProfiles/` contains no `Profile` class; `DeserializeDict` survives in a sensible
      namespace and still compiles.
- [ ] `grep -rn "AutoMapper" ADP.ClaimableItems` returns zero hits in `.cs`, `.csproj`, `.md`.
- [ ] All four projects build clean.
- [ ] **Zero** `SHENGEN007`, `008`, `010`. Every baseline `SHENGEN004` is either resolved or carries a
      justification block naming each member (`Certificate.ReimbursementItemClaims` is the known
      legitimate case).
- [ ] Emitted-code audit (item I) complete: traps 1, 2 and 3-write each explicitly checked and the
      finding recorded, not merely "no warnings".
- [ ] All 5 replication delegates written and **reviewed line-by-line against the old profile maps**,
      with the `ItemClaimModel.id` composite verified character-for-character.
- [ ] `ItemClaimCertificateRepository` no longer references `AutoMapper.IMapper`; the repository's
      construction sites and DI registration are updated.
- [ ] SPIKE-11 `RESOLVED` with the recorded behaviour of both `Default*AfterMap` helpers, and that
      behaviour either reproduced or explicitly determined unnecessary.
- [ ] SPIKE-8 `RESOLVED` — the `Certificate` mapper's assembly and coexistence answered from emitted
      code.
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

Revert this step's commits. The group is a leaf. Keep the migration in one commit, the replication
delegates in a second (they are separately reviewable and separately risky), and the prose sweep in a
third.

---

## Effort & risk

**Effort:** the largest migration step. The 5 replication delegates are roughly half of it, and the
`ClaimableItem → ServiceItemModel` map alone is ~25 members.

**Risks:**

| Risk | Mitigation |
|---|---|
| **The frozen Cosmos document id is altered**, re-keying live documents | Item F: transcribe character-for-character; the profile's own comment flags it as byte-frozen and notes the `CampaignVinEntryID` asymmetry |
| **Replication delegates are wrong and nothing detects it** — failures are swallowed, surfacing as dirty rows under a clean watermark | Line-by-line review is the only control. Stated twice. Consider a scratch-environment Cosmos round-trip. |
| **SPIKE-11's dropped `Default*AfterMap` behaviour is silently lost** on four maps | Blocking spike; must be resolved before items C and D |
| `Costs` JSON serializer options substituted, corrupting the stored column | Item D: default options carried over verbatim |
| **Trap 2 in the select-DTO shuttling** | Emitted-pair audit in item I; this is the group's main mapper risk |
| Mounted host cannot boot → no verification at all | SPIKE-2, resolved at Step 00. Two named fallbacks (a ~200-line sample API, or per-triple mapper goldens with the reduced claim written down) — this group is **not** allowed to end at `BLOCKED` with nothing after it |
| SPIKE-8 was deferred from Step 03 and this step treats it as an unmet precondition | Preconditions accept the deferral explicitly and make it item 0 here |
| A green mounted-host run is read as full endpoint parity | Caveat stated in Verification and required in the exit criteria wording |

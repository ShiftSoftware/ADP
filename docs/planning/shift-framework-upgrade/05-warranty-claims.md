# Step 05 — `ADP.WarrantyClaims`

**Status:** `NOT STARTED` (authoritative value lives in `STATUS.md`)
**Terminal status:** `VERIFIED`

**Goal:** bump this group's 7 package references, migrate 7 triples off 2 profiles — and close the
**data-exposure hazard** that makes this the highest-risk group in the repo.

> **Read §"The hazard" before doing anything else in this step.**

---

## Projects touched

| Path | Bumped line |
|---|---|
| `ADP.WarrantyClaims/ADP.WarrantyClaims.API/ADP.WarrantyClaims.API.csproj` | `ShiftEntity.Web` (32) |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj` | `ShiftEntity.EFCore` (48), `CosmosDbReplication` (49), `ShiftEntity.Print` (52), `ShiftIdentity.Core` (53) |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Shared/ADP.WarrantyClaims.Shared.csproj` | `ShiftEntity.Model` (33) |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Web/ADP.WarrantyClaims.Web.csproj` | `ShiftBlazor` (34) |

Those seven `PackageReference` lines are **this step's own first commit** (item A). No other step
touches them — there is no solution-wide version bump anywhere in this plan.

---

## Preconditions

- Step 00 `CLOSED`, Step 01 `VERIFIED` — the baseline recording and the parity harness must exist.
- **Step 04 (`ADP.ClaimableItems`) `VERIFIED` — a *knowledge* dependency, not a build one.** Nothing
  in `ADP.WarrantyClaims` references `ADP.ClaimableItems`; the two groups are independent at compile
  time. What this step takes from Step 04 is the shared `Certificate` mapper precedent (SPIKE-8), the
  replication-delegate approach, and the SPIKE-11 finding. Run out of order and this group still
  compiles — it would just be solving all three of those for the first time, on the riskiest group.
- **This step bumps its own package references** (item A, first commit). It does not inherit a
  half-upgraded tree from anywhere: the solution is green when this step starts and green when it
  ends.
- **SPIKE-2 resolved** — no sample host exists for this group either.
- **SPIKE-7 resolved** — `IgnoreList` must be proven to bake, from emitted code. This is the fix for
  the hazard below; if it does not work, the whole step is `BLOCKED`.
- **SPIKE-8, SPIKE-9, SPIKE-11** resolved — SPIKE-9 in Step 01, SPIKE-8 and SPIKE-11 in Step 04
  (which carries 4 of SPIKE-11's 6 call sites; the remaining 2 are here, item H).
- **The WarrantyClaims seed must contain a claim with all five distributor-side members non-null**
  (Step 00 item D). Without it the hazard is invisible to the harness.

**Why last of the group migrations: risk, explicitly overriding simplicity.** By profile count this
group is *simpler* than ClaimableItems — 2 profiles against 4 — so a simplicity-only rule would swap
the two and run this at 04. It goes last of the four group steps because it is the only group
carrying a data-**exposure** hazard. That is level 2 of the ordering rule doing real work
(`README.md` §2). It also benefits from Step 04 having already established the shared `Certificate`
mapper pattern and the replication-delegate approach.

---

## The hazard

`ADP.WarrantyClaims/ADP.WarrantyClaims.Shared/DTOs/Financial/DealerFinancialListDTO.cs`:

```csharp
public class DealerFinancialListDTO : DistributorFinancialListDTO { }   // literally empty
```

`ADP.WarrantyClaims/ADP.WarrantyClaims.Data/AutoMapperProfiles/Financial.cs:34–49`:

```csharp
// The Dealer map keeps its Ignore list EXACTLY as before (dealers never see the
// distributor-side figures); the ignored members stay null even though the entity
// carries values.
CreateMap<Entities.WarrantyClaim, DealerFinancialListDTO>()
    .ForMember(x => x.ProcessDate,                  ...)
    .ForMember(x => x.DistributorProcessDate,       ...)
    .ForMember(x => x.ReferenceWarrantyClaimNumber, ...)
    .ForMember(x => x.DistComment1,                 x => x.Ignore())
    .ForMember(x => x.HourTotalDistributor,         x => x.Ignore())
    .ForMember(x => x.LaborTotalAmountDistributor,  x => x.Ignore())
    .ForMember(x => x.SubletTotalAmountDistributor, x => x.Ignore())
    .ForMember(x => x.PartsTotalAmountDistributor,  x => x.Ignore());
```

**The dealer DTO is the distributor DTO with five fields blanked by the mapper, and nothing else.**
The subclass adds nothing. All five members exist on the entity under the same names, so **under
name-convention generation they will be populated.**

Both are real repository triples over the same entity:

```
DealerFinancialRepository      : ShiftRepository<ShiftDbContext, WarrantyClaim, DealerFinancialListDTO,      WarrantyClaimDTO>
DistributorFinancialRepository : ShiftRepository<ShiftDbContext, WarrantyClaim, DistributorFinancialListDTO, WarrantyClaimDTO>
```

This is **trap 3-read** (`conventions.md` §5), and it behaves as follows:

| | |
|---|---|
| Status code | **200** |
| Response shape | **identical** |
| Compiler diagnostic | **none** — `SHENGEN008` will not fire (the member *is* mapped now); `SHENGEN004`/`007` will not fire (nothing is unmapped) |
| Effect | distributor-side financial figures served on a plain `GET` to a lower-privilege audience |

It is not data *loss*, it is data *exposure*.

### How it is detected — corrected

An earlier draft of this file said the exposure is invisible under a full-access token and that only
a restricted-grant pass could catch it. **That is wrong, and believing it would make you discount the
strongest signal the harness produces for the highest-risk item in the plan.**

The dealer view is **not** a privilege-filtered projection of the distributor view. It is a separate
controller, on its own route, with its own DTO:

- `ADP.WarrantyClaims.API/Controllers/DealerFinancialController.cs:21-22` —
  `[Route("[controller]")]`, `ShiftEntitySecureControllerAsync<DealerFinancialRepository,
  WarrantyClaim, DealerFinancialListDTO, WarrantyClaimDTO>`.
- Its only gate (`Get` override, lines 35-42) is
  `!typeAuthService.CanRead(options.Value.WarrantyClaimFinancialAction) → Unauthorized()`. **A
  full-access principal passes that check.**
- `DealerFinancialRepository` is bare `base(db)` — no `FilterByTypeAuthValues`, so no row scoping
  either.

So `GET /DealerFinancial` **under the ordinary full-access pass** returns the five members `null` in
the baseline and **populated** post-upgrade: a plain value diff, provided the seed contains a claim
with those five entity columns non-null — which Step 00 item D already requires.

**The restricted pass stays mandatory**, but for what it is actually good at: an independent control
on the dealer/distributor split, and coverage of surfaces that *are* genuinely row-scoped (the
pattern to compare against is `MenuRepository.cs:23-26`'s `FilterByTypeAuthValues`). Treat it as the
second of four controls, not the only one.

The fix exists: `ShiftMapperBuilder` exposes `IgnoreList` alongside `IgnoreEntity`. It has to be
*found* and *proven to bake* — hence SPIKE-7.

---

## The survey

**Two profiles in `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/AutoMapperProfiles/`** (150 lines):

| File | Lines | Contains |
|---|---|---|
| `Financial.cs` | 50 | `WarrantyClaim → DistributorFinancialListDTO`, `WarrantyClaim → DealerFinancialListDTO` |
| `WarrantyClaim.cs` | 100 | `WarrantyClaim → WarrantyClaimListDTO`, `WarrantyClaim → WarrantyCertificateLineDTO`, `CertificateDTO ↔ Certificate`, three line-item pairs, `WarrantyClaim → WarrantyClaimModel` (Cosmos) |

**Seven triples**, all in `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Repositories/`:

| Repository | Triple |
|---|---|
| `WarrantyClaimRepository.cs` | `WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO` |
| `DealerFinancialRepository.cs` | `WarrantyClaim, DealerFinancialListDTO, WarrantyClaimDTO` |
| `DistributorFinancialRepository.cs` | `WarrantyClaim, DistributorFinancialListDTO, WarrantyClaimDTO` |
| `WarrantyCertificateRepository.cs` | `Certificate, CertificateListDTO, CertificateDTO` — **entity owned by `ADP.Cases`** |
| `WarrantyRatesRepository.cs` | `WarrantyRates, WarrantyRatesListDTO, WarrantyRatesDTO` |
| `AdditionalLaborOperationCodeRepository.cs` | `AdditionalLaborOperationCode, …ListDTO, …DTO` |
| `ManufacturerSettlmentSheetRepository.cs` | `ManufacturerSettlmentSheet, …ListDTO, …DTO` |

**Three triples share the `WarrantyClaim` entity**, differing only in list DTO, all with
`WarrantyClaimDTO` as the view DTO. That is unusual and is the structural root of the hazard.

**Baseline diagnostics — 2 `SHENGEN004`:**

```
Generated_Certificate_CertificateListDTO_CertificateDTO_df5083c3            does not map: WarrantyClaims, Notes
Generated_Pair_WarrantyClaimPartLine_WarrantyClaimPartLineDTO_7551e8fe      does not map: Loading
```

**Trap tally:**

| Trap | Count | Where |
|---|---|---|
| 1 — soft-delete filtering | **0 found** in the profiles | verify against emitted code regardless |
| 2 — link-row PK leak | **suspected** | three line-item pairs with `.ReverseMap()` — see item F |
| 3-write — reverse-map `Ignore()` | **0** | verified |
| **3-read — forward-map `Ignore()`** | **5** | **the hazard.** `Financial.cs:45–49` |

---

## Work items

### A. Bump this group's package references

Seven lines, `2026.7.31.1` → `2026.8.30.1`. This is the step's first commit and it is mechanical.

| csproj | Line | Package |
|---|---|---|
| `ADP.WarrantyClaims/ADP.WarrantyClaims.API/ADP.WarrantyClaims.API.csproj` | 32 | `ShiftEntity.Web` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj` | 48 | `ShiftEntity.EFCore` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj` | 49 | `ShiftEntity.CosmosDbReplication` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj` | 52 | `ShiftEntity.Print` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj` | 53 | `ShiftIdentity.Core` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Shared/ADP.WarrantyClaims.Shared.csproj` | 33 | `ShiftEntity.Model` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Web/ADP.WarrantyClaims.Web.csproj` | 34 | `ShiftBlazor` |

- [ ] Edit exactly those seven. **`TypeAuth` stays at `1.6.28`** — separate version line, no bump.
- [ ] `ADP.WarrantyClaims.Shared:33` is one of only three projects carrying **both** an `ADP.Models`
      project reference and their own direct `ShiftEntity.Model` pin. Bumping it here, *before* the
      shared floor moves in Step 06, lifts this project's pin **above** `ADP.Models`' floor — an
      upgrade, which resolves silently. `NU1605` is a **downgrade** diagnostic, so on this ordering
      no window for it ever opens. (Shared-first is the ordering that produces it.)
- [ ] Commit the seven lines alone, before any mapper work, so a bisect can separate "the version
      moved" from "the mappers changed". **This intermediate commit does not compile** — the two
      `Default*AfterMap` call sites at `WarrantyClaim.cs:51,53` are absent at `2026.8.30.1`
      (SPIKE-11). That is an intra-step state, not the step's outcome; items B–J end it green.
- [ ] `dotnet build` from the repo root once the step's code work is done — not just this group's
      four projects. The other groups and the still-unbumped shared floor must keep building around
      this change.

### B. Close the hazard first — `DealerFinancialRepository`

Do this before any other mapper work in the step — immediately after item A's bump commit — so it is
never left to the end.

- [ ] **Resolve SPIKE-7.** Add the five `IgnoreList` calls to `DealerFinancialRepository`'s
      `UseGeneratedMapper`, build with `-p:EmitCompilerGeneratedFiles=true`, and **prove from the
      emitted `Generated_WarrantyClaim_DealerFinancialListDTO_WarrantyClaimDTO_*.g.cs`** that:
      - `__shiftBakedIgnored` contains all five member names, and
      - `__shiftListProjection` contains **no** assignment for any of them.
      A green build is not proof. If `IgnoreList` does not bake, **stop** — the step is `BLOCKED` and
      the group must not ship.
- [ ] Add the five calls:
      `IgnoreList(d => d.DistComment1)`, `.HourTotalDistributor`, `.LaborTotalAmountDistributor`,
      `.SubletTotalAmountDistributor`, `.PartsTotalAmountDistributor`.
- [ ] Comment them with the **reason**, not the mechanism: these five are withheld from the dealer
      audience; the entity carries values and the mapper is the only thing keeping them out of the
      response.
- [ ] **Add a standalone regression test** asserting all five are null on a dealer list response for
      a claim whose entity has all five populated. This is both the proof and the permanent guard.
      It must fail if any future change re-populates them.
- [ ] **Confirm the three shared members are still mapped on the dealer DTO** — `ProcessDate`,
      `DistributorProcessDate`, `ReferenceWarrantyClaimNumber` are configured on *both* maps.
      Blanking too much is as wrong as blanking too little.
- [ ] **Audit `DistributorFinancialListDTO`'s own members** for anything else that differs between
      the two maps. The two `CreateMap`s are otherwise identical; the exit criteria require that
      confirmed, not assumed.

### C. `DistributorFinancialRepository`

Same three `ForMember`s, no `Ignore()`:

- [ ] **`ProcessDate` and `DistributorProcessDate` → `ForList`.** Both perform a hand-written
      `DateTime → DateTimeOffset` conversion:
      `y.ProcessDate.HasValue ? new DateTimeOffset(y.ProcessDate.Value, TimeSpan.Zero) : null`.
      **Reproduce the `TimeSpan.Zero` offset exactly.** If the generator's conversion differs in
      offset, kind or precision, every timestamp on these lists shifts — and `verification.md`
      Rule 2 deliberately compares these literally, so it will show.
- [ ] **`ReferenceWarrantyClaimNumber` → `ForList`**, `y.ReferenceWarrantyClaim!.ClaimNumber`. The
      profile comment records that this member no longer decomposes to a valid navigation +
      property path by convention, so the flattening is pinned explicitly. **It must stay pinned** —
      if the convention cannot derive it, omitting the `ForList` leaves the column empty
      (`SHENGEN007` should fire; do not rely on that alone).
- [ ] Both must stay EF-translatable.

### D. `WarrantyClaimRepository` — `WarrantyClaimListDTO`

From `WarrantyClaim.cs:17–37`:

- [ ] **`ProcessDate`, `DistributorProcessDate` → `ForList`**, same `TimeSpan.Zero` conversion as
      item C.
- [ ] **`HasAttachment` → `ForList`**, preserving
      `Attachments == null || Attachments == "[]" ? No : Yes`. Note the `"[]"` literal — an empty
      JSON array counts as *no* attachment.
- [ ] **`ReferenceWarrantyClaimNumber` → `ForList`**, same pinned flattening as item C.
- [ ] Note the source type is `Entities.WarrantyClaim?` (nullable) in this `CreateMap` — check the
      generated projection handles the same nullability without throwing.

### E. `WarrantyCertificateRepository` — the shared `Certificate` entity

- [ ] Apply the SPIKE-8 finding from Step 04. This is the **second** triple over
      `ADP.Cases`' `Certificate`, with a different view DTO from ClaimableItems'.
- [ ] `SHENGEN004` reports `WarrantyClaims` and `Notes` unmapped. `WarrantyClaims` is populated by
      the repository's `ViewAsync` override, not the mapper — the profile comment says the
      entity→DTO name-match that used to fill it "no longer has a source". **The warning is expected
      and correct.** Do not add a `ForView` duplicating the repository's work. Determine `Notes`
      separately.
- [ ] `CertificateDTO ↔ Certificate` used both `DefaultDtoToEntityAfterMap()` and
      `DefaultEntityToDtoAfterMap()` (`WarrantyClaim.cs:51,53`) — **apply the SPIKE-11 finding**
      (see item H).

### F. The three line-item pairs — trap 2 territory

```csharp
CreateMap<WarrantyClaimLaborLine,  WarrantyClaimLaborLineDTO>().ReverseMap();
CreateMap<WarrantyClaimSubletLine, WarrantyClaimSubletLineDTO>().ReverseMap();
CreateMap<WarrantyClaimPartLine,   WarrantyClaimPartLineDTO>().ReverseMap();
```

Bare `CreateMap` + `ReverseMap` — under the recipe these normally delete with no replacement. **But
they are child collections on a `ViewAndUpsert` DTO, which is where traps 2 and 4 live.**

- [ ] **Trap 2:** read each emitted pair mapper and confirm `dto.ID` carries what the old map
      carried. These are child rows with their own PKs, so `dto.ID = source.ID.ToString()` is
      *correct* here — but verify rather than assume, and confirm no member is silently substituted.
- [ ] **Trap 4 / `SHENGEN010`:** these are tracked children with required FKs back to the claim. If
      the generator emits replace-with-new on the write side, saving will either fail on the FK or
      orphan and duplicate rows. Expect `SHENGEN010`; fix with `IgnoreEntity` + `AfterEntity`
      reconciliation by business key, never replace-with-new.
- [ ] **`WarrantyClaimPartLineDTO.Loading` is reported unmapped** by the baseline `SHENGEN004` on
      that pair. Determine its source and either `ForView` it or `IgnoreView` it deliberately.
- [ ] `WarrantyClaim → WarrantyCertificateLineDTO` builds a `ShiftEntitySelectDTO` by hand with both
      `Value` (the claim ID) and `Text` (the claim number). Check whether the convention now derives
      it; if so delete the customization rather than restating it (`conventions.md` §3).

### G. Rewrite the Cosmos replication delegate — **compile break**

`ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Extensions/WarrantyClaimsReplicationExtensions.cs:31` —
`Replicate<WarrantyClaimModel>`, currently passing no `mapping` delegate.

- [ ] Transcribe from `WarrantyClaim.cs:63–99` — `WarrantyClaim → WarrantyClaimModel`, ~25 members
      with substantial renaming (`DealerClaimNo → DealerClaimNumber`,
      `InvoiceNo → InvoiceNumber`, `RepairOrderNo → RepairOrderNumber`,
      `LaborOperationNoMain → LaborOperationNumberMain`, `DistComment1 → DistributorComment`).
      **Convention will not derive these renames — every one must be written out.**
- [ ] `LaborLines` projects a nested collection into `WarrantyClaimLaborLineModel` with its own
      renames (`OperationNumber → LaborCode`). Transcribe it whole.
- [ ] `BrandID` is derived from a franchise-key comparison to a numeric literal. Carry the mapping
      over verbatim; do not "improve" it into an enum lookup inside this migration.
- [ ] There is a **commented-out** `Brand` mapping in the profile. Leave it out — it is not live
      behaviour. Do not resurrect it.
- [ ] `ClaimStatus` and `ManufacturerStatus` use `!.Value` on nullable sources; preserve the same
      null handling so a null does not start throwing on the replication path.

> **Zero harness coverage** — replication is disabled during parity runs and its failures are
> swallowed. Line-by-line review against the profile is the only control.

### H. Port the `IMapper` sites — **compile break**

| Site | Action |
|---|---|
| `Repositories/WarrantyCertificateRepository.cs:38,43,61` — `mapper.Map<List<WarrantyCertificateLineDTO>>(claims)` | port to a `[ShiftEntityMapper] IShiftObjectMapper<WarrantyClaim, WarrantyCertificateLineDTO>`, or route through the generated mapper |
| `Repositories/WarrantyRatesRepository.cs:17,19,32` — `this.mapper.Map<WarrantyRatesDTO>(rates)` | same; this repository's own triple already covers `WarrantyRates → WarrantyRatesDTO`, so the generated mapper is likely the right route |
| `Services/WarrantyClaimService.cs:23,26,29` | **The field is assigned and never used.** Verified: no `.Map` call anywhere in the file. **Delete the field, the constructor parameter and the assignment** — do not port it. Check every construction site, noting the parameter is optional (`IMapper? mapper = null`), so callers may or may not pass it. |

- [ ] Apply SPIKE-11 for the two `Default*AfterMap` call sites at `WarrantyClaim.cs:51,53`.

### I. Delete the profiles, the registration call, and sweep prose

- [ ] Delete `Financial.cs` and `WarrantyClaim.cs`, and the `AutoMapperProfiles/` directory.
- [ ] `ADP.WarrantyClaims/ADP.WarrantyClaims.API/Extensions/WarrantyClaimsApiExtensions.cs:47` —
      delete `o.AddAutoMapper(typeof(DataMarker).Assembly);`, keep `o.AddDataAssembly(...)`, rewrite
      the comment.
- [ ] `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj:16` —
      `<Description>` says "AutoMapper profiles".
- [ ] `grep -rn "AutoMapper\|AfterMap\|mapping profile" ADP.WarrantyClaims --include=*.cs --include=*.csproj --include=*.md`.

### J. Emit and audit

```bash
rm -rf ADP.WarrantyClaims/ADP.WarrantyClaims.Data/obj
dotnet build ADP.WarrantyClaims/ADP.WarrantyClaims.Data -p:EmitCompilerGeneratedFiles=true
```

- [ ] **The dealer triple is the priority read.** Confirm the five `IgnoreList` calls baked (item B).
- [ ] Confirm the **three triples over `WarrantyClaim`** generated **three distinct list
      projections**, and that the dealer one differs from the distributor one by exactly the five
      omitted members and nothing else.
- [ ] Trap 1: check auto-composed collections for missing `IsDeleted` predicates.
- [ ] Trap 2: read all three line-item pair mappers (item F).
- [ ] Trap 3-write: diff `MapToEntityGenerated` against each old reverse map.
- [ ] Expect and resolve `SHENGEN010` on the line-item collections.

---

## Verification

```bash
dotnet build ADP.WarrantyClaims/ADP.WarrantyClaims.Data
dotnet build ADP.WarrantyClaims/ADP.WarrantyClaims.API
dotnet build ADP.WarrantyClaims/ADP.WarrantyClaims.Web
dotnet build                       # the whole solution — this step must end green
```

The solution-wide build is not a formality here. Item A moved seven package lines while the shared
floor (`ADP.Models`, `ADP.Cases`, `Lookup.Services`) is still at `2026.7.31.1` and does not move
until Step 06. That mixed state is exactly what the plan's ordering asserts is safe, and this build
is where the assertion is tested.

```powershell
.\tools\parity.ps1 verify -Group WarrantyClaims
# NOT OPTIONAL for this group:
.\tools\parity.ps1 verify -Group WarrantyClaims -Grant Restricted
```

**Group-specific caveats.**

- **No sample host.** Mounted host only — same limitation as Step 04: no consumer middleware order,
  localization, CORS or JSON overrides. **Given what `Financial.cs` is hiding, this is the one group
  where writing a real sample host is justified.** `ADP.WarrantyClaims/samples/ADP.WarrantyClaims.Sample.API`
  mirroring the Surveys sample is ~200 lines plus a disposable DB (skip migrations entirely). Strongly
  consider it rather than accepting the mounted-host gap here.
- **Both passes are mandatory, and the full-access one is not decorative.** `GET /DealerFinancial` is
  a distinct route with a distinct DTO whose only gate a full-access principal passes, so the
  five-member exposure **shows as a value diff on the full-access pass** (see §"How it is detected").
  Read that diff first. The restricted pass is the independent second control and covers the
  genuinely row-scoped surfaces; run both, explain both.
- **Needs SQL.** Cosmos unconfigured.
- The `.xlsx` export on the distributor financial controller is `PARTIAL` (SPIKE-10). Do not count it
  as covered.
- The replication delegate has no coverage at all.

---

## Exit criteria

- [ ] **The whole solution builds green** — `dotnet build` from the repo root, not just this group's
      four projects. Every step in this plan ends green; this one is no exception.
- [ ] All seven package lines at `2026.8.30.1`, committed separately from the mapper work.
      `TypeAuth` untouched at `1.6.28`. No `NU1605` anywhere in the build output.
- [ ] **All five `IgnoreList` calls proven baked from emitted code** — present in
      `__shiftBakedIgnored`, absent from `__shiftListProjection`. (A green build is not proof.)
- [ ] **The standalone dealer regression test exists and passes**, asserting all five members are
      null for a claim whose entity has all five populated.
- [ ] **Both harness passes were run** — full-access **and** restricted-grant — and the diffs of
      each explained. Specifically: the full-access `GET /DealerFinancial` list case shows the five
      members `null`, matching baseline, for the seeded claim whose entity has all five populated.
- [ ] The three `WarrantyClaim` list projections are confirmed distinct, and dealer-vs-distributor
      differs by exactly the five members.
- [ ] `ProcessDate` / `DistributorProcessDate` conversions verified to produce identical values
      (offset, kind, precision) to the old profile — the harness compares these literally.
- [ ] `ReferenceWarrantyClaimNumber` is pinned on all three maps that had it, and no column comes back
      empty.
- [ ] `SHENGEN010` on the line-item collections resolved with `IgnoreEntity` + `AfterEntity`
      reconciliation by business key — **not** by suppression.
- [ ] Zero `SHENGEN007` / `008`. Every remaining `SHENGEN004` carries a justification block naming
      each member (`Certificate.WarrantyClaims` is the known legitimate case).
- [ ] Replication delegate written and reviewed line-by-line, with every rename verified.
- [ ] All three `IMapper` sites resolved; `WarrantyClaimService`'s dead field, parameter and
      assignment deleted, and all construction sites updated.
- [ ] `grep -rn "AutoMapper" ADP.WarrantyClaims` returns zero hits.
- [ ] All four projects build clean.
- [ ] SPIKE-7 `RESOLVED`; SPIKE-8, SPIKE-9, SPIKE-11 findings applied.
- [ ] `STATUS.md` `VERIFIED`, with `Verified by` naming both harness passes and stating whether a
      real sample host or the mounted host was used.
- [ ] No BOM churn.

---

## Rollback

Revert this step's commits. The group is a leaf — nothing else in the repo references
`ADP.WarrantyClaims` — and reverting takes item A's seven package lines back to `2026.7.31.1` along
with the code. That is safe precisely because the shared floor has not moved yet: the tree returns to
a state already proven green.

Keep the hazard fix (item B) in its **own commit, immediately after the bump**, with its regression
test — so that if the rest of the step is rolled back, the exposure fix and its guard survive
independently.

---

## Effort & risk

**Effort:** comparable to Step 04. The seven-line bump is minutes. The hazard fix is small; proving
it, and the `WarrantyClaimModel` replication delegate, are the bulk.

**Risks:**

| Risk | Mitigation |
|---|---|
| This group's `ShiftEntity.Model` pin sits above `ADP.Models`' floor until Step 06 | It is an **upgrade**, not a downgrade — the Shift nuspecs declare minimum-version dependencies, not exact pins, and the repo already ships this arrangement today: `ADP.Menus.Shared` is at `2026.8.30.1` over an `ADP.Models` still at `2026.7.31.1` (`ADP.Models/Models/Models.csproj:48`), and it builds |
| **The five-field exposure ships.** 200, correct shape, no diagnostic, invisible under a full-access token | Item B first; emitted-code proof; a standalone regression test; mandatory restricted-grant pass. Four independent controls, because no single one is reliable. |
| `IgnoreList` does not bake as expected | SPIKE-7 is blocking. If it fails, the step is `BLOCKED` — the group must not ship with the exposure. |
| **A green run is mistaken for proof without reading the dealer list body** | Four independent controls, and the full-access `GET /DealerFinancial` case is named explicitly in the exit criteria so it gets read rather than skimmed |
| **The operator is told the full-access pass proves nothing and stops looking at it** | Corrected in §"How it is detected". The earlier claim was false; discounting the full-access diff would have discarded the clearest evidence available for this hazard. |
| `DateTime → DateTimeOffset` conversion differs subtly, shifting every timestamp | `verification.md` Rule 2 compares business dates literally; item C calls out `TimeSpan.Zero` |
| Line-item collections get replace-with-new, orphaning or duplicating rows | `SHENGEN010` plus item F's explicit reconciliation requirement |
| Replication delegate renames missed — 5 renamed members plus a nested collection | Item G enumerates them; line-by-line review is the only control |
| Mounted-host gap leaves host-wiring changes undetected on the riskiest group | Recommendation to write a real sample host here specifically |

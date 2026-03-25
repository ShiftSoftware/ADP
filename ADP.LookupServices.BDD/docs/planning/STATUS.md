# BDD Expansion — Status Tracker

> Update this document as work progresses. Check off items when done, add notes and dates.
> For full details on each item, see the corresponding phase document.

---

## Phase 0: Foundation Refactoring ([01-phase0-foundation.md](01-phase0-foundation.md))

**Status:** Complete

### Part A: BDD Infrastructure Fixes
- [x] Delete `SharedSetup.feature`
- [x] Remove `AssertEntireDataSet()` and collection-membership Then steps from `SharedStepDefinitions.cs`
- [x] Fix typos in `Authorized.feature` (`Authroized`, `Unauthroized`, `Vehciles`)
- [x] Fix typos in `AuthorizedStepDefinitions.cs` bindings
- [x] Create `Support/TestContext.cs`
- [x] Create `Support/Hooks.cs` (Reqnroll DI registration)
- [x] Refactor `SharedStepDefinitions.cs` to use `TestContext` instead of `ScenarioContext`
- [x] Rename `AuthorizedStepDefinitions.cs` → `VehicleAuthorizationStepDefinitions.cs`, refactor to `TestContext`
- [x] Delete `Support/ScenarioContextExtensions.cs`
- [x] Rename `Authorized.feature` → `VehicleAuthorization.feature`
- [x] Add NSubstitute package to `.csproj`

### Part B: Shared Test Data Directory
- [x] Create `ADP.TestData/` directory structure at repo root
- [x] Create `ADP.TestData/environments/standard-dealer.json` (initial data)
- [x] Create `Support/TestEnvironment.cs` (deserialization model)
- [x] Add `Given the "{environment}" environment is loaded` step
- [x] Add `Given loading vehicle "{vin}" from the environment` step
- [x] Scaffold `ADP.TestData/Generator/` console app (can be empty — just project structure)

### Part C: Update VehicleAuthorization Feature
- [x] Rewrite feature with environment-based and inline scenarios
- [x] Verify all scenarios pass: `dotnet test ADP.LookupServices.BDD`

**Notes:**
2026-03-22: Part A complete. Added `xunit.runner.visualstudio` package to fix `dotnet test` discovery (xunit v3 needed it for VSTest compatibility). Used `Support.TestContext` qualified name in VehicleAuthorizationStepDefinitions to avoid ambiguity with `Xunit.TestContext`. All 4 scenarios pass.
2026-03-23: Parts B & C complete. Uses case-sensitive JSON deserialization (default System.Text.Json) with PascalCase property names matching C# exactly — avoids `id`/`ID` collision in Cosmos DB models and allows copy-pasting real data from Cosmos. Flattened environment path from `environments/standard-dealer/input.json` to `environments/standard-dealer.json`. Path resolution walks up from Assembly.Location to find ADP.TestData/. All 6 scenarios pass (4 inline + 2 environment-based).

---

## Phase 1: Simple Evaluators ([02-phase1-simple-evaluators.md](02-phase1-simple-evaluators.md))

**Status:** Complete
**Prerequisite:** Phase 0 complete

### Given Steps (SharedStepDefinitions.cs)
- [x] Enrich SSC Given step with full `SSCAffectedVINModel` columns
- [x] Add warranty claims Given step (`WarrantyClaimModel`)
- [x] Add labor lines Given step (`OrderLaborLineModel`)
- [x] Enrich vehicle entry Given step with additional columns (VariantCode, Katashiki, etc.)

### Feature Files & Step Definitions
- [x] `VehicleEntry.feature` + `VehicleEntryStepDefinitions.cs`
- [x] `VehicleIdentifiers.feature` + `VehicleIdentifierStepDefinitions.cs`
- [x] `VehicleSSC.feature` + `VehicleSSCStepDefinitions.cs`
- [x] All Phase 0 + Phase 1 scenarios pass

**Notes:**
2026-03-23: Phase 1 complete. Replaced old `FeatureData`/`ParseDataTable` pattern in SharedStepDefinitions with generic `GetOptionalString`/`GetOptionalDate`/`GetOptionalLong` helpers using `DataTableRow` directly — supports arbitrary optional columns without a fixed DTO. Added second binding aliases (`"vehicles in dealer stock:"`, `"SSC affected vehicles:"`) for shorter Gherkin in Phase 1 features while keeping original verbose bindings for VehicleAuthorization. Warranty claims Given step builds `LaborLines` sub-collection from a single `LaborCode` column. Used `Enum.Parse<ClaimStatus>` for claim status mapping (uses enum member names, not descriptions). Added extra scenario for labor line status "C" beyond what planning doc specified. All 23 scenarios pass (6 Phase 0 + 4 VehicleEntry + 2 VehicleIdentifiers + 11 VehicleSSC).

---

## Phase 2: Medium Evaluators + LookupOptions ([03-phase2-lookup-options.md](03-phase2-lookup-options.md))

**Status:** Not Started
**Prerequisite:** Phase 1 complete

### Given Steps
- [ ] Vehicle service activations step (`VehicleServiceActivation`)
- [ ] Warranty date shifts step (`WarrantyDateShiftModel`)
- [ ] Free service item date shifts step (`FreeServiceItemDateShiftModel`)
- [ ] Extended warranty entries step (`ExtendedWarrantyModel`)
- [ ] Create `LookupOptionsStepDefinitions.cs` (warranty defaults, brand warranty periods)
- [ ] Sale information Given step (for non-broker scenarios)

### Feature Files & Step Definitions
- [ ] `WarrantyDates.feature` + `WarrantyDateStepDefinitions.cs`
- [ ] Decide on broker scenario approach (defer to Phase 4, or add Given step for `VehicleSaleInformation`)
- [ ] All previous + Phase 2 scenarios pass

**Notes:**
<!--
-->

---

## Phase 3: Async Evaluators + IServiceProvider ([04-phase3-async-evaluators.md](04-phase3-async-evaluators.md))

**Status:** Not Started
**Prerequisite:** Phase 2 complete

### Infrastructure
- [ ] Create `Support/MockSetup.cs` (resolver delegate helpers)
- [ ] Extend `LookupOptionsStepDefinitions.cs` with resolver Given steps (image URLs, company/branch names)

### Given Steps
- [ ] Accessories step (`VehicleAccessoryModel`)
- [ ] Paint thickness inspections step (`PaintThicknessInspectionModel`)
- [ ] Paint thickness panels step (nested data)
- [ ] Part lines step (`OrderPartLineModel`)

### Feature Files & Step Definitions
- [ ] `VehicleAccessories.feature` + `VehicleAccessoriesStepDefinitions.cs`
- [ ] `VehiclePaintThickness.feature` + `VehiclePaintThicknessStepDefinitions.cs`
- [ ] `VehicleServiceHistory.feature` + `VehicleServiceHistoryStepDefinitions.cs`
- [ ] All previous + Phase 3 scenarios pass

**Notes:**
<!--
-->

---

## Phase 4: Full-Dependency Evaluators + IVehicleLoockupStorageService ([05-phase4-storage-service.md](05-phase4-storage-service.md))

**Status:** Not Started
**Prerequisite:** Phase 3 complete

### Infrastructure
- [ ] Create `MockStorageStepDefinitions.cs` (NSubstitute `.Returns()` setup for storage service)

### Given Steps
- [ ] Broker stock mock setup step
- [ ] Customer mock setup step
- [ ] Service items mock setup step
- [ ] Paid service invoices step (`PaidServiceInvoiceModel`)
- [ ] Item claims step (`ItemClaimModel`)
- [ ] Free service item excluded VINs step (`FreeServiceItemExcludedVINModel`)
- [ ] Vehicle inspections step (`VehicleInspectionModel`)

### Feature Files & Step Definitions
- [ ] `VehicleSaleInformation.feature` + `VehicleSaleInformationStepDefinitions.cs`
- [ ] `VehicleSpecification.feature` + `VehicleSpecificationStepDefinitions.cs`
- [ ] `ServiceItems/Eligibility.feature` (refine scenarios after reading evaluator source)
- [ ] `ServiceItems/Status.feature`
- [ ] `ServiceItems/Expiration.feature`
- [ ] `VehicleServiceItemStepDefinitions.cs`
- [ ] All previous + Phase 4 scenarios pass

**Notes:**
<!--
-->

---

## Phase 5: Part Evaluators ([06-phase5-part-evaluators.md](06-phase5-part-evaluators.md))

**Status:** Not Started
**Prerequisite:** Phase 4 complete (can be deprioritized if part lookup is stable)

### Infrastructure
- [ ] Add `[assembly: InternalsVisibleTo("LookupServices.BDD")]` to Lookup.Services
- [ ] Add `PartAggregate` to `TestContext`
- [ ] Create `PartSetupStepDefinitions.cs`

### Given Steps
- [ ] Part catalog data step (`CatalogPartModel`)
- [ ] Stock for part step (`StockPartModel`)
- [ ] Dead stock for part step (`CompanyDeadStockPartModel`)

### Feature Files & Step Definitions
- [ ] `Parts/PartPrice.feature` + `PartPriceStepDefinitions.cs`
- [ ] `Parts/PartDeadStock.feature` + `PartDeadStockStepDefinitions.cs`
- [ ] `Parts/PartStock.feature` + `PartStockStepDefinitions.cs`
- [ ] All previous + Phase 5 scenarios pass

**Notes:**
<!--
-->

---

## Cross-Cutting: Shared Data & Generator ([shared-data-architecture.md](shared-data-architecture.md))

These can be done incrementally alongside the phases above.

### Test Data
- [ ] `standard-dealer` environment: initial data (Phase 0), enriched as phases add evaluators
- [ ] `broker-dealer` environment (Phase 4+)
- [ ] `edge-cases` environment (as needed for regression scenarios)

### Output Generator (`ADP.TestData/Generator/`)
- [ ] Generator console app reads environment JSON files, runs evaluators, writes output
- [ ] Generated output committed to `adp-web-components/src/features/mocks/data/generated/`
- [ ] Generated output committed to `ADP.Docs/Docs/docs/web-components/demo-data/`

### Web Component Integration
- [ ] Decide on vehicle-lookup `mockUrl` approach (options a/b/c in shared-data-architecture.md)
- [ ] Update `MockFiles` registry in `types.ts` to point at generated data
- [ ] Verify components work with generated data
- [ ] Remove old hand-crafted mock data (`mock-data.js`, existing `part-lookup.json`)

### Documentation Site
- [ ] Embed web component demos in MkDocs pages with generated data
- [ ] Verify `mkdocs serve` works with demo data

---

## Open Decisions

Track decisions that need to be made during implementation.

| # | Decision | Options | Status | Resolution |
|---|----------|---------|--------|------------|
| 1 | Vehicle-lookup `mockUrl` prop | (a) Add prop, (b) Update `getMockFile()`, (c) Keep `setMockData()` | Open | |
| 2 | Broker scenario approach in Phase 2 | (a) Given step for `VehicleSaleInformation`, (b) Defer to Phase 4 | Open | |
| 3 | Broker data placement in environment JSON | Environment-level (current plan) vs per-VIN | Open | |

---

## Changelog

| Date | Phase | Change |
|------|-------|--------|
| 2026-03-22 | — | Plan reviewed, inconsistencies fixed, status tracker created |
| 2026-03-23 | 0 | Phase 0 complete. Used real framework types (LookupOptions, CompanyDataAggregateModel) instead of custom wrappers. Flattened environment paths. Case-sensitive JSON with PascalCase. |
| 2026-03-23 | 1 | Phase 1 complete. 17 new scenarios across 3 feature files. Refactored SharedStepDefinitions to use generic column helpers. |

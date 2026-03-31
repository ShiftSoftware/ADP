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

**Status:** Complete
**Prerequisite:** Phase 1 complete

### Given Steps
- [x] Vehicle service activations step (`VehicleServiceActivation`)
- [x] Warranty date shifts step (`WarrantyDateShiftModel`)
- [x] Free service item date shifts step (`FreeServiceItemDateShiftModel`)
- [x] Extended warranty entries step (`ExtendedWarrantyModel`)
- [x] Create `LookupOptionsStepDefinitions.cs` (warranty defaults, brand warranty periods)
- [x] Add `WarrantyActivationDate` column to "vehicles in dealer stock" Given step

### Feature Files & Step Definitions
- [x] `WarrantyDates.feature` + `WarrantyDateStepDefinitions.cs`
- [x] Broker scenarios deferred to Phase 4 (requires `IVehicleLoockupStorageService` mocking)
- [x] All previous + Phase 2 scenarios pass

**Notes:**
2026-03-28: Phase 2 complete. 9 new warranty scenarios. "When evaluating warranty dates" step chains VehicleEntryEvaluator → builds VehicleSaleInformation from selected vehicle entry (mapping InvoiceDate/WarrantyActivationDate) → runs WarrantyAndFreeServiceDateEvaluator. SaleInformation constructed directly from vehicle entry data for non-broker scenarios; broker scenarios deferred to Phase 4 where VehicleSaleInformationEvaluator + storage service mocking are available. LookupOptionsStepDefinitions uses `long` for brandId param which auto-boxes to `long?` dictionary key. All 32 scenarios pass (23 Phase 0+1 + 9 Phase 2).

---

## Phase 3: Async Evaluators + IServiceProvider ([04-phase3-async-evaluators.md](04-phase3-async-evaluators.md))

**Status:** Complete
**Prerequisite:** Phase 2 complete

### Infrastructure
- [x] Resolver mock delegates configured inline in `LookupOptionsStepDefinitions.cs` (no separate MockSetup.cs needed)
- [x] Extend `LookupOptionsStepDefinitions.cs` with resolver Given steps (image URLs, company/branch names)

### Given Steps
- [x] Accessories step (`VehicleAccessoryModel`)
- [x] Paint thickness inspections step (`PaintThicknessInspectionModel`)
- [x] Paint thickness panels step (nested data, with enum parsing for PanelType/PanelSide/PanelPosition)
- [x] Part lines step (`OrderPartLineModel`)
- [x] Enhanced labor lines step with service history columns (OrderDocumentNumber, ServiceDescription, etc.)

### Feature Files & Step Definitions
- [x] `VehicleAccessories.feature` + `VehicleAccessoriesStepDefinitions.cs` (3 scenarios)
- [x] `VehiclePaintThickness.feature` + `VehiclePaintThicknessStepDefinitions.cs` (2 scenarios)
- [x] `VehicleServiceHistory.feature` + `VehicleServiceHistoryStepDefinitions.cs` (4 scenarios)
- [x] All previous + Phase 3 scenarios pass

**Notes:**
2026-03-29: Phase 3 complete. 9 new scenarios (3 accessories, 2 paint thickness, 4 service history). Resolver delegates configured inline in LookupOptionsStepDefinitions using instance dictionaries — simpler than a separate MockSetup.cs. PaintThicknessInspections on aggregate is `IEnumerable<>` not `List<>`, so Given step converts to list. PanelSide enum is Left/Center/Right (not Front), PanelPosition is Front/Middle/Rear. All 41 scenarios pass (32 Phase 0-2 + 9 Phase 3).

---

## Phase 4: Full-Dependency Evaluators + IVehicleLoockupStorageService ([05-phase4-storage-service.md](05-phase4-storage-service.md))

**Status:** Complete
**Prerequisite:** Phase 3 complete

### Infrastructure
- [x] Create `MockStorageStepDefinitions.cs` (NSubstitute `.Returns()` for broker stock, customer, vehicle model, service items)

### Given Steps
- [x] Broker stock mock setup step (`GetBrokerStockAsync`)
- [x] Customer mock setup step (`GetCustomerAsync`)
- [x] Vehicle model mock setup step (`GetVehicleModelsAsync`)
- [x] Service items mock setup step (`GetServiceItemsAsync`)
- [x] Paid service invoices step (`PaidServiceInvoiceModel`)
- [x] Item claims step (`ItemClaimModel`)
- [x] Free service item excluded VINs step (`FreeServiceItemExcludedVINModel`)
- [x] Vehicle inspections step (`VehicleInspectionModel`)
- [x] Free service start date Given step (for direct service item evaluation)
- [x] LookupOptions broker stock lookup enabled step

### Feature Files & Step Definitions
- [x] `VehicleSaleInformation.feature` + `VehicleSaleInformationStepDefinitions.cs` (6 scenarios: direct sale, broker at stock, broker with invoice, end customer from DB, end customer from broker invoice, no vehicles)
- [x] `VehicleSpecification.feature` + `VehicleSpecificationStepDefinitions.cs` (2 scenarios)
- [x] `ServiceItems.feature` + `VehicleServiceItemStepDefinitions.cs` (5 scenarios: pending, processed, VIN exclusion, paid items, dynamic cancellation)
- [x] All previous + Phase 4 scenarios pass

**Notes:**
2026-03-29: Phase 4 complete. 13 new scenarios. MockStorageStepDefinitions configures NSubstitute mocks on IVehicleLoockupStorageService for broker stock, customer, vehicle model, and service items. Broker invoice DateTimeOffset must use UTC (TimeSpan.Zero) to avoid timezone drift in `.ToUniversalTime().Date`. Service item tests use future-valid dates (2026+) because rolling expiry sets ExpiresAt relative to freeServiceStartDate and checks against DateTime.Now. Items without MaximumMileage in RelativeToActivation mode get ExpiresAt=freeServiceStartDate (immediately expired) — they need MaximumMileage to become sequential and get proper expiry. Service item feature consolidated into single file instead of separate Eligibility/Status/Expiration files. Added end customer scenarios: direct sale via GetCustomerAsync and broker sale via TBP_Invoice customer fields. All 54 scenarios pass (41 Phase 0-3 + 13 Phase 4).

---

## Phase 5: Part Evaluators ([06-phase5-part-evaluators.md](06-phase5-part-evaluators.md))

**Status:** Complete
**Prerequisite:** Phase 4 complete (can be deprioritized if part lookup is stable)

### Infrastructure
- [x] Add `[assembly: InternalsVisibleTo("LookupServices.BDD")]` to Lookup.Services
- [x] Add `PartAggregate` to `TestContext`
- [x] Create `PartSetupStepDefinitions.cs`

### Given Steps
- [x] Part catalog data step (`CatalogPartModel`)
- [x] Stock for part step (`StockPartModel`)
- [x] Dead stock for part step (`CompanyDeadStockPartModel`)

### Feature Files & Step Definitions
- [x] `PartPrice.feature` + `PartPriceStepDefinitions.cs` (4 scenarios)
- [x] `PartDeadStock.feature` + `PartDeadStockStepDefinitions.cs` (3 scenarios)
- [x] `PartStock.feature` + `PartStockStepDefinitions.cs` (8 scenarios)
- [x] All previous + Phase 5 scenarios pass

**Notes:**
2026-03-30: Phase 5 complete. 15 new scenarios (4 part price, 3 dead stock, 8 stock). PartAggregateCosmosModel collections (CatalogParts, StockParts, CompanyDeadStockParts) default to null — initialized to empty arrays in TestContext to avoid NREs. PartPriceEvaluator returns (null, []) when PartLookupPriceResolver is null — prices are built up internally but only returned via the resolver; added "pass-through resolver" Given step for meaningful price testing. DistributorStockPartLookupQuantityThreshold defaults to null (GetValueOrDefault=0), meaning any non-zero lookup quantity ≥ 0 hits QuantityNotWithinLookupThreshold — stock scenarios that test Available/PartiallyAvailable/NotAvailable must set threshold explicitly. Feature files placed in Features/ root (not Parts/ subdirectory) matching existing convention. Added country/region/location name resolver Given steps and threshold/show-quantity LookupOptions steps to LookupOptionsStepDefinitions. All 69 scenarios pass (54 Phase 0-4 + 15 Phase 5).

---

## Cross-Cutting: Shared Data & Generator ([shared-data-architecture.md](shared-data-architecture.md))

These can be done incrementally alongside the phases above.

### Test Data
- [x] `standard-dealer` environment: enriched with SSCs, warranty claims, service history, accessories, paint thickness, parts, vehicle models
- [ ] `broker-dealer` environment (Phase 4+)
- [ ] `edge-cases` environment (as needed for regression scenarios)

### Output Generator (`ADP.TestData/Generator/`)
- [x] Generator console app reads environment JSON files, runs evaluators, writes output
- [x] Generated output written to `adp-web-components/src/features/mocks/data/generated/`
- [x] Generated output written to `ADP.Docs/Docs/docs/web-components/demo-data/`

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
| 2 | Broker scenario approach in Phase 2 | (a) Given step for `VehicleSaleInformation`, (b) Defer to Phase 4 | Resolved | Deferred to Phase 4. Broker warranty scenarios require `VehicleSaleInformation.Broker` which comes from `VehicleSaleInformationEvaluator` + `IVehicleLoockupStorageService.GetBrokerStockAsync()`. Testing these properly requires the storage service mocking infrastructure from Phase 4. |
| 3 | Broker data placement in environment JSON | Environment-level (current plan) vs per-VIN | Resolved | Environment-level. `BrokerInitialVehicles`/`BrokerInvoices` stay at root of environment JSON (matches aggregate). Actual broker stock data (`TBP_StockModel`) comes from `IVehicleLoockupStorageService.GetBrokerStockAsync()` — not part of the aggregate or environment JSON; handled via NSubstitute mocks in Phase 4. |

---

## Evaluator Issues / Refactoring Notes

Evaluators are not considered golden — they may have flaws or unnecessary complexity. Strategy: finish BDD coverage first across all phases, then refactor with tests as a safety net. Log observations here as they come up.

| # | Evaluator | Observation | Spotted During |
|---|-----------|-------------|----------------|
| 1 | VehicleSSCEvaluator | Returns `null` instead of empty collection when SSC list is empty (line 28). Callers must null-check. | Phase 1 review |
| 2 | VehicleSSCEvaluator | Dead code: `partNumbers` extracted on line 119 but never used. Looks like leftover from an incomplete feature. | Phase 1 review |
| 3 | VehicleSSCEvaluator | Heavy duplication: LaborCode1/2/3 and PartNumber1/2/3 each handled with copy-pasted if-blocks (lines 76-110). Could loop over an array. Same for labor matching on line 57. | Phase 1 review |
| 4 | VehicleSSCEvaluator | `new List<ClaimStatus> { ... }` allocated on every SSC × every warranty claim iteration (line 41). Should be a static readonly. | Phase 1 review |
| 5 | VehicleSSCEvaluator | `s.LaborCode.Equals(...)` and `s.InvoiceStatus.Equals(...)` (lines 57-58) will throw `NullReferenceException` if `LaborCode` or `InvoiceStatus` is null. | Phase 1 review |
| 6 | VehicleEntryEvaluator | Commented-out code on lines 22-23 (`.Select`/`.Concat` with VehicleServiceActivations). Should be removed or documented. | Phase 0 review |
| 7 | VehicleEntryEvaluator | When multiple vehicles have null InvoiceDate, `FirstOrDefault` returns whichever is first in the list — non-deterministic. May need a tiebreaker. | Phase 0 review |
| 8 | WarrantyAndFreeServiceDateEvaluator | Duplicated fallback chain: lines 29-35 (normal sale) and lines 48-54 (broker stock + ignoreBrokerStock) contain identical activation → warranty date → invoice date logic. Could extract to a helper. | Phase 2 review |
| 9 | WarrantyAndFreeServiceDateEvaluator | `vehicle` parameter is only used for `BrandID` (line 76). The rest of the vehicle data comes through `saleInformation`. Signature could be simplified. | Phase 2 review |
| 10 | WarrantyAndFreeServiceDateEvaluator | Default warranty period of 3 years is a magic number (line 82). Should be a named constant or come from `LookupOptions`. | Phase 2 review |
| 11 | VehiclePaintThicknessEvaluator | Returns `null` instead of empty collection when `PaintThicknessInspections` is null (line 25). Same pattern as SSC evaluator issue #1. | Phase 3 review |
| 12 | VehicleServiceHistoryEvaluator | `invoice.LaborLines.Where(x => x.JobDescription is not null)?.FirstOrDefault()` (line 95): the `?.` after `.Where()` is unnecessary since `Where` never returns null. Minor but misleading. | Phase 3 review |
| 13 | VehicleSaleInformationEvaluator | `lastBrokerInvoice.InvoiceDate.ToUniversalTime().Date` (line 144): timezone-sensitive. If `DateTimeOffset` is created with local timezone offset, `.ToUniversalTime()` shifts the date. Callers must ensure UTC. | Phase 4 review |
| 14 | VehicleServiceItemEvaluator | Rolling expiry for non-sequential items (no MaximumMileage) sets `ExpiresAt = startDate` (line 328) which equals `freeServiceStartDate` if no sequential items exist — effectively immediately expired. Unclear if intentional. | Phase 4 review |
| 15 | VehicleServiceItemEvaluator | ~530 lines, deeply nested, mixes multiple concerns (eligibility, status, expiration, cancellation, signature). Would benefit from being split into smaller focused methods or separate evaluators. | Phase 4 review |
| 16 | VehicleSpecificationEvaluator | Commented-out `//if (vtModel is not null)` with braces that still execute (lines 24-49). Dead comment that's misleading about the actual control flow. | Phase 4 review |
| 17 | PartPriceEvaluator | Returns `(null, [])` when `PartLookupPriceResolver` is null (line 75), discarding all built-up pricing data. The evaluator builds country/region prices but only returns them through the resolver — without one, the entire result is empty. | Phase 5 review |
| 18 | PartPriceEvaluator | `cosmosPartCatalog?.CountryData` null-conditional on line 29 inside `foreach` is unnecessary — already guarded by `is not null` check on line 27. | Phase 5 review |
| 19 | PartStockEvaluator | `DistributorStockPartLookupQuantityThreshold.GetValueOrDefault()` returns 0 when null. Combined with `>=`, any non-zero lookup quantity hits `QuantityNotWithinLookupThreshold` when no threshold is configured. Likely should check `HasValue` first. | Phase 5 review |
| 20 | PartAggregateCosmosModel | Collections (`StockParts`, `CatalogParts`, `CompanyDeadStockParts`) default to null instead of empty. Evaluators iterate directly without null checks, causing NRE on empty aggregate. | Phase 5 review |

---

## Changelog

| Date | Phase | Change |
|------|-------|--------|
| 2026-03-22 | — | Plan reviewed, inconsistencies fixed, status tracker created |
| 2026-03-23 | 0 | Phase 0 complete. Used real framework types (LookupOptions, CompanyDataAggregateModel) instead of custom wrappers. Flattened environment paths. Case-sensitive JSON with PascalCase. |
| 2026-03-23 | 1 | Phase 1 complete. 17 new scenarios across 3 feature files. Refactored SharedStepDefinitions to use generic column helpers. |
| 2026-03-28 | 2 | Phase 2 complete. 9 warranty scenarios, LookupOptionsStepDefinitions, WarrantyDateStepDefinitions. Broker scenarios deferred to Phase 4. |
| 2026-03-29 | 3 | Phase 3 complete. 9 scenarios across 3 async evaluators (accessories, paint thickness, service history). Resolver mocks inline in LookupOptionsStepDefinitions. |
| 2026-03-29 | 4 | Phase 4 complete. 11 scenarios across 3 evaluators (sale info, specification, service items). MockStorageStepDefinitions for NSubstitute. Service items consolidated into single feature. |
| 2026-03-30 | 5 | Phase 5 complete. 15 scenarios across 3 part evaluators (price, dead stock, stock). PartAggregateCosmosModel + InternalsVisibleTo. Pass-through price resolver pattern. |
| 2026-03-31 | Cross-cutting | Output Generator implemented. Enriched standard-dealer.json with all evaluator data. Generator reads environments, runs all vehicle/part evaluators with NSubstitute mocks, writes camelCase JSON to web components and docs directories. Added JsonStringEnumConverter to BDD environment deserialization. |

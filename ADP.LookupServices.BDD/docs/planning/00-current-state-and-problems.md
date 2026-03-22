# Current State and Problems

## What Exists Today

### BDD Tests (ADP.LookupServices.BDD/)

The BDD project tests a single evaluator (`VehicleAuthorizationEvaluator`) as a proof of concept.

```
ADP.LookupServices.BDD/
├── Features/
│   ├── SharedSetup.feature          # Tests data parsing (low value)
│   └── Authorized.feature           # Tests VehicleAuthorizationEvaluator
├── StepDefinitions/
│   ├── SharedStepDefinitions.cs     # Given steps for 3 data sources + data parsing assertions
│   └── AuthorizedStepDefinitions.cs # Then steps for authorized/unauthorized
└── Support/
    └── ScenarioContextExtensions.cs # Helper to retrieve CompanyDataAggregate from ScenarioContext
```

### Web Component Mock Data

Mock data exists in two separate locations with different formats:

| Location | Format | Content | Published |
|----------|--------|---------|-----------|
| `adp-web-components/src/features/mocks/data/part-lookup.json` | JSON | Part lookup DTOs keyed by part number | Yes (CDN via npm) |
| `adp-web-components/src/templates/vehicle-lookup/mock-data.js` | JavaScript | Vehicle lookup DTOs keyed by VIN (~4800 lines) | No (dev only) |

The web components have an `isDev` mode that loads mock data via `getMockFile()`. In dev it fetches from `localhost:3000/mocks/`, in prod from `cdn.jsdelivr.net`. A `vehicle-lookup.json` mock file is declared in `types.ts` but doesn't exist yet. Part lookup components have a `mockUrl` prop for custom mock URLs; vehicle lookup components do not — they use `setMockData()` programmatically.

### Documentation Site (ADP.Docs/)

MkDocs Material site deployed to `adp.shift.software`. Web component documentation pages exist in navigation but are all placeholder ("Documentation In Progress"). No embedded component demos yet, though the MkDocs config supports `md_in_html` for embedding HTML.

### Type Generation Pipeline

34 C# DTOs with `[TypeScriptModel]` attribute are auto-generated to TypeScript via the `WebComponentModelGenerator` (Roslyn-based). This establishes a type-safe bridge between .NET output DTOs and TypeScript.

---

## Problems

### Test Data Problems

1. **No shared data between .NET and web components.** BDD tests build data inline via Gherkin DataTables. Web component mocks are hand-crafted JS/JSON files. Changes in one don't reflect in the other.

2. **No "environment" concept.** BDD tests create throwaway data per scenario. There's no persistent, realistic dataset representing a company with its vehicles, parts, service history, warranty configuration, etc. Real bugs surface when data combinations interact — per-scenario isolation misses these.

3. **Mock data is scattered.** Part mock is JSON in `mocks/data/`, vehicle mock is JS in `templates/`, and BDD test data is inline C#. No single source of truth.

4. **No data pipeline.** The web component mock data (output DTOs) is manually crafted. There's no connection between the evaluator input data and these output DTOs. When evaluator logic changes, mock data doesn't update.

5. **Web component docs have no demos.** The documentation site has placeholders for 10 component pages. Shared test data could power interactive demos.

### BDD Infrastructure Problems

6. **`AssertEntireDataSet()` in SharedStepDefinitions** (lines 128-144) hardcodes specific VIN assertions, coupling it to one scenario.

7. **Only 3 of 18 aggregate collections** have Given steps (`VehicleEntries`, `SSCAffectedVINs`, `InitialOfficialVINs`).

8. **`FeatureData` helper is too narrow** — only supports VIN + InvoiceDate columns.

9. **No LookupOptions support** — evaluators like `WarrantyAndFreeServiceDateEvaluator` need configuration.

10. **No mocking infrastructure** — async evaluators need `IServiceProvider` and `IVehicleLoockupStorageService`.

11. **`SharedSetup.feature` tests `List.Add()`/`List.Contains()`** — not business logic.

12. **Typos** in feature files and bindings: `Authroized`, `Unauthroized`, `Vehciles`.

13. **No `TestContext` class** — aggregate is stored via magic string `"companyData"` in `ScenarioContext`.

---

## What Works Well (Keep)

1. **Reqnroll + xUnit integration** — solid framework choice
2. **Separation of Given steps (shared) from Then steps (per-evaluator)** — good pattern
3. **Direct evaluator instantiation in Then steps** — tests evaluators in isolation
4. **The `[TypeScriptModel]` pipeline** — proven C# → TypeScript type bridge
5. **Web component `isDev` / `mockUrl` system** — flexible mock loading already built
6. **`getMockFile()` with CDN fallback** — production-ready mock serving
7. **MkDocs `md_in_html` extension** — can embed web components directly in docs pages

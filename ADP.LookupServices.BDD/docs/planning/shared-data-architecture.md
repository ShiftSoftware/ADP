# Shared Test Data Architecture

## Vision

A single source of truth for test data that powers three consumers:
1. **.NET BDD tests** — verify evaluator logic against realistic environments
2. **Web component dev/demos** — display real-looking data in components
3. **MkDocs documentation site** — interactive component demos with live data

Each consumer gets a **committed copy** of the generated data in its own source tree — self-contained for builds and deployments, no runtime dependency on external paths. This follows the same pattern as the `WebComponentModelGenerator` which injects TypeScript types into the web components' source.

## Data Flow

```
ADP.TestData/                                (canonical source — input only)
├── environments/
│   ├── standard-dealer.json
│   ├── broker-dealer.json
│   └── edge-cases.json
│
│         ▼ Output Generator (dotnet run) ▼
│
├── Reads environment JSON, runs evaluators, writes output to each consumer:
│
│   ┌──────────────────────────────────────────────────────────────────┐
│   │                                                                  │
│   ▼                              ▼                              ▼    │
│                                                                      │
│  adp-web-components/          ADP.Docs/Docs/docs/         (BDD tests │
│  src/features/mocks/          web-components/              read input │
│  data/generated/              demo-data/                   directly)  │
│  ├── standard-dealer/         ├── standard-dealer/                   │
│  │   ├── vehicle-lookup.json  │   ├── vehicle-lookup.json            │
│  │   └── part-lookup.json     │   └── part-lookup.json               │
│  └── broker-dealer/           └── broker-dealer/                     │
│      └── ...                      └── ...                            │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

All generated files are committed to source control.
Each project builds and deploys independently.
```

## Key Principle: Generate and Commit

Like the TypeScript type generation pipeline:
- The generator runs during development (not at build time or runtime)
- Generated files are **committed to git** alongside hand-written source
- Each project is **self-contained** — `npm start`, `mkdocs serve`, and `dotnet test` all work without cross-project dependencies
- When input data or evaluator logic changes, re-run the generator and commit the updated output

## Canonical Data Location

A new top-level directory: **`ADP.TestData/`**

Contains **only input data** — no generated output lives here.

```
ADP.TestData/
├── environments/
│   ├── standard-dealer.json     (CompanyDataAggregateModel + LookupOptions)
│   ├── broker-dealer.json
│   └── edge-cases.json
├── Generator/                   (.NET console app — runs evaluators, writes output)
│   ├── Generator.csproj
│   └── Program.cs
└── README.md
```

## Where Generated Output Lives

Each consumer gets output injected into its source tree in a `generated` or dedicated directory:

| Consumer | Generated output location | What's generated |
|----------|--------------------------|------------------|
| **Web components** | `adp-web-components/src/features/mocks/data/generated/{environment}/` | `vehicle-lookup.json`, `part-lookup.json` |
| **MkDocs docs** | `ADP.Docs/Docs/docs/web-components/demo-data/{environment}/` | Same JSON files |
| **BDD tests** | N/A — reads environment JSON directly from `ADP.TestData/environments/` | BDD tests consume input, not output |

### Why BDD tests don't get a copy

BDD tests verify that evaluators **produce correct output from input**. They read the environment JSON file, run evaluators, and assert results. They don't need pre-generated output — that would be testing the generator, not the evaluators.

### Web Components

Generated files go into `src/features/mocks/data/generated/` (separate from any remaining hand-crafted mocks). The Stencil build copies these to `dist/mocks/` as it already does for the existing `data/` directory.

The existing `getMockFile()` function and `isDev` props continue to work. The mock file registry (`types.ts`) is updated to reference environment-specific files:

```typescript
export const MockFiles = {
  'vehicle-lookup': 'generated/standard-dealer/vehicle-lookup.json',
  'part-lookup': 'generated/standard-dealer/part-lookup.json',
} as const;
```

**Part lookup components** already have a `mockUrl` prop and can switch environments directly:
```html
<dead-stock-lookup
  is-dev="true"
  mock-url="/mocks/generated/broker-dealer/part-lookup.json">
</dead-stock-lookup>
```

**Vehicle lookup components** do NOT currently have a `mockUrl` prop — they use `setMockData()` to receive mock data programmatically. To support environment switching, one of:
- (a) Add a `mockUrl` prop to `vehicle-lookup-wrapper` (mirrors part-lookup pattern)
- (b) Update `getMockFile()` to accept a path and have the component load generated data via `isDev` mode
- (c) Keep using `setMockData()` and have template HTML load the JSON and pass it in

**Decision needed during implementation.** Option (a) is the simplest and most consistent.

### MkDocs Docs

Generated files go into `docs/web-components/demo-data/{environment}/`. MkDocs serves these as static assets alongside the documentation pages.

Documentation pages embed web components pointing at the local demo data:

```markdown
## Live Demo

<div markdown="0">
  <script type="module"
    src="https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/shift-components/shift-components.esm.js">
  </script>

  <!-- Part lookup: uses mockUrl prop (already supported) -->
  <dead-stock-lookup
    is-dev="true"
    mock-url="demo-data/standard-dealer/part-lookup.json">
  </dead-stock-lookup>

  <!-- Vehicle lookup: requires mockUrl prop to be added (see web component section above) -->
  <!-- Once added, the pattern would be: -->
  <vehicle-lookup-wrapper
    is-dev="true"
    mock-url="demo-data/standard-dealer/vehicle-lookup.json">
  </vehicle-lookup-wrapper>
</div>
```

When running `mkdocs serve` locally, the demo data is served from the local filesystem. When deployed to GitHub Pages, the demo data is deployed alongside the docs — fully self-contained.

## Input Data Format (environment JSON)

Each environment JSON file (e.g., `standard-dealer.json`) contains:

```json
{
  "LookupOptions": {
    "WarrantyStartDateDefaultsToInvoiceDate": true,
    "BrandStandardWarrantyPeriodsInYears": { "1": 3, "2": 5 },
    "IncludeInactivatedFreeServiceItems": false,
    "LookupBrokerStock": false
  },
  "Companies": [
    {
      "CompanyId": 1,
      "CompanyName": "Toyota Iraq",
      "Branches": [
        { "BranchId": 10, "BranchName": "Erbil Showroom" },
        { "BranchId": 20, "BranchName": "Baghdad Service Center" }
      ]
    }
  ],
  "Vehicles": {
    "JTMHX01J8L4198293": {
      "VehicleEntries": [
        {
          "VIN": "JTMHX01J8L4198293",
          "InvoiceDate": "2024-01-15",
          "CompanyID": 1,
          "BranchID": 10,
          "BrandID": 1,
          "VariantCode": "VAR001",
          "Katashiki": "KAT-12345",
          "ExteriorColorCode": "WHT",
          "InteriorColorCode": "BLK"
        }
      ],
      "InitialOfficialVINs": [],
      "SSCAffectedVINs": [
        {
          "VIN": "JTMHX01J8L4198293",
          "CampaignCode": "SSC-2024-001",
          "Description": "Airbag inflator replacement",
          "LaborCode1": "LAB001",
          "PartNumber1": "PRT-AIR-001",
          "RepairDate": null
        }
      ],
      "WarrantyClaims": [],
      "LaborLines": [],
      "PartLines": [],
      "VehicleServiceActivations": [],
      "Accessories": [],
      "PaintThicknessInspections": [],
      "ExtendedWarrantyEntries": [],
      "WarrantyDateShifts": [],
      "FreeServiceItemDateShifts": [],
      "ItemClaims": [],
      "PaidServiceInvoices": [],
      "FreeServiceItemExcludedVINs": [],
      "VehicleInspections": []
    }
  },
  "BrokerInitialVehicles": [],
  "BrokerInvoices": [],
  "Parts": {
    "SU00302474": {
      "CatalogParts": [ ... ],
      "StockParts": [ ... ],
      "CompanyDeadStockParts": [ ... ]
    }
  }
}
```

**Key design decisions:**
- Vehicle data is organized **per VIN** (mirrors how real lookups work — you look up one VIN at a time)
- Part data is organized **per part number**
- `LookupOptions` and `Companies` are environment-level configuration
- `BrokerInitialVehicles` and `BrokerInvoices` are **environment-level** (not per-VIN) because in production these are looked up from `IVehicleLoockupStorageService` by brand, not by VIN. The generator can map them to the correct VINs.
- `VehicleInspections` is per-VIN (used by `VehicleServiceItemEvaluator` for mileage-based expiration)
- Part JSON keys match `PartAggregateCosmosModel` property names: `CatalogParts`, `StockParts`, `CompanyDeadStockParts`
- All field names use **PascalCase** matching C# property names exactly — we use case-sensitive deserialization (default `System.Text.Json` behavior) so JSON property names must match C# property names. This avoids the `id`/`ID` collision in Cosmos DB models and allows copy-pasting real data from Cosmos.
- The generated TypeScript types use camelCase (handled by the web component output generator, not by the input JSON)

## Output Data Format (generated JSON)

Format matches existing web component DTO expectations:

**vehicle-lookup.json:**
```json
{
  "JTMHX01J8L4198293": {
    "isAuthorized": true,
    "identifiers": { "vin": "JTMHX01J8L4198293", "variant": "VAR001", ... },
    "saleInformation": { ... },
    "warranty": { ... },
    "sscs": [ ... ],
    "serviceHistory": [ ... ],
    "serviceItems": [ ... ],
    "accessories": [ ... ],
    "paintThicknessInspections": [ ... ],
    "specification": { ... }
  }
}
```

**part-lookup.json:**
```json
{
  "SU00302474": {
    "partNumber": "SU00302474",
    "partDescription": "CLAMP",
    "prices": [ ... ],
    "stockParts": [ ... ],
    "deadStock": [ ... ]
  }
}
```

## Output Generator

A .NET console app at `ADP.TestData/Generator/`:

```
dotnet run --project ADP.TestData/Generator
```

What it does:
1. Reads each `ADP.TestData/environments/*.json`
2. For each VIN in the environment:
   - Builds `CompanyDataAggregateModel` from the per-VIN input data
   - Configures `LookupOptions` from the environment config
   - Runs all evaluators (with mocked external dependencies via NSubstitute)
   - Collects the `VehicleLookupDTO` result
3. For each part number:
   - Builds `PartAggregateCosmosModel`
   - Runs part evaluators
   - Collects the `PartLookupDTO` result
4. Writes output to **both** consumer locations:
   - `adp-web-components/src/features/mocks/data/generated/{env}/vehicle-lookup.json`
   - `adp-web-components/src/features/mocks/data/generated/{env}/part-lookup.json`
   - `ADP.Docs/Docs/docs/web-components/demo-data/{env}/vehicle-lookup.json`
   - `ADP.Docs/Docs/docs/web-components/demo-data/{env}/part-lookup.json`

The generator references `ADP.LookupServices` for the evaluator classes and models. It's similar to `WebComponentModelGenerator` — a dev-time tool, not a runtime dependency.

### When to re-run the generator

- After changing environment JSON data
- After changing evaluator logic (business logic changes)
- After adding a new environment
- Commit the regenerated output files alongside the code changes

## Planned Environments (2-3)

### 1. standard-dealer
A typical authorized dealer with straightforward data:
- A few vehicles with normal warranty flow (service activation → warranty start)
- Authorized and unauthorized vehicles
- SSCs with mixed repair statuses
- Service history with labor and part lines
- Standard 3-year warranty
- Parts with stock and pricing across countries

**Purpose:** Covers the most common evaluator paths. Good for documentation demos.

### 2. broker-dealer
A dealer that works with brokers:
- `LookupOptions.LookupBrokerStock = true`
- Vehicles at broker stock (no invoice yet)
- Vehicles sold through broker with invoice
- Broker-specific warranty date logic
- Extended warranty entries
- Date shifts (warranty and free service)

**Purpose:** Covers broker-specific code paths that the standard dealer doesn't exercise.

### 3. edge-cases
Unusual data combinations that have caused or could cause bugs:
- Vehicles with null invoice dates
- Multiple vehicles for the same VIN
- SSC with all three labor codes and all three part numbers
- Warranty claims with different statuses (Accepted, Rejected, Certified)
- Empty collections (no service history, no accessories)
- Large datasets to test performance-sensitive paths

**Purpose:** Regression testing and boundary conditions.

## How BDD Tests Use Environments

BDD tests read the environment JSON file directly (not generated output). Feature files reference environments by name:

```gherkin
Feature: Vehicle Authorization

Background:
  Given the "standard-dealer" environment is loaded

Scenario: Vehicle in dealer stock is Authorized
  When Checking "JTMHX01J8L4198293"
  Then the vehicle is considered Authorized

Scenario: Vehicle not in any source is Unauthorized
  When Checking "UNKNOWN_VIN_12345"
  Then the vehicle is considered Unauthorized
```

Scenarios can also modify environment data for edge-case testing:

```gherkin
Scenario: Adding a new vehicle to stock makes it Authorized
  Given the "standard-dealer" environment is loaded
  And an additional vehicle in dealer stock:
    | VIN               |
    | NEW_VIN_123456789 |
  When Checking "NEW_VIN_123456789"
  Then the vehicle is considered Authorized
```

This supports the goal of detecting issues when data is added, removed, or changed — because the environment data is persistent and modifications are explicit.

## Integration with Existing Systems

### Type Generation Pipeline
The existing `WebComponentModelGenerator` generates TypeScript types from C# DTOs. The output generator produces JSON that conforms to these same types. No changes needed to the type pipeline.

### Web Component Mock System
The existing `getMockFile()` function, `MockFiles` registry, and `isDev` mode continue to work unchanged. Generated files slot into the existing `mocks/data/` directory structure. The Stencil build's `copy` config already copies `src/features/mocks/data` → `dist/mocks`.

### Stencil Build Config
No changes needed — the existing copy target already covers `src/features/mocks/data/` recursively, so files in the new `generated/` subdirectory are included automatically.

### MkDocs
The `docs/web-components/demo-data/` directory is served as static assets by MkDocs. No plugin or configuration changes needed — MkDocs serves all files in the docs directory by default.

## Development Workflow

```
1. Edit environment JSON      (change test data or add a new environment)
   OR edit evaluator code    (change business logic)

2. dotnet run --project ADP.TestData/Generator
   → Regenerates output JSON in web components and docs source trees

3. git diff                  (review what changed in generated output)

4. git add + commit          (commit input changes + regenerated output together)
```

This mirrors the existing workflow for TypeScript type generation:
- Edit C# DTOs → run `WebComponentModelGenerator` → commit generated `.ts` files

## Migration Path

1. Create `ADP.TestData/` directory with `environments/standard-dealer.json`
2. Build the output generator console app (`ADP.TestData/Generator/`)
3. Create `adp-web-components/src/features/mocks/data/generated/` directory
4. Create `ADP.Docs/Docs/docs/web-components/demo-data/` directory
5. Run the generator to produce initial output
6. Verify web components work with generated data (compare against existing hand-crafted mocks)
7. Update BDD tests to support environment loading
8. Update `MockFiles` registry in web components to point at generated data
9. Remove hand-crafted mock data (old `part-lookup.json`, `mock-data.js` in templates)
10. Add `broker-dealer` and `edge-cases` environments
11. Embed web components with demo data in MkDocs documentation pages

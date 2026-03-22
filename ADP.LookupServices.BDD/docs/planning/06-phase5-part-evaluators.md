# Phase 5: Part Evaluators

**Goal:** Add BDD coverage for part lookup evaluators that use `PartAggregateCosmosModel` (a different aggregate model than the vehicle evaluators).

**Prerequisite:** Phase 4 complete

---

## Evaluators in Scope

| Evaluator | Returns | Key Logic |
|-----------|---------|-----------|
| PartPriceEvaluator | `(decimal?, IEnumerable<PartPriceDTO>)` | Resolves distributor purchase price and per-country/region pricing via resolver delegate |
| PartDeadStockEvaluator | `IEnumerable<DeadStockDTO>` | Identifies dead stock inventory items |
| PartStockEvaluator | `IEnumerable<StockPartDTO>` | Evaluates stock availability with quantity thresholds; optionally calls resolver for additional stock data |

---

## Key Differences from Vehicle Evaluators

1. **Different aggregate model:** `PartAggregateCosmosModel` instead of `CompanyDataAggregateModel`. It has three collections: `StockParts`, `CatalogParts`, `CompanyDeadStockParts`.
2. **All three evaluators are async** and require `LookupOptions` + `IServiceProvider`
3. **Part evaluators are `internal`** — add `[assembly: InternalsVisibleTo("LookupServices.BDD")]` to the Lookup.Services project.
4. **`CatalogParts`** is used by `PartPriceEvaluator` for base pricing data — Given steps must populate this collection.

---

## Infrastructure Changes

### Separate Given Steps for Part Aggregate

Create `StepDefinitions/PartSetupStepDefinitions.cs` with Given steps that populate a `PartAggregateCosmosModel` on the `TestContext`:

Add to `TestContext`:
```csharp
public PartAggregateCosmosModel PartAggregate { get; set; } = new();
```

### Given Steps

```gherkin
Given a part with number "PRT-001":
  | Field              | Value      |
  | Description        | Oil filter |
  | BrandID            | 1          |
  | DistributorPrice   | 25.50      |

Given stock for part "PRT-001":
  | LocationID | Quantity | CompanyID | BranchID |
  | LOC-001    | 50       | 1         | 10       |
  | LOC-002    | 10       | 1         | 20       |

Given dead stock for part "PRT-001":
  | LocationID | Quantity | LastMovementDate |
  | LOC-003    | 5        | 2023-01-15       |

Given catalog data for part "PRT-001":
  | Field              | Value      |
  | Description        | Oil filter |
  | BrandID            | 1          |
  | DistributorPrice   | 25.50      |
```
Maps to `CatalogPartModel` → populates `PartAggregate.CatalogParts`. Needed by `PartPriceEvaluator`.

---

## Feature Files

### Parts/PartPrice.feature

```gherkin
Feature: Part Pricing
  Part pricing includes the distributor purchase price and per-country/region
  retail prices. Prices can be resolved through a configurable resolver
  that may enrich or override the base pricing data.

Scenario: Part with distributor price and country prices
  Given a part with number "PRT-001" and distributor price 25.50
  And part prices for "PRT-001":
    | CountryID | Price  | Currency |
    | 1         | 35.00  | USD      |
    | 2         | 30.00  | EUR      |
  When evaluating price for part "PRT-001"
  Then the distributor price is 25.50
  And there are 2 country prices

Scenario: Price resolver enriches pricing data
  Given a part with number "PRT-001" and distributor price 25.50
  And the part price resolver is configured
  When evaluating price for part "PRT-001"
  Then the resolver-enriched prices are returned

Scenario: Part with no pricing data
  Given a part with number "PRT-001"
  When evaluating price for part "PRT-001"
  Then the distributor price is empty
  And there are 0 country prices
```

### Parts/PartDeadStock.feature

```gherkin
Feature: Part Dead Stock
  Dead stock identification flags parts that have been sitting in
  inventory without movement for an extended period.

Scenario: Part with dead stock entries
  Given dead stock for part "PRT-001":
    | LocationID | Quantity | LastMovementDate |
    | LOC-001    | 5        | 2023-01-15       |
    | LOC-002    | 3        | 2023-06-01       |
  When evaluating dead stock for part "PRT-001"
  Then there are 2 dead stock entries

Scenario: Part with no dead stock
  When evaluating dead stock for part "PRT-001"
  Then there are 0 dead stock entries
```

### Parts/PartStock.feature

```gherkin
Feature: Part Stock Availability
  Stock availability checks how many units of a part are available
  across locations. A quantity threshold can filter out parts below
  a minimum stock level.

Scenario: Part available at multiple locations
  Given stock for part "PRT-001":
    | LocationID | Quantity | CompanyID | BranchID |
    | LOC-001    | 50       | 1         | 10       |
    | LOC-002    | 10       | 1         | 20       |
  When evaluating stock for part "PRT-001"
  Then there are 2 stock locations
  And total stock quantity is 60

Scenario: Stock resolver enriches stock data
  Given stock for part "PRT-001":
    | LocationID | Quantity | CompanyID | BranchID |
    | LOC-001    | 50       | 1         | 10       |
  And the part stock resolver is configured
  When evaluating stock for part "PRT-001"
  Then the resolver-enriched stock is returned

Scenario: Quantity threshold filters distributor stock
  Given LookupOptions distributor stock threshold is 5
  And stock for part "PRT-001":
    | LocationID | Quantity | CompanyID | BranchID |
    | LOC-001    | 3        | 1         | 10       |
  When evaluating stock for part "PRT-001" with quantity 10
  Then the distributor stock lookup result is "QuantityNotWithinLookupThreshold"

Scenario: Part with no stock
  When evaluating stock for part "PRT-001"
  Then there are 0 stock locations
```

---

## Implementation Notes

- **Visibility:** All three part evaluator classes are `internal`. Add `[assembly: InternalsVisibleTo("LookupServices.BDD")]` to the Lookup.Services project (e.g., in `AssemblyInfo.cs` or a `Properties/` file).
- **PartAggregateCosmosModel structure:** Needs exploration during implementation to understand all fields and sub-collections. The feature files above are starting points based on the evaluator signatures.
- **Deprioritization:** If part lookup is stable and low-risk, this phase can be deferred. The vehicle evaluator phases (0-4) provide the highest value.
- **PartLookupSource parameter:** `PartPriceEvaluator.Evaluate()` takes a `PartLookupSource` enum (ReCaptcha, AzureFunctionKey, AppCheck, Anonymous, Token, Internal). This may affect pricing logic — scenarios should cover relevant sources.

---

## Verification

```bash
dotnet test ADP.LookupServices.BDD
```

All previous phase scenarios plus Part evaluator scenarios must pass.

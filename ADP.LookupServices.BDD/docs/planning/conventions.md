# BDD Conventions

Standards and patterns for BDD tests in this project. Follow these conventions when writing new feature files and step definitions.

---

## Feature File Naming

- Name after **business concepts**, not class names: `VehicleAuthorization.feature` not `VehicleAuthorizationEvaluator.feature`
- Use PascalCase: `VehicleServiceHistory.feature`
- Group complex evaluators into subdirectories: `ServiceItems/Eligibility.feature`

## Scenario Naming

- Describe **business situations**: `"Vehicle sold through broker with completed invoice"`
- Not test names: ~~`"Test broker invoice not null"`~~
- Not implementation details: ~~`"VehicleSaleInformationEvaluator returns broker with InvoiceDate"`~~

## Data Setup: Two Approaches

### 1. Environment-based (preferred for comprehensive tests)
```gherkin
Given the "standard-dealer" environment is loaded
And loading vehicle "JTMHX01J8L4198293" from the environment
```
Uses JSON fixtures from `ADP.TestData/environments/`. Reflects realistic data. Changes to environment data can surface regressions across evaluators.

### 2. Inline DataTables (for targeted edge cases)
```gherkin
Given vehicles in dealer stock:
  | VIN               | InvoiceDate |
  | 1FDKF37GXVEB34368 |             |
```
Self-contained within the feature file. Best for testing specific evaluator edge cases in isolation.

**Both approaches coexist.** Use environments for "does this evaluator work in a real-world scenario" and inline DataTables for "does this evaluator handle this edge case".

## Given Steps

- Use **domain language** for data conditions:
  - `Given vehicles in dealer stock:` (not `Given VehicleEntries:`)
  - `Given warranty claims:` (not `Given WarrantyClaimModels:`)
  - `Given the vehicle was sold through a broker:` (not `Given SaleInformation.Broker is not null`)
- Use DataTables for tabular data with column headers matching business concepts
- Optional DataTable columns default to null when omitted
- Dates use `YYYY-MM-DD` format in DataTables
- Relative dates (e.g., "2 years ago") use the format `"N years"` and are converted in step definitions

## When Steps

- Describe the **action being tested**:
  - `When Checking "VIN"` — sets the VIN being looked up (shared across evaluators)
  - `When evaluating warranty dates for "VIN"` — runs the warranty evaluator
  - `When evaluating service history with language "en"` — runs service history evaluator
- Evaluator-specific When steps go in the evaluator's step definitions file

## Then Steps

- Describe **business outcomes**:
  - `Then the vehicle is considered Authorized` (not `Then Evaluate() returns true`)
  - `Then the warranty start date is "2024-02-01"` (not `Then result.WarrantyStartDate == ...`)
  - `Then SSC "SSC-001" is marked as repaired` (not `Then sscDto.Repaired == true`)
- Use DataTables for multi-field assertions when it improves readability

## Gherkin Patterns

- Use `Background:` for setup shared across all scenarios in a file
- Use `Scenario Outline:` with `Examples:` tables when testing the same logic with different data
- Keep scenarios focused — one business rule per scenario
- Prefer multiple simple scenarios over one complex scenario with many assertions

---

## Step Definition Structure

### File Organization

- **One step definitions file per evaluator**: `VehicleSSCStepDefinitions.cs`, `WarrantyDateStepDefinitions.cs`
- **Shared data setup**: `SharedStepDefinitions.cs` — all Given steps that populate `CompanyDataAggregateModel`
- **LookupOptions setup**: `LookupOptionsStepDefinitions.cs` — Given steps for options configuration
- **Mock storage setup**: `MockStorageStepDefinitions.cs` — Given steps for `IVehicleLoockupStorageService` mock configuration

### Dependency Injection

All step definition classes inject `TestContext` via constructor:

```csharp
[Binding]
public class VehicleSSCStepDefinitions
{
    private readonly TestContext _context;

    public VehicleSSCStepDefinitions(TestContext context)
    {
        _context = context;
    }
}
```

### Evaluator Instantiation

Create evaluators directly in When/Then steps. Do not register evaluators in DI:

```csharp
[When("evaluating SSC for {string}")]
public void WhenEvaluatingSSC(string vin)
{
    _context.Aggregate.VIN = vin;
    _result = new VehicleSSCEvaluator(_context.Aggregate).Evaluate();
}
```

### Result Storage

Store evaluator results as private fields in the step definitions class. For results needed across step definition classes (e.g., `VehicleEntryModel` used by downstream evaluators), store in `TestContext`.

---

## TestContext

Shared test state held in `Support/TestContext.cs`:

```
TestContext
├── Aggregate (CompanyDataAggregateModel)     — populated by Given steps
├── PartAggregate (PartAggregateCosmosModel)  — populated by Given steps (Phase 5)
├── Options (LookupOptions)                    — configured by Given steps
├── ServiceProvider (IServiceProvider)         — NSubstitute mock
├── StorageService (IVehicleLoockupStorageService) — NSubstitute mock
├── CurrentVehicle (VehicleEntryModel?)        — result of VehicleEntryEvaluator
└── SaleInformation (VehicleSaleInformation?)  — result of VehicleSaleInformationEvaluator
```

Registered per-scenario via Reqnroll `[BeforeScenario]` hook.

---

## DataTable Column Conventions

| Column Name | Type | Format | Example |
|-------------|------|--------|---------|
| VIN | string | 17-char VIN | `1FDKF37GXVEB34368` |
| InvoiceDate | DateTime? | `YYYY-MM-DD` | `2024-01-15` |
| Invoiced Since | string | `N years` | `3 years` |
| ClaimStatus | enum | Name | `Accepted`, `Certified`, `Invoiced` |
| CompanyID | long? | Number | `1` |
| BranchID | long? | Number | `10` |
| BrandID | long? | Number | `1` |
| LaborCode | string | Code | `LAB001` |
| PartNumber | string | Code | `PRT-001` |
| CampaignCode | string | Code | `SSC-2024-001` |

Empty cells in DataTables are treated as null.

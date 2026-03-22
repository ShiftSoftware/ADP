# Phase 4: Full-Dependency Evaluators (+ IVehicleLoockupStorageService)

**Goal:** Add BDD coverage for evaluators that call `IVehicleLoockupStorageService` methods in addition to using `CompanyDataAggregateModel`, `LookupOptions`, and `IServiceProvider`.

**Prerequisite:** Phase 3 complete

---

## Evaluators in Scope

| Evaluator | Returns | Constructor Dependencies | Key Logic |
|-----------|---------|-------------------------|-----------|
| VehicleSaleInformationEvaluator | `Task<VehicleSaleInformation>` | Aggregate + Options + IServiceProvider + IVehicleLoockupStorageService | Determines sale details, broker stock status, end customer info; calls `GetBrokerStockAsync`, `GetCustomerAsync` |
| VehicleSpecificationEvaluator | `Task<VehicleSpecificationDTO>` | IVehicleLoockupStorageService only | Fetches vehicle specifications from storage service (simplest in this tier — no aggregate or options) |
| VehicleServiceItemEvaluator | `Task<(IEnumerable<VehicleServiceItemDTO>, bool)>` | IVehicleLoockupStorageService + Aggregate + Options + IServiceProvider | Most complex evaluator (~530 lines). Free/paid service item eligibility, campaign activation, claim status processing, mileage-based expiration |

---

## Mocking Infrastructure

### StepDefinitions/MockStorageStepDefinitions.cs (new)

Given steps that configure NSubstitute mock responses on `TestContext.StorageService`:

```gherkin
Given broker stock for brand 1:
  | BrokerID | BrokerName   | IsAtStock | InvoiceDate | InvoiceNumber | IsDeleted |
  | 100      | ABC Motors   | true      |             |               |           |

Given customer "CUST-001" at company 1 has name "John Smith" and phone "555-0100"

Given service items for brand 1:
  | ServiceItemID | Name          | Type | ExpirationMonths | ExpirationMileage |
  | SI-001        | Oil Change    | Free | 12               | 10000             |
```

These steps call NSubstitute's `.Returns()` on the mocked `IVehicleLoockupStorageService` to set up return values for specific method calls.

---

## New Given Steps

### Data Setup Steps (SharedStepDefinitions.cs)

```gherkin
Given paid service invoices:
  | InvoiceNumber | InvoiceDate | CompanyID | ServiceItemID |
  | PSI-001       | 2024-03-15  | 1         | SI-001        |
```
Maps to `PaidServiceInvoiceModel` → populates `Aggregate.PaidServiceInvoices`.

```gherkin
Given item claims:
  | ServiceItemID | ClaimStatus | ClaimDate  |
  | SI-001        | Processed   | 2024-04-01 |
```
Maps to `ItemClaimModel` → populates `Aggregate.ItemClaims`.

```gherkin
Given free service item excluded VINs:
  | VIN               | ServiceItemID |
  | 1FDKF37GXVEB34368 | SI-002        |
```
Maps to `FreeServiceItemExcludedVINModel` → populates `Aggregate.FreeServiceItemExcludedVINs`.

```gherkin
Given vehicle inspections:
  | InspectionDate | InspectorName | Mileage |
  | 2024-02-01     | John Smith    | 15000   |
```
Maps to `VehicleInspectionModel` → populates `Aggregate.VehicleInspections`.

---

## Feature Files

### VehicleSaleInformation.feature

```gherkin
Feature: Vehicle Sale Information
  Sale information identifies the selling company, branch, invoice details,
  and whether the vehicle was sold through a broker or directly.

Scenario: Direct sale with company and branch
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | InvoiceNumber | AccountNumber |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | INV-001       | ACC-001       |
  And company 1 is named "Toyota Motors"
  And branch 10 is named "Downtown Branch"
  When evaluating sale information for "1FDKF37GXVEB34368"
  Then the sale company is "Toyota Motors"
  And the sale branch is "Downtown Branch"
  And the invoice date is "2024-01-15"

Scenario: Vehicle at broker stock (no broker invoice)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When evaluating sale information for "1FDKF37GXVEB34368"
  Then the broker is "ABC Motors"
  And the broker invoice date is empty

Scenario: Vehicle sold through broker with invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock | InvoiceDate | InvoiceNumber |
    | 100      | ABC Motors | false     | 2024-02-01  | BINV-001      |
  And LookupOptions has broker stock lookup enabled
  When evaluating sale information for "1FDKF37GXVEB34368"
  Then the broker is "ABC Motors"
  And the broker invoice date is "2024-02-01"

Scenario: No vehicles returns null
  When evaluating sale information for "1FDKF37GXVEB34368"
  Then no sale information is available
```

### VehicleSpecification.feature

```gherkin
Feature: Vehicle Specification
  Vehicle specifications (engine, transmission, model details) are
  retrieved from the storage service based on the vehicle's variant and brand.

Scenario: Specification retrieved for known vehicle
  Given vehicles in dealer stock:
    | VIN               | VariantCode | BrandID |
    | 1FDKF37GXVEB34368 | VAR-001     | 1       |
  And vehicle specification for variant "VAR-001" brand 1 exists in storage
  When evaluating specification for "1FDKF37GXVEB34368"
  Then the specification is populated

Scenario: No specification available
  Given vehicles in dealer stock:
    | VIN               | VariantCode | BrandID |
    | 1FDKF37GXVEB34368 | VAR-999     | 1       |
  When evaluating specification for "1FDKF37GXVEB34368"
  Then the specification is empty
```

### ServiceItems/Eligibility.feature

```gherkin
Feature: Service Item Eligibility
  Free service items are filtered by brand, company, country, and VIN
  exclusion lists. A vehicle must meet all eligibility criteria for a
  service item to appear in its service plan.

Scenario: Service item eligible for vehicle's brand
  # Setup: service item configured for brand 1, vehicle has brand 1
  # Expected: service item appears in results

Scenario: Service item not eligible for different brand
  # Setup: service item configured for brand 2, vehicle has brand 1
  # Expected: service item does not appear

Scenario: Vehicle excluded from specific service item
  Given free service item excluded VINs:
    | VIN               | ServiceItemID |
    | 1FDKF37GXVEB34368 | SI-001        |
  # Expected: SI-001 does not appear for this VIN

Scenario: Per-vehicle eligibility check
  # Setup: PerVehicleEligibilitySupport = true in LookupOptions
  # Tests the per-vehicle eligibility resolution
```

**Note:** The exact scenarios for ServiceItems will need refinement after reading `VehicleServiceItemEvaluator.cs` in detail during implementation. The evaluator is ~530 lines with many code paths. The feature file outlines above are starting points.

### ServiceItems/Status.feature

```gherkin
Feature: Service Item Claim Status
  Each service item's status is determined by its claim history:
  Pending (no claim), Processed (claimed and accepted), Expired (past
  expiration), ActivationRequired (needs activation trigger), Cancelled.

Scenario: Service item with no claim is Pending
Scenario: Service item with processed claim is Processed
Scenario: Service item past expiration date is Expired
Scenario: Service item requiring activation trigger is ActivationRequired
Scenario: Service item with cancelled claim is Cancelled
```

### ServiceItems/Expiration.feature

```gherkin
Feature: Service Item Expiration
  Service items can expire based on time (months from warranty start)
  or mileage. Rolling expiration recalculates based on the last service date.

Scenario: Service item expires after configured months
Scenario: Service item expires after configured mileage
Scenario: Rolling expiration extends from last service date
Scenario: Service item not expired within both time and mileage limits
```

---

## Implementation Notes

- `VehicleSaleInformationEvaluator` requires `IVehicleLoockupStorageService` for `GetBrokerStockAsync` and `GetCustomerAsync`. These need NSubstitute `.Returns()` setup.
- `VehicleServiceItemEvaluator` is the most complex evaluator. Consider implementing its BDD tests incrementally — start with the simplest scenarios and add complexity over multiple iterations.
- The ServiceItems feature files are intentionally skeletal. They should be fleshed out during implementation after a thorough read of `VehicleServiceItemEvaluator.cs`.
- `VehicleSaleInformationEvaluator.Evaluate()` takes a `VehicleLookupRequestOptions` parameter with `LanguageCode` and `LookupEndCustomer` flags.

---

## Verification

```bash
dotnet test ADP.LookupServices.BDD
```

All previous phase scenarios plus Phase 4 scenarios must pass.

# Phase 2: Medium Evaluators (+ LookupOptions)

**Goal:** Add BDD coverage for evaluators that need `LookupOptions` configuration in addition to `CompanyDataAggregateModel`.

**Prerequisite:** Phase 1 complete

---

## Evaluators in Scope

| Evaluator | Returns | Key Logic |
|-----------|---------|-----------|
| WarrantyAndFreeServiceDateEvaluator | `VehicleWarrantyDTO` | Determines warranty start/end dates and free service dates based on sale type, broker involvement, date shifts, and brand-specific periods |

---

## LookupOptions Properties Used

The `WarrantyAndFreeServiceDateEvaluator` uses these `LookupOptions` properties:
- `WarrantyStartDateDefaultsToInvoiceDate` (bool, default: true) — whether to fall back to invoice date when no activation date exists
- `BrandStandardWarrantyPeriodsInYears` (Dictionary<long?, int>) — brand-specific warranty durations (defaults to 3 years if not configured)

---

## New Given Steps

### Data Setup Steps (SharedStepDefinitions.cs)

```gherkin
Given vehicle service activations:
  | WarrantyActivationDate | CompanyID |
  | 2024-01-15             | 1         |
```
Maps to `VehicleServiceActivation` → populates `Aggregate.VehicleServiceActivations`.

```gherkin
Given warranty date shifts:
  | NewDate    |
  | 2024-06-01 |
```
Maps to `WarrantyDateShiftModel` → populates `Aggregate.WarrantyDateShifts`.

```gherkin
Given free service item date shifts:
  | NewDate    |
  | 2024-06-01 |
```
Maps to `FreeServiceItemDateShiftModel` → populates `Aggregate.FreeServiceItemDateShifts`.

```gherkin
Given extended warranty entries:
  | StartDate  | EndDate    |
  | 2024-01-01 | 2027-01-01 |
```
Maps to `ExtendedWarrantyModel` → populates `Aggregate.ExtendedWarrantyEntries`.

### LookupOptions Steps (LookupOptionsStepDefinitions.cs — new file)

```gherkin
Given warranty start date defaults to invoice date
Given warranty start date does not default to invoice date
Given brand 1 has a warranty period of 5 years
```

These steps configure properties on `TestContext.Options`.

---

## Feature File: WarrantyDates.feature

```gherkin
Feature: Warranty and Free Service Dates
  Warranty and free service start dates are determined by the vehicle's sale
  circumstances. The system checks service activation records, warranty activation
  dates, and invoice dates (in that priority order). Broker sales have separate
  logic. Date shifts can override calculated dates. Extended warranty entries
  are tracked independently.

# --- Normal Sale (no broker) ---

Scenario: Warranty date from service activation
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-02-01"

Scenario: Warranty date falls back to vehicle warranty activation date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | WarrantyActivationDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 2024-01-20             |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-20"

Scenario: Warranty date defaults to invoice date when enabled
  Given warranty start date defaults to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-15"

Scenario: Warranty date is null when no activation and defaulting disabled
  Given warranty start date does not default to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is empty

# --- Warranty End Date ---

Scenario: Default warranty period is 3 years
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2027-02-01"

Scenario: Brand-specific warranty period
  Given brand 1 has a warranty period of 5 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2029-02-01"

# --- Date Shifts ---

Scenario: Warranty date shift overrides calculated start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And warranty date shifts:
    | NewDate    |
    | 2023-06-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2023-06-01"

Scenario: Free service date shift overrides free service start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And free service item date shifts:
    | NewDate    |
    | 2023-09-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2023-09-01"

# --- Extended Warranty ---

Scenario: Extended warranty dates from entries
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | StartDate  | EndDate    |
    | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

# --- Broker Sales ---
# Note: Broker scenarios depend on VehicleSaleInformation which requires
# VehicleSaleInformationEvaluator (Phase 4). Consider one of:
# a) Create a Given step that directly sets VehicleSaleInformation on TestContext
# b) Defer broker scenarios to Phase 4
# Decision needed during implementation.
```

---

## Implementation Notes

### Evaluate() Method Signature

`WarrantyAndFreeServiceDateEvaluator.Evaluate(VehicleEntryModel vehicle, VehicleSaleInformation saleInformation, bool ignoreBrokerStock)`

This method takes three parameters:
1. `VehicleEntryModel vehicle` — run `VehicleEntryEvaluator` first and use result
2. `VehicleSaleInformation saleInformation` — this comes from `VehicleSaleInformationEvaluator` which requires `IVehicleLoockupStorageService` (Phase 4)
3. `bool ignoreBrokerStock` — a flag from the lookup request

**For non-broker scenarios:** `saleInformation` can be constructed directly in a Given step with just `InvoiceDate` and `WarrantyActivationDate` fields. No need for the full `VehicleSaleInformationEvaluator`.

**For broker scenarios:** Need a Given step that sets `saleInformation.Broker` with appropriate fields. Defer to Phase 4 or create a simple Given step.

### Suggested Approach

Create a Given step for sale information:

```gherkin
Given the vehicle sale information:
  | InvoiceDate | WarrantyActivationDate |
  | 2024-01-15  | 2024-01-20             |
```

And for broker scenarios:

```gherkin
Given the vehicle was sold through a broker:
  | BrokerID | InvoiceDate |
  | 1        | 2024-03-01  |
```

These populate `TestContext.SaleInformation` directly without needing the full evaluator.

---

## Verification

```bash
dotnet test ADP.LookupServices.BDD
```

All Phase 0 + Phase 1 scenarios plus new WarrantyDates scenarios must pass.

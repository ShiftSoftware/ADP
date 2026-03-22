# Phase 1: Simple Evaluators

**Goal:** Add BDD coverage for all evaluators that only need `CompanyDataAggregateModel` — no `LookupOptions`, no mocked services.

**Prerequisite:** Phase 0 (foundation refactoring) complete

---

## Evaluators in Scope

| Evaluator | Returns | Key Logic |
|-----------|---------|-----------|
| VehicleAuthorizationEvaluator | `bool` | Already covered — refactored in Phase 0 |
| VehicleEntryEvaluator | `VehicleEntryModel` | Selects vehicle by invoice date (null takes priority, then most recent) |
| VehicleIdentifierEvaluator | `VehicleIdentifiersDTO` | Maps vehicle fields to identifier DTO; returns VIN-only DTO if vehicle is null |
| VehicleSSCEvaluator | `IEnumerable<SscDTO>` | Cross-references SSCs with warranty claims and labor lines to determine repair status |

---

## New Given Steps Needed

These go in `SharedStepDefinitions.cs` and populate additional collections on `CompanyDataAggregateModel`:

### Enrich Existing SSC Given Step

The current SSC Given step only takes a VIN column. Expand it to support all `SSCAffectedVINModel` fields:

```gherkin
Given SSC affected vehicles:
  | VIN               | CampaignCode | Description       | LaborCode1 | LaborCode2 | LaborCode3 | PartNumber1 | PartNumber2 | PartNumber3 | RepairDate |
  | 1G1ZC5E17BF283048 | SSC-2024-001 | Airbag recall     | LAB001     | LAB002     |            | PRT001      |             |             |            |
```

Optional columns should default to null when not provided.

### New: Warranty Claims

```gherkin
Given warranty claims:
  | ClaimStatus | RepairCompletionDate | DistributorComment           | LaborCode |
  | Accepted    | 2024-03-15           | Related to campaign SSC-2024-001 | LAB001    |
```

Maps to `WarrantyClaimModel`. The `ClaimStatus` column maps to the `ClaimStatus` enum (Accepted, Certified, Invoiced, etc.).

### New: Labor Lines

```gherkin
Given labor lines:
  | LaborCode | InvoiceDate | InvoiceStatus | CompanyID | BranchID | InvoiceNumber |
  | LAB001    | 2024-06-01  | X             | 1         | 10       | INV-001       |
```

Maps to `OrderLaborLineModel`. Needed for VehicleSSCEvaluator (repair detection via labor line) and later for VehicleServiceHistoryEvaluator (Phase 3).

### Enrich Existing Vehicle Entry Given Step

Add more columns to the dealer stock Given step to support `VehicleEntryModel` fields needed by `VehicleIdentifierEvaluator`:

```gherkin
Given vehicles in dealer stock:
  | VIN               | InvoiceDate | VariantCode | Katashiki    | ExteriorColorCode | InteriorColorCode | BrandID |
  | 1FDKF37GXVEB34368 | 2024-01-15  | VAR001      | KAT-12345    | WHT               | BLK               | 1       |
```

---

## Feature Files

### VehicleEntry.feature

Tests how `VehicleEntryEvaluator` selects the "current" vehicle from the dealer stock.

```gherkin
Feature: Vehicle Entry Selection
  When a dealer has multiple entries for the same VIN in their stock,
  the system selects the most relevant vehicle entry based on invoice date.
  Vehicles without an invoice date (still in stock, not yet sold) take priority.

Scenario: Vehicle without invoice date takes priority
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 |             |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When Checking "1FDKF37GXVEB34368"
  Then the selected vehicle has no invoice date

Scenario: Most recent invoice date selected when all have dates
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2023-06-01  |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When Checking "1FDKF37GXVEB34368"
  Then the selected vehicle has invoice date "2024-01-15"

Scenario: Single vehicle returned as-is
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When Checking "1FDKF37GXVEB34368"
  Then the selected vehicle has invoice date "2024-01-15"

Scenario: No vehicles returns null
  When Checking "1FDKF37GXVEB34368"
  Then no vehicle is selected
```

**Step definitions file:** `StepDefinitions/VehicleEntryStepDefinitions.cs`

### VehicleIdentifiers.feature

Tests how `VehicleIdentifierEvaluator` maps vehicle fields to identifier DTO.

```gherkin
Feature: Vehicle Identifiers
  The vehicle identifier provides key identification fields (VIN, variant,
  color codes, brand) extracted from the vehicle entry record.

Scenario: Identifiers populated from vehicle entry
  Given vehicles in dealer stock:
    | VIN               | VariantCode | Katashiki | ExteriorColorCode | InteriorColorCode | BrandID |
    | 1FDKF37GXVEB34368 | VAR001      | KAT-123   | WHT               | BLK               | 1       |
  When Checking "1FDKF37GXVEB34368"
  Then the vehicle identifiers are:
    | Field    | Value            |
    | VIN      | 1FDKF37GXVEB34368 |
    | Variant  | VAR001           |
    | Katashiki| KAT-123          |
    | Color    | WHT              |
    | Trim     | BLK              |
    | BrandID  | 1                |

Scenario: No vehicle entry returns VIN only
  When Checking "1FDKF37GXVEB34368"
  Then the vehicle identifiers are:
    | Field    | Value              |
    | VIN      | 1FDKF37GXVEB34368  |
    | Variant  |                    |
    | Katashiki|                    |
    | Color    |                    |
    | Trim     |                    |
    | BrandID  |                    |
```

**Step definitions file:** `StepDefinitions/VehicleIdentifierStepDefinitions.cs`

**Note:** `VehicleIdentifierEvaluator.Evaluate()` takes a `VehicleEntryModel` parameter. The Then step should first run `VehicleEntryEvaluator` to get the vehicle (or pass null if no entries), then pass it to `VehicleIdentifierEvaluator`.

### VehicleSSC.feature

Tests how `VehicleSSCEvaluator` determines SSC (Safety Service Campaign) repair status. This is the most complex evaluator at this tier.

```gherkin
Feature: Vehicle Safety Service Campaigns (SSC)
  Safety Service Campaigns (SSCs) are manufacturer-issued recalls or safety notices.
  Each SSC is checked for repair status by looking at three sources:
  1. Direct RepairDate on the SSC record
  2. Matching warranty claim (by campaign code in distributor comment, or by labor code)
  3. Matching labor line (by labor code with invoice status X or C)

Scenario: SSC repaired via direct RepairDate
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | RepairDate |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 2024-03-15 |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as repaired
  And SSC "SSC-001" has repair date "2024-03-15"

Scenario: SSC repaired via warranty claim matching campaign code in comment
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  And warranty claims:
    | ClaimStatus | RepairCompletionDate | DistributorComment       |
    | Accepted    | 2024-04-01           | Repair for SSC-001 done  |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as repaired
  And SSC "SSC-001" has repair date "2024-04-01"

Scenario: SSC repaired via warranty claim matching labor code
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  And warranty claims:
    | ClaimStatus | RepairCompletionDate | LaborCode |
    | Certified   | 2024-05-10           | LAB001    |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as repaired

Scenario: SSC repaired via labor line
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  And labor lines:
    | LaborCode | InvoiceDate | InvoiceStatus |
    | LAB001    | 2024-06-01  | X             |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as repaired
  And SSC "SSC-001" has repair date "2024-06-01"

Scenario: SSC not repaired when no evidence found
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as not repaired

Scenario: Warranty claim with non-matching status is ignored
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  And warranty claims:
    | ClaimStatus | RepairCompletionDate | DistributorComment     |
    | Rejected    | 2024-04-01           | Repair for SSC-001     |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as not repaired

Scenario: Labor line with non-matching status is ignored
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
  And labor lines:
    | LaborCode | InvoiceDate | InvoiceStatus |
    | LAB001    | 2024-06-01  | O             |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as not repaired

Scenario: Multiple SSCs with mixed repair status
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description       | LaborCode1 | RepairDate |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall     | LAB001     | 2024-03-15 |
    | 1G1ZC5E17BF283048 | SSC-002      | Seatbelt recall   | LAB002     |            |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" is marked as repaired
  And SSC "SSC-002" is marked as not repaired

Scenario: SSC parts and labor codes appear in result
  Given SSC affected vehicles:
    | VIN               | CampaignCode | Description   | LaborCode1 | LaborCode2 | PartNumber1 | PartNumber2 |
    | 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     | LAB002     | PRT001      | PRT002      |
  When Checking "1G1ZC5E17BF283048"
  Then SSC "SSC-001" has 2 labor codes
  And SSC "SSC-001" has 2 part numbers

Scenario: No SSC records returns null
  When Checking "1G1ZC5E17BF283048"
  Then there are no SSC records
```

**Step definitions file:** `StepDefinitions/VehicleSSCStepDefinitions.cs`

---

## Step Definitions Structure

Each evaluator gets its own step definitions file with:
- A **When** step that runs the evaluator and stores the result in `TestContext`
- **Then** steps that assert specific properties of the result

The shared Given steps in `SharedStepDefinitions.cs` are reused across all evaluators.

### Example: VehicleSSCStepDefinitions.cs

```csharp
[Binding]
public class VehicleSSCStepDefinitions
{
    private readonly TestContext _context;
    private IEnumerable<SscDTO>? _result;

    public VehicleSSCStepDefinitions(TestContext context)
    {
        _context = context;
    }

    [When("the SSC evaluation is performed")]
    // Or reuse "When Checking {string}" if the evaluator runs automatically
    public void WhenTheSSCEvaluationIsPerformed()
    {
        _result = new VehicleSSCEvaluator(_context.Aggregate).Evaluate();
    }

    [Then("SSC {string} is marked as repaired")]
    public void ThenSSCIsRepaired(string sscCode)
    {
        var ssc = _result?.FirstOrDefault(x => x.SSCCode == sscCode);
        Assert.NotNull(ssc);
        Assert.True(ssc.Repaired);
    }

    // ... more Then steps
}
```

---

## Implementation Notes

- `VehicleIdentifierEvaluator.Evaluate(VehicleEntryModel vehicle)` requires a vehicle parameter. The step definition should first run `VehicleEntryEvaluator` to get the vehicle, or accept null. Consider whether to make this implicit (always run VehicleEntryEvaluator first) or explicit (a dedicated When step).
- The warranty claims DataTable needs to support `LaborLines` as a sub-collection on `WarrantyClaimModel`. Consider using multiple Given steps: one for claims, one for claim labor lines.
- VehicleSSCEvaluator only considers warranty claims with status `Accepted`, `Certified`, or `Invoiced`. Feature scenarios should cover ignored statuses (e.g., `Rejected`).

---

## Verification

```bash
dotnet test ADP.LookupServices.BDD
```

All Phase 0 scenarios (VehicleAuthorization) plus new Phase 1 scenarios must pass.

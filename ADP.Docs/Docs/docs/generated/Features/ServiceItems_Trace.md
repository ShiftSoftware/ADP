---
hide:
    - toc
---

```gherkin
Feature: Service Item Diagnostic Trace
  When the evaluator is run with a ServiceItemTraceCollector wired in, it
  records every eligibility decision (accepted + rejected with reason),
  expansion outputs, status verdicts, and the final result. Production
  callers opt in by setting VehicleLookupRequestOptions.TraceServiceItemEvaluation;
  here the collector is wired directly because BDD instantiates the evaluator.

Scenario: Trace records each eligibility decision with rejection reason
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name            | BrandID | ActiveForMonths |
    | SI-MATCH      | 5K Service      | 1       | 24              |
    | SI-OTHERBRAND | Other-Brand Svc | 2       | 24              |
  And the trace free service start date is "2026-01-15"
  When evaluating service items with trace for "1FDKF37GXVEB34368" with language "en"
  Then the trace records 2 eligibility decisions
  And the trace records "SI-MATCH" as accepted
  And the trace records "SI-OTHERBRAND" as rejected at "Brand"
  And the trace final result has 1 items
  And the trace has at least 1 stage timing

Scenario: Trace explains base schedule cap contributors and role exclusions
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And the trace free service start date is "2026-01-15"
  When evaluating service items with trace for "1FDKF37GXVEB34368" with language "en"
  Then the trace base schedule cap is 40000
  And the trace records "SI-BASE" as a base schedule cap contributor
  And the trace excludes "SI-REWARD" from the base schedule cap because of "ProgramRole"
  And the trace records "SI-REWARD" as accepted
```

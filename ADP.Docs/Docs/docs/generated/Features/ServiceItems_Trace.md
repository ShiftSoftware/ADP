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

# The measurement the incident was missing. A code that did not count towards a milestone rule is
# invisible on the screen — indistinguishable from a service the customer never had — so the trace
# names each one and why it did not count. A code dropped on its qualifier is a rule to calibrate
# with the deployment; a code the reader made nothing of is a convention that has stopped fitting.
Scenario: Trace names every code a milestone rule passed over, and why
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name             | BrandID | ActiveForMonths |
    | SI-REWARD     | Milestone reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode      |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K   |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50KQA |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | ALT MDL100 50K   |
    | 1         | 10       | INV-4         | JOB-4               | 2026-05-01  | BRAKE PADS       |
  And the trace free service start date is "2026-01-15"
  When evaluating service items with trace for "1FDKF37GXVEB34368" with language "en"
  Then the trace reports the milestone reader uses convention "scenario"
  And the trace records a "QualifierFiltered" near miss for "SI-REWARD" on "PGM MDL100 50KQA"
  And the trace records a "ProgrammeFiltered" near miss for "SI-REWARD" on "ALT MDL100 50K"
  And the trace records a "Unresolved" near miss for "SI-REWARD" on "BRAKE PADS"
  And the trace records no near miss for "SI-REWARD" on "PGM MDL100 45K"

# A reader configured to read nothing and a customer who has never been back produce the same empty
# result. The trace reports the reader's state separately so the two can be told apart without
# waiting for somebody to complain about a specific vehicle.
Scenario: Trace tells a reader that reads nothing from a vehicle that has nothing to read
  Given LookupOptions milestone conventions:
    | Name     | Pattern                                     |
    | unusable | ^(?<program>PGM)\s+[A-Z0-9]+\s+[0-9]{1,3}K$ |
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name             | BrandID | ActiveForMonths |
    | SI-REWARD     | Milestone reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And the trace free service start date is "2026-01-15"
  When evaluating service items with trace for "1FDKF37GXVEB34368" with language "en"
  Then the trace reports the milestone reader cannot read
  And the trace reports the milestone convention "unusable" as "MissingMilestoneGroup"
  And the trace records a "Unresolved" near miss for "SI-REWARD" on "PGM MDL100 45K"
  And the trace records a "Unresolved" near miss for "SI-REWARD" on "PGM MDL100 50K"
```

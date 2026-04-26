---
hide:
    - toc
---

```gherkin
Feature: Free Service Item Date Shifts
  When a FreeServiceItemDateShift exists for a vehicle's VIN, its
  NewDate overrides the free service start date passed to the evaluator.
  Campaign-window checks and rolling expiry are calculated from the
  shifted date, not the original.

Scenario: Date shift for the vehicle's VIN overrides the provided free service start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And free service item date shifts:
    | VIN               | NewDate    |
    | 1FDKF37GXVEB34368 | 2026-03-01 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has activation "2026-03-01"
  And service item "SI-001" has expiration "2028-03-01"

Scenario: Date shift for a different VIN leaves the provided free service start date untouched
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And free service item date shifts:
    | VIN               | NewDate    |
    | OTHER_VIN_NO_HIT  | 2026-03-01 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has activation "2026-01-15"
  And service item "SI-001" has expiration "2028-01-15"
```

---
hide:
    - toc
---

```gherkin
Feature: De Facto Service Start Date drives Service Items
  When a vehicle is sold through a broker who has not yet inserted an invoice,
  the bulk lookup (IgnoreBrokerStock=false) would otherwise produce no service
  items at all — there is no FreeServiceStartDate to anchor eligibility. If
  the dealer has already claimed against the vehicle (UI path with
  IgnoreBrokerStock=true allows this), the earliest non-deleted claim date is
  used as the de facto service start so downstream items still project
  correctly. The act of claiming is itself evidence that the customer's
  service period has begun.

Scenario: Broker without invoice plus a claim activates the catalog
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And the sale has a broker without invoice
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-OIL        | Oil Change  | 1       | 12              | 5000           |
    | SI-INSP       | Inspection  | 1       | 24              | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-OIL        | 2025-06-15 | JOB-001   | INV-001       |
  When evaluating service items end-to-end for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OIL" is in the result
  And service item "SI-OIL" has status "processed"
  And service item "SI-INSP" is in the result
  And service item "SI-INSP" has status "pending"
  And service item "SI-INSP" has activation "2026-06-15"

Scenario: Broker without invoice and no claims projects no items (regression guard)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And the sale has a broker without invoice
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-OIL        | Oil Change | 1       | 12              | 5000           |
  When evaluating service items end-to-end for "1FDKF37GXVEB34368" with language "en"
  Then there are 0 service items

Scenario: Orphan claim still surfaces while de facto activates the eligible catalog
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And the sale has a broker without invoice
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths | MaximumMileage |
    | SI-OLD        | Brand-99 promo | 99      | 24              | 5000           |
    | SI-OK         | Brand-1 promo  | 1       | 24              | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-OLD        | 2025-06-15 | JOB-OLD   | INV-OLD       |
  When evaluating service items end-to-end for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OLD" is in the result
  And service item "SI-OLD" has status "processed"
  And service item "SI-OK" is in the result
  And service item "SI-OK" has status "pending"

Scenario: De facto anchor beats DateTime.Now when IncludeInactivatedFreeServiceItems is enabled
  # With a real claim available, the de facto fallback fills FreeServiceStartDate
  # in the warranty evaluator. The synthetic DateTime.Now path inside
  # ResolveActivationMode only kicks in when FreeServiceStartDate is still null
  # at the time the service item evaluator runs, so the claim wins and items are
  # plain "pending" — not "activationRequired".
  Given LookupOptions has include-inactivated-free-service-items enabled
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And the sale has a broker without invoice
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-OIL        | Oil Change | 1       | 24              | 5000           |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-OIL        | 2025-06-15 | JOB-001   | INV-001       |
  When evaluating service items end-to-end for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OIL" has status "processed"
  And service item "SI-OIL" has activation "2025-06-15"
  And activation is not required for the result
```

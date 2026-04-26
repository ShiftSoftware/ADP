---
hide:
    - toc
---

```gherkin
Feature: Ineligible Service Items with Existing Claims
  When a vehicle has a claim for a service item that is no longer
  in the vehicle's current eligible list (for example, because a
  brand filter excludes it), the evaluator still surfaces it as a
  Processed item so the claim history is visible. Its claim fields
  (ClaimDate, JobNumber, InvoiceNumber, PackageCode) come from the
  claim itself, not from the service item.

Scenario: Claimed item that is no longer eligible surfaces as processed with claim details
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths |
    | SI-OLDOIL     | Old Oil Svc | 99      | 24              |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-OLDOIL     | 2025-08-01 | JOB-OLD   | INV-OLD       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OLDOIL" has status "processed"
  And service item "SI-OLDOIL" has job number "JOB-OLD"
  And service item "SI-OLDOIL" has invoice number "INV-OLD"

Scenario: Ineligible processed item carries the package code from the claim
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths |
    | SI-OLDOIL     | Old Oil Svc | 99      | 24              |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber | PackageCode |
    | SI-OLDOIL     | 2025-08-01 | JOB-OLD   | INV-OLD       | PKG-CLAIM   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OLDOIL" has package code "PKG-CLAIM"
```

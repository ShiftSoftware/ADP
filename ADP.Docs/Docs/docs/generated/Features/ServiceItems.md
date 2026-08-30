---
hide:
    - toc
---

```gherkin
Feature: Vehicle Service Items
  Service items include free (campaign-based) and paid items. Free items
  are filtered by brand eligibility, VIN exclusions, and campaign dates.
  Each item has a status determined by claim history and expiration.
  Claiming a higher-mileage free item cancels the lower-mileage ones it
  skipped, and that reading outlives their own expiry dates — an item
  ended by cancellation keeps saying so, while one the customer let lapse
  before the higher claim stays expired.

# --- Free Service Items ---

Scenario: Free service item eligible for vehicle brand is pending
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "pending"

Scenario: Free service item with processed claim
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths |
    | SI-001        | Oil Change | 1       | 24              |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-001        | 2026-06-01 | JOB-001   | INV-001       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "processed"

Scenario: VIN excluded from free service items
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths |
    | SI-001        | Oil Change | 1       | 24              |
  And free service item excluded VINs:
    | VIN               |
    | 1FDKF37GXVEB34368 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then there are 0 service items

# --- Paid Service Items ---

Scenario: Paid service item appears in results
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName  |
    | 2026-03-15  | 1001          | PSI-001       | Extended Service |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "PSI-001" has type "paid"

# --- Dynamic Cancellation ---

Scenario: Pending item cancelled when higher-mileage item is processed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name         | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | 5K Service   | 1       | 24              | 5000           |
    | SI-002        | 10K Service  | 1       | 48              | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-002        | 2026-06-01 | JOB-001   | INV-001       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "cancelled"
  And service item "SI-002" has status "processed"

Scenario: A superseded item still reads cancelled once its own expiry date has passed
  Given the current UTC time is "2026-06-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service  | 1       | 3               | 5000           |
    | SI-10K        | 10K Service | 1       | 3               | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-10K        | 2026-02-01 | JOB-001   | INV-001       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-5K" has expiration "2026-04-15"
  And service item "SI-5K" has status "cancelled"
  And service item "SI-5K" is not claimable

Scenario: An item the customer let lapse before the higher-mileage claim stays expired
  Given the current UTC time is "2026-06-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service  | 1       | 3               | 5000           |
    | SI-10K        | 10K Service | 1       | 3               | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-10K        | 2026-05-01 | JOB-001   | INV-001       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-5K" has expiration "2026-04-15"
  And service item "SI-5K" has status "expired"

Scenario: A claim on the last valid day cancels the item when expiry is end-of-day
  Given the current UTC time is "2026-06-01 09:00:00"
  And LookupOptions has end-of-day service item expiry enabled
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service  | 1       | 3               | 5000           |
    | SI-10K        | 10K Service | 1       | 3               | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-10K        | 2026-04-15 | JOB-001   | INV-001       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-5K" has expiration "2026-04-15"
  And service item "SI-5K" has status "cancelled"
```

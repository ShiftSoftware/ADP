Feature: Vehicle Service Items
  Service items include free (campaign-based) and paid items. Free items
  are filtered by brand eligibility, VIN exclusions, and campaign dates.
  Each item has a status determined by claim history and expiration.

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

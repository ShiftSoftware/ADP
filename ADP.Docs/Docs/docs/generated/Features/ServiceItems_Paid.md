---
hide:
    - toc
---

```gherkin
Feature: Paid Service Items
  Paid service items come from PaidServiceInvoices. They're activated
  on the invoice date, expire on the invoice line's ExpireDate, and
  carry the invoice's PackageCode. VIN exclusion for free items does
  not affect paid items, because the exclusion only drops
  warranty-activated free items. In the final result, free items come
  before paid items.

Scenario: Paid service item carries invoice date, expire date and package code
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName  | ExpireDate | PackageCode |
    | 2026-03-15  | 1001          | PSI-001       | Extended Service | 2028-03-15 | PKG-EXT     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "PSI-001" has type "paid"
  And service item "PSI-001" has activation "2026-03-15"
  And service item "PSI-001" has expiration "2028-03-15"
  And service item "PSI-001" has package code "PKG-EXT"

Scenario: VIN exclusion drops free warranty-activated items but keeps paid items
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-FREE       | Oil Change | 1       | 24              | 10000          |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName  |
    | 2026-03-15  | 1001          | PSI-001       | Extended Service |
  And free service item excluded VINs:
    | VIN               |
    | 1FDKF37GXVEB34368 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-FREE" is not in the result
  And service item "PSI-001" is in the result

Scenario: Free service items are ordered before paid items in the result
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name    | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Svc | 1       | 24              | 10000          |
    | SI-5K         | 5K Svc  | 1       | 24              | 5000           |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName  |
    | 2026-03-15  | 1001          | PSI-001       | Extended Service |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the service items in order are:
    | ServiceItemID |
    | SI-5K         |
    | SI-10K        |
    | PSI-001       |
```

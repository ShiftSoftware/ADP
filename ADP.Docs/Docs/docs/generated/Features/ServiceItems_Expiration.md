---
hide:
    - toc
---

```gherkin
Feature: Service Item Expiration & Activation Dates
  Service items derive their ActivatedAt and ExpiresAt from their
  ValidityMode. FixedDateRange items use ValidFrom/ValidTo directly.
  RelativeToActivation items are rolled from the free service start
  date: items with a MaximumMileage chain sequentially (each item
  starts when the previous one expires), and items without a
  MaximumMileage activate at the start date and expire at the end of
  the sequential chain.

Scenario: FixedDateRange item uses ValidFrom and ValidTo directly
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ValidityMode   | ValidFrom  | ValidTo    |
    | SI-FIXED      | Promo      | 1       | FixedDateRange | 2027-01-01 | 2028-12-31 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-FIXED" has activation "2027-01-01"
  And service item "SI-FIXED" has expiration "2028-12-31"

Scenario: RelativeToActivation item with MaximumMileage expires ActiveFor after the start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-SINGLE     | Oil Change | 1       | 24              | 10000          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-SINGLE" has activation "2026-01-15"
  And service item "SI-SINGLE" has expiration "2028-01-15"

Scenario: Sequential items chain — each item activates when the previous expires
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name    | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Svc  | 1       | 24              | 5000           |
    | SI-10K        | 10K Svc | 1       | 24              | 10000          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-5K" has activation "2026-01-15"
  And service item "SI-5K" has expiration "2028-01-15"
  And service item "SI-10K" has activation "2028-01-15"
  And service item "SI-10K" has expiration "2030-01-15"

Scenario: Non-sequential item alongside sequential items activates at start and expires at chain end
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name    | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Svc  | 1       | 24              | 5000           |
    | SI-ANY        | Anytime | 1       | 12              |                |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-ANY" has activation "2026-01-15"
  And service item "SI-ANY" has expiration "2028-01-15"

# Pins current behavior of non-sequential rolling when there are no sequential items.
# This looks like a bug (item appears immediately expired) and is tracked as
# issue #14 in bdd-expansion STATUS.md. Test locks current behavior so a refactor
# change is a conscious decision, not an accidental regression.
Scenario: Sole non-sequential item expires at the free service start date (issue #14 — pinned)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name    | BrandID | ActiveForMonths |
    | SI-ANY        | Anytime | 1       | 12              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-ANY" has activation "2026-01-15"
  And service item "SI-ANY" has expiration "2026-01-15"
  And service item "SI-ANY" has status "expired"
```

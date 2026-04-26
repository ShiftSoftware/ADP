---
hide:
    - toc
---

```gherkin
Feature: Service Item Eligibility Filters
  Free service items are filtered by brand, company, country, campaign
  date window, and model-code (Katashiki/VariantCode prefix) before
  they are offered for a vehicle. Items with no per-model costs are
  eligible for any vehicle passing the upstream filters; items with
  per-model costs are only eligible when the vehicle's Katashiki or
  VariantCode prefix-matches one of those costs.

# --- Brand filter ---

Scenario: Service item for a different brand is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths |
    | SI-OTHER      | Oil Change | 2       | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHER" is not in the result

# --- Company filter ---

Scenario: Service item matching vehicle's company is included
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CompanyID | ActiveForMonths |
    | SI-COMPANY    | Oil Change | 1       | 1         | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-COMPANY" is in the result

Scenario: Service item for a different company is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CompanyID | ActiveForMonths |
    | SI-OTHERCO    | Oil Change | 1       | 99        | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHERCO" is not in the result

# --- Country filter ---

Scenario: Service item matching the sale country is included
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CountryID | ActiveForMonths |
    | SI-COUNTRY    | Oil Change | 1       | 42        | 24              |
  And the sale country is "42"
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-COUNTRY" is in the result

Scenario: Service item for a different country is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CountryID | ActiveForMonths |
    | SI-OTHERCTRY  | Oil Change | 1       | 42        | 24              |
  And the sale country is "99"
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHERCTRY" is not in the result

# --- Campaign date window (WarrantyActivation trigger) ---

Scenario: Service item with free service start date outside the campaign window is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CampaignStartDate | CampaignEndDate | ActiveForMonths |
    | SI-OLD        | Oil Change | 1       | 2023-01-01        | 2023-12-31      | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OLD" is not in the result

# --- Model-cost matching (Katashiki / Variant) ---

Scenario: Service item with ModelCosts but no matching Katashiki or Variant is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostKatashiki | ModelCostVariant |
    | SI-MODELONLY  | Oil Change | 1       | 24              | ZZZ999             | QQQ              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MODELONLY" is not in the result

Scenario: Service item with a matching Katashiki prefix is included even when ModelCosts are present
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostKatashiki |
    | SI-KATA       | Oil Change | 1       | 24              | ABC                |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-KATA" is in the result

Scenario: Service item with a matching Variant prefix is included even when ModelCosts are present
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostVariant |
    | SI-VAR        | Oil Change | 1       | 24              | XY               |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-VAR" is in the result
```

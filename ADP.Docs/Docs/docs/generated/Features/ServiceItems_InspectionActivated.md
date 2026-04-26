---
hide:
    - toc
---

```gherkin
Feature: Vehicle-Inspection Activated Service Items
  Service items with CampaignActivationTrigger = VehicleInspection are
  only eligible when the vehicle has an inspection matching the item's
  VehicleInspectionTypeID and falling inside the item's campaign window.
  Their ActivatedAt and ExpiresAt come from the matching inspection(s)
  based on CampaignActivationType:
   - FirstTriggerOnly: the earliest matching inspection
   - ExtendOnEachTrigger: the latest matching inspection
   - EveryTrigger: one claim item is emitted per matching inspection

Scenario: Inspection-activated item is eligible when a matching inspection exists
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-INSP       | Inspection  | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-INSP" is in the result

Scenario: Inspection-activated item with no matching inspection is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-INSP       | Inspection  | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-INSP" is not in the result

Scenario: FirstTriggerOnly uses the earliest matching inspection
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-FIRST      | Inspection  | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-05-01     | 7                       |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-FIRST" has activation "2026-02-01"
  And service item "SI-FIRST" has expiration "2027-02-01"

Scenario: ExtendOnEachTrigger uses the latest matching inspection
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActivationTrigger | ActivationType      | VehicleInspectionTypeID | ActiveForMonths |
    | SI-EXTEND     | Inspection  | 1       | VehicleInspection | ExtendOnEachTrigger | 7                       | 12              |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
    | 2026-05-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-EXTEND" has activation "2026-05-01"
  And service item "SI-EXTEND" has expiration "2027-05-01"

Scenario: EveryTrigger emits one claim item per matching inspection
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActivationTrigger | ActivationType | VehicleInspectionTypeID | ActiveForMonths |
    | SI-EVERY      | Inspection  | 1       | VehicleInspection | EveryTrigger   | 7                       | 12              |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
    | 2026-05-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then there are 2 service items with ID "SI-EVERY"
```

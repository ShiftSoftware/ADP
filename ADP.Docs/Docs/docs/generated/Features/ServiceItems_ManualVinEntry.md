---
hide:
    - toc
---

```gherkin
Feature: Manual-VIN-Entry Activated Service Items
  Service items with CampaignActivationTrigger = ManualVinEntry are only
  eligible when an admin has recorded a CampaignVinEntry for the VIN whose
  CampaignID matches the item's CampaignID and whose RecordedDate falls
  inside the item's campaign window.
  ActivatedAt and ExpiresAt come from the matching entry's RecordedDate
  based on CampaignActivationType:
   - FirstTriggerOnly: the earliest matching entry
   - ExtendOnEachTrigger: the latest matching entry
   - EveryTrigger: one claim item is emitted per matching entry
  Claims tie back to the activating entry via CampaignVinEntryID, so the
  same item can be processed for one entry while still pending for another.

Scenario: ManualVinEntry-activated item is eligible when a matching entry exists
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType   | CampaignID | ActiveForMonths |
    | SI-MVE        | Reward | 1       | ManualVinEntry    | FirstTriggerOnly | 500        | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MVE" is in the result

Scenario: ManualVinEntry-activated item with no matching entry is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType   | CampaignID | ActiveForMonths |
    | SI-MVE        | Reward | 1       | ManualVinEntry    | FirstTriggerOnly | 500        | 12              |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MVE" is not in the result

Scenario: Entry for a different campaign does not activate the item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType   | CampaignID | ActiveForMonths |
    | SI-MVE        | Reward | 1       | ManualVinEntry    | FirstTriggerOnly | 500        | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 999        | 2026-02-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MVE" is not in the result

Scenario: Entry recorded outside the campaign window is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType   | CampaignID | CampaignStartDate | CampaignEndDate | ActiveForMonths |
    | SI-MVE        | Reward | 1       | ManualVinEntry    | FirstTriggerOnly | 500        | 2026-01-01        | 2026-03-31      | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-05-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MVE" is not in the result

Scenario: FirstTriggerOnly uses the earliest matching entry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType   | CampaignID | ActiveForMonths |
    | SI-FIRST      | Reward | 1       | ManualVinEntry    | FirstTriggerOnly | 500        | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-05-01   |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-FIRST" has activation "2026-02-01"
  And service item "SI-FIRST" has expiration "2027-02-01"

Scenario: ExtendOnEachTrigger uses the latest matching entry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType      | CampaignID | ActiveForMonths |
    | SI-EXTEND     | Reward | 1       | ManualVinEntry    | ExtendOnEachTrigger | 500        | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
    | 1FDKF37GXVEB34368 | 500        | 2026-05-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-EXTEND" has activation "2026-05-01"
  And service item "SI-EXTEND" has expiration "2027-05-01"

Scenario: EveryTrigger emits one claim item per matching entry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType | CampaignID | ActiveForMonths |
    | SI-EVERY      | Reward | 1       | ManualVinEntry    | EveryTrigger   | 500        | 12              |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
    | 1FDKF37GXVEB34368 | 500        | 2026-05-01   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then there are 2 service items with ID "SI-EVERY"

Scenario: Claim tied to a specific entry processes only that entry's item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name   | BrandID | ActivationTrigger | ActivationType | CampaignID | ActiveForMonths |
    | SI-EVERY      | Reward | 1       | ManualVinEntry    | EveryTrigger   | 500        | 12              |
  And campaign VIN entries:
    | id      | VIN               | CampaignID | RecordedDate |
    | ENTRY-A | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
    | ENTRY-B | 1FDKF37GXVEB34368 | 500        | 2026-05-01   |
  And item claims:
    | ServiceItemID | CampaignVinEntryID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-EVERY      | ENTRY-A            | 2026-02-15 | JOB-1     | INV-1         |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-EVERY" for campaign VIN entry "ENTRY-A" has status "processed"
  And service item "SI-EVERY" for campaign VIN entry "ENTRY-B" has status "pending"
```

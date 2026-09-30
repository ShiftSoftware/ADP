Feature: Offers consumed by service history
  An explicit VIN offer can be processed by a service visit without a persisted claim.
  No official vehicle entry, inspection or manual claim interaction is required.

Background:
  Given service lookup time is "2026-02-15T00:00:00Z"
  And service items:
    | ServiceItemID | Name  | ActivationTrigger | ActivationType   | CampaignID | ActiveForMonths | ServiceConsumption |
    | OFFER         | Offer | ManualVinEntry    | FirstTriggerOnly | 500        | 12              | true               |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |

Scenario: Gray VIN with a qualifying visit is processed by the full lookup without a financial claim
  Given labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-02  | GENERAL     |
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | IgnoreBrokerStock |
    | false             |
  Then service item "OFFER" has status "processed"
  And service item "OFFER" has service evidence dated "2026-02-02" and no financial claim
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | IgnoreBrokerStock |
    | false             |
  Then service item "OFFER" has service evidence dated "2026-02-02" and no financial claim

Scenario: Earlier service does not consume a gray VIN offer
  Given labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate |
    | 1         | 10       | INV-1         | JOB-1               | 2026-01-31  |
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | IgnoreBrokerStock |
    | false             |
  Then service item "OFFER" has status "pending"
  And service item "OFFER" is not claimable

Scenario: Official VIN uses the same explicit eligibility date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2025-01-15  | 1         | 10       | 1       |
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | IgnoreBrokerStock |
    | false             |
  Then service item "OFFER" has activation "2026-02-01"
  And service item "OFFER" has status "pending"
  And service item "OFFER" is not claimable

Feature: Service Item Activation-Required Status
  When LookupOptions.IncludeInactivatedFreeServiceItems is enabled
  and no free service start date is provided, warranty-activated items
  are returned with an "activationRequired" status instead of being
  filtered out, and the evaluator's activationRequired return flag is
  set so the caller can prompt for activation.

Scenario: Warranty-activated item is shown as activationRequired when option is enabled and no start date is provided
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And LookupOptions has include-inactivated-free-service-items enabled
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "activationRequired"

Scenario: activationRequired return flag is true when any item is activationRequired
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And LookupOptions has include-inactivated-free-service-items enabled
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then activation is required for the result

Scenario: With a free service start date provided, items are pending and activation is not required
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And LookupOptions has include-inactivated-free-service-items enabled
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "pending"
  And activation is not required for the result

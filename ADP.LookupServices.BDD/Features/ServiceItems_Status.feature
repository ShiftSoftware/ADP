Feature: Service Item Status & Claimability
  A service item's status is Expired when ExpiresAt is past, Processed
  when a matching claim exists, or Pending otherwise. Pending items are
  Claimable, but a FixedDateRange item with a future ActivatedAt is not
  yet Claimable even though it is Pending. Claim matching uses both the
  ServiceItemID and the VehicleInspectionID so that when an item is
  cloned per inspection, only the clone tied to the claimed inspection
  becomes Processed.

Scenario: Item with ExpiresAt in the past has status "expired"
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ValidityMode   | ValidFrom  | ValidTo    |
    | SI-PAST       | Promo      | 1       | FixedDateRange | 2023-01-01 | 2024-12-31 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-PAST" has status "expired"

Scenario: FixedDateRange item activated in the future is pending but not claimable
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ValidityMode   | ValidFrom  | ValidTo    |
    | SI-FUTURE     | Promo      | 1       | FixedDateRange | 2030-01-01 | 2031-12-31 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-FUTURE" has status "pending"
  And service item "SI-FUTURE" is not claimable

Scenario: Pending item is claimable
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "pending"
  And service item "SI-001" is claimable

Scenario: Claim match distinguishes cloned inspection items by VehicleInspectionID
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActivationTrigger | ActivationType | VehicleInspectionTypeID | ActiveForMonths |
    | SI-EVERY      | Inspection | 1       | VehicleInspection | EveryTrigger   | 7                       | 12              |
  And vehicle inspections:
    | id       | InspectionDate | VehicleInspectionTypeID |
    | INSP-A   | 2026-02-01     | 7                       |
    | INSP-B   | 2026-05-01     | 7                       |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber | VehicleInspectionID |
    | SI-EVERY      | 2026-02-10 | JOB-A     | INV-A         | INSP-A              |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then there are 2 service items with ID "SI-EVERY"
  And service item "SI-EVERY" for inspection "INSP-A" has status "processed"
  And service item "SI-EVERY" for inspection "INSP-B" has status "pending"

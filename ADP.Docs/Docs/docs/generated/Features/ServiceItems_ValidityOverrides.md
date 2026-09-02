---
hide:
    - toc
---

```gherkin
Feature: Free Service Item Validity Overrides
  A FreeServiceItemValidityOverride moves one free service item's dates on one
  vehicle, after the rolling sequence and the reward unlock anchor have both had
  their say. It exists for the case a rule about a population gets wrong about a
  single vehicle — most often a customer already told they hold an item, under
  dates the rule has since recomputed.

  It moves dates and grants nothing. An item the vehicle is not offered, or one
  locked or missed because its conditions are unmet, is left where it is.

  Package codes below are invented and follow the shape
  <PROGRAM> <MODEL> <MILESTONE>K [<QUALIFIER>].

# The case the override was built for. The reward unlocked in March and its three months ran out in
# June, while the rolling slot it used to sit on runs to October — so a customer told in August that
# they held it now reads expired. The override restates when this one vehicle earned it.
Scenario: An override re-dates a reward whose window has already closed
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And free service item validity overrides:
    | VIN               | ServiceItemID | UnlockedOn | Reason                              |
    | 1FDKF37GXVEB34368 | SI-REWARD     | 2026-08-15 | Reward already promised to customer |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has activation "2026-08-15"
  And service item "SI-REWARD" has expiration "2026-11-15"
  And service item "SI-REWARD" is claimable

# Without the override the same vehicle reads expired. The anchor is doing what it was built to do,
# and the override is an exception to it rather than a repair of it.
Scenario: The same reward without an override stays on its unlock date
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has activation "2026-03-01"
  And service item "SI-REWARD" has expiration "2026-06-01"
  And service item "SI-REWARD" has status "expired"

# An expiry on its own extends an item where it stands. When the item was earned is a fact, and an
# operator buying more time is not claiming it was earned later.
Scenario: An expiry alone extends an item without moving its activation
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And free service item validity overrides:
    | VIN               | ServiceItemID | ExpiresAt  |
    | 1FDKF37GXVEB34368 | SI-REWARD     | 2026-12-31 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has activation "2026-03-01"
  And service item "SI-REWARD" has expiration "2026-12-31"
  And service item "SI-REWARD" is claimable

# Both dates given states the window outright: the expiry is the one written down, not the one the
# item's own ActiveFor would compute from the new activation.
Scenario: An explicit expiry outranks the one the re-dated activation would compute
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And free service item validity overrides:
    | VIN               | ServiceItemID | UnlockedOn | ExpiresAt  |
    | 1FDKF37GXVEB34368 | SI-REWARD     | 2026-08-15 | 2026-09-30 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has activation "2026-08-15"
  And service item "SI-REWARD" has expiration "2026-09-30"

# The override rewrites the named item's two dates and nothing else. The rolling sequence has already
# been computed by the time it runs, so the items around it keep the slots they always had.
Scenario: An overridden item leaves the rest of the sequence where it was
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
    | SI-LATE       | Later item        | 1       | 3               | 60000          |             |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
  And free service item validity overrides:
    | VIN               | ServiceItemID | UnlockedOn |
    | 1FDKF37GXVEB34368 | SI-REWARD     | 2026-08-15 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has activation "2026-08-15"
  And service item "SI-BASE" has activation "2026-01-15"
  And service item "SI-BASE" has expiration "2026-07-15"
  And service item "SI-LATE" has activation "2026-10-15"
  And service item "SI-LATE" has expiration "2027-01-15"

# An override names one item. Everything else on the vehicle is dated as it always was.
Scenario: An override on one item leaves the others alone
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil change | 1       | 6               | 10000          |
    | SI-002        | Service B  | 1       | 6               | 20000          |
  And free service item validity overrides:
    | VIN               | ServiceItemID | ExpiresAt  |
    | 1FDKF37GXVEB34368 | SI-002        | 2027-06-30 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has activation "2026-01-15"
  And service item "SI-001" has expiration "2026-07-15"
  And service item "SI-002" has activation "2026-07-15"
  And service item "SI-002" has expiration "2027-06-30"

# An override moves dates. It is not a way to hand out an item whose conditions are unmet, and a
# locked one keeps the cleared expiry it shows while it waits.
Scenario: An override does not unlock an item whose prerequisites are outstanding
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
  And free service item validity overrides:
    | VIN               | ServiceItemID | UnlockedOn | ExpiresAt  |
    | 1FDKF37GXVEB34368 | SI-REWARD     | 2026-08-15 | 2026-12-31 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "Locked"
  And service item "SI-REWARD" is not claimable
  And service item "SI-REWARD" has no expiry

# A revoked override is a deleted row, and a deleted row is not read. The storage layer filters these
# out too; the evaluator does not depend on it having done so.
Scenario: A deleted override is ignored
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil change | 1       | 6               | 10000          |
  And free service item validity overrides:
    | VIN               | ServiceItemID | ExpiresAt  | IsDeleted |
    | 1FDKF37GXVEB34368 | SI-001        | 2027-06-30 | true      |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has expiration "2026-07-15"

# An override naming an item this vehicle is not offered is inert rather than an error — the catalog
# moves, and a row outliving the item it names must not take a lookup down with it.
Scenario: An override naming an item the vehicle is not offered changes nothing
  Given the current UTC time is "2026-09-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil change | 1       | 6               | 10000          |
  And free service item validity overrides:
    | VIN               | ServiceItemID | ExpiresAt  |
    | 1FDKF37GXVEB34368 | SI-GONE       | 2027-06-30 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then there are 1 service items
  And service item "SI-001" has expiration "2026-07-15"
```

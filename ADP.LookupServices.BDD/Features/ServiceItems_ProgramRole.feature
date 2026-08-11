Feature: Service Item Program Roles and Base Schedule Cap
  A catalog item's program role controls only whether it can define the base
  scheduled-service mileage cap. Reward items remain ordinary free service
  items for ordering, rolling expiry, status, cancellation, warnings, signing,
  VIN exclusion, and claimed-item recovery.

Scenario Outline: Reward tiers require their vehicle's base schedule cap and latest paid-service history
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | <BaseCap>       |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | <RewardMileage> | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | Selection | Count | Values              |
    | serviceItems.baseSchedule.maximumMileage   | Equals      |           |       | <BaseCap>           |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Latest    | 2     | <PreviousA>,<PreviousB> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-A         | JOB-A               | 2026-02-01  | <PreviousA> |
    | 1         | 10       | INV-B         | JOB-B               | 2026-03-01  | <PreviousB> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

  Examples:
    | BaseCap | RewardMileage | PreviousA | PreviousB |
    | 40000   | 55000         | 45000     | 50000     |
    | 60000   | 75000         | 65000     | 70000     |
    | 80000   | 95000         | 85000     | 90000     |

Scenario: A reward is excluded when its configured base cap is wrong
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 60000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: A reward is excluded when no base schedule cap exists
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-REWARD     | Mileage reward | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: Missing required service history excludes a reward even when the cap matches
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator    | Selection | Count | Values      |
    | serviceItems.baseSchedule.maximumMileage | Equals      |           |       | 40000       |
    | serviceHistory.laborLines.packageCode    | ContainsAll | Latest    | 2     | 45000,50000 |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-045       | JOB-045             | 2026-02-01  | 45000       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: Static catalog filters are applied before base schedule cap membership
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | CountryID | Katashiki |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | 42        | ABC123    |
  And service items:
    | ServiceItemID | Name                | BrandID | CompanyID | CountryID | CampaignStartDate | CampaignEndDate | ModelCostKatashiki | IsDeleted | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end   | 1       | 1         | 42        |                   |                 |                     | false     | 6               | 40000          |             |
    | SI-DELETED    | Deleted schedule    | 1       | 1         | 42        |                   |                 |                     | true      | 6               | 99000          |             |
    | SI-BRAND      | Other brand         | 2       | 1         | 42        |                   |                 |                     | false     | 6               | 98000          |             |
    | SI-COMPANY    | Other company       | 1       | 99        | 42        |                   |                 |                     | false     | 6               | 97000          |             |
    | SI-COUNTRY    | Other country       | 1       | 1         | 99        |                   |                 |                     | false     | 6               | 96000          |             |
    | SI-WINDOW     | Old campaign        | 1       | 1         | 42        | 2020-01-01        | 2020-12-31      |                     | false     | 6               | 95000          |             |
    | SI-MODEL      | Other vehicle model | 1       | 1         | 42        |                   |                 | ZZZ                 | false     | 6               | 94000          |             |
    | SI-REWARD     | Mileage reward      | 1       | 1         | 42        |                   |                 |                     | false     | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

Scenario: A scheduled item contributes to the cap before its own custom conditions are evaluated
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-CONDITIONAL | Conditional schedule | 1       | 6               | 80000          |             |
    | SI-REWARD      | Mileage reward       | 1       | 3               | 95000          | Reward      |
  And service item "SI-CONDITIONAL" has eligibility conditions:
    | Field                                 | Operator    | Selection | Count | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Latest    | 1     | absent |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 80000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-CONDITIONAL" is not in the result
  And service item "SI-REWARD" is in the result

Scenario Outline: Malformed base schedule scalar values fail closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | ValuesJson   |
    | serviceItems.baseSchedule.maximumMileage | Equals   | <ValuesJson> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | ValuesJson          |
    | []                  |
    | ["0"]               |
    | ["-1"]              |
    | ["40,000"]          |
    | [" 40000"]          |
    | ["40000","50000"]  |

Scenario: A base schedule scalar condition with collection scope fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Selection | Count | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | Latest    | 1     | 40000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: A base schedule scalar condition with the wrong operator fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator    | Values |
    | serviceItems.baseSchedule.maximumMileage | ContainsAll | 40000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: Rewards remain in mileage order and rolling expiry with their own duration
  Given the current UTC time is "2026-02-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And a skipped items claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the service items in order are:
    | ServiceItemID |
    | SI-BASE       |
    | SI-REWARD     |
  And service item "SI-REWARD" has type "free"
  And service item "SI-REWARD" has activation "2026-07-15"
  And service item "SI-REWARD" has expiration "2026-10-15"
  And service item "SI-REWARD" has status "pending"
  And service item "SI-REWARD" has a signature
  And the warning with key "skippedItems" on service item "SI-REWARD" has body "SI-BASE"

Scenario: A processed reward cancels lower-mileage pending free items
  Given the current UTC time is "2026-02-01 09:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-REWARD     | 2026-08-01 | JOB-055   | INV-055       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-BASE" has status "cancelled"
  And service item "SI-REWARD" has status "processed"

Scenario: Free-item VIN exclusion removes a reward
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And free service item excluded VINs:
    | VIN               |
    | 1FDKF37GXVEB34368 |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario: A claimed reward remains visible when its eligibility conditions no longer match
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 60000  |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-REWARD     | 2026-08-01 | JOB-055   | INV-055       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" has status "processed"

Scenario: Paid invoice items never contribute to the catalog base schedule cap
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Mileage reward    | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values |
    | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName | MaximumMileage | ProgramRole     |
    | 2026-03-15  | 1001          | PSI-090       | Paid service    | 90000          | ScheduledService |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result
  And service item "PSI-090" has type "paid"

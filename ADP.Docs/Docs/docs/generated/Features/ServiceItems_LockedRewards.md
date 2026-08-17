---
hide:
    - toc
---

```gherkin
Feature: Locked and Missed Reward Items
  A reward gated on services the customer has yet to perform is shown from the
  first lookup rather than appearing out of nowhere once it is earned. It is
  returned locked and unclaimable, naming the services it waits on, and turns to
  missed once the window has closed. Which unmet conditions produce a card and
  which hide the item outright is declared per condition in the catalog, and the
  default hides — so a condition written before any of this existed behaves
  exactly as it did.

  Package codes below are invented and follow the shape
  <PROGRAM> <MODEL> <MILESTONE>K [<QUALIFIER>].

Scenario Outline: A reward moves from locked to claimable to missed as services are recorded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
    | serviceItems.baseSchedule.maximumMileage   | Equals      |            |         |           |           | 40000       | Hide      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <CodeA>     |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | <CodeB>     |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | <CodeC>     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result
  And service item "SI-REWARD" is not claimable

  Examples:
    | History                       | CodeA          | CodeB          | CodeC          |
    | nothing yet                   |                |                |                |
    | the first prerequisite only   | PGM MDL100 45K |                |                |
    | the second prerequisite only  | PGM MDL100 50K |                |                |
    | both, then a later service    | PGM MDL100 45K | PGM MDL100 50K | PGM MDL100 55K |
    | both, then a much later one   | PGM MDL100 45K | PGM MDL100 50K | PGM MDL100 70K |

Scenario Outline: An unearned reward is locked, and a lapsed one is missed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <CodeA>     |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | <CodeB>     |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | <CodeC>     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "<State>"

  Examples:
    | History                              | CodeA          | CodeB          | CodeC          | State  |
    | a brand-new vehicle                  |                |                |                | Locked |
    | part-way through the prerequisites   | PGM MDL100 45K |                |                | Locked |
    | prerequisites done out of order      | PGM MDL100 50K |                |                | Locked |
    | unrelated work only                  | BRAKE PADS     |                |                | Locked |
    | one prerequisite and a later service | PGM MDL100 45K | PGM MDL100 55K |                | Locked |
    | both prerequisites, then one further | PGM MDL100 45K | PGM MDL100 50K | PGM MDL100 55K | Missed |
    | both, then several further           | PGM MDL100 45K | PGM MDL100 50K | PGM MDL100 70K | Missed |

Scenario: A reward whose prerequisites are complete is offered normally
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | BRAKE PADS     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result
  And service item "SI-REWARD" is offered
  And service item "SI-REWARD" is claimable

# The prerequisites decompose into ticks, which is the whole reason the rule is two clauses rather
# than one predicate: a windowed condition can only ever say yes or no.
Scenario: A locked card names each outstanding service and when the done ones happened
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-REWARD     | Return reward | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "Locked"
  And service item "SI-REWARD" has prerequisites:
    | Mileage | Label | Satisfied | SatisfiedOn |
    | 45000   | 45K   | true      | 2026-02-01  |
    | 50000   | 50K   | false     |             |

Scenario: A service performed twice reports when the prerequisite was first met
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-REWARD     | Return reward | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-2         | JOB-2               | 2026-05-01  | PGM MDL100 45K |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "Locked"
  And service item "SI-REWARD" has prerequisites:
    | Mileage | Label | Satisfied | SatisfiedOn |
    | 45000   | 45K   | true      | 2026-02-01  |
    | 50000   | 50K   | false     |             |

# A locked reward is active for three months from the moment it unlocks, so a rolling expiry date
# computed from warranty activation would count down against a customer who cannot claim yet.
Scenario: A locked reward shows no expiry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "Locked"
  And service item "SI-REWARD" has no expiry
  And service item "SI-BASE" has expiration "2026-07-15"

# Showing a rejection can leak another market's catalog onto this dealer's screen, so only
# conditions the author has marked produce a card. Everything else drops as it always did.
Scenario Outline: Rejections that are facts about the vehicle stay hidden
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | CountryID | Katashiki |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | 42        | ABC123    |
  And service items:
    | ServiceItemID | Name              | BrandID   | CompanyID   | CountryID   | ActiveForMonths | MaximumMileage | ModelCostKatashiki   |
    | SI-BASE       | Base schedule end | 1         |             |             | 6               | 40000          |                      |
    | SI-REWARD     | Return reward     | <BrandID> | <CompanyID> | <CountryID> | 3               |                | <ModelCostKatashiki> |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator | Values    | WhenUnmet |
    | serviceItems.baseSchedule.maximumMileage | Equals   | <BaseCap> | Hide      |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Rejection                 | BrandID | CompanyID | CountryID | ModelCostKatashiki | BaseCap |
    | another brand             | 2       |           |           |                    | 40000   |
    | another company           | 1       | 99        |           |                    | 40000   |
    | another country           | 1       |           | 99        |                    | 40000   |
    | a model it does not cover | 1       |           |           | ZZZ999             | 40000   |
    | a different programme cap | 1       |           |           |                    | 60000   |

# The default is what every condition did before the property existed, so an untouched catalog is
# untouched behaviour.
Scenario: A condition that does not say what an unmet reading means hides the item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 50000  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

# One hiding condition settles the matter however the rest read: an item on another programme is
# not locked, it is irrelevant.
Scenario: A hiding condition outranks a locking one
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |             |
    | SI-REWARD     | Return reward     | 1       | 3               | 75000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                    | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode    | ContainsAll | Milestone  | PGM     | None      | All       | 65000,70000 | Lock      |
    | serviceItems.baseSchedule.maximumMileage | Equals      |            |         |           |           | 60000       | Hide      |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

# Both clauses fail on an empty history — the maximum is null, which equals nothing. A customer who
# has not started has not missed anything.
Scenario: Locked outranks missed when both clauses fail
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-REWARD     | Return reward | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is "Locked"

# A customer who earned the reward, claimed it, and then carried on servicing their car has not
# missed anything. Telling them so would be false.
Scenario: A claim outranks a window that has since closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
    | SI-REWARD     | Return reward | 1       | 3               | 55000          | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 | Lock      |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       | Miss      |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
    | 1         | 10       | INV-3         | JOB-3               | 2026-06-01  | PGM MDL100 55K |
  And item claims:
    | ServiceItemID | ClaimDate  | CompanyID | InvoiceNumber | JobNumber |
    | SI-REWARD     | 2026-04-01 | 1         | INV-R         | JOB-R     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result
  And service item "SI-REWARD" is offered
  And service item "SI-REWARD" has status "processed"
```

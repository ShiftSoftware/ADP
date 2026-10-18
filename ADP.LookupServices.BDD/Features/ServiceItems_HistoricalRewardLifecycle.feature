@historical-rewards
Feature: Historical reward lifecycle boundaries
  Historical customers earn the two named prerequisites through distinct maintenance jobs.
  Package evidence stays separate from expected labels. Source codes and identities here
  are synthetic. These scenarios call the normal service-item evaluator and lifecycle.

  Background:
    Given vehicles in dealer stock:
      | VIN         | InvoiceDate | CompanyID | BranchID | BrandID |
      | TEST-REWARD | 2029-01-01  | 1         | 10       | 1       |
    And service items:
      | ServiceItemID | Name             | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
      | BASE          | Standard service | 1       | 12              | 40000          |             |
      | REWARD        | Return reward    | 1       | 4               | 55000          | Reward      |
    And service item "REWARD" has eligibility conditions:
      | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      | WhenUnmet |
      | serviceItems.baseSchedule.maximumMileage   | Equals      |            |         |           |           | 40000       | Hide      |
      | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | Any       | All       | 45000,50000 | Lock      |
      | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | Any       |           | 50000       | Miss      |
    And the free service start date is "2029-01-01"
    And the current UTC time is "2030-11-01 09:00:00"

  Scenario Outline: Actual claims retain precedence after the third return
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | SECOND              | SECOND        | PGM MDL 5K  | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-20  | THIRD               | THIRD         | PGM MDL 55K | MAINT     |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Missed"
    Given item claims:
      | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber | Cost |
      | REWARD        | 2030-10-10 | CLAIM-JOB | CLAIM-INVOICE | 27   |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Processed"
    And service item "REWARD" retains claim "CLAIM-INVOICE" on "2030-10-10"
    And service item "REWARD" retains claimed cost 27
    Examples:
      | Mode            |
      | ImmediateMiss   |
      | CountInSequence |

  Scenario Outline: The calculated date shift controls the exclusive cohort cutoff
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And free service item date shifts:
      | VIN         | NewDate |
      | TEST-REWARD | <Shift> |
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-07-15  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2031-08-01  | FIRST               | FIRST         | PGM MDL 55K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2031-10-01  | SECOND              | SECOND        | PGM MDL 5K  | MAINT     |
    And the current UTC time is "2031-11-01"
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" has historical window "<Start>" through "<End>"
    Examples:
      | Mode            | Shift      | Outcome   | Start      | End        |
      | ImmediateMiss   | 2030-06-30 | Missed    | -          | -          |
      | CountInSequence | 2030-06-30 | Available | 2031-10-01 | 2032-02-01 |
      | ImmediateMiss   | 2030-07-01 | Missed    | -          | -          |
      | CountInSequence | 2030-07-01 | Missed    | -          | -          |
      | CountInSequence | 2030-07-02 | Missed    | -          | -          |

  Scenario Outline: Missing or conflicting last standard service keeps strict evaluation with diagnostics
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode    | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | <StandardCode> | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | OTHER               | OTHER         | <StandardCode> | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST         | PGM MDL 45K    | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | SECOND              | SECOND        | PGM MDL 50K    | MAINT     |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Available"
    And service item "REWARD" has historical window "2030-10-01" through "2031-02-01"
    And historical evaluation reports "missing or conflicting"
    And historical evaluation reports "Strict catalog evaluation retained."
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-08-01  | 2030-08-01   | PGM MDL 45K | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-10-01  | 2030-10-01   | PGM MDL 50K | MAINT     | SECOND    | SECOND        |
    Examples:
      | Mode            | StandardCode |
      | ImmediateMiss   |              |
      | CountInSequence |              |
      | ImmediateMiss   | ALT MDL 40K  |
      | CountInSequence | ALT MDL 40K  |

  Scenario Outline: Both policy choices apply independently to every reward tier
    Given service items:
      | ServiceItemID | Name             | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
      | BASE          | Standard service | 1       | 12              | <Cap>          |             |
      | REWARD        | Return reward    | 1       | 4               | <Reward>       | Reward      |
    And service item "REWARD" has eligibility conditions:
      | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values           | WhenUnmet |
      | serviceItems.baseSchedule.maximumMileage   | Equals      |            |         |           |           | <Cap>            | Hide      |
      | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | Any       | All       | <First>,<Second> | Lock      |
      | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | Any       |           | <Second>         | Miss      |
    And reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode           | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL <CapLabel>    | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST         | PGM MDL <RewardLabel> | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | SECOND              | SECOND        | PGM MDL 5K            | MAINT     |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" has historical window "<Start>" through "<End>"
    And service item "REWARD" retains historical prerequisites:
      | Label         | Mileage  | SatisfiedOn | EvidenceDate | PackageCode         | LaborCode | JobNumber | InvoiceNumber |
      | <FirstLabel>  | <First>  | <Date1>     | <Date1>      | PGM MDL <Evidence1> | MAINT     | <Job1>    | <Job1>        |
      | <SecondLabel> | <Second> | <Date2>     | <Date2>      | <Evidence2>         | <Labor2>  | <Job2>    | <Job2>        |
    Examples:
      | Cap   | CapLabel | Reward | RewardLabel | First | FirstLabel | Second | SecondLabel | Mode            | Outcome   | Start      | End        | Date1      | Evidence1 | Job1   | Date2      | Evidence2  | Labor2 | Job2   |
      | 40000 | 40K      | 55000  | 55K         | 45000 | 45K        | 50000  | 50K         | ImmediateMiss   | Missed    | -          | -          | 2030-10-01 | 5K        | SECOND | -          | -          | -      | -      |
      | 40000 | 40K      | 55000  | 55K         | 45000 | 45K        | 50000  | 50K         | CountInSequence | Available | 2030-10-01 | 2031-02-01 | 2030-08-01 | 55K       | FIRST  | 2030-10-01 | PGM MDL 5K | MAINT  | SECOND |
      | 60000 | 60K      | 75000  | 75K         | 65000 | 65K        | 70000  | 70K         | ImmediateMiss   | Missed    | -          | -          | 2030-10-01 | 5K        | SECOND | -          | -          | -      | -      |
      | 60000 | 60K      | 75000  | 75K         | 65000 | 65K        | 70000  | 70K         | CountInSequence | Available | 2030-10-01 | 2031-02-01 | 2030-08-01 | 75K       | FIRST  | 2030-10-01 | PGM MDL 5K | MAINT  | SECOND |
      | 80000 | 80K      | 95000  | 95K         | 85000 | 85K        | 90000  | 90K         | ImmediateMiss   | Missed    | -          | -          | 2030-10-01 | 5K        | SECOND | -          | -          | -      | -      |
      | 80000 | 80K      | 95000  | 95K         | 85000 | 85K        | 90000  | 90K         | CountInSequence | Available | 2030-10-01 | 2031-02-01 | 2030-08-01 | 95K       | FIRST  | 2030-10-01 | PGM MDL 5K | MAINT  | SECOND |

  Scenario Outline: Later invoices for completed jobs do not reset an expired reward
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | FIRST               | FIRST         |             | OIL-CHANGE |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | SECOND              | SECOND        | PGM MDL 45K | MAINT      |
      | TEST-REWARD | 1         | 10       | 2030-10-20  | FIRST               | LATER         |             | OIL-CHANGE |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Expired"
    And service item "REWARD" has historical window "2030-06-01" through "2030-10-01"
    Examples:
      | Mode            |
      | ImmediateMiss   |
      | CountInSequence |

  Scenario Outline: A validity override cannot grant a missing prerequisite
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     |
    And free service item validity overrides:
      | VIN         | ServiceItemID | UnlockedOn | ExpiresAt  |
      | TEST-REWARD | REWARD        | 2030-10-01 | 2031-02-01 |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Locked"
    Examples:
      | Mode            |
      | ImmediateMiss   |
      | CountInSequence |

  Scenario: An inactivated display date cannot select the historical cohort
    Given reward "REWARD" uses historical visits before "2030-07-01" with "CountInSequence"
    And the free service start is unknown
    And LookupOptions has include-inactivated-free-service-items enabled
    And the current UTC time is "2030-06-01"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-01-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-02-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | SECOND              | SECOND        | PGM MDL 5K  | MAINT     |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Locked"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | -           | -            | -           | -         | -         | -             |
      | 50K   | 50000   | -           | -            | -           | -         | -         | -             |

  Scenario Outline: Static brand and schedule filters still hide unrelated rewards
    Given reward "REWARD" uses historical visits before "2030-07-01" with "CountInSequence"
    And service items:
      | ServiceItemID | Name             | BrandID | ActiveForMonths | MaximumMileage | ProgramRole |
      | BASE          | Standard service | 1       | 12              | <Cap>          |             |
      | REWARD        | Return reward    | <Brand> | 4               | 55000          | Reward      |
    And service item "REWARD" has eligibility conditions:
      | Field                                    | Operator | Values | WhenUnmet |
      | serviceItems.baseSchedule.maximumMileage | Equals   | 40000  | Hide      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" is not in the result
    Examples:
      | Brand | Cap   |
      | 999   | 40000 |
      | 1     | 60000 |

  Scenario Outline: Split invoices within two jobs neither miss nor renew the reward
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST-SPLIT   | PGM MDL 5K  | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | SECOND              | SECOND        | PGM MDL 45K | MAINT     |
      | TEST-REWARD | 1         | 10       | 2030-10-20  | SECOND              | SECOND-LATER  | PGM MDL 45K | MAINT     |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Available"
    And service item "REWARD" has historical window "2030-10-01" through "2031-02-01"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-08-01  | 2030-08-01   | PGM MDL 5K  | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-10-01  | 2030-10-01   | PGM MDL 45K | MAINT     | SECOND    | SECOND        |
    Examples:
      | Mode            |
      | ImmediateMiss   |
      | CountInSequence |

  Scenario Outline: First job invoice dates the visit while evidence retains the actual oil work date
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST-WASH    |             | WASH       |
      | TEST-REWARD | 1         | 10       | 2030-08-03  | FIRST               | FIRST-OIL     |             | OIL-CHANGE |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | SECOND              | SECOND        | PGM MDL 45K | MAINT      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "Available"
    And service item "REWARD" has historical window "2030-10-01" through "2031-02-01"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode  | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-08-01  | 2030-08-03   | -           | OIL-CHANGE | FIRST     | FIRST-OIL     |
      | 50K   | 50000   | 2030-10-01  | 2030-10-01   | PGM MDL 45K | MAINT      | SECOND    | SECOND        |
    Examples:
      | Mode            |
      | ImmediateMiss   |
      | CountInSequence |



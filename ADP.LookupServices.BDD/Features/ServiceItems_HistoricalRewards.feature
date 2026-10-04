@historical-rewards
Feature: Historical conditional reward visits
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

  Scenario Outline: Lower periodic work followed by the second named milestone
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-10-20  | SECOND              | SECOND        | PGM MDL 50K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | REPAIR              | REPAIR        | REPAIR      | REPAIR    | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-08-01  | 2030-08-01   | PGM MDL 5K  | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-10-20  | 2030-10-20   | PGM MDL 50K | MAINT     | SECOND    | SECOND        |
    And service item "REWARD" has activation "2030-10-20"
    And service item "REWARD" has expiration "2031-02-20"

    Examples:
      | Mode            | Outcome   |
      | ImmediateMiss   | Available |
      | CountInSequence | Available |

  Scenario Outline: Repeating the first named milestone still completes two visits
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-05-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-15  | FIRST               | FIRST         | PGM MDL 45K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-10-20  | SECOND              | SECOND        | PGM MDL 45K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-08-15  | 2030-08-15   | PGM MDL 45K | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-10-20  | 2030-10-20   | PGM MDL 45K | MAINT     | SECOND    | SECOND        |
    And service item "REWARD" has activation "2030-10-20"
    And service item "REWARD" has expiration "2031-02-20"

    Examples:
      | Mode            | Outcome   |
      | ImmediateMiss   | Available |
      | CountInSequence | Available |

  Scenario Outline: A third oil visit closes the window before ordinary expiry
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  | Odometer |
      | TEST-REWARD | 1         | 10       | 2029-10-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-01-10  | FIRST               | FIRST         | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-04-10  | SECOND              | SECOND        | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-07-01  | THIRD               | THIRD         | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | FOURTH              | FOURTH        | OIL-MENU    | OIL-CHANGE | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode  | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-01-10  | 2030-01-10   | OIL-MENU    | OIL-CHANGE | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-04-10  | 2030-04-10   | OIL-MENU    | OIL-CHANGE | SECOND    | SECOND        |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Missed  |
      | CountInSequence | Missed  |

  Scenario Outline: Oil work closes the window before later reward and higher packages
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  | Odometer |
      | TEST-REWARD | 1         | 10       | 2029-11-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-01-15  | FIRST               | FIRST         | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-03-15  | SECOND              | SECOND        | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-05-15  | THIRD               | THIRD         | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-07-10  | FOURTH              | FOURTH        | PGM MDL 55K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-15  | FIFTH               | FIFTH         | PGM MDL 65K | MAINT      | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode  | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-01-15  | 2030-01-15   | OIL-MENU    | OIL-CHANGE | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-03-15  | 2030-03-15   | OIL-MENU    | OIL-CHANGE | SECOND    | SECOND        |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Missed  |
      | CountInSequence | Missed  |

  Scenario Outline: Late standard claim and confirmed source mirrors preserve three distinct visits
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And historical jobs from sources 1/10 and 2/20 are confirmed mirrors
    And item claims:
      | ServiceItemID | ClaimDate  | CompanyID | JobNumber | InvoiceNumber |
      | BASE          | 2029-12-01 | 1         | STANDARD  | STANDARD      |
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  | Odometer |
      | TEST-REWARD | 1         | 10       | 2029-09-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2029-12-01  | FIRST               | FIRST         | PGM MDL 45K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | SECOND              | SECOND        |             | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | SECOND              | SECOND        | ADDITIVE44K | ADDITIVE   | 100      |
      | TEST-REWARD | 1         | 10       | 2030-07-01  | THIRD               | THIRD         | PGM MDL 55K | MAINT      | 100      |
      | TEST-REWARD | 2         | 20       | 2029-09-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      | 100      |
      | TEST-REWARD | 2         | 20       | 2029-12-01  | FIRST               | FIRST         | PGM MDL 45K | MAINT      | 100      |
      | TEST-REWARD | 2         | 20       | 2030-04-01  | SECOND              | SECOND        |             | OIL-CHANGE | 100      |
      | TEST-REWARD | 2         | 20       | 2030-04-01  | SECOND              | SECOND        | ADDITIVE44K | ADDITIVE   | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode  | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2029-12-01  | 2029-12-01   | PGM MDL 45K | MAINT      | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-04-01  | 2030-04-01   | -           | OIL-CHANGE | SECOND    | SECOND        |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Missed  |
      | CountInSequence | Missed  |

  Scenario Outline: Three lower periodic jobs count despite split invoices
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-05-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | FIRST               | FIRST         | PGM MDL 5K  | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-07-01  | SECOND              | SECOND        | PGM MDL 5K  | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | THIRD               | THIRD         | PGM MDL 5K  | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | THIRD               | THIRD-SPLIT   | PGM MDL 5K  | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-06-01  | 2030-06-01   | PGM MDL 5K  | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-07-01  | 2030-07-01   | PGM MDL 5K  | MAINT     | SECOND    | SECOND        |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Missed  |
      | CountInSequence | Missed  |

  Scenario Outline: One reward level return is missed by default or earns only the first prerequisite
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-07-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | FIRST               | FIRST         | PGM MDL 55K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate    | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | <Date1>     | <EvidenceDate1> | <Package1>  | <Labor1>  | <Job1>    | <Invoice1>    |
      | 50K   | 50000   | <Date2>     | <EvidenceDate2> | <Package2>  | <Labor2>  | <Job2>    | <Invoice2>    |

    Examples:
      | Mode            | Outcome | Date1      | EvidenceDate1 | Package1    | Labor1 | Job1  | Invoice1 | Date2 | EvidenceDate2 | Package2 | Labor2 | Job2 | Invoice2 |
      | ImmediateMiss   | Missed  | -          | -             | -           | -      | -     | -        | -     | -             | -        | -      | -    | -        |
      | CountInSequence | Locked  | 2030-09-01 | 2030-09-01    | PGM MDL 55K | MAINT  | FIRST | FIRST    | -     | -             | -        | -      | -    | -        |

  Scenario Outline: Reward level then lower periodic work can unlock under sequence counting
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | FIRST               | FIRST         | PGM MDL 55K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | FIRST               | FIRST         | ADDITIVE44K | ADDITIVE  | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | SECOND              | SECOND        | PGM MDL 45K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | SECOND              | SECOND        |             | WASH      | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" has historical window "<WindowStart>" through "<WindowEnd>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate    | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | <Date1>     | <EvidenceDate1> | <Package1>  | <Labor1>  | <Job1>    | <Invoice1>    |
      | 50K   | 50000   | <Date2>     | <EvidenceDate2> | <Package2>  | <Labor2>  | <Job2>    | <Invoice2>    |

    Examples:
      | Mode            | Outcome   | Date1      | EvidenceDate1 | Package1    | Labor1 | Job1   | Invoice1 | Date2      | EvidenceDate2 | Package2    | Labor2 | Job2   | Invoice2 | WindowStart | WindowEnd  |
      | ImmediateMiss   | Missed    | 2030-08-01 | 2030-08-01    | PGM MDL 45K | MAINT  | SECOND | SECOND   | -          | -             | -           | -      | -      | -        | -           | -          |
      | CountInSequence | Available | 2030-06-01 | 2030-06-01    | PGM MDL 55K | MAINT  | FIRST  | FIRST    | 2030-08-01 | 2030-08-01    | PGM MDL 45K | MAINT  | SECOND | SECOND   | 2030-08-01  | 2030-12-01 |

  Scenario Outline: Later invoices for an earlier job do not create new visits or renew validity
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | FIRST               | FIRST         | PGM MDL 55K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-20  | SECOND              | SECOND        | PGM MDL 50K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | FIRST               | FIRST-LATER   | PGM MDL 55K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-05  | FIRST               | FIRST-LAST    | PGM MDL 55K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" has historical window "<WindowStart>" through "<WindowEnd>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate    | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | <Date1>     | <EvidenceDate1> | <Package1>  | <Labor1>  | <Job1>    | <Invoice1>    |
      | 50K   | 50000   | <Date2>     | <EvidenceDate2> | <Package2>  | <Labor2>  | <Job2>    | <Invoice2>    |

    Examples:
      | Mode            | Outcome   | Date1      | EvidenceDate1 | Package1    | Labor1 | Job1   | Invoice1 | Date2      | EvidenceDate2 | Package2    | Labor2 | Job2   | Invoice2 | WindowStart | WindowEnd  |
      | ImmediateMiss   | Missed    | 2030-08-20 | 2030-08-20    | PGM MDL 50K | MAINT  | SECOND | SECOND   | -          | -             | -           | -      | -      | -        | -           | -          |
      | CountInSequence | Available | 2030-06-01 | 2030-06-01    | PGM MDL 55K | MAINT  | FIRST  | FIRST    | 2030-08-20 | 2030-08-20    | PGM MDL 50K | MAINT  | SECOND | SECOND   | 2030-08-20  | 2030-12-20 |

  Scenario Outline: Exact prerequisites followed by reward and higher work are missed
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2029-11-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-02-01  | FIRST               | FIRST         | PGM MDL 45K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | SECOND              | SECOND        | PGM MDL 50K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-05-01  | THIRD               | THIRD         | PGM MDL 55K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | FOURTH              | FOURTH        | PGM MDL 60K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-02-01  | 2030-02-01   | PGM MDL 45K | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-04-01  | 2030-04-01   | PGM MDL 50K | MAINT     | SECOND    | SECOND        |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Missed  |
      | CountInSequence | Missed  |

  Scenario Outline: One return stays locked even with a low recorded odometer
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | FIRST               | FIRST         | PGM MDL 45K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-10-01  | 2030-10-01   | PGM MDL 45K | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | -           | -            | -           | -         | -         | -             |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Locked  |
      | CountInSequence | Locked  |

  Scenario Outline: Two visits expire at the ordinary four month boundary
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode  | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-03-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-04-01  | FIRST               | FIRST         | OIL-MENU    | OIL-CHANGE | 100      |
      | TEST-REWARD | 1         | 10       | 2030-06-01  | SECOND              | SECOND        | PGM MDL 45K | MAINT      | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode  | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-04-01  | 2030-04-01   | OIL-MENU    | OIL-CHANGE | FIRST     | FIRST         |
      | 50K   | 50000   | 2030-06-01  | 2030-06-01   | PGM MDL 45K | MAINT      | SECOND    | SECOND        |
    And service item "REWARD" has activation "2030-06-01"
    And service item "REWARD" has expiration "2030-10-01"

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Expired |
      | CountInSequence | Expired |

  Scenario Outline: Additive wash and repair work cannot establish maintenance
    Given reward "REWARD" uses historical visits before "2030-07-01" with "<Mode>"
    And labor lines:
      | VIN         | CompanyID | BranchID | InvoiceDate | OrderDocumentNumber | InvoiceNumber | PackageCode | LaborCode | Odometer |
      | TEST-REWARD | 1         | 10       | 2030-08-01  | STANDARD            | STANDARD      | ALT MDL 40K | MAINT     | 100      |
      | TEST-REWARD | 1         | 10       | 2030-08-02  | ADDITIVE            | ADDITIVE      | ADDITIVE44K | ADDITIVE  | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-01  | REPAIR              | REPAIR        |             | REPAIR    | 100      |
      | TEST-REWARD | 1         | 10       | 2030-09-02  | WASH                | WASH          |             | WASH      | 100      |
      | TEST-REWARD | 1         | 10       | 2030-10-01  | FIRST               | FIRST         | PGM MDL 45K | MAINT     | 100      |
    When evaluating service items for "TEST-REWARD" with language "en"
    Then service item "REWARD" has reward outcome "<Outcome>"
    And service item "REWARD" retains historical prerequisites:
      | Label | Mileage | SatisfiedOn | EvidenceDate | PackageCode | LaborCode | JobNumber | InvoiceNumber |
      | 45K   | 45000   | 2030-10-01  | 2030-10-01   | PGM MDL 45K | MAINT     | FIRST     | FIRST         |
      | 50K   | 50000   | -           | -            | -           | -         | -         | -             |

    Examples:
      | Mode            | Outcome |
      | ImmediateMiss   | Locked  |
      | CountInSequence | Locked  |


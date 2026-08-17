---
hide:
    - toc
---

```gherkin
Feature: Service Item Eligibility Filters
  Free service items are filtered by brand, company, country, campaign
  date window, and model-code (Katashiki/VariantCode prefix) before
  they are offered for a vehicle. Items with no per-model costs are
  eligible for any vehicle passing the upstream filters; items with
  per-model costs are only eligible when the vehicle's Katashiki or
  VariantCode prefix-matches one of those costs.

# --- Brand filter ---

Scenario: Service item for a different brand is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths |
    | SI-OTHER      | Oil Change | 2       | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHER" is not in the result

# --- Company filter ---

Scenario: Service item matching vehicle's company is included
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CompanyID | ActiveForMonths |
    | SI-COMPANY    | Oil Change | 1       | 1         | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-COMPANY" is in the result

Scenario: Service item for a different company is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CompanyID | ActiveForMonths |
    | SI-OTHERCO    | Oil Change | 1       | 99        | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHERCO" is not in the result

# --- Country filter ---

Scenario: Service item matching the vehicle country is included
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | CountryID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | 42        |
  And service items:
    | ServiceItemID | Name       | BrandID | CountryID | ActiveForMonths |
    | SI-COUNTRY    | Oil Change | 1       | 42        | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-COUNTRY" is in the result

Scenario: Service item for a different country is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | CountryID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | 99        |
  And service items:
    | ServiceItemID | Name       | BrandID | CountryID | ActiveForMonths |
    | SI-OTHERCTRY  | Oil Change | 1       | 42        | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OTHERCTRY" is not in the result

# --- Campaign date window (WarrantyActivation trigger) ---

Scenario: Service item with free service start date outside the campaign window is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | CampaignStartDate | CampaignEndDate | ActiveForMonths |
    | SI-OLD        | Oil Change | 1       | 2023-01-01        | 2023-12-31      | 24              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OLD" is not in the result

# --- Model-cost matching (Katashiki / Variant) ---

Scenario: Service item with ModelCosts but no matching Katashiki or Variant is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostKatashiki | ModelCostVariant |
    | SI-MODELONLY  | Oil Change | 1       | 24              | ZZZ999             | QQQ              |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MODELONLY" is not in the result

Scenario: Service item with a matching Katashiki prefix is included even when ModelCosts are present
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostKatashiki |
    | SI-KATA       | Oil Change | 1       | 24              | ABC                |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-KATA" is in the result

Scenario: Service item with a matching Variant prefix is included even when ModelCosts are present
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID | Katashiki | VariantCode |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       | ABC123    | XYZ         |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | ModelCostVariant |
    | SI-VAR        | Oil Change | 1       | 24              | XY               |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-VAR" is in the result

# --- Service-history eligibility conditions ---

Scenario: Omitted value matching preserves exact case-insensitive package-code matching
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                      | Operator    | Selection | Count | Values                      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Latest    | 2     | package-45,package-50       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-045       | JOB-045             | 2026-02-01  | PACKAGE-45  |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | Package-50  |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is in the result

Scenario: Package-code suffixes collectively satisfy an eligibility condition
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson         |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 2     | [" 45K"," 50K"] |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-045       | JOB-045             | 2026-02-01  | MODEL 45K   |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | MODEL 50K   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is in the result

Scenario: Package-code suffix matching is case-insensitive
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 50k"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | MODEL 50K   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is in the result

Scenario Outline: A leading-space suffix does not match a longer numeric package code
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 50K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode  |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | <PackageCode> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is not in the result

  Examples:
    | PackageCode |
    | 150K        |
    | 250K        |

Scenario Outline: Empty package-code suffixes fail closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson  |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | <ValuesJson> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | MODEL 50K   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is not in the result

  Examples:
    | ValuesJson |
    | [""]       |
    | ["   "]    |
    | [null]     |

Scenario: An unsupported package-code value match fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | 99         | Latest    | 1     | [" 50K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | MODEL 50K   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is not in the result

Scenario: Latest service history package codes may occur in either order
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | Selection | Count | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Latest    | 2     | 45,50  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-050       | JOB-050             | 2026-02-01  | 50          |
    | 1         | 10       | INV-045       | JOB-045             | 2026-03-01  | 45          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is in the result

# Latest is a moving window and this is what that means: unrelated work occupies a slot and
# displaces a prerequisite. Retained deliberately — a condition asking "is this the case right
# now" has to behave this way. A condition that instead asks "has this ever happened" is written
# with All scope and milestone matching, and is covered further down.
Scenario: Under Latest scope a later unrelated service makes a history-dependent item ineligible
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | Selection | Count | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Latest    | 2     | 45,50  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-045       | JOB-045             | 2026-01-01  | 45          |
    | 1         | 10       | INV-050       | JOB-050             | 2026-02-01  | 50          |
    | 1         | 10       | INV-OTHER     | JOB-OTHER           | 2026-03-01  | 60          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is not in the result

Scenario: A package code containing a required value does not satisfy the condition
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name                 | BrandID | ActiveForMonths |
    | SI-HISTORY    | Follow-up inspection | 1       | 24              |
  And service item "SI-HISTORY" has eligibility conditions:
    | Field                                 | Operator    | Selection | Count | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Latest    | 2     | 45,50  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-045       | JOB-045             | 2026-02-01  | 450         |
    | 1         | 10       | INV-050       | JOB-050             | 2026-03-01  | 50          |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-HISTORY" is not in the result

# --- Milestone eligibility conditions ---
#
# A milestone condition reads the scheduled service out of a package code and compares it as a
# number, so its values are mileages rather than text. Codes below are invented and follow the
# shape <PROGRAM> <MODEL> <MILESTONE>K [<QUALIFIER>].

Scenario Outline: Prerequisite milestones count however the service history is arranged
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <CodeA>     |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | <CodeB>     |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | <CodeC>     |
    | 1         | 10       | INV-4         | JOB-4               | 2026-05-01  | <CodeD>     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

  Examples:
    | Arrangement                 | CodeA          | CodeB          | CodeC          | CodeD   |
    | in order                    | PGM MDL100 45K | PGM MDL100 50K |                |         |
    | in reverse order            | PGM MDL100 50K | PGM MDL100 45K |                |         |
    | unrelated work between      | PGM MDL100 45K | BRAKE PADS     | PGM MDL100 50K |         |
    | unrelated work after        | PGM MDL100 45K | PGM MDL100 50K | BRAKE PADS     | BATTERY |
    | split across two invoices   | PGM MDL100 45K | PGM MDL100 45K | PGM MDL100 50K |         |
    | the same service done twice | PGM MDL100 45K | PGM MDL100 50K | PGM MDL100 50K |         |

Scenario Outline: The maximum milestone must sit exactly on the configured ceiling
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator | Program | Qualifier | Values |
    | serviceHistory.laborLines.maximumMilestone | Equals   | PGM     | None      | 50000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <PackageCode> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is <Presence> the result

  Examples:
    | Position      | PackageCode    | Presence |
    | below         | PGM MDL100 45K | not in   |
    | exactly on    | PGM MDL100 50K | in       |
    | one step past | PGM MDL100 55K | not in   |
    | well past     | PGM MDL100 70K | not in   |

Scenario Outline: Later periodic work closes the window on an otherwise satisfied reward
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | <LaterCode>    |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is <Presence> the result

  Examples:
    | LaterWork                 | LaterCode      | Presence |
    | unrelated parts work      | BRAKE PADS     | in       |
    | the next periodic service | PGM MDL100 55K | not in   |
    | a much later service      | PGM MDL100 70K | not in   |

Scenario Outline: A milestone reward is withheld until both prerequisites are recorded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <CodeA>     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Recorded         | CodeA          |
    | the first only   | PGM MDL100 45K |
    | the second only  | PGM MDL100 50K |
    | unrelated work   | BRAKE PADS     |

# An empty history has no maximum milestone at all. That is not zero and not a match, and reading
# it must not throw: a brand-new vehicle has simply not started.
Scenario: A vehicle with no service history is not offered a milestone reward
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

Scenario Outline: Only the configured programmes satisfy a milestone prerequisite
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program   | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | <Program> | None      | All       | 45000,50000 |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <CodeA>     |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | <CodeB>     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is <Presence> the result

  Examples:
    | Case                            | Program | CodeA          | CodeB          | Presence |
    | the configured programme        | PGM     | PGM MDL100 45K | PGM MDL100 50K | in       |
    | another programme entirely      | PGM     | ALT MDL100 45K | ALT MDL100 50K | not in   |
    | one prerequisite from another   | PGM     | PGM MDL100 45K | ALT MDL100 50K | not in   |
    | two configured programmes       | PGM,ALT | PGM MDL100 45K | ALT MDL100 50K | in       |
    | no programme filter configured  |         | ALT MDL100 45K | OTH MDL100 50K | in       |
    | a code carrying no programme    | PGM     | 45K            | 50K            | not in   |

# The maximum is programme-scoped in the same way as the prerequisites, so a later service booked
# under a programme this rule does not count leaves the window open.
Scenario: A later milestone under an uncounted programme does not close the window
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | ALT MDL100 55K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

Scenario Outline: Each qualifier selection decides which variant codes count
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier   | QualifierValues   | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | <Qualifier> | <QualifierValues> | All       | 50000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <PackageCode> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is <Presence> the result

  Examples:
    | Qualifier | QualifierValues | PackageCode       | Presence |
    | None      |                 | PGM MDL100 50K    | in       |
    | None      |                 | PGM MDL100 50K QA | not in   |
    | Any       |                 | PGM MDL100 50K    | in       |
    | Any       |                 | PGM MDL100 50K QA | in       |
    | Any       |                 | PGM MDL100 50K QB | in       |
    | Only      | QA              | PGM MDL100 50K QA | in       |
    | Only      | QA              | PGM MDL100 50K QB | not in   |
    | Only      | QA              | PGM MDL100 50K    | not in   |
    | Only      | QA,QB           | PGM MDL100 50K QB | in       |
    | Except    | QA              | PGM MDL100 50K QA | not in   |
    | Except    | QA              | PGM MDL100 50K QB | in       |
    | Except    | QA              | PGM MDL100 50K    | in       |
    | Except    | QA,QB           | PGM MDL100 50K QB | not in   |

Scenario Outline: A milestone is read out of the code, or the code carries none
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | Any       | All       | 50000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <PackageCode> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is <Presence> the result

  Examples:
    | Reading                          | PackageCode       | Presence |
    | a plain milestone code           | PGM MDL100 50K    | in       |
    | lower case                       | pgm mdl100 50k    | in       |
    | a qualified code                 | PGM MDL100 50K QA | in       |
    | the milestone alone              | 50K               | in       |
    | a longer number ending in 50K    | 150K              | not in   |
    | another longer number            | 250K              | not in   |
    | a different milestone            | PGM MDL100 55K    | not in   |
    | no milestone token at all        | BRAKE PADS        | not in   |
    | a model code with digits only    | PGM MDL100        | not in   |
    | a number with no K               | PGM MDL100 50     | not in   |
    | two milestone tokens in one code | PGM 50K 100K      | not in   |

Scenario Outline: An implausible milestone reading is discarded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Qualifier | Selection | Values    |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | Any       | All       | <Mileage> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <PackageCode> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Reason                       | PackageCode      | Mileage |
    | off the scheduled interval   | PGM MDL100 7K    | 7000    |
    | below the plausible minimum  | PGM MDL100 1K    | 1000    |
    | above the plausible maximum  | PGM MDL100 999K  | 999000  |

# The bounds are deployment configuration, not a rule of the grammar: a deployment scheduling its
# services on a different interval says so here rather than living with silently discarded readings.
Scenario: Configured bounds admit a milestone the defaults discard
  Given LookupOptions milestone bounds are minimum 1000 maximum 500000 step 1000
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | Any       | All       | 7000   |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 7K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

Scenario: A deployment writing its milestones differently configures the pattern
  Given LookupOptions milestone package-code pattern is "(?<![A-Z0-9])([0-9]+)000\s*KM(?![A-Z0-9])"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 50000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode       |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 50000KM |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

# Reading a milestone is a seam, not a rule of the grammar. Swapping the reader for one that states
# milestones outright must change nothing about how eligibility is decided.
Scenario: Eligibility follows the milestone reader, not the shape of the code
  Given the milestone resolver reads:
    | PackageCode | Milestone | Program | Qualifier |
    | JOB-ALPHA   | 45000     | PGM     |           |
    | JOB-BETA    | 50000     | PGM     |           |
    | JOB-GAMMA   | 55000     | ALT     | QA        |
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values      |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | 45000,50000 |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | 50000       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | JOB-ALPHA   |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | JOB-BETA    |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | JOB-GAMMA   |
    | 1         | 10       | INV-4         | JOB-4               | 2026-05-01  | PGM MDL100 70K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

Scenario Outline: The complete reward rule combines the base cap with milestone history
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage  | ProgramRole |
    | SI-BASE       | Base schedule end | 1       | 6               | <BaseCap>       |             |
    | SI-REWARD     | Milestone reward  | 1       | 3               | <RewardMileage> | Reward      |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator    | ValueMatch | Program | Qualifier | Selection | Values          |
    | serviceHistory.laborLines.packageCode      | ContainsAll | Milestone  | PGM     | None      | All       | <First>,<Second> |
    | serviceHistory.laborLines.maximumMilestone | Equals      |            | PGM     | None      |           | <Second>        |
    | serviceItems.baseSchedule.maximumMileage   | Equals      |            |         |           |           | <BaseCap>       |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | <FirstCode>    |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | <SecondCode>   |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | BRAKE PADS     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is in the result

  Examples:
    | BaseCap | RewardMileage | First | Second | FirstCode      | SecondCode     |
    | 40000   | 55000         | 45000 | 50000  | PGM MDL100 45K | PGM MDL100 50K |
    | 60000   | 75000         | 65000 | 70000  | PGM MDL100 65K | PGM MDL100 70K |
    | 80000   | 95000         | 85000 | 90000  | PGM MDL100 85K | PGM MDL100 90K |

# Every row here is a condition the evaluator cannot interpret safely. The history would satisfy a
# well-formed version of the same rule, so a row that passes would be the grammar guessing.
Scenario Outline: A malformed milestone condition fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | ProgramJson | Qualifier   | QualifierValuesJson   | Selection | ValuesJson   |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | <Program>   | <Qualifier> | <QualifierValuesJson> | All       | <ValuesJson> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 50K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Fault                                | Program | Qualifier | QualifierValuesJson | ValuesJson               |
    | qualifier omitted                    | ["PGM"] |           |                     | ["50000"]                |
    | values named under None              | ["PGM"] | None      | ["QA"]              | ["50000"]                |
    | values named under Any               | ["PGM"] | Any       | ["QA"]              | ["50000"]                |
    | no values under Only                 | ["PGM"] | Only      |                     | ["50000"]                |
    | empty values under Only              | ["PGM"] | Only      | []                  | ["50000"]                |
    | blank value under Only               | ["PGM"] | Only      | ["  "]              | ["50000"]                |
    | null value under Only                | ["PGM"] | Only      | [null]              | ["50000"]                |
    | no values under Except               | ["PGM"] | Except    |                     | ["50000"]                |
    | empty values under Except            | ["PGM"] | Except    | []                  | ["50000"]                |
    | an unsupported qualifier selection   | ["PGM"] | 99        |                     | ["50000"]                |
    | an empty programme list              | []      | None      |                     | ["50000"]                |
    | a blank programme                    | ["  "]  | None      |                     | ["50000"]                |
    | a null programme                     | [null]  | None      |                     | ["50000"]                |
    | a non-numeric mileage                | ["PGM"] | None      |                     | ["fifty"]                |
    | a zero mileage                       | ["PGM"] | None      |                     | ["0"]                    |
    | a negative mileage                   | ["PGM"] | None      |                     | ["-50000"]               |
    | a mileage with a group separator     | ["PGM"] | None      |                     | ["50,000"]               |
    | a mileage too large for the type     | ["PGM"] | None      |                     | ["99999999999999999999"] |
    | one good mileage and one bad         | ["PGM"] | None      |                     | ["50000","fifty"]        |

Scenario Outline: A malformed maximum-milestone condition fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                      | Operator   | ValueMatch   | Program | Qualifier   | Selection   | Count   | ValuesJson   |
    | serviceHistory.laborLines.maximumMilestone | <Operator> | <ValueMatch> | PGM     | <Qualifier> | <Selection> | <Count> | <ValuesJson> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 50K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Fault                        | Operator    | ValueMatch | Qualifier | Selection | Count | ValuesJson          |
    | qualifier omitted            | Equals      |            |           |           |       | ["50000"]           |
    | an aggregating operator      | ContainsAll |            | None      |           |       | ["50000"]           |
    | a suffix comparison          | Equals      | EndsWith   | None      |           |       | ["50000"]           |
    | a milestone comparison       | Equals      | Milestone  | None      |           |       | ["50000"]           |
    | a collection scope           | Equals      |            | None      | All       |       | ["50000"]           |
    | a windowed scope             | Equals      |            | None      | Latest    | 2     | ["50000"]           |
    | more than one mileage        | Equals      |            | None      |           |       | ["50000","55000"]   |
    | no mileage at all            | Equals      |            | None      |           |       | []                  |
    | a blank mileage              | Equals      |            | None      |           |       | ["  "]              |
    | a non-numeric mileage        | Equals      |            | None      |           |       | ["fifty"]           |
    | a zero mileage               | Equals      |            | None      |           |       | ["0"]               |

Scenario Outline: Programme and qualifier filters are rejected where nothing reads them
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActiveForMonths | MaximumMileage |
    | SI-BASE       | Base schedule end | 1       | 6               | 40000          |
    | SI-REWARD     | Return reward     | 1       | 24              |                |
  And service item "SI-REWARD" has eligibility conditions:
    | Field   | Operator   | ValueMatch   | Program   | Qualifier   | Selection   | Count   | Values   |
    | <Field> | <Operator> | <ValueMatch> | <Program> | <Qualifier> | <Selection> | <Count> | <Values> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 50K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

  Examples:
    | Fault                             | Field                                    | Operator    | ValueMatch | Program | Qualifier | Selection | Count | Values         |
    | a programme on exact matching     | serviceHistory.laborLines.packageCode    | ContainsAll |            | PGM     |           | All       |       | PGM MDL100 50K |
    | a qualifier on exact matching     | serviceHistory.laborLines.packageCode    | ContainsAll |            |         | None      | All       |       | PGM MDL100 50K |
    | a programme on suffix matching    | serviceHistory.laborLines.packageCode    | ContainsAll | EndsWith   | PGM     |           | All       |       | 50K            |
    | a qualifier on suffix matching    | serviceHistory.laborLines.packageCode    | ContainsAll | EndsWith   |         | Any       | All       |       | 50K            |
    | a programme on the base-cap field | serviceItems.baseSchedule.maximumMileage | Equals      |            | PGM     |           |           |       | 40000          |
    | a qualifier on the base-cap field | serviceItems.baseSchedule.maximumMileage | Equals      |            |         | None      |           |       | 40000          |

Scenario: An All scope carrying a count fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Count | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | All       | 2     | 50000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 50K |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result

# Milestone matching is opt-in: a Latest window still works alongside it, and a milestone condition
# is free to use one when the question really is about recent visits.
Scenario: A milestone condition may still be scoped to the latest invoices
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths |
    | SI-REWARD     | Return reward | 1       | 24              |
  And service item "SI-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program | Qualifier | Selection | Count | Values      |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | PGM     | None      | Latest    | 2     | 45000,50000 |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode    |
    | 1         | 10       | INV-1         | JOB-1               | 2026-02-01  | PGM MDL100 45K |
    | 1         | 10       | INV-2         | JOB-2               | 2026-03-01  | PGM MDL100 50K |
    | 1         | 10       | INV-3         | JOB-3               | 2026-04-01  | BRAKE PADS     |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-REWARD" is not in the result
```

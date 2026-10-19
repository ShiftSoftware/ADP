---
hide:
    - toc
---

```gherkin
Feature: Service Item Eligibility Alternatives and Vehicle Evidence
  Every eligibility condition on a service item must hold. One entry in that
  list may instead be an "at least one of" group: it holds when any one of its
  alternatives holds. The group alone decides what an unmet group means
  (hidden by default, or locked or missed), and a group that is not well formed
  hides the item even when one of its alternatives holds.
  Conditions can also read evidence about the vehicle: whether the distributor
  imported it, which campaigns list it, which companies inspected it, and the
  classifications the host application resolves for it.

# --- At least one of ---

Scenario Outline: An item is offered when any one of its alternatives holds
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths |
    | SI-LOYALTY    | Loyalty reward | 1       | 24              |
  And service item "SI-LOYALTY" also requires at least one of the following:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
    | campaignVinEntries.campaignID | ContainsAll | 600    |
  And campaign VIN entries:
    | VIN               | CampaignID   | RecordedDate |
    | 1FDKF37GXVEB34368 | <CampaignID> | 2026-02-01   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-LOYALTY" is <Outcome>

  Examples:
    | Vehicle is listed in               | CampaignID | Outcome           |
    | the first alternative's campaign   | 500        | in the result     |
    | the second alternative's campaign  | 600        | in the result     |
    | neither alternative's campaign     | 700        | not in the result |

Scenario: Alternatives do not relax the item's other conditions
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths |
    | SI-LOYALTY    | Loyalty reward | 1       | 24              |
  And service item "SI-LOYALTY" has eligibility conditions:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 900    |
  And service item "SI-LOYALTY" also requires at least one of the following:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
    | campaignVinEntries.campaignID | ContainsAll | 600    |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-LOYALTY" is not in the result

Scenario: A group set to lock shows the item locked when no alternative holds
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths |
    | SI-LOYALTY    | Loyalty reward | 1       | 24              |
  And service item "SI-LOYALTY" also requires at least one of the following, with WhenUnmet "Lock":
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
    | campaignVinEntries.campaignID | ContainsAll | 600    |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-LOYALTY" is "Locked"

# Only the group may say what failing means, and every alternative must be one ADP can read. A group
# that breaks either rule is an authoring mistake, and it fails closed rather than guessing.
Scenario Outline: A malformed group hides the item even though one of its alternatives holds
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths |
    | SI-LOYALTY    | Loyalty reward | 1       | 24              |
  And service item "SI-LOYALTY" also requires at least one of the following:
    | Field                         | Operator    | Values | WhenUnmet   |
    | campaignVinEntries.campaignID | ContainsAll | 500    |             |
    | <Field>                       | ContainsAll | 600    | <WhenUnmet> |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-02-01   |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-LOYALTY" is not in the result

  Examples:
    | Fault                                     | Field                         | WhenUnmet |
    | an alternative that says to lock the item | campaignVinEntries.campaignID | Lock      |
    | an alternative on a field ADP cannot read | vehicle.exteriorColour        |           |

# --- Native authorization ---

Scenario: An item reserved for unofficial imports is offered to a vehicle the distributor did not import
  Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
    | VIN               |
    | 2C3CCAGG1DH549029 |
  And service items:
    | ServiceItemID | Name                     | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-UNOFFICIAL | Import intake inspection | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-UNOFFICIAL" has eligibility conditions:
    | Field        | Operator | Values |
    | isAuthorized | Equals   | false  |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "WMWZB3C55BWM46667" with language "en"
  Then service item "SI-UNOFFICIAL" is in the result

Scenario: An item reserved for unofficial imports is hidden from a vehicle the distributor imported
  Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
    | VIN               |
    | 1FDKF37GXVEB34368 |
  And service items:
    | ServiceItemID | Name                     | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-UNOFFICIAL | Import intake inspection | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-UNOFFICIAL" has eligibility conditions:
    | Field        | Operator | Values |
    | isAuthorized | Equals   | false  |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-UNOFFICIAL" is not in the result

Scenario: An item reserved for official imports is offered to a vehicle the distributor imported
  Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
    | VIN               |
    | 1FDKF37GXVEB34368 |
  And service items:
    | ServiceItemID | Name                    | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-OFFICIAL   | Official import checkup | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-OFFICIAL" has eligibility conditions:
    | Field        | Operator | Values |
    | isAuthorized | Equals   | true   |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-OFFICIAL" is in the result

# --- Campaign membership ---
# Membership is evidence that the vehicle belongs to the campaign. It does not activate anything: an
# item triggered by a vehicle inspection still waits for that inspection.

Scenario: A campaign member is offered the inspection-triggered item once inspected
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | CampaignID | ActiveForMonths |
    | SI-MEMBERS    | Members' check-up | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 500        | 12              |
  And service item "SI-MEMBERS" has eligibility conditions:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-01-20   |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MEMBERS" is in the result
  And service item "SI-MEMBERS" has activation "2026-02-01"

Scenario: Campaign membership alone does not activate an inspection-triggered item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | CampaignID | ActiveForMonths |
    | SI-MEMBERS    | Members' check-up | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 500        | 12              |
  And service item "SI-MEMBERS" has eligibility conditions:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
  And campaign VIN entries:
    | VIN               | CampaignID | RecordedDate |
    | 1FDKF37GXVEB34368 | 500        | 2026-01-20   |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MEMBERS" is not in the result

Scenario Outline: An inspected vehicle that is not a current campaign member is not offered the item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name              | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | CampaignID | ActiveForMonths |
    | SI-MEMBERS    | Members' check-up | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 500        | 12              |
  And service item "SI-MEMBERS" has eligibility conditions:
    | Field                         | Operator    | Values |
    | campaignVinEntries.campaignID | ContainsAll | 500    |
  And campaign VIN entries:
    | VIN               | CampaignID   | RecordedDate | IsDeleted   |
    | 1FDKF37GXVEB34368 | <CampaignID> | 2026-01-20   | <IsDeleted> |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-MEMBERS" is not in the result

  Examples:
    | Situation                           | CampaignID | IsDeleted |
    | the membership was deleted          | 500        | true      |
    | the vehicle is in another campaign  | 600        | false     |

# --- Inspecting company ---

Scenario Outline: An item requiring an inspection by a named company
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name               | BrandID | ActiveForMonths |
    | SI-CERTIFIED  | Certified-checkout | 1       | 24              |
  And service item "SI-CERTIFIED" has eligibility conditions:
    | Field                        | Operator    | Selection | InspectionTypeID | Values |
    | vehicleInspections.companyID | ContainsAll | All       | 7                | 3      |
  And vehicle inspections:
    | VIN               | InspectionDate | VehicleInspectionTypeID | CompanyID   |
    | 1FDKF37GXVEB34368 | 2026-02-01     | <InspectionTypeID>      | <CompanyID> |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-CERTIFIED" is <Outcome>

  Examples:
    | Situation                                     | InspectionTypeID | CompanyID | Outcome           |
    | the named company ran that inspection         | 7                | 3         | in the result     |
    | another company ran that inspection           | 7                | 4         | not in the result |
    | the named company ran another inspection type | 8                | 3         | not in the result |

# --- Host classifications ---
# Classifications come from the host application's resolver, run once per lookup. Codes are matched
# exactly, including case, and a host that classifies nothing satisfies no classification condition.

Scenario: An item for a host classification is offered when the host classifies the vehicle that way
  Given the current UTC time is "2026-03-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-FLEET      | Fleet checkup | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-FLEET" has eligibility conditions:
    | Field                       | Operator    | Values |
    | vehicleClassifications.code | ContainsAll | fleet  |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  And the host classifies the vehicle as "fleet"
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then service item "SI-FLEET" is in the result

Scenario: An item for a host classification is hidden when the host classifies nothing
  Given the current UTC time is "2026-03-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-FLEET      | Fleet checkup | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-FLEET" has eligibility conditions:
    | Field                       | Operator    | Values |
    | vehicleClassifications.code | ContainsAll | fleet  |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  And the host does not classify vehicles
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then service item "SI-FLEET" is not in the result

Scenario: A classification code that differs only in case does not satisfy the condition
  Given the current UTC time is "2026-03-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-FLEET      | Fleet checkup | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And service item "SI-FLEET" has eligibility conditions:
    | Field                       | Operator    | Values |
    | vehicleClassifications.code | ContainsAll | fleet  |
  And vehicle inspections:
    | InspectionDate | VehicleInspectionTypeID |
    | 2026-02-01     | 7                       |
  And the host classifies the vehicle as "Fleet"
  When looking up service items for "1FDKF37GXVEB34368" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then service item "SI-FLEET" is not in the result
```

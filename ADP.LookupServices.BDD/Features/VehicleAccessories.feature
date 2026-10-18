Feature: Vehicle Accessories
  Vehicle accessories are listed with their part numbers, descriptions,
  and images. Image URLs are resolved through a configurable resolver.

Scenario: Accessories listed with resolved image URLs
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
    | ACC-002    | Roof rack       | img002.jpg |
  And the accessory image resolver maps "img001.jpg" to "https://cdn.example.com/img001.jpg"
  And the accessory image resolver maps "img002.jpg" to "https://cdn.example.com/img002.jpg"
  When evaluating accessories with language "en"
  Then there are 2 accessories
  And accessory "ACC-001" has image "https://cdn.example.com/img001.jpg"
  And accessory "ACC-002" has image "https://cdn.example.com/img002.jpg"

Scenario: Accessories without image resolver return raw image value
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has image "img001.jpg"

Scenario: No accessories returns empty list
  When evaluating accessories with language "en"
  Then there are 0 accessories


Scenario: Job number and invoice number are mapped through
  Given accessories:
    | PartNumber | PartDescription | JobNumber | InvoiceNumber |
    | ACC-001    | Floor mats      | 1001      | 2002          |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has job number 1001
  And accessory "ACC-001" has invoice number 2002

Scenario: Zero job number and invoice number map to null
  Given accessories:
    | PartNumber | PartDescription | JobNumber | InvoiceNumber |
    | ACC-001    | Floor mats      | 0         | 0             |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has no job number
  And accessory "ACC-001" has no invoice number

Scenario: Company name is resolved from the company ID
  Given accessories:
    | PartNumber | PartDescription | CompanyID |
    | ACC-001    | Floor mats      | 1         |
  And company 1 is named "Dealer A"
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has company name "Dealer A"

Scenario: Unresolved company leaves the company name null
  Given accessories:
    | PartNumber | PartDescription | CompanyID |
    | ACC-001    | Floor mats      | 2         |
  And company 1 is named "Dealer A"
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has no company name

Scenario: No company name resolver leaves the company name null
  Given accessories:
    | PartNumber | PartDescription | CompanyID |
    | ACC-001    | Floor mats      | 1         |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has no company name

Scenario: Each distinct company is resolved once and names map to the right accessories
  Given accessories:
    | PartNumber | PartDescription | CompanyID |
    | ACC-001    | Floor mats      | 1         |
    | ACC-002    | Mud flaps       | 1         |
    | ACC-003    | Roof rails      | 2         |
    | ACC-004    | Cargo net       |           |
  And company 1 is named "Dealer A"
  And company 2 is named "Dealer B"
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has company name "Dealer A"
  And accessory "ACC-002" has company name "Dealer A"
  And accessory "ACC-003" has company name "Dealer B"
  And accessory "ACC-004" has no company name
  And the company name resolver was called 3 times, once for each of company IDs "1, 2, null"

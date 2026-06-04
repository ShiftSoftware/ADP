---
hide:
    - toc
---

```gherkin
Feature: Vehicle Paint Thickness Certificate
  The Paint Thickness Certificate shows the readings from the latest PDI
  paint-thickness inspection taken strictly before the DISTRIBUTOR's sale
  invoice date. The anchor entry must belong to the distributor company
  (CompanyID == DistributorCompanyID); if there is no such invoiced entry,
  no certificate is produced.

Background:
  Given the distributor company id is 1

Scenario: A PDI inspection before the distributor invoice date produces a certificate
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And paint thickness panels for inspection on "2024-01-10":
    | PanelType | PanelSide | PanelPosition | MeasuredThickness |
    | Hood      | Center    | Front         | 120               |
    | Roof      | Left      | Middle        | 95                |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate is based on the inspection on "2024-01-10"
  And the certificate invoice date is "2024-01-15"
  And the certificate has 2 readings
  And the certificate has a reading "Hood (Front Center)" with thickness 120

Scenario: The distributor invoice date is the anchor, not a dealer's later sale
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
    | JTMBFREVXKD123456 | 2024-03-20  | 2         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
    | 2024-03-18     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate invoice date is "2024-01-15"
  And the certificate is based on the inspection on "2024-01-10"

Scenario: A VIN with only a dealer entry yields no certificate
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-03-20  | 2         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: The latest PDI inspection before the invoice date wins
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-06-01  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
    | 2024-05-20     | PDI    |
    | 2024-03-05     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate is based on the inspection on "2024-05-20"

Scenario: A PDI inspection on or after the invoice date is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-15     | PDI    |
    | 2024-02-01     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: A non-PDI inspection before the invoice date is ignored
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | Dealer |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: A PDI inspection with no inspection date is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    |                | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: A distributor entry with no invoice date yields no certificate
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 |             | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: The PDI source match is case-insensitive
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | pdi    |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate is based on the inspection on "2024-01-10"

Scenario: Panel images flow to the certificate gallery
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And paint thickness panels for inspection on "2024-01-10":
    | PanelType | PanelSide | PanelPosition | MeasuredThickness | Images                |
    | Hood      | Center    | Front         | 120               | hood-1.jpg,hood-2.jpg |
    | Roof      | Left      | Middle        | 95                | roof-1.jpg            |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate has 2 readings
  And the certificate gallery has 3 images
```

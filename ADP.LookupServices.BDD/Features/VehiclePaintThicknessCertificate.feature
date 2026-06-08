Feature: Vehicle Paint Thickness Certificate
  The Paint Thickness Certificate shows the readings from the latest PDI
  paint-thickness inspection taken on or before the DISTRIBUTOR's sale
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

Scenario: A PDI inspection after the invoice date is excluded
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-14  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-15     | PDI    |
    | 2024-02-01     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then no paint thickness certificate is produced

Scenario: A PDI inspection on the same day as the invoice date is included
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-15     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate is based on the inspection on "2024-01-15"
  And the certificate invoice date is "2024-01-15"

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

Scenario: Certificate produced end-to-end from the standard-dealer environment
  Given the "standard-dealer" environment is loaded
  And loading vehicle "JTMHX01J8L4198293" from the environment
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate is based on the inspection on "2024-01-10"
  And the certificate invoice date is "2024-01-12"
  And the certificate has 11 readings
  And the certificate has a reading "Hood (Front)" with thickness 125.5
  And the certificate gallery has 16 images

Scenario: The availability flag is true when the environment vehicle qualifies
  Given the "standard-dealer" environment is loaded
  And loading vehicle "JTMHX01J8L4198293" from the environment
  When checking paint thickness certificate availability
  Then the paint thickness certificate is reported as available

Scenario: The availability flag is false for a dealer-only vehicle
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 2         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  When checking paint thickness certificate availability
  Then the paint thickness certificate is reported as unavailable

Scenario: The serial number resolver stamps the certificate deterministically
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And a paint thickness certificate serial number resolver that returns "3F09A-12B45"
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate serial number is "3F09A-12B45"

Scenario: Without a serial number resolver the certificate has no serial
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  When evaluating the paint thickness certificate with language "en"
  Then a paint thickness certificate is produced
  And the certificate has no serial number

Scenario: The lookup carries a signed certificate url per supported language when the request opts in
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And a paint thickness certificate url resolver that returns "https://lookup.example/certificate/{vin}" for languages "en, ar, ku"
  When looking up the vehicle "JTMBFREVXKD123456" with certificate url generation requested
  Then the lookup reports the paint thickness certificate as available
  And the lookup has 3 certificate urls
  And the lookup certificate url for "ar" is "https://lookup.example/certificate/JTMBFREVXKD123456?lang=ar"

Scenario: The certificate urls are omitted when the request does not opt in
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And a paint thickness certificate url resolver that returns "https://lookup.example/certificate/{vin}" for languages "en, ar, ku"
  When looking up the vehicle "JTMBFREVXKD123456" without certificate url generation
  Then the lookup reports the paint thickness certificate as available
  And the lookup has no certificate urls

Scenario: The certificate urls are omitted when no certificate is available
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 2         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And a paint thickness certificate url resolver that returns "https://lookup.example/certificate/{vin}" for languages "en, ar, ku"
  When looking up the vehicle "JTMBFREVXKD123456" with certificate url generation requested
  Then the lookup reports the paint thickness certificate as unavailable
  And the lookup has no certificate urls

Scenario: The certificate urls are omitted without a resolver
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  When looking up the vehicle "JTMBFREVXKD123456" with certificate url generation requested
  Then the lookup reports the paint thickness certificate as available
  And the lookup has no certificate urls

Scenario: An empty url set from the resolver stays null on the lookup
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | JTMBFREVXKD123456 | 2024-01-15  | 1         |
  And paint thickness inspections:
    | InspectionDate | Source |
    | 2024-01-10     | PDI    |
  And a paint thickness certificate url resolver that returns "https://lookup.example/certificate/{vin}" for languages ""
  When looking up the vehicle "JTMBFREVXKD123456" with certificate url generation requested
  Then the lookup reports the paint thickness certificate as available
  And the lookup has no certificate urls

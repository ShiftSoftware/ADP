Feature: Service Item Resolvers — Company Name & Print URL
  LookupOptions resolvers enrich service items with external data.
  When a claim has a non-zero CompanyID, the CompanyNameResolver is
  called to populate CompanyName. For print URLs, the Vehicle-Inspection
  resolver applies to items with a VehicleInspectionID, but if a
  VehicleServiceActivation is present and the ServiceActivation resolver
  is configured, it takes priority and overrides the inspection URL.

Scenario: Processed item enriches its company name via the resolver
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber | CompanyID |
    | SI-001        | 2026-06-01 | JOB-001   | INV-001       | 1         |
  And company 1 is named "Toyota Motors"
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has status "processed"
  And service item "SI-001" has company name "Toyota Motors"

Scenario: Service activation print URL overrides the inspection print URL
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | ActiveForMonths |
    | SI-INSP       | Inspection | 1       | VehicleInspection | FirstTriggerOnly | 7                       | 12              |
  And vehicle inspections:
    | id     | InspectionDate | VehicleInspectionTypeID |
    | INSP-A | 2026-02-01     | 7                       |
  And vehicle service activations:
    | id    | WarrantyActivationDate |
    | ACT-1 | 2026-01-15             |
  And an inspection pre-claim voucher URL resolver is configured
  And a service activation pre-claim voucher URL resolver is configured
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-INSP" has print url "activation/ACT-1/SI-INSP"

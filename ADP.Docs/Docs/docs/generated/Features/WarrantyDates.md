---
hide:
    - toc
---

```gherkin
Feature: Warranty and Free Service Dates
  Warranty and free service start dates are determined by the vehicle's sale
  circumstances. The system checks service activation records, warranty activation
  dates, and invoice dates (in that priority order). Broker sales have separate
  logic. Date shifts can override calculated dates. Extended warranty entries
  are tracked independently.

  Extended warranty has two independent outputs. ExtendedWarranties lists every
  coverage — persisted entries plus any awarded by a configured definition. The
  flat HasExtendedWarranty/ExtendedWarrantyStartDate/ExtendedWarrantyEndDate
  fields are older output that describes only the latest-ending *persisted*
  entry, and only while that entry is still running. Configured coverage never
  reaches them.

# --- Normal Sale (no broker) ---

Scenario: Warranty date from service activation
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-02-01"

Scenario: Warranty date falls back to vehicle warranty activation date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | WarrantyActivationDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 2024-01-20             |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-20"

Scenario: Warranty date defaults to invoice date when enabled
  Given warranty start date defaults to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-15"

Scenario: Warranty date is null when no activation and defaulting disabled
  Given warranty start date does not default to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is empty

# --- Warranty End Date ---

Scenario: Default warranty period is 3 years
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2027-02-01"

Scenario: Brand-specific warranty period
  Given brand 1 has a warranty period of 5 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2029-02-01"

# --- Date Shifts ---

Scenario: Warranty date shift overrides calculated start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And warranty date shifts:
    | NewDate    |
    | 2023-06-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2023-06-01"

Scenario: Free service date shift overrides free service start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And free service item date shifts:
    | NewDate    |
    | 2023-09-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2023-09-01"

# --- Extended Warranty ---

Scenario: Extended warranty dates from entries
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | ID       | CompanyID | StartDate  | EndDate    |
    | EW-ENTRY | 1         | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

Scenario: Multiple extended warranties preserve provider details
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And extended warranty entries:
    | ID       | CompanyID | StartDate  | EndDate    |
    | EW-LATE  | 202       | 2029-06-01 | 2031-06-01 |
    | EW-EARLY | 101       | 2027-02-01 | 2028-02-01 |
  And company logos resolve as:
    | CompanyID | Logo                      |
    | 101       | https://images.test/a.png |
    | 202       | https://images.test/b.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle has extended warranty
  # Legacy fields describe EW-LATE alone — not 2027-02-01 → 2031-06-01 across both.
  And the extended warranty start date is "2029-06-01"
  And the extended warranty end date is "2031-06-01"
  And extended warranties are:
    | ID       | ProviderCompanyID | ProviderCompanyLogo          | StartDate  | EndDate    |
    | EW-LATE  | 202               | https://images.test/b.png    | 2029-06-01 | 2031-06-01 |
    | EW-EARLY | 101               | https://images.test/a.png    | 2027-02-01 | 2028-02-01 |

Scenario: The vehicle lookup API returns resolved provider detail
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 1       |
  And extended warranty entries:
    | ID     | CompanyID | StartDate  | EndDate    |
    | EW-API | 101       | 2027-02-01 | 2028-02-01 |
  And company logos resolve as:
    | CompanyID | Logo                        |
    | 101       | https://images.test/api.png |
  When looking up warranty details for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID     | ProviderCompanyID | ProviderCompanyLogo        | StartDate  | EndDate    |
    | EW-API | 101               | https://images.test/api.png | 2027-02-01 | 2028-02-01 |

Scenario: Historical extended coverage stays listed but clears the legacy flag
  Given the current UTC time is "2035-01-01 00:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And extended warranty entries:
    | ID     | CompanyID | StartDate  | EndDate    |
    | EW-OLD | 101       | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 1 extended warranties
  And the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

Scenario: Configured coverage joins the collection without moving the legacy fields
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | ID        | CompanyID | StartDate  | EndDate    |
    | EW-STORED | 101       | 2026-01-01 | 2027-06-01 |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then there are 2 extended warranties
  # CFG-REWARD runs to 2028-02-01 but the legacy end date stays on EW-STORED.
  And the extended warranty start date is "2026-01-01"
  And the extended warranty end date is "2027-06-01"

Scenario Outline: A configured extended warranty uses the shared package suffix grammar
  Given brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson  |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | ["<Suffix>"] |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode  |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | <PackageCode> |
  And company logos resolve as:
    | CompanyID | Logo                                |
    | 901       | https://images.test/distributor.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # No persisted entry, so the legacy flag stays false however the definition matches.
  Then the vehicle does not have extended warranty
  And extended warranties are:
    | ID         | ProviderCompanyID | ProviderCompanyLogo                    | StartDate  | EndDate    |
    | CFG-REWARD | 901               | https://images.test/distributor.png   | 2027-02-01 | 2028-02-01 |

  Examples:
    | Suffix | PackageCode |
    |  60K   | MODEL 60K   |
    |  75K   | MODEL 75K   |

Scenario: A configured warranty can fall back to the tenant distributor as provider
  Given the distributor company id is 901
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD |                   | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  And company logos resolve as:
    | CompanyID | Logo                                |
    | 901       | https://images.test/distributor.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID         | ProviderCompanyID | ProviderCompanyLogo                    | StartDate  | EndDate    |
    | CFG-REWARD | 901               | https://images.test/distributor.png   | 2027-02-01 | 2028-02-01 |

Scenario Outline: A configured warranty fails closed when its package condition does not match
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode  |
    | 1         | 10       | INV-OTHER     | JOB-OTHER           | 2026-03-01  | <PackageCode> |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 0 extended warranties

  Examples:
    | PackageCode |
    | MODEL 55K   |
    | 160K        |

Scenario: An extended warranty definition without conditions fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 0 extended warranties

# --- De Facto Service Start Date ---
# The earliest non-deleted ItemClaim.ClaimDate is always exposed as DeFactoServiceStartDate.
# When the regular fallback chain leaves FreeServiceStartDate=null (typically broker-without-invoice
# + IgnoreBrokerStock=false), the de facto value is used as the effective FreeServiceStartDate so
# downstream service items still project. FreeServiceItemDateShifts still override.

Scenario: De facto date exposed even when the regular chain produces a date (direct sale)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-08-10 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-02-01"
  And the de facto service start date is "2024-08-10"

Scenario: Broker without invoice and no claims leaves both dates empty
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is empty
  And the de facto service start date is empty

Scenario: Broker without invoice falls back to the only claim date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-06-15"
  And the de facto service start date is "2024-06-15"

Scenario: Broker without invoice picks earliest among multiple claims
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-A          | 2024-09-01 |
    | SI-B          | 2024-06-15 |
    | SI-C          | 2025-01-10 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-06-15"
  And the de facto service start date is "2024-06-15"

Scenario: Deleted claims are excluded from de facto computation
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  | IsDeleted |
    | SI-DELETED    | 2024-03-01 | true      |
    | SI-ACTIVE     | 2024-08-20 | false     |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-08-20"
  And the de facto service start date is "2024-08-20"

Scenario: Free service date shift still overrides the de facto fallback
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  And free service item date shifts:
    | NewDate    |
    | 2023-09-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2023-09-01"
  And the de facto service start date is "2024-06-15"

Scenario: Broker invoice still wins over the de facto fallback when both exist
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker with invoice date "2024-02-10"
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-02-10"
  And the de facto service start date is "2024-06-15"
```

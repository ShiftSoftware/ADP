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
    | StartDate  | EndDate    |
    | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

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

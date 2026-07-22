Feature: Vehicle Entry Selection Consistency
  Sale Information and Service Item eligibility must agree on the same owning
  company/country. Ownership is resolved from the service activation (authoritative),
  falling back to the selected vehicle entry — so when a VIN has entries at several
  companies, both consumers follow the activation's company/country, not whichever
  entry happens to be the latest-invoiced, and selection never crashes.

# --- Activation company has no matching entry: the activation wins (and never crashes) ---

Scenario: Sale information uses the activation company and country when no entry matches it
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CountryID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 42        |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 5         | 7         | 2024-02-01             |
  And company 5 is named "Distributor B"
  And country 7 is named "Country 7"
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  Then the sale company is "Distributor B"
  And the sale country is "Country 7"

# --- Company: ownership follows the activation, not the latest-invoiced entry ---

Scenario: Service item company eligibility follows the activation company across multiple entries
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CountryID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-01  | 2         | 20       | 99        | 1       |
    | 1FDKF37GXVEB34368 | 2024-06-01  | 1         | 10       | 42        | 1       |
  And vehicle service activations:
    | CompanyID | WarrantyActivationDate |
    | 2         | 2024-02-01             |
  And company 1 is named "Distributor A"
  And company 2 is named "Distributor B"
  And service items:
    | ServiceItemID | Name            | BrandID | CompanyID | ActiveForMonths |
    | SI-CO2        | Oil Change C99  | 1       | 2         | 24              |
    | SI-CO1        | Oil Change C42  | 1       | 1         | 24              |
  And the free service start date is "2024-06-01"
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  And evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the sale company is "Distributor B"
  And service item "SI-CO2" is in the result
  And service item "SI-CO1" is not in the result

# --- Country: same, on the country eligibility filter (activation carries its country) ---

Scenario: Service item country eligibility follows the activation country across multiple entries
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CountryID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-01  | 2         | 20       | 99        | 1       |
    | 1FDKF37GXVEB34368 | 2024-06-01  | 1         | 10       | 42        | 1       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 2         | 99        | 2024-02-01             |
  And country 42 is named "Country 42"
  And country 99 is named "Country 99"
  And service items:
    | ServiceItemID | Name            | BrandID | CountryID | ActiveForMonths |
    | SI-C99        | Oil Change C99  | 1       | 99        | 24              |
    | SI-C42        | Oil Change C42  | 1       | 42        | 24              |
  And the free service start date is "2024-06-01"
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  And evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the sale country is "Country 99"
  And service item "SI-C99" is in the result
  And service item "SI-C42" is not in the result

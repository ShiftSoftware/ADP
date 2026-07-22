Feature: Vehicle Activation Ownership
  Reproduces prod VIN JTEAAHAJ20K049868: the vehicle is still national stock
  (a single VehicleEntry with CompanyID = null, CountryID = 5, OrderStatus
  "Available", no invoice) but it has already been service-activated at
  Company 10 — whose own VehicleEntry has not synced yet and may never match
  until the upstream system allocates the vehicle to that company.

  The activation is the authoritative "this vehicle is now owned, for service
  purposes, by Company 10" record. Service Item eligibility (company/country)
  and the reported sale company/country must therefore follow Company 10, NOT
  the stale national-stock entry. Location fields the activation does not carry
  may be supplied only by the activating company's OWN entry — a different
  company's entry describes a different company's location, so when the country
  cannot be resolved from those two sources the lookup refuses
  (IncompleteVehicleServiceActivationException) rather than list wrong items.

# --- Critical: service item company eligibility must follow the activation company ---

Scenario: Service items follow the activation company when its entry has not synced yet
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID | Katashiki      | VariantCode              |
    | JTEAAHAJ20K049868 |             |           | 5         | 2       | TJA250L-GNZAZX | TJA250L-GNZAZX-D3-089-20 |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And service items:
    | ServiceItemID | Name          | BrandID | CompanyID | ActiveForMonths |
    | SI-CO10       | Free Service  | 2       | 10        | 24              |
    | SI-CO1        | Wrong Company | 2       | 1         | 24              |
  When evaluating service items end-to-end for "JTEAAHAJ20K049868" with language "en"
  Then service item "SI-CO10" is in the result
  And service item "SI-CO1" is not in the result

# --- Strict cross-contamination: a different company's stock entry must not leak its items ---

Scenario: Service items do not leak from a non-activation company entry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             | 1         | 5         | 2       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And service items:
    | ServiceItemID | Name          | BrandID | CompanyID | ActiveForMonths |
    | SI-CO10       | Free Service  | 2       | 10        | 24              |
    | SI-CO1        | Wrong Company | 2       | 1         | 24              |
  When evaluating service items end-to-end for "JTEAAHAJ20K049868" with language "en"
  Then service item "SI-CO10" is in the result
  And service item "SI-CO1" is not in the result

# --- Reported sale company must be the activation company, not the stale stock entry ---

Scenario: Sale information reports the activation company when its entry has not synced yet
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             |           | 5         | 2       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And company 10 is named "Distributor C"
  When evaluating sale information for "JTEAAHAJ20K049868" with language "en"
  Then the sale company is "Distributor C"

# --- Completing the activation: only the activating company's OWN entry may supply location ---

Scenario: An unstamped activation is completed by the activating company's own entry
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CountryID | BrandID |
    | JTEAAHAJ20K049868 | 2026-06-01  | 10        | 40       | 5         | 2       |
  And vehicle service activations:
    | CompanyID | WarrantyActivationDate |
    | 10        | 2026-06-02             |
  And company 10 is named "Taj Motors"
  And country 5 is named "Country 5"
  And branch 40 is named "Dushanbe Showroom"
  And service items:
    | ServiceItemID | Name         | BrandID | CompanyID | CountryID | ActiveForMonths |
    | SI-MATCH      | Free Service | 2       | 10        | 5         | 24              |
  When evaluating sale information for "JTEAAHAJ20K049868" with language "en"
  And evaluating service items end-to-end for "JTEAAHAJ20K049868" with language "en"
  Then the sale company is "Taj Motors"
  And the sale country is "Country 5"
  And the sale branch is "Dushanbe Showroom"
  And service item "SI-MATCH" is in the result

Scenario: A different company's entry never supplies region or branch
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CountryID | BrandID |
    | JTEAAHAJ20K049868 | 2026-06-01  | 1         | 10       | 42        | 2       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And company 10 is named "Distributor C"
  And country 7 is named "Country 7"
  And branch 10 is named "Central Showroom"
  When evaluating sale information for "JTEAAHAJ20K049868" with language "en"
  Then the sale company is "Distributor C"
  And the sale country is "Country 7"
  And the sale has no branch

# --- Unresolvable ownership: refuse loudly instead of listing wrong items ---
# The exact prod shape before upstream stamps the activation: national-stock entry
# (CompanyID null) + activation at Company 10 without a CountryID. The entry's country
# belongs to nobody we can trust here — throwing beats silently mis-scoped items.

Scenario: An unstamped activation with no entry of its own refuses the lookup
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             |           | 5         | 2       |
  And vehicle service activations:
    | CompanyID | WarrantyActivationDate |
    | 10        | 2026-06-02             |
  When resolving ownership for "JTEAAHAJ20K049868"
  Then ownership resolution fails because the service activation is incomplete

Scenario: An activation without a company refuses the lookup
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             | 1         | 5         | 2       |
  And vehicle service activations:
    | WarrantyActivationDate |
    | 2026-06-02             |
  When resolving ownership for "JTEAAHAJ20K049868"
  Then ownership resolution fails because the service activation is incomplete

# --- Ownership filters apply even when the VIN has no vehicle entries at all ---
# Inspection-triggered items don't require a vehicle entry; their company/country
# eligibility must still follow the activation's ownership — an unknown owner matches
# nothing scoped (fail closed), never everything.

Scenario: Inspection items follow the activation ownership when no entry exists
  Given vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And vehicle inspections:
    | id     | InspectionDate | VehicleInspectionTypeID |
    | INSP-A | 2026-06-05     | 7                       |
  And service items:
    | ServiceItemID | Name       | ActivationTrigger | ActivationType   | VehicleInspectionTypeID | CompanyID | ActiveForMonths |
    | SI-CO10       | Inspect 10 | VehicleInspection | FirstTriggerOnly | 7                       | 10        | 12              |
    | SI-CO1        | Inspect 1  | VehicleInspection | FirstTriggerOnly | 7                       | 1         | 12              |
  When evaluating service items for "JTEAAHAJ20K049868" with language "en"
  Then service item "SI-CO10" is in the result
  And service item "SI-CO1" is not in the result

# --- Country: the activation's company sits in a different country than the stock entry ---

Scenario: Service item country eligibility follows the activation country, not the stock-entry country
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             |           | 5         | 2       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And service items:
    | ServiceItemID | Name             | BrandID | CountryID | ActiveForMonths |
    | SI-CTRY7      | Activation Ctry  | 2       | 7         | 24              |
    | SI-CTRY5      | Stock Ctry       | 2       | 5         | 24              |
  When evaluating service items end-to-end for "JTEAAHAJ20K049868" with language "en"
  Then service item "SI-CTRY7" is in the result
  And service item "SI-CTRY5" is not in the result

Scenario: Sale information reports the activation country when the entry has not synced yet
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | CountryID | BrandID |
    | JTEAAHAJ20K049868 |             |           | 5         | 2       |
  And vehicle service activations:
    | CompanyID | CountryID | WarrantyActivationDate |
    | 10        | 7         | 2026-06-02             |
  And country 7 is named "Country 7"
  When evaluating sale information for "JTEAAHAJ20K049868" with language "en"
  Then the sale country is "Country 7"

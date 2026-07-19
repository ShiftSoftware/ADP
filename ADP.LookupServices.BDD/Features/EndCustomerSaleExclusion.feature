Feature: End-Customer Sale Exclusion (Distributor & Intermediary)
  A distributor and any intermediary normally only move a vehicle through the supply chain toward
  the dealer, rather than selling to the end customer. So their VehicleEntry must not anchor the
  warranty/free-service start date or the reported sale when a real dealer sale exists, and when
  only their entry has synced (sync delay) the start date must stay empty until the dealer's sale
  appears. The vehicle is still found for spec/identifiers in the meantime.

  Two layers decide whether an entry is an end-customer sale. The company layer classifies via
  LookupOptions.DistributorCompanyID + IntermediaryCompanyIDs; with neither configured, behaviour
  is unchanged (latest-invoiced wins). The entry layer can then mark an individual entry as a
  direct sale to a customer — a DirectEndCustomerSaleAccountNumbers account number together with
  the DirectEndCustomerSaleItemStatus ("D") — which overrides the company layer. That is how a
  supply-chain company's own sale straight to a customer is recognised.

# --- Entry selection: the dealer's entry anchors the lookup, not the distributor's ---

Scenario: The dealer entry is selected over the distributor entry on the same invoice date
  Given the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 5         | 30018218      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When Checking "JTMAB7BJ0T4224184"
  Then the selected vehicle has invoice number "11000191"

Scenario: With no distributor configured the latest-invoiced entry still wins
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-30  | 5         | 30018218      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When Checking "JTMAB7BJ0T4224184"
  Then the selected vehicle has invoice number "30018218"

# --- Warranty / free-service start: never seeded by a distributor/intermediary invoice ---

Scenario: Distributor invoiced later than the dealer still anchors warranty on the dealer's sale
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-30  | 5         | 30018218      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is "2026-05-25"
  And the free service start date is "2026-05-25"

Scenario: Sync delay leaves the warranty start empty while only the distributor entry has synced
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 5         | 30018218      |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is empty
  And the free service start date is empty

Scenario: The vehicle is still found during sync delay (distributor entry anchors spec)
  Given the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 5         | 30018218      |
  When Checking "JTMAB7BJ0T4224184"
  Then the selected vehicle has invoice number "30018218"

# --- Intermediary companies follow the same rule (and there can be more than one) ---

Scenario: An intermediary entry is excluded just like the distributor
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And intermediary companies are "7"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-30  | 7         | 70000001      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is "2026-05-25"
  And the selected vehicle has invoice number "11000191"

Scenario: With only distributor and intermediary entries the warranty start stays empty
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And intermediary companies are "7, 8"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-20  | 7         | 70000001      |
    | JTMAB7BJ0T4224184 | 2026-05-28  | 8         | 80000002      |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is empty

# --- Direct sale to an end customer: the entry-level marker that overrides the company layer ---
# A supply-chain company occasionally sells straight to a customer. Such an entry carries a configured
# direct-sale account number together with ItemStatus "D", and must anchor warranty / free-service / the
# reported sale exactly like a dealer's — not be excluded. Configuring the account numbers turns the
# feature on; a deployment that configures none is unaffected (the entry stays excluded).

Scenario: A direct distributor-to-customer sale anchors warranty like a dealer sale
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And the direct end-customer sale account numbers are "DIST-DIRECT-01"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber  | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 5         | 20024815      | DIST-DIRECT-01 | D          |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is "2024-11-01"
  And the free service start date is "2024-11-01"
  And the selected vehicle has invoice number "20024815"

Scenario: A distributor entry with the direct-sale account but not the direct-sale status stays excluded
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And the direct end-customer sale account numbers are "DIST-DIRECT-01"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber  | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 5         | 20024815      | DIST-DIRECT-01 |            |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is empty

Scenario: With no direct-sale accounts configured the distributor direct sale stays excluded
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber  | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 5         | 20024815      | DIST-DIRECT-01 | D          |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is empty

Scenario: A real dealer sale still wins over a direct distributor sale on a later invoice
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And the direct end-customer sale account numbers are "DIST-DIRECT-01"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber  | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 5         | 20024815      | DIST-DIRECT-01 | D          |
    | JTMAB7BJ0T4224184 | 2024-11-10  | 10        | 11000191      |                |            |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is "2024-11-10"
  And the selected vehicle has invoice number "11000191"

# The marker overrides the company layer, so it is not distributor-specific: an intermediary selling
# straight to a customer is recognised by the same rule, with no separate handling.

Scenario: An intermediary selling straight to a customer is an end-customer sale too
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And intermediary companies are "7"
  And the direct end-customer sale account numbers are "DIRECT-ACC-07"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 7         | 70000001      | DIRECT-ACC-07 | D          |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is "2024-11-01"
  And the selected vehicle has invoice number "70000001"

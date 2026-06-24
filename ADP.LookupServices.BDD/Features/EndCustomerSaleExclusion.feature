Feature: End-Customer Sale Exclusion (Distributor & Intermediary)
  A distributor and any intermediary only move a vehicle through the supply chain toward the
  dealer — they never make the end-customer sale. So their VehicleEntry must never anchor the
  warranty/free-service start date or the reported sale when a real dealer sale exists, and when
  only their entry has synced (sync delay) the start date must stay empty until the dealer's sale
  appears. The vehicle is still found for spec/identifiers in the meantime. Companies are
  classified via LookupOptions.DistributorCompanyID + IntermediaryCompanyIDs; with neither
  configured, behaviour is unchanged (latest-invoiced wins).

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

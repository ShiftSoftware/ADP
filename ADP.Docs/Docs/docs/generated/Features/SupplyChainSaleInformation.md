---
hide:
    - toc
---

```gherkin
Feature: Supply Chain Sale Information
  Beside the dealer's end-customer sale, the lookup surfaces the upstream supply-chain legs the
  vehicle passed through — the distributor and any intermediaries. These companies never make the
  end-customer sale, so the blocks are informational and never change the resolved sale. They are
  assembled from two source shapes: multiple vehicle entries (per-dealer DMS feeds) classified by
  company role, or a single entry that carries the supply-chain legs inline.

Scenario: Distributor leg is surfaced beside the dealer sale (multi-entry)
  Given the distributor company id is 5
  And company 5 is named "Sample Distributor"
  And company 10 is named "Sample Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-20  | 5         | 30018218      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When evaluating sale information for "JTMAB7BJ0T4224184" with language "en"
  Then the sale company is "Sample Dealer"
  And the distributor is "Sample Distributor"
  And the distributor invoice number is "30018218"
  And the distributor invoice date is "2026-05-20"
  And there are no intermediaries

Scenario: Distributor and two intermediaries are surfaced earliest-first (multi-entry)
  Given the distributor company id is 5
  And intermediary companies are "7, 8"
  And company 5 is named "Sample Distributor"
  And company 7 is named "First Importer"
  And company 8 is named "Second Importer"
  And company 10 is named "Sample Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-10  | 5         | 30018218      |
    | JTMAB7BJ0T4224184 | 2026-05-15  | 8         | 80000002      |
    | JTMAB7BJ0T4224184 | 2026-05-12  | 7         | 70000001      |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | 11000191      |
  When evaluating sale information for "JTMAB7BJ0T4224184" with language "en"
  Then the sale company is "Sample Dealer"
  And the distributor is "Sample Distributor"
  And the intermediaries count is 2
  And intermediary 1 is "First Importer"
  And intermediary 1 invoice number is "70000001"
  And intermediary 2 is "Second Importer"
  And intermediary 2 invoice number is "80000002"

Scenario: A company configured as both distributor and intermediary is surfaced only as the distributor
  Given the distributor company id is 5
  And intermediary companies are "5, 7"
  And company 5 is named "Dual Role Co"
  And company 7 is named "Real Intermediary"
  And company 10 is named "Selling Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-10  | 5         | DIST-1        |
    | JTMAB7BJ0T4224184 | 2026-05-12  | 7         | INT-1         |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 10        | DEALER-1      |
  When evaluating sale information for "JTMAB7BJ0T4224184" with language "en"
  Then the sale company is "Selling Dealer"
  And the distributor is "Dual Role Co"
  And the intermediaries count is 1
  And intermediary 1 is "Real Intermediary"

Scenario: Single-entry two-leg source surfaces each party's own sale (distributor→intermediary, intermediary→dealer)
  Given the distributor company id is 3
  And company 3 is named "Acme Distributor"
  And company 7 is named "Regional Importer"
  And company 10 is named "Sample Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMW43FV10D999999 | 2026-03-01  | 10        | DLR-0001      |
  And vehicle "JTMW43FV10D999999" has an embedded distributor leg with invoice "DIST-INT-9001" dated "2026-02-10"
  And vehicle "JTMW43FV10D999999" has an embedded intermediary leg with company 7 invoice "INT-DLR-9002" dated "2026-02-20"
  When evaluating sale information for "JTMW43FV10D999999" with language "en"
  Then the sale company is "Sample Dealer"
  And the distributor is "Acme Distributor"
  And the distributor invoice number is "DIST-INT-9001"
  And the distributor invoice date is "2026-02-10"
  And the intermediaries count is 1
  And intermediary 1 is "Regional Importer"
  And intermediary 1 invoice number is "INT-DLR-9002"
  And intermediary 1 invoice date is "2026-02-20"

Scenario: Single-entry source surfaces the implicit distributor even without an intermediary
  Given the distributor company id is 3
  And company 3 is named "Acme Distributor"
  And company 10 is named "Sample Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMW43FV10D888888 | 2026-03-01  | 10        | DLR-0002      |
  When evaluating sale information for "JTMW43FV10D888888" with language "en"
  Then the sale company is "Sample Dealer"
  And the distributor is "Acme Distributor"
  And the distributor invoice number is empty
  And there are no intermediaries

Scenario: Single-entry direct source surfaces the distributor's own invoice
  Given the distributor company id is 3
  And company 3 is named "Acme Distributor"
  And company 10 is named "Sample Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMW43FV10D777777 | 2026-03-01  | 10        | DLR-0003      |
  And vehicle "JTMW43FV10D777777" has an embedded distributor leg with invoice "DIST-2026-555" dated "2026-01-15"
  When evaluating sale information for "JTMW43FV10D777777" with language "en"
  Then the sale company is "Sample Dealer"
  And the distributor is "Acme Distributor"
  And the distributor invoice number is "DIST-2026-555"
  And the distributor invoice date is "2026-01-15"
  And there are no intermediaries

Scenario: No distributor or intermediary legs when none are present
  Given company 10 is named "Plain Dealer"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMW43FV10D123456 | 2026-01-15  | 10        | INV-100       |
  When evaluating sale information for "JTMW43FV10D123456" with language "en"
  Then the sale company is "Plain Dealer"
  And there is no distributor
  And there are no intermediaries

# The distributor is the entity that imported the vehicle — a role it keeps however the vehicle was later
# sold. So when it sells straight to a customer it is reported both as the sale and as the distributor.

Scenario: A distributor that sold straight to a customer is still reported as the distributor
  Given the distributor company id is 5
  And the direct end-customer sale account numbers are "DIST-DIRECT-01"
  And company 5 is named "Sample Distributor"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | AccountNumber  | ItemStatus |
    | JTMAB7BJ0T4224184 | 2024-11-01  | 5         | 20024815      | DIST-DIRECT-01 | D          |
  When evaluating sale information for "JTMAB7BJ0T4224184" with language "en"
  Then the sale company is "Sample Distributor"
  And the distributor is "Sample Distributor"
  And the distributor invoice number is "20024815"
  And the distributor invoice date is "2024-11-01"
  And there are no intermediaries
```

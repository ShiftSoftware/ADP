---
hide:
    - toc
---

```gherkin
Feature: Service Item Signature
  Every service item returned by the evaluator is stamped with an HMAC
  signature (for downstream claim-request validation) and a
  SignatureExpiry. The expiry defaults to now but is extended by
  LookupOptions.SignatureValidityDuration when configured.

Scenario: Every service item has a signature and an expiry honoring the configured validity duration
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And LookupOptions has signature validity duration of 30 minutes
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-001" has a signature
  And service item "SI-001" has signature expiry within 30 minutes of now
```

---
hide:
    - toc
---

```gherkin
Feature: Vehicle Sale Information
  Sale information identifies the selling company, branch, invoice details,
  and whether the vehicle was sold through a broker or directly.

Scenario: Direct sale with company and branch
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | InvoiceNumber |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | INV-001       |
  And company 1 is named "Toyota Motors"
  And branch 10 is named "Downtown Branch"
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  Then the sale company is "Toyota Motors"
  And the sale branch is "Downtown Branch"
  And the sale invoice date is "2024-01-15"

Scenario: Vehicle at broker stock (no broker invoice)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  Then the broker is "ABC Motors"
  And the broker invoice date is empty

Scenario: Vehicle sold through broker with invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock | InvoiceDate | InvoiceNumber |
    | 100      | ABC Motors | false     | 2024-02-01  | 5001          |
  And LookupOptions has broker stock lookup enabled
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  Then the broker is "ABC Motors"
  And the broker invoice date is "2024-02-01"

Scenario: End customer from direct sale via customer database
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | CustomerID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | CUST-001   |
  And customer "CUST-001" at company 1 has name "John Doe" and phone "+964770000001"
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en" and end customer lookup
  Then the end customer name is "John Doe"
  And the end customer phone is "+964770000001"

Scenario: End customer from broker invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 10       | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock | InvoiceDate | InvoiceNumber | IsCompleted | CustomerName | CustomerPhone |
    | 100      | ABC Motors | false     | 2024-02-01  | 5001          | true        | Jane Smith   | +964770000002 |
  And LookupOptions has broker stock lookup enabled
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en" and end customer lookup
  Then the end customer name is "Jane Smith"
  And the end customer phone is "+964770000002"

Scenario: No vehicles returns null
  When evaluating sale information for "1FDKF37GXVEB34368" with language "en"
  Then no sale information is available
```

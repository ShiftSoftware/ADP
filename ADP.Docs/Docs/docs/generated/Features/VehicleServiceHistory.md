---
hide:
    - toc
---

```gherkin
Feature: Vehicle Service History
  Service history groups labor lines and part lines into invoices,
  identified by company, branch, invoice number, and order document number.

Scenario: Labor and part lines grouped into a single invoice
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | ServiceDescription |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | Oil change         |
  And part lines:
    | PartNumber | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | SoldQuantity |
    | PRT-001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 2            |
  And company 1 is named "Toyota Motors"
  And branch 10 is named "Downtown Service"
  When evaluating service history with language "en"
  Then there is 1 service history invoice
  And invoice "INV-001" has 1 labor line and 1 part line
  And invoice "INV-001" company is "Toyota Motors"
  And invoice "INV-001" branch is "Downtown Service"

Scenario: Lines from different invoices create separate entries
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  |
    | LAB002    | 1         | 10       | INV-002       | JOB-002             | 2024-07-01  |
  When evaluating service history with language "en"
  Then there are 2 service history invoices

Scenario: Strong consistency excludes invoices with mismatched line counts
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | NumberOfPartLines |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 2                 |
  And part lines:
    | PartNumber | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | NumberOfLaborLines |
    | PRT-001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 1                  |
  When evaluating service history with language "en" and strong consistency
  Then there are 0 service history invoices

Scenario: No labor or part lines returns empty history
  When evaluating service history with language "en"
  Then there are 0 service history invoices
```

---
hide:
    - toc
---
Refers to a Labor Line on an Order (Typically a Job Card on Workshop Module).

| Property | Summary |
|----------|---------|
| LineID <div><strong>``string``</strong></div> | The unique identifier of the Labor Line. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date at which this line was invoiced. |
| OrderDocumentNumber <div><strong>``string``</strong></div> | The Order Number associated with this line. (Job Card Number, Counter Sale Order Number) |
| InvoiceNumber <div><strong>``string``</strong></div> | The Invoice Number associated with this labor line. |
| ParentInvoiceNumber <div><strong>``string``</strong></div> | The Parent Invoice Number associated with this labor line. (In case of Credit Notes or Debit Notes) |
| InvoiceCurrency <div><strong>``Currencies?``</strong></div> | The Invoice [Currency](/generated/Models/Enums/Currencies.html) |
| OrderQuantity <div><strong>``decimal?``</strong></div> | The quantity of the labor line that ordered. |
| SoldQuantity <div><strong>``decimal?``</strong></div> | The quantity of the labor line that sold. |
| SaleType <div><strong>``string``</strong></div> | The type of sale. (e.g. Internal, Bulk, Retail, etc.) |
| PackageCode <div><strong>``string``</strong></div> | The Package Code in case this labor line is a package item. |
| ExtendedPrice <div><strong>``decimal?``</strong></div> | The final price of this line item after accounting for quantity, discounts, and any applicable taxes or additional charges |
| LaborCode <div><strong>``string``</strong></div> | The uniqe Labor/Operation Code |
| ServiceCode <div><strong>``string``</strong></div> | The service code of the parent job associated with this labor line. |
| ServiceDescription <div><strong>``string``</strong></div> | The description of the parent job associated with this labor line. |
| AccountNumber <div><strong>``string``</strong></div> | The Account Number from the Accounting System. |
| CustomerAccountNumber <div><strong>``string``</strong></div> | The Customer Account Number from the Accounting System. |
| CustomerID <div><strong>``string``</strong></div> | The Company Specific Customer ID. |
| GoldenCustomerID <div><strong>``string``</strong></div> | The Centralized unique Golden Customer ID. |
| Department <div><strong>``string``</strong></div> | The Department Code/ID. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) of the vehicle associated with this labor line. |
| LoadDate <div><strong>``DateTime?``</strong></div> | The date at which this line was loaded into the Order Document. |
| PostDate <div><strong>``DateTime?``</strong></div> | The date at which this line was posted. This could mean (Job Completed, Part Dispatched, Vehicle Allocated, etc. based on the type of the Order Document). |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| InvoiceStatus <div><strong>``string``</strong></div> |  |
| ItemStatus <div><strong>``string``</strong></div> |  |
| OrderStatus <div><strong>``string``</strong></div> |  |
| NextServiceDate <div><strong>``DateTime?``</strong></div> | The date for the next scheduled service of the vehicle associated with this labor line. |
| NumberOfPartLines <div><strong>``int``</strong></div> | The number of corresponding part lines associated with the parent order of this line. |
| CompanyIntegrationID <div><strong>``string``</strong></div> | An External Identifier that can be used for system to system Integration |
| BranchIntegrationID <div><strong>``string``</strong></div> | An External Identifier that can be used for system to system Integration |

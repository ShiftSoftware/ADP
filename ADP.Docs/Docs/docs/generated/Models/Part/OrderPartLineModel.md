---
hide:
    - toc
---
Refers to a Part Line on an Order (This might be a Job Card on Workshop Module, or a Counter Sale on Parts Module).

| Property | Summary |
|----------|---------|
| LineID <div><strong>``string``</strong></div> | The unique identifier of the Order Line. |
| OrderStatus <div><strong>``string``</strong></div> |  |
| LoadStatus <div><strong>``string``</strong></div> |  |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date at which this line was invoiced. |
| OrderDocumentNumber <div><strong>``string``</strong></div> | The Order Number associated with this line. (Job Card Number, Counter Sale Order Number) |
| InvoiceNumber <div><strong>``string``</strong></div> | The Invoice Number associated with this part line. |
| InvoiceCurrency <div><strong>``Currencies?``</strong></div> | The Invoice [Currency](/generated/Models/Enums/Currencies.html) |
| SoldQuantity <div><strong>``decimal?``</strong></div> | The quantity of the part line that sold. |
| OrderQuantity <div><strong>``decimal?``</strong></div> | The quantity of the part line that ordered. |
| SaleType <div><strong>``string``</strong></div> | The type of sale. (e.g. Internal, Bulk, Retail, etc.) |
| MenuCode <div><strong>``string``</strong></div> | The Menu Code in case this part line is a menu item. |
| ExtendedPrice <div><strong>``decimal?``</strong></div> | The final price of this line item after accounting for quantity, discounts, and any applicable taxes or additional charges |
| PartNumber <div><strong>``string``</strong></div> | The uniqe Part Number of the [Catalog Part](/generated/Models/Part/CatalogPartModel.html). |
| AccountNumber <div><strong>``string``</strong></div> | The Account Number from the Accounting System. |
| CustomerAccountNumber <div><strong>``string``</strong></div> | The Customer Account Number from the Accounting System. |
| CustomerID <div><strong>``string``</strong></div> | The Company Specific Customer ID. |
| GoldenCustomerID <div><strong>``string``</strong></div> | The Centralized unique Golden Customer ID. |
| Department <div><strong>``string``</strong></div> | The Department Code/ID. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) of the vehicle associated with this part line. |
| LoadDate <div><strong>``DateTime?``</strong></div> | The date at which this line was loaded into the Order Document. |
| PostDate <div><strong>``DateTime?``</strong></div> | The date at which this line was posted. This could mean (Job Completed, Part Dispatched, Vehicle Allocated, etc. based on the type of the Order Document). |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |

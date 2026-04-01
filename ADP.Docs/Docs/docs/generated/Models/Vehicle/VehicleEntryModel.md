---
hide:
    - toc
---
Represents a vehicle entry in the dealer stock. Each entry corresponds to a vehicle record from a dealer's system,
 capturing vehicle specifications, sale/invoice information, and warranty activation details.

| Property | Summary |
|----------|---------|
| BrandHashID <div><strong>``string``</strong></div> | The Brand Hash ID from the Identity System. |
| ProductionDate <div><strong>``DateTime?``</strong></div> | The date the vehicle was produced by the manufacturer. |
| ModelYear <div><strong>``int?``</strong></div> | The model year of the vehicle. |
| ExteriorColorCode <div><strong>``string``</strong></div> | The exterior color code of the vehicle. |
| InteriorColorCode <div><strong>``string``</strong></div> | The interior color code of the vehicle. |
| ModelCode <div><strong>``string``</strong></div> | The model code that identifies the vehicle model. |
| ModelDescription <div><strong>``string``</strong></div> | A human-readable description of the vehicle model. |
| Katashiki <div><strong>``string``</strong></div> | The Katashiki (model-specific identifier used by the manufacturer). |
| VariantCode <div><strong>``string``</strong></div> | The variant code of the vehicle within its model range. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN). |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date on the sale invoice for this vehicle. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| BranchHashID <div><strong>``string``</strong></div> | The Branch Hash ID from the Identity System. |
| WarrantyActivationDate <div><strong>``DateTime?``</strong></div> | The date when the vehicle's warranty was activated. This may differ from the invoice date. |
| InvoiceNumber <div><strong>``string``</strong></div> | The invoice number for the vehicle sale transaction. |
| AccountNumber <div><strong>``string``</strong></div> | The dealer account number associated with this transaction. |
| CustomerAccountNumber <div><strong>``string``</strong></div> | The customer's account number at the dealer. |
| CustomerID <div><strong>``string``</strong></div> | The customer ID associated with this vehicle sale. |
| InvoiceTotal <div><strong>``decimal?``</strong></div> | The total amount on the sale invoice. |
| RegionHashID <div><strong>``string``</strong></div> | The Region Hash ID from the Identity System. |
| SaleType <div><strong>``string``</strong></div> | The type of sale (e.g., Retail, Fleet, Internal). |
| OrderStatus <div><strong>``string``</strong></div> | The current status of the order associated with this vehicle entry. |
| Location <div><strong>``string``</strong></div> | The location or warehouse identifier where the vehicle is held. |
| InvoiceCurrency <div><strong>``Currencies?``</strong></div> | The currency used on the sale invoice. |
| LineID <div><strong>``string``</strong></div> | The line item identifier within the order document. |
| LoadDate <div><strong>``DateTime?``</strong></div> | The date when this record was loaded into the system. |
| PostDate <div><strong>``DateTime?``</strong></div> | The date when this record was posted/finalized. |
| EligibleServiceItemUniqueReferences <div><strong>``IEnumerable<string>``</strong></div> | Per Vehicle Service Item Eligibility. A list of unique references for service items this vehicle is eligible for. |
| OrderDocumentNumber <div><strong>``string``</strong></div> | The order document number for this vehicle transaction. |
| OrderQuantity <div><strong>``decimal?``</strong></div> | The quantity ordered. |
| SoldQuantity <div><strong>``decimal?``</strong></div> | The quantity sold. |
| ExtendedPrice <div><strong>``decimal?``</strong></div> | The extended price (unit price multiplied by quantity). |
| ItemStatus <div><strong>``string``</strong></div> | The status of the line item (e.g., Open, Closed). |
| InvoiceStatus <div><strong>``string``</strong></div> | The status of the invoice (e.g., Paid, Pending). |
| CompanyIntegrationID <div><strong>``string``</strong></div> | An external identifier used for company-level system-to-system integration. |
| BranchIntegrationID <div><strong>``string``</strong></div> | An external identifier used for branch-level system-to-system integration. |

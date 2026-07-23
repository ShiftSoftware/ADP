---
hide:
    - toc
---
Contains the sale information for a vehicle — the dealer, invoice details, warranty activation, broker, and end customer.

| Property | Summary |
|----------|---------|
| CountryID <div><strong>``string``</strong></div> | The Country Hash ID where the vehicle was sold. |
| CountryName <div><strong>``string``</strong></div> | The resolved country name. |
| CompanyID <div><strong>``string``</strong></div> | The Company Hash ID of the selling dealer. |
| CompanyName <div><strong>``string``</strong></div> | The resolved company (dealer) name. |
| BranchID <div><strong>``string``</strong></div> | The Branch Hash ID of the selling branch. |
| BranchName <div><strong>``string``</strong></div> | The resolved branch name. |
| CustomerAccountNumber <div><strong>``string``</strong></div> | The customer's account number at the dealer. |
| CustomerID <div><strong>``string``</strong></div> | The customer ID from the dealer's system. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date on the sale invoice. |
| WarrantyActivationDate <div><strong>``DateTime?``</strong></div> | The date the vehicle's warranty was activated. |
| InvoiceNumber <div><strong>``string``</strong></div> | The sale invoice number. |
| Broker <div><strong>``VehicleBrokerSaleInformation``</strong></div> | The [broker sale information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleBrokerSaleInformation.html) if the vehicle was sold through a broker. |
| Distributor <div><strong>``VehicleDistributorSaleInformation``</strong></div> | The [distributor leg](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleDistributorSaleInformation.html) of the supply chain, when the vehicle passed through a configured distributor before the dealer's sale. Null when there is no distributor leg (or none is configured). The distributor never makes the end-customer sale; this is informational context for the dealer sale above. |
| Intermediaries <div><strong>``List<VehicleIntermediarySaleInformation>``</strong></div> | The [intermediary legs](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleIntermediarySaleInformation.html) (0..n) the vehicle passed through between the distributor and the dealer. Empty when there are none. Intermediaries never make the end-customer sale; this is informational context for the dealer sale above. |
| RegionID <div><strong>``string``</strong></div> | The Region Hash ID from the Identity System. |
| EndCustomer <div><strong>``VehicleSaleEndCustomerInformationDTO``</strong></div> | The [end customer information](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleSaleEndCustomerInformationDTO.html) for this sale. |
| CityID <div><strong>``string``</strong></div> | The City Hash ID from the Identity System. |
| CityName <div><strong>``string``</strong></div> | The resolved City Name. |

---
hide:
    - toc
---
Represents a service history entry for a vehicle — a past service invoice with its labor and part lines.

| Property | Summary |
|----------|---------|
| ServiceType <div><strong>``string``</strong></div> | The type of service performed (e.g., Warranty, Paid, Internal). |
| ServiceDate <div><strong>``DateTime?``</strong></div> | The date the service was performed. |
| Mileage <div><strong>``int?``</strong></div> | The vehicle's odometer reading at the time of service. |
| CompanyName <div><strong>``string``</strong></div> | The localized name of the company that performed the service. |
| BranchName <div><strong>``string``</strong></div> | The localized name of the branch that performed the service. |
| AccountNumber <div><strong>``string``</strong></div> | The dealer account number. |
| InvoiceNumber <div><strong>``string``</strong></div> | The service invoice number. |
| ParentInvoiceNumber <div><strong>``string``</strong></div> | The parent invoice number (in case of credit/debit notes). |
| JobNumber <div><strong>``string``</strong></div> | The job/work order number. |
| LaborLines <div><strong>``IEnumerable<VehicleLaborDTO>``</strong></div> | The [labor lines](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleLaborDTO.html) on this service invoice. |
| PartLines <div><strong>``IEnumerable<VehiclePartDTO>``</strong></div> | The [part lines](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehiclePartDTO.html) on this service invoice. |

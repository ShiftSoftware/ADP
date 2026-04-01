---
hide:
    - toc
---
Represents a comprehensive vehicle inspection record.
 Captures inspection details, technician information, vehicle photos, and customer information at the time of the inspection.

| Property | Summary |
|----------|---------|
| VehicleInspectionTypeID <div><strong>``long``</strong></div> | The type of vehicle inspection performed. |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) of the inspected vehicle. |
| InspectionDate <div><strong>``DateTimeOffset``</strong></div> | The date the inspection was performed. |
| Model <div><strong>``string``</strong></div> | The vehicle model description. |
| ModelYear <div><strong>``int``</strong></div> | The model year of the inspected vehicle. |
| ModelCode <div><strong>``string``</strong></div> | The model code of the inspected vehicle. |
| JobNumber <div><strong>``string``</strong></div> | The job number from the dealer's service system for this inspection. |
| TechnicianName <div><strong>``string``</strong></div> | The name of the technician who performed the inspection. |
| QualityControlName <div><strong>``string``</strong></div> | The name of the quality control reviewer for this inspection. |
| FrontPhoto <div><strong>``string``</strong></div> | URL of the front photo of the vehicle taken during the inspection. |
| RearPhoto <div><strong>``string``</strong></div> | URL of the rear photo of the vehicle taken during the inspection. |
| CustomerCountryID <div><strong>``long?``</strong></div> | The country ID of the customer at the time of inspection. |
| CustomerCityID <div><strong>``long?``</strong></div> | The city ID of the customer at the time of inspection. |
| CustomerType <div><strong>``CustomerTypes?``</strong></div> | The type of customer (e.g., Individual, Organization). |
| OrganizationName <div><strong>``string``</strong></div> | The organization name if the customer is an organization. |
| CustomerFirstName <div><strong>``string``</strong></div> | The customer's first name. |
| CustomerMiddleName <div><strong>``string``</strong></div> | The customer's middle name. |
| CustomerLastName <div><strong>``string``</strong></div> | The customer's last name. |
| CustomerPhone <div><strong>``string``</strong></div> | The customer's phone number. |
| CustomerEmail <div><strong>``string``</strong></div> | The customer's email address. |
| CustomerGender <div><strong>``Genders``</strong></div> | The customer's gender. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this inspection record has been deleted. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |

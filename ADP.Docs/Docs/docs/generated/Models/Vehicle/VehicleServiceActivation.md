---
hide:
    - toc
---
Represents the activation of warranty services for a vehicle.
 Captures when the warranty was activated and the customer information at the time of activation.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN). |
| WarrantyActivationDate <div><strong>``DateTime?``</strong></div> | The date when the vehicle's warranty services were activated. |
| CustomerCountryID <div><strong>``long?``</strong></div> | The country ID of the customer at the time of service activation. |
| CustomerCityID <div><strong>``long?``</strong></div> | The city ID of the customer at the time of service activation. |
| CustomerType <div><strong>``CustomerTypes?``</strong></div> | The type of customer (e.g., Individual, Organization). |
| OrganizationName <div><strong>``string``</strong></div> | The organization name if the customer is an organization. |
| CustomerFirstName <div><strong>``string``</strong></div> | The customer's first name. |
| CustomerMiddleName <div><strong>``string``</strong></div> | The customer's middle name. |
| CustomerLastName <div><strong>``string``</strong></div> | The customer's last name. |
| CustomerPhone <div><strong>``string``</strong></div> | The customer's phone number. |
| CustomerEmail <div><strong>``string``</strong></div> | The customer's email address. |
| CustomerGender <div><strong>``Genders``</strong></div> | The customer's gender. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this activation record has been deleted. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |

---
hide:
    - toc
---
Represents a customer record from a dealer's system.
 Each dealer may have its own customer record for the same person. The Golden Customer links these into a unified identity.

| Property | Summary |
|----------|---------|
| GoldenCustomerID <div><strong>``string``</strong></div> | The corresponding [Golden Customer](/generated/Models/Customer/GoldenCustomerModel.html) ID that this dealer customer is linked to. |
| CustomerID <div><strong>``string``</strong></div> | The original customer ID from the dealer's system. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| FullName <div><strong>``string``</strong></div> | The customer's full name. |
| PhoneNumbers <div><strong>``IEnumerable<string>``</strong></div> | A list of phone numbers associated with the customer. |
| Address <div><strong>``IEnumerable<string>``</strong></div> | The customer's address lines. |
| Gender <div><strong>``Genders``</strong></div> | The customer's gender. |
| DateOfBirth <div><strong>``DateTime?``</strong></div> | The customer's date of birth. |
| IDNumber <div><strong>``string``</strong></div> | Social ID, Driver License Number, Passport Number, or any other government-issued ID number that can be used to uniquely identify a customer across different systems. |

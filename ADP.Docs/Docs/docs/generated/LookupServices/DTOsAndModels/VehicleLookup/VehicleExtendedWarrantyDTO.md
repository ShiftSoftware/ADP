---
hide:
    - toc
---
One extended-warranty coverage and its provider.

| Property | Summary |
|----------|---------|
| ID <div><strong>``string``</strong></div> | The persisted warranty-entry or configured definition identifier. |
| Name <div><strong>``string?``</strong></div> | The coverage's display name. Comes from the configured definition's `ShiftSoftware.ADP.Models.Vehicle.ExtendedWarrantyDefinitionModel.Name` or the persisted entry's name. Null when neither carries one, in which case the consumer is expected to fall back to its own generic "extended warranty" wording rather than showing `ID`. |
| ProviderCompanyID <div><strong>``string``</strong></div> | The Identity company ID of the warranty provider. |
| ProviderCompanyLogo <div><strong>``string?``</strong></div> | The resolved logo URL of the warranty provider. |
| ProviderCompanyName <div><strong>``string?``</strong></div> | The resolved display name of the warranty provider. |
| StartDate <div><strong>``DateTime?``</strong></div> | The first date covered by this extended warranty. |
| EndDate <div><strong>``DateTime?``</strong></div> | The coverage end date. |

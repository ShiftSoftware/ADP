---
hide:
    - toc
---
Contains the warranty status and dates for a vehicle, including standard warranty, extended warranty, and free service eligibility.

| Property | Summary |
|----------|---------|
| HasActiveWarranty <div><strong>``bool``</strong></div> | Whether the vehicle currently has an active standard warranty (end date is in the future). |
| WarrantyStartDate <div><strong>``DateTime?``</strong></div> | The start date of the standard warranty period. |
| WarrantyEndDate <div><strong>``DateTime?``</strong></div> | The end date of the standard warranty period. |
| ActivationIsRequired <div><strong>``bool``</strong></div> | Indicates whether warranty activation is required before the warranty becomes effective. |
| HasExtendedWarranty <div><strong>``bool``</strong></div> | Whether the vehicle currently has an active extended warranty (end date is in the future). |
| ExtendedWarrantyStartDate <div><strong>``DateTime?``</strong></div> | The start date of the extended warranty period. |
| ExtendedWarrantyEndDate <div><strong>``DateTime?``</strong></div> | The end date of the extended warranty period. |
| FreeServiceStartDate <div><strong>``DateTime?``</strong></div> | The start date from which free service items become eligible. |

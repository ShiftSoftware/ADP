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
| DeFactoServiceStartDate <div><strong>``DateTime?``</strong></div> | The earliest non-deleted `ItemClaim.ClaimDate` for this vehicle. Always populated
 when at least one non-deleted claim exists, regardless of whether it ends up being used.
 When the regular fallback chain (service activation / sale warranty / sale invoice /
 broker invoice) would otherwise leave `FreeServiceStartDate` null, this
 value is used as the effective `FreeServiceStartDate` so downstream items
 project as if activation had occurred — the act of claiming is itself evidence the
 vehicle has been serviced. `FreeServiceItemDateShiftModel` overrides still
 win over this fallback. |

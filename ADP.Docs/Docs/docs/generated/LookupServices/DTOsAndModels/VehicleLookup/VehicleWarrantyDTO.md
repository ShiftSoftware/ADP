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
| ActivationIsRequired <div><strong>``bool``</strong></div> | Indicates whether warranty activation is due for this vehicle (it has pending warranty-activation–triggered
 free service items). Company-agnostic — it does not consider which dealer is asking. Consumed by bulk
 reporting/exports. For the dealer-facing activation affordance use `ActivationStatus`. |
| ActivationStatus <div><strong>``WarrantyActivationStatus``</strong></div> | The company-scoped activation state for the requesting dealer, used to drive the lookup UI.
 `WarrantyActivationStatus.Required` offers activation (the vehicle is allocated to the requester's
 company); `WarrantyActivationStatus.BlockedNotAllocated` warns that activation is due but the
 vehicle is not allocated to the requester; `WarrantyActivationStatus.NotRequired` shows nothing.
 Driven by `LookupOptions.RequireAllocationForActivation` and the caller-supplied
 `VehicleLookupRequestOptions.RequestingCompanyID`; with the guard off it mirrors
 `ActivationIsRequired`, and with no requesting company the affordance is suppressed. |
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

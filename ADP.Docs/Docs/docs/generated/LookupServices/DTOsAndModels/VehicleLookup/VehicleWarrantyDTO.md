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
| StartState <div><strong>``WarrantyStartState``</strong></div> | Why the warranty has or has not started. When this is not `WarrantyStartState.Started` there is no coverage period yet and `WarrantyStartDate` is null — the reason is the vehicle's possession state, not missing data, and is meant to be shown rather than hidden. |
| ActivatedByBrokerName <div><strong>``string?``</strong></div> | The broker whose invoice anchored the warranty, when the vehicle was sold through one. Null in every other case, including a broker that has not invoiced yet. |
| ActivationIsRequired <div><strong>``bool``</strong></div> | Indicates whether warranty activation is due for this vehicle (it has pending warranty-activation–triggered free service items). Company-agnostic — it does not consider which dealer is asking. Consumed by bulk reporting/exports. For the dealer-facing activation affordance use `ActivationStatus`. |
| ActivationStatus <div><strong>``WarrantyActivationStatus``</strong></div> | The company-scoped activation state for the requesting dealer, used to drive the lookup UI. `WarrantyActivationStatus.Required` offers activation (the vehicle is allocated to the requester's company); `WarrantyActivationStatus.BlockedNotAllocated` warns that activation is due but the vehicle is not allocated to the requester; `WarrantyActivationStatus.NotRequired` shows nothing. Driven by `LookupOptions.RequireAllocationForActivation` and the caller-supplied `VehicleLookupRequestOptions.RequestingCompanyID`; with the guard off it mirrors `ActivationIsRequired`, and with no requesting company the affordance is suppressed. |
| HasExtendedWarranty <div><strong>``bool``</strong></div> | Whether the vehicle currently has an active extended warranty (end date is in the future). |
| ExtendedWarrantyStartDate <div><strong>``DateTime?``</strong></div> | The start date of the extended warranty period. |
| ExtendedWarrantyEndDate <div><strong>``DateTime?``</strong></div> | The end date of the extended warranty period. |
| ExtendedWarranties <div><strong>``List<VehicleExtendedWarrantyDTO>``</strong></div> | Every extended-warranty coverage awarded to the vehicle: persisted entries plus any awarded by a configured `LookupOptions.ExtendedWarrantyDefinitions` definition. The flat `ExtendedWarranty*` fields above are unrelated legacy output describing only the latest-ending persisted entry, and are not a summary of this collection. |
| FreeServiceStartDate <div><strong>``DateTime?``</strong></div> | The start date from which free service items become eligible. |
| DeFactoServiceStartDate <div><strong>``DateTime?``</strong></div> | The earliest non-deleted `ItemClaim.ClaimDate` for this vehicle. Always populated when at least one non-deleted claim exists, regardless of whether it ends up being used. When the regular fallback chain (service activation / sale warranty / sale invoice / broker invoice) would otherwise leave `FreeServiceStartDate` null, this value is used as the effective `FreeServiceStartDate` so downstream items project as if activation had occurred — the act of claiming is itself evidence the vehicle has been serviced. `FreeServiceItemDateShiftModel` overrides still win over this fallback. |

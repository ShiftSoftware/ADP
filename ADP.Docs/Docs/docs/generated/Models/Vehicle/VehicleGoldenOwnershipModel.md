---
hide:
    - toc
---
The by-VIN projection of vehicle ownership: every `GoldenCustomerModel` linked to
 one VIN, ordered as an ownership timeline — current-owner lookup and ownership history for a
 vehicle. One document per VIN that has any customer links.
 The current owner is the "owner"-role link with a null `GoldenVehicleLinkModel.EffectiveTo`.
 Note: golden ownership deliberately spans companies, so this document carries no CompanyID —
 it lives at the container's undefined CompanyID partition level.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The vehicle's VIN. Same value as `id`. |
| Owners <div><strong>``IEnumerable<GoldenVehicleLinkModel>``</strong></div> | The vehicle's customer links: "owner"-role links first in ownership-timeline order (each closed by the next owner's `GoldenVehicleLinkModel.EffectiveFrom`), then "service-contact" links. |

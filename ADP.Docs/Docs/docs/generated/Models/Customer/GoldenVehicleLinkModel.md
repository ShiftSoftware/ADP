---
hide:
    - toc
---
One resolved vehicle↔customer ownership link: a Golden Customer
 holding one role on one VIN over one time period, with the evidence that supports it.
 The element type shared by both projections of the ownership data:
 `GoldenCustomerVehicleLinksModel` (by customer) and
 `Vehicle.VehicleGoldenOwnershipModel` (by VIN).

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The vehicle's VIN. |
| GoldenCustomerID <div><strong>``string``</strong></div> | The [Golden Customer](/generated/Models/Customer/GoldenCustomerModel.html) this link belongs to. |
| FullName <div><strong>``string``</strong></div> | The golden customer's survived full name — denormalized for display (service-bay forms, ownership-history views) without a second read. |
| Role <div><strong>``string``</strong></div> | The role the customer holds on the vehicle. Extensible vocabulary; currently "owner" (sale-grade evidence binds the VIN to this customer) or "service-contact" (the customer is only observed bringing the vehicle in for service — possibly a driver, relative, or later owner; never treated as ownership). |
| EffectiveFrom <div><strong>``DateTime?``</strong></div> | When the link becomes effective. For "owner" links: the first sale-grade evidence date. For "service-contact" links: the first observed service visit. Null when the evidence carries no usable date. |
| EffectiveTo <div><strong>``DateTime?``</strong></div> | When the link ends. For "owner" links: the next owner's `EffectiveFrom` when an ownership transfer is detected (a later sale of the same VIN to a different golden customer); null while the link is current. For "service-contact" links: the last observed service visit (an observation window, not a tenure claim). |
| SaleCount <div><strong>``int``</strong></div> | Number of sale-grade evidence records (vehicle sales / warranty activations) behind this link. |
| ServiceCount <div><strong>``int``</strong></div> | Number of service-visit evidence records behind this link. |
| FirstSale <div><strong>``DateTime?``</strong></div> | Date range covered by the sale-grade evidence, when dated. |
| LastSale <div><strong>``DateTime?``</strong></div> | See `FirstSale`. |
| FirstService <div><strong>``DateTime?``</strong></div> | Date range covered by the service-visit evidence, when dated. |
| LastService <div><strong>``DateTime?``</strong></div> | See `FirstService`. |
| Sources <div><strong>``IEnumerable<string>``</strong></div> | The source records that contributed evidence to this link, as "sourceSystem\|sourceRecordId" keys (the same key form as `GoldenCustomerModel.SourceProfiles`). |

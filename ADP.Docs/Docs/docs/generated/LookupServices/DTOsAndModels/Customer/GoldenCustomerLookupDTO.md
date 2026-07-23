---
hide:
    - toc
---
One golden-customer match from a customer lookup: the unified identity plus the vehicles the
 identity-resolution engine links to it. A lookup by phone can return SEVERAL matches — phone
 numbers are legitimately shared (family members, a company and its contact person, dealer
 front-desk numbers) — so consumers must disambiguate by name/role, never assume one hit.

| Property | Summary |
|----------|---------|
| Customer <div><strong>``GoldenCustomerModel``</strong></div> | The unified golden-customer identity (survived name, phones, city, e-mail, source backlinks). |
| Vehicles <div><strong>``List<GoldenVehicleLinkModel>``</strong></div> | The vehicles linked to this identity, with role (`owner` on sale-grade evidence, `service-contact` for service-only observation), effective period, and per-source counts. Empty when the identity has no vehicle links. |

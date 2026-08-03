---
hide:
    - toc
---
Why the vehicle's service menu section holds what it holds.

| Value | Summary |
|-------|---------|
| NoBasicModelCode | The vehicle carries no Katashiki, so no basic model code could be derived and no lookup was attempted. Not a miss — there was nothing to join on. |
| NotFound | A basic model code was derived, and no menu is replicated under it. Either nobody authored a menu for this model, or the derived code does not match the authored one — the two are indistinguishable from here, and together they are the O3 miss rate. |
| Found | A menu was found for the derived code. `Services` can still be empty: a menu whose every variant is deleted, whose intervals carry no labour details, or whose variants were all excluded by `VehicleServiceMenuRequestOptions.FreeFilter`, exists and generates nothing — a different thing from having no menu, and usually rendered differently. |
| Unavailable | The menu lookup could not be consulted — the Cosmos containers are not provisioned, the documents reference master data they do not carry, or the read itself failed. The vehicle lookup succeeds anyway; only this section is missing. Remediation is in the menus host: provision with `MenuCosmosProvisioning.EnsureContainersAsync`, then run a full catch-up sweep. |
| NotRegistered | The host never registered the service-menu lookup, so there is nothing to ask. Distinct from `Unavailable` because the fix is different: call `AddServiceMenuLookup` (or use `AddLookupService`, which now does) rather than provisioning anything. |

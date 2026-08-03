---
hide:
    - toc
---
Whether to attach the model's service menu to a vehicle lookup, and how to generate it. Supplied on
 `VehicleLookupRequestOptions.ServiceMenuOptions`; set `Include` to turn the
 section on, and everything else is optional.

| Property | Summary |
|----------|---------|
| Include <div><strong>``bool``</strong></div> | Whether to include the model's service menu (`VehicleLookupDTO.ServiceMenu`) — the DMS menu codes, labour codes and prices for every service the vehicle's model offers. Off by default, so a caller that supplies this object for its other settings still has to ask for the section. |
| CountryID <div><strong>``long?``</strong></div> | The country whose part prices and labour rate the menu is priced for. When null, the menu lookup's own `ServiceMenuLookupOptions.DefaultCountryID` applies, then 0 — which is the convention a deployment with no configured countries stores its prices under, not a magic value. |
| TransferRate <div><strong>``decimal?``</strong></div> | Scales the consumable charge on every generated line; 1 leaves it unscaled. When set, this WINS over the host's `ServiceMenuLookupOptions.CountrySettingsResolver` — it is the caller's explicit choice, and a value that is silently ignored is worse than one that is absent. When null the resolver applies, then 1. |

---
hide:
    - toc
---
The service menu offered for this vehicle's model: the DMS menu codes, labour codes and prices for every
 service the model's menu offers, flattened into one list. Read `Status` before reading
 `Services` — an empty list means four different things (no join key, no menu authored for the
 model, a menu that generates nothing, or a menu subsystem that could not be consulted) and only the
 status separates them. A menu fault never fails the vehicle lookup; it arrives here as
 `VehicleServiceMenuStatus.Unavailable`.

| Property | Summary |
|----------|---------|
| Status <div><strong>``VehicleServiceMenuStatus``</strong></div> | Whether a menu was found, and if not, why. Also the per-lookup signal a deployment counts to measure the derived-key hit rate (open item O3) — see [VehicleServiceMenuStatus](/generated/LookupServices/DTOsAndModels/VehicleLookup/VehicleServiceMenuStatus.html). |
| BasicModelCode <div><strong>``string``</strong></div> | The join key that was tried: the basic model code derived from the vehicle's Katashiki (`VehicleLookupDTO.BasicModelCode`), matched exactly against the authored menu code. Null when the vehicle has no Katashiki. Echoed even on a miss, because "which code did it look for" is the first question anyone asks about one. |
| CountryID <div><strong>``long``</strong></div> | The country the part prices and labour rate were resolved for. 0 unless a menu was actually generated. |
| Language <div><strong>``string``</strong></div> | The language the codes and descriptions were generated in — the request's language code. |
| TransferRate <div><strong>``decimal``</strong></div> | The transfer rate the consumable was scaled by; 1 means unscaled. 0 unless a menu was actually generated. |
| Services <div><strong>``List<VehicleServiceMenuLineDTO>``</strong></div> | Every service the model's menu offers, flat: per variant, the scheduled services in distance order followed by the standalone ones. Each line carries its variant, so a UI that wants the nested shape groups by `VehicleServiceMenuLineDTO.VariantID`. Empty whenever `Status` is not `VehicleServiceMenuStatus.Found` — and it can be empty even then, for a menu that exists but generates nothing. |

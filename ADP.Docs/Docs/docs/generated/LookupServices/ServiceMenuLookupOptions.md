---
hide:
    - toc
---
Configuration for the service-menu lookup, registered with
 `AddServiceMenuLookup` and injected as `IOptions&lt;ServiceMenuLookupOptions&gt;`.

| Property | Summary |
|----------|---------|
| CosmosDatabaseNameSuffix <div><strong>``string``</strong></div> | Optional suffix appended to the platform-standard Cosmos database name for menu reads (e.g. "-alt" resolves "Services" as "Services-alt"). Intended for shared-emulator dev scenarios where more than one projection set coexists on one local emulator; a production deployment has its own Cosmos account and keeps the standard names (leave unset). |
| DefaultCountryID <div><strong>``long?``</strong></div> | The country used when a request does not name one. A deployment serving a single country sets this once instead of threading it through every call. When neither this nor the request supplies a country, 0 is used — what a deployment with no configured countries stores its prices under. |
| CountrySettingsResolver <div><strong>``Func<long, ValueTask<ServiceMenuCountrySettings>>``</strong></div> | Resolves the transfer rate and labour-rate mode for the country being looked up. |

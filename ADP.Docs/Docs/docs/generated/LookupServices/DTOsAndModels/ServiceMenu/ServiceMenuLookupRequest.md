---
hide:
    - toc
---
What to generate a service menu for. Everything except the basic model code is optional; the defaults come
 from `ServiceMenuLookupOptions`, the feature's own options — not from
 `LookupOptions`.

| Property | Summary |
|----------|---------|
| BasicModelCode <div><strong>``string``</strong></div> | The model to generate for. This is the Cosmos partition key, so the lookup is a single-partition read. Matched exactly (after trimming) against the authored basic model code — no prefix or fuzzy matching, because a near-miss would serve another model's menu codes. |
| CountryID <div><strong>``long?``</strong></div> | The country whose part prices and labour rate apply. When null, falls back to `ServiceMenuLookupOptions.DefaultCountryID` and then to 0 — which is the single-country deployment's own convention, not a magic value: a deployment with one country stores its prices under whatever id it uses, and one with none uses 0. |
| Language <div><strong>``string``</strong></div> | Language for the multi-language parts of a code (prefixes, postfixes, operation codes). A two-letter code or a culture name; null or empty means English. One request generates ONE language — call again to correlate another, matching lines by `ServiceMenuLineDTO.LineKey`. |
| TransferRate <div><strong>``decimal?``</strong></div> | Scales the consumable charge. When set, it WINS over `ServiceMenuLookupOptions.CountrySettingsResolver`; when null the resolver applies, then 1 (no scaling). Setting a value and silently getting a different one back is the worse failure, so an explicit choice is honoured — a host that wants the resolver to be the sole authority does not expose this to its callers. It moves money, never codes: the labour-rate mapping is always keyed by the variant's primary rate. |
| FreeFilter <div><strong>``ServiceMenuFreeFilter``</strong></div> | Which variants to return, by their free-of-charge flag: all of them (the default), only the free ones, or only the ones that are not free. See [ServiceMenuFreeFilter](/generated/LookupServices/DTOsAndModels/ServiceMenu/ServiceMenuFreeFilter.html). |

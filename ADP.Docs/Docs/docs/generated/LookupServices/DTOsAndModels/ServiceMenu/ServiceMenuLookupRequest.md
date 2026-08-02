---
hide:
    - toc
---
What to generate a service menu for. Everything except the basic model code is optional; the
 defaults are resolved from `LookupOptions` (see
 `LookupOptions.ServiceMenuCountrySettingsResolver`).

| Property | Summary |
|----------|---------|
| BasicModelCode <div><strong>``string``</strong></div> | The model to generate for. This is the Cosmos partition key, so the lookup is a single-partition read. Matched exactly (after trimming) against the authored basic model code — no prefix or fuzzy matching, because a near-miss would serve another model's menu codes. |
| CountryID <div><strong>``long?``</strong></div> | The country whose part prices and labour rate apply. When null, falls back to `LookupOptions.ServiceMenuDefaultCountryID` and then to 0 — which is the single-country deployment's own convention, not a magic value: a deployment with one country stores its prices under whatever id it uses, and one with none uses 0. |
| Language <div><strong>``string``</strong></div> | Language for the multi-language parts of a code (prefixes, postfixes, operation codes). A two-letter code or a culture name; null or empty means English. One request generates ONE language — call again to correlate another, matching lines by `ServiceMenuLineDTO.LineKey`. |
| TransferRate <div><strong>``decimal?``</strong></div> | Scales the consumable charge. Ignored when `LookupOptions.ServiceMenuCountrySettingsResolver` is configured — the host's resolver is then the authority, exactly as the export's country normalisation is. Defaults to 1 (no scaling). |

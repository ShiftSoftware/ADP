---
hide:
    - toc
---
A labor operation code that satisfied (or may satisfy) one of an SSC campaign's own labor codes during repair
 detection: the code as it appeared on the evidence, and the campaign code it stands for. When the two are
 equal the match was direct; when they differ the code was accepted through
 `LookupOptions.SSCInterchangeableLaborCodeGroups`.

| Property | Summary |
|----------|---------|
| LaborCode <div><strong>``string``</strong></div> | The labor operation code as it appears on the evidence (claim line, service-history line) or in the configured interchangeable group. |
| CampaignLaborCode <div><strong>``string``</strong></div> | The campaign's own labor operation code this code stands for. |

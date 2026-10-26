---
hide:
    - toc
---
Which source of evidence decided that a Special Service Campaign (SSC) / safety recall is repaired.
 The sources are consulted in the order they are declared here, and the first one that holds wins. A deployment
 that sets `LookupOptions.PreferSSCRepairEvidenceOverRecordDate` consults the SSC record last instead.

| Value | Summary |
|-------|---------|
| None | No repair evidence was found; the campaign is still open on this vehicle. |
| SSCRecord | The SSC record itself carries a repair date. |
| WarrantyClaim | A warranty claim in a qualifying status (Accepted, Certified or Invoiced) references the campaign — either by naming the campaign code in the distributor comment, or by carrying one of the campaign's labor operation codes (or a code configured as interchangeable with one). |
| ServiceHistory | A finished service-history labor line carries one of the campaign's labor operation codes (or a code configured as interchangeable with one). By default a line is finished when its invoice status is X or C; a deployment can set its own rule (`LookupOptions.SSCQualifyingLaborLine`). |

---
hide:
    - toc
---
Which source of evidence decided that a Special Service Campaign (SSC) / safety recall is repaired.
 The sources are consulted in the order they are declared here, and the first one that holds wins.

| Value | Summary |
|-------|---------|
| None | No repair evidence was found; the campaign is still open on this vehicle. |
| SSCRecord | The SSC record itself carries a repair date. |
| WarrantyClaim | A warranty claim in a qualifying status (Accepted, Certified or Invoiced) references the campaign — either by naming the campaign code in the distributor comment, or by carrying one of the campaign's labor operation codes (or a code configured as interchangeable with one). |
| ServiceHistory | An invoiced service-history labor line (invoice status X or C) carries one of the campaign's labor operation codes (or a code configured as interchangeable with one). |

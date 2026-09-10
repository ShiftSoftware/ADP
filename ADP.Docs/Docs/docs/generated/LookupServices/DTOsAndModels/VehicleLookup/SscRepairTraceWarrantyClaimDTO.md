---
hide:
    - toc
---
One warranty claim on the vehicle as the SSC repair check saw it. Every claim on the VIN is listed, whether or
 not it matched, so a reader can see why a claim was or was not taken as repair evidence.

| Property | Summary |
|----------|---------|
| ClaimNumber <div><strong>``string``</strong></div> | The distributor's claim number. |
| DealerClaimNumber <div><strong>``string``</strong></div> | The dealer's own claim number, when different. |
| ClaimStatus <div><strong>``ClaimStatus``</strong></div> | The claim's status at lookup time. |
| RepairCompletionDate <div><strong>``DateTime?``</strong></div> | When the claimed repair was completed. This becomes the SSC repair date when the claim is selected. |
| StatusQualifies <div><strong>``bool``</strong></div> | Whether the status is one that counts as repair evidence (Accepted, Certified or Invoiced). |
| CampaignCodeInComment <div><strong>``bool``</strong></div> | Whether the campaign code was found in the claim's distributor comment. |
| DistributorComment <div><strong>``string``</strong></div> | The distributor comment that was searched for the campaign code. |
| MatchedLaborCodes <div><strong>``List<SscRepairTraceLaborCodeDTO>``</strong></div> | The claim's labor lines whose code is one of the campaign's codes, or interchangeable with one. Empty when none match. |
| Matches <div><strong>``bool``</strong></div> | Whether the claim references the campaign at all — by comment or by labor code — regardless of status. |
| Selected <div><strong>``bool``</strong></div> | Whether this is the claim that decided the verdict: the most recently completed claim that both qualifies by status and matches. |

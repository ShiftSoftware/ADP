---
hide:
    - toc
---
Overrides the validity window of a single free service item on a single vehicle.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this override applies to. |
| ServiceItemID <div><strong>``string``</strong></div> | The service item whose dates are overridden, matched against `ServiceItemModel.IntegrationID` — the same identifier `ItemClaimModel.ServiceItemID` is matched by. An override naming an item this vehicle is not offered is inert; the lookup trace reports it rather than failing. |
| UnlockedOn <div><strong>``DateTime?``</strong></div> | The moment to treat the item as earned. The item activates here and expires its own `ServiceItemModel.ActiveFor` later, which is the same arithmetic the evaluator applies to a reward's real unlock date — so an operator states the fact ("this customer earned it on the 1st") rather than back-computing a window from it. |
| ExpiresAt <div><strong>``DateTime?``</strong></div> | The date the item expires, overriding both the schedule's answer and anything `UnlockedOn` computes. Set it alone to extend an item in place, or alongside `UnlockedOn` to state both ends of the window outright. |
| Reason <div><strong>``string``</strong></div> | Why the override exists, in the operator's own words. Carried into the lookup trace, so the answer to "why is this one vehicle's item still open" is the reason it was granted rather than a reconstruction of it. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this override has been deleted (returning the item to the dates the schedule computes for it). |

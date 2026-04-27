---
hide:
    - toc
---
Represents a per-VIN entry recorded against a service campaign.
 Used by campaigns whose  is
  — an admin tags a VIN
 as eligible for a specific campaign (e.g., the 500th customer reward), and the evaluator activates
 every  in that campaign for the VIN at .

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) that qualifies for the campaign. |
| CampaignID <div><strong>``long?``</strong></div> | The ID of the [campaign](/generated/Models/Vehicle/ServiceCampaignModel.html) this entry qualifies the VIN for.
 Matched against  by the evaluator. |
| CampaignUniqueReference <div><strong>``string``</strong></div> | The unique reference of the parent campaign (mirror of ),
 useful for human-readable lookups and auditing. |
| RecordedDate <div><strong>``DateTimeOffset``</strong></div> | The date this entry was recorded. Used as the activation date for items with
  validity. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this entry has been deleted (effectively revoking the VIN's eligibility). |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |

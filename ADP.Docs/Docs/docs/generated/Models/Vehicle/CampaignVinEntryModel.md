---
hide:
    - toc
---
Represents a per-VIN entry recorded against a service campaign.
 Used by campaigns whose `Enums.ClaimableItemCampaignActivationTrigger` is
 `Enums.ClaimableItemCampaignActivationTrigger.ManualVinEntry` — an admin tags a VIN
 as eligible for a specific campaign (e.g., the 500th customer reward), and the evaluator activates
 every `ServiceItemModel` in that campaign for the VIN at `RecordedDate`.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) that qualifies for the campaign. |
| CampaignID <div><strong>``long?``</strong></div> | The ID of the [campaign](/generated/Models/Vehicle/ServiceCampaignModel.html) this entry qualifies the VIN for. Matched against `ServiceItemModel.CampaignID` by the evaluator. |
| CampaignUniqueReference <div><strong>``string``</strong></div> | The unique reference of the parent campaign (mirror of `ServiceItemModel.CampaignUniqueReference`), useful for human-readable lookups and auditing. |
| PackageCode <div><strong>``string``</strong></div> | Optional menu/package code explicitly assigned to this VIN's activation. Overrides the catalog/model-derived display code. When service consumption is enabled, a nonblank code also restricts this activation's consumption to that package. A blank code preserves the catalog consumption rule. |
| SourceIdentifierWarning <div><strong>``string``</strong></div> | Optional source audit warning when an approved import preserves a noncanonical identifier. |
| RecordedDate <div><strong>``DateTimeOffset``</strong></div> | The date this entry was recorded. Used as the activation date for items with `Enums.ClaimableItemValidityMode.RelativeToActivation` validity. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this entry has been deleted (effectively revoking the VIN's eligibility). |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |

---
hide:
    - toc
---
Represents a claimable service item offered through a Service Campaign.
 Service items define what services are available to vehicles, including validity periods, costs (fixed or per-model), and campaign activation rules.

| Property | Summary |
|----------|---------|
| Name <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual name of the service item (keyed by language code). |
| Photo <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual photo URLs for the service item (keyed by language code). |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this service item has been deleted. |
| CampaignStartDate <div><strong>``DateTime``</strong></div> | The start date of the campaign that this service item belongs to. |
| CampaignEndDate <div><strong>``DateTime``</strong></div> | The end date of the campaign that this service item belongs to. |
| ValidFrom <div><strong>``DateTime?``</strong></div> | The date from which this service item becomes valid for claiming. |
| ValidTo <div><strong>``DateTime?``</strong></div> | The date after which this service item is no longer valid for claiming. |
| PrintoutTitle <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual printout title (keyed by language code). |
| PrintoutDescription <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual printout description (keyed by language code). |
| BrandIDs <div><strong>``IEnumerable<long?>``</strong></div> | The brand IDs this service item is available for. |
| CountryIDs <div><strong>``IEnumerable<long?>``</strong></div> | The country IDs this service item is available in. |
| CompanyIDs <div><strong>``IEnumerable<long?>``</strong></div> | The company IDs this service item is available for. |
| ActiveFor <div><strong>``int?``</strong></div> | The duration for which this service item remains active after activation. |
| ActiveForDurationType <div><strong>``DurationType?``</strong></div> | The unit of time for the  duration (e.g., Days, Months, Years). |
| MaximumMileage <div><strong>``long?``</strong></div> | The maximum mileage up to which this service item is valid. Used in sequential validity calculations. |
| PackageCode <div><strong>``string``</strong></div> | The package code that groups related service items together. |
| UniqueReference <div><strong>``string``</strong></div> | A unique reference identifier for this service item. |
| FixedCost <div><strong>``decimal?``</strong></div> | The fixed cost for this service item. Will be null if costing type is 'Per Model'. |
| ModelCosts <div><strong>``IEnumerable<ServiceItemCostModel>``</strong></div> | Per-model [costs](/generated/Models/Vehicle/ServiceItemCostModel.html) for this service item. Will be null if costing type is 'Fixed'. |
| CampaignID <div><strong>``long?``</strong></div> | The ID of the [campaign](/generated/Models/Vehicle/ServiceCampaignModel.html) this service item belongs to. |
| CampaignName <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual name of the parent campaign (keyed by language code). |
| CampaignUniqueReference <div><strong>``string``</strong></div> | The unique reference of the parent campaign. |
| CampaignActivationTrigger <div><strong>``ClaimableItemCampaignActivationTrigger``</strong></div> | What triggers the campaign to activate (e.g., on vehicle sale or manually). |
| CampaignActivationType <div><strong>``ClaimableItemCampaignActivationTypes``</strong></div> | The activation type for the campaign (e.g., Automatic, Manual). |
| ValidityMode <div><strong>``ClaimableItemValidityMode``</strong></div> | How the validity period is calculated (e.g., relative to activation or fixed dates). |
| ClaimingMethod <div><strong>``ClaimableItemClaimingMethod``</strong></div> | The method used to claim this service item. |
| AttachmentFieldBehavior <div><strong>``ClaimableItemAttachmentFieldBehavior``</strong></div> | How attachment fields behave when claiming this service item. |
| VehicleInspectionTypeID <div><strong>``long?``</strong></div> | The vehicle inspection type required for claiming this service item, if any. |
| IntegrationID <div><strong>``string``</strong></div> | An external identifier used for system-to-system integration. |

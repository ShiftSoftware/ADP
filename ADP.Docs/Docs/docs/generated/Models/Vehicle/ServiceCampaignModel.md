---
hide:
    - toc
---
Represents a service campaign that offers  to vehicles.
 Campaigns define the time period, target brands/countries/companies, and activation rules for their service items.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this campaign. |
| Name <div><strong>``Dictionary<string, string>``</strong></div> | The multilingual name of the campaign (keyed by language code). |
| UniqueReference <div><strong>``string``</strong></div> | A unique reference identifier for this campaign. |
| StartDate <div><strong>``DateTime``</strong></div> | The date from which this campaign is active. |
| ExpireDate <div><strong>``DateTime``</strong></div> | The date after which this campaign expires. |
| BrandIDs <div><strong>``IEnumerable<long?>``</strong></div> | The brand IDs this campaign targets. |
| CountryIDs <div><strong>``IEnumerable<long?>``</strong></div> | The country IDs this campaign is available in. |
| CompanyIDs <div><strong>``IEnumerable<long?>``</strong></div> | The company IDs this campaign is available for. |
| ActivationTrigger <div><strong>``ClaimableItemCampaignActivationTrigger``</strong></div> | What triggers this campaign to activate (e.g., on vehicle sale or manually). |
| ActivationType <div><strong>``ClaimableItemCampaignActivationTypes``</strong></div> | The activation type for this campaign (e.g., Automatic, Manual). |
| VehicleInspectionTypeID <div><strong>``long?``</strong></div> | The vehicle inspection type required for this campaign, if any. |

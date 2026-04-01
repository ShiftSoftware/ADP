---
hide:
    - toc
---
Represents a Special Service Campaign (SSC) / Safety Recall record for a specific vehicle.
 Each record links a VIN to a recall campaign, including the labor operations and parts required for the repair.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) affected by this recall campaign. |
| CampaignCode <div><strong>``string``</strong></div> | The unique code identifying the recall/SSC campaign. |
| Description <div><strong>``string``</strong></div> | A description of the recall campaign and the issue being addressed. |
| LaborCode1 <div><strong>``string``</strong></div> | The first labor operation code required for the recall repair. |
| LaborHour1 <div><strong>``double?``</strong></div> | The estimated hours for the first labor operation. |
| LaborCode2 <div><strong>``string``</strong></div> | The second labor operation code, if applicable. |
| LaborHour2 <div><strong>``double?``</strong></div> | The estimated hours for the second labor operation. |
| LaborCode3 <div><strong>``string``</strong></div> | The third labor operation code, if applicable. |
| LaborHour3 <div><strong>``double?``</strong></div> | The estimated hours for the third labor operation. |
| PartNumber1 <div><strong>``string``</strong></div> | The first part number required for the recall repair. |
| PartNumber2 <div><strong>``string``</strong></div> | The second part number required for the recall repair, if applicable. |
| PartNumber3 <div><strong>``string``</strong></div> | The third part number required for the recall repair, if applicable. |
| RepairDate <div><strong>``DateTime?``</strong></div> | The date the recall repair was completed. Null if the recall has not been repaired yet. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |

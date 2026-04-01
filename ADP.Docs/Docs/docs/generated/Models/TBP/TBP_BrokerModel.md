---
hide:
    - toc
---
Represents a broker entity in the Third-Party Broker (TBP) system.
 Contains the broker's identity, region, account numbers, and brand access permissions.

| Property | Summary |
|----------|---------|
| ID <div><strong>``long``</strong></div> | The unique identifier for this broker. |
| Name <div><strong>``string``</strong></div> | The broker's name. |
| IsDeleted <div><strong>``bool``</strong></div> | Indicates whether this broker has been deleted. |
| AccountNumbers <div><strong>``IEnumerable<TBP_BrokerAccountNumberModel>``</strong></div> | The [account numbers](/generated/Models/TBP/TBP_BrokerAccountNumberModel.html) associated with this broker across different companies. |
| RegionHashID <div><strong>``string``</strong></div> | The Region Hash ID from the Identity System. |
| BrandAccesses <div><strong>``IEnumerable<TBP_BrokerBrokerBrandAccessModel>``</strong></div> | The [brand access](/generated/Models/TBP/TBP_BrokerBrokerBrandAccessModel.html) permissions for this broker. |

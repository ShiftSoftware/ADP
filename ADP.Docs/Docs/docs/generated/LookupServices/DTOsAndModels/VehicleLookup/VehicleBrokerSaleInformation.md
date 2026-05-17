---
hide:
    - toc
---
Contains broker sale information when a vehicle was sold through a third-party broker (TBP).

| Property | Summary |
|----------|---------|
| BrokerID <div><strong>``long``</strong></div> | The ID of the broker that sold the vehicle. |
| BrokerName <div><strong>``string``</strong></div> | The name of the broker. |
| NonOfficialBrokerName <div><strong>``string``</strong></div> | The name of the Non Official Broker in case the vehicle was sold through a non-official broker. |
| InvoiceNumber <div><strong>``long?``</strong></div> | The broker's invoice number for the sale. |
| InvoiceDate <div><strong>``DateTime?``</strong></div> | The date of the broker invoice. |
| CityID <div><strong>``string``</strong></div> | The City Hash ID from the Identity System. |
| CityName <div><strong>``string``</strong></div> | The resolved City Name. |

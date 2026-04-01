---
hide:
    - toc
---
Represents a broker's stock record for a specific vehicle (VIN) under a specific brand.
 Tracks the vehicle's quantity at the broker, service dates, transfers, invoices, and related vehicle entries.

| Property | Summary |
|----------|---------|
| BrokerID <div><strong>``long``</strong></div> | The broker ID (second partition key). |
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (third partition key). |
| Quantity <div><strong>``int``</strong></div> | The calculated stock quantity for this vehicle at the broker. Derived from vehicle entries, invoices, and transfers. |
| OneKOrFiveKServieDate <div><strong>``DateTime?``</strong></div> | The date of the 1K or 5K service performed on this vehicle. |
| FirstServieDate <div><strong>``DateTime?``</strong></div> | The date of the first service performed on this vehicle. |
| Broker <div><strong>``TBP_BrokerModel?``</strong></div> | The [broker](/generated/Models/TBP/TBP_BrokerModel.html) that holds this stock. |
| Vehicle <div><strong>``BrokerVehicleModel?``</strong></div> | The [vehicle](/generated/Models/TBP/BrokerVehicleModel.html) details for this stock record. |
| Transfers <div><strong>``IEnumerable<TBP_VehicleTransferModel>``</strong></div> | The [transfers](/generated/Models/TBP/TBP_VehicleTransferModel.html) involving this vehicle at this broker. |
| Invoices <div><strong>``IEnumerable<TBP_Invoice>``</strong></div> | The [invoices](/generated/Models/TBP/TBP_Invoice.html) for this vehicle at this broker. |
| VehilceEntries <div><strong>``IEnumerable<TBP_VehicleEntryModel>``</strong></div> | The [vehicle entries](/generated/Models/TBP/TBP_VehicleEntryModel.html) from the dealer system for this vehicle. |
| IsAtStock <div><strong>``bool``</strong></div> |  |

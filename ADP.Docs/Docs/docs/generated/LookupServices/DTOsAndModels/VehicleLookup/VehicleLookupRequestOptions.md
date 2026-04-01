---
hide:
    - toc
---
Options passed to the vehicle lookup service to control lookup behavior, language, logging, and consistency level.

| Property | Summary |
|----------|---------|
| LanguageCode <div><strong>``string``</strong></div> | The language code for localized content (defaults to "en"). |
| IgnoreBrokerStock <div><strong>``bool``</strong></div> | Whether to skip broker stock lookup for this request. |
| InsertSSCLog <div><strong>``bool``</strong></div> | Whether to insert an SSC lookup audit log entry. |
| SSCLogInfo <div><strong>``SSCLogInfo``</strong></div> | The SSC log info to record if InsertSSCLog is true. |
| InsertCustomerVehcileLookupLog <div><strong>``bool``</strong></div> | Whether to insert a customer vehicle lookup audit log entry. |
| CustomerVehicleLookupLogInfo <div><strong>``CustomerVehicleLookupLogInfo``</strong></div> | The customer vehicle lookup log info to record. |
| VehicleServiceHistoryConsistencyLevel <div><strong>``ConsistencyLevels``</strong></div> | The consistency level for service history queries (defaults to Strong). |
| LookupEndCustomer <div><strong>``bool``</strong></div> | Whether to look up end customer information for the vehicle. |
| LegacyPaintThickness <div><strong>``bool``</strong></div> | Whether to include the legacy paint thickness format in the response. |

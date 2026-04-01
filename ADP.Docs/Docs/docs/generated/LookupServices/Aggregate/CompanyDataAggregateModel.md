---
hide:
    - toc
---
The aggregate root model containing all company-related data for a vehicle (identified by VIN) from Cosmos DB.
 This is the primary data structure passed to evaluators for processing vehicle lookups.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number this aggregate belongs to. |
| VehicleEntries <div><strong>``List<VehicleEntryModel>``</strong></div> | All vehicle entries (sale records) for this VIN across companies. |
| VehicleServiceActivations <div><strong>``List<VehicleServiceActivation>``</strong></div> | Service activation records for this VIN. |
| VehicleInspections <div><strong>``List<VehicleInspectionModel>``</strong></div> | Vehicle inspection records for this VIN. |
| InitialOfficialVINs <div><strong>``List<InitialOfficialVINModel>``</strong></div> | Initial official VIN registration records. |
| LaborLines <div><strong>``List<OrderLaborLineModel>``</strong></div> | Service history labor lines for this VIN. |
| PartLines <div><strong>``List<OrderPartLineModel>``</strong></div> | Service history part lines for this VIN. |
| SSCAffectedVINs <div><strong>``List<SSCAffectedVINModel>``</strong></div> | Special Service Campaign (SSC) / recall records for this VIN. |
| WarrantyClaims <div><strong>``List<WarrantyClaimModel>``</strong></div> | Warranty claim records for this VIN. |
| BrokerInitialVehicles <div><strong>``List<BrokerInitialVehicleModel>``</strong></div> | Initial vehicle records from broker inventory for this VIN. |
| BrokerInvoices <div><strong>``List<BrokerInvoiceModel>``</strong></div> | Broker invoice records for this VIN. |
| PaidServiceInvoices <div><strong>``List<PaidServiceInvoiceModel>``</strong></div> | Paid service invoice records for this VIN. |
| ItemClaims <div><strong>``List<ItemClaimModel>``</strong></div> | Service item claims made for this VIN. |
| FreeServiceItemExcludedVINs <div><strong>``List<FreeServiceItemExcludedVINModel>``</strong></div> | VINs excluded from free service item campaigns. |
| FreeServiceItemDateShifts <div><strong>``List<FreeServiceItemDateShiftModel>``</strong></div> | Free service item date shift overrides for this VIN. |
| WarrantyDateShifts <div><strong>``List<WarrantyDateShiftModel>``</strong></div> | Warranty date shift overrides for this VIN. |
| PaintThicknessInspections <div><strong>``IEnumerable<PaintThicknessInspectionModel>``</strong></div> | Paint thickness inspection records for this VIN. |
| Accessories <div><strong>``List<VehicleAccessoryModel>``</strong></div> | Accessories installed on this vehicle. |
| ExtendedWarrantyEntries <div><strong>``List<ExtendedWarrantyModel>``</strong></div> | Extended warranty records for this VIN. |

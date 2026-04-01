---
hide:
    - toc
---
Represents vehicle details as stored in the broker stock context.
 Contains the vehicle specifications and color information relevant to broker inventory.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN). |
| Model <div><strong>``string``</strong></div> | The vehicle model description. |
| ModelYear <div><strong>``string``</strong></div> | The model year of the vehicle. |
| Engine <div><strong>``string``</strong></div> | The engine description. |
| VariantDescription <div><strong>``string``</strong></div> | A description of the vehicle variant. |
| VariantCode <div><strong>``string``</strong></div> | The variant code within the model range. |
| Katashiki <div><strong>``string``</strong></div> | The Katashiki (manufacturer-specific model identifier). |
| Cylinders <div><strong>``string``</strong></div> | The number of cylinders in the engine. |
| StockRegionID <div><strong>``long?``</strong></div> | The region ID where this vehicle is stocked. |
| ExteriorColor <div><strong>``VehicleColorModel``</strong></div> | The [exterior color](/generated/Models/TBP/VehicleColorModel.html) of the vehicle. |
| InteriorColor <div><strong>``VehicleColorModel``</strong></div> | The [interior color](/generated/Models/TBP/VehicleColorModel.html) of the vehicle. |

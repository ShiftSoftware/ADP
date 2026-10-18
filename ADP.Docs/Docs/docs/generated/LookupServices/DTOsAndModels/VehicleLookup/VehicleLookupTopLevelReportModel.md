---
hide:
    - toc
---
One row per vehicle in the top-level vehicle report: the lookup's headline fields, flattened into
 columns. Text columns hold an empty string when the lookup has no value.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> |  |
| IsAuthorized <div><strong>``bool``</strong></div> |  |
| NextServiceDate <div><strong>``DateTime?``</strong></div> |  |
| SSCLogId <div><strong>``Guid?``</strong></div> |  |
| BasicModelCode <div><strong>``string``</strong></div> |  |
| IdentifiersVin <div><strong>``string``</strong></div> |  |
| IdentifiersVariant <div><strong>``string``</strong></div> |  |
| IdentifiersKatashiki <div><strong>``string``</strong></div> |  |
| IdentifiersColor <div><strong>``string``</strong></div> |  |
| IdentifiersTrim <div><strong>``string``</strong></div> |  |
| IdentifiersBrandId <div><strong>``string``</strong></div> |  |
| SaleCountryId <div><strong>``string``</strong></div> |  |
| SaleCountryName <div><strong>``string``</strong></div> |  |
| SaleCompanyId <div><strong>``string``</strong></div> |  |
| SaleCompanyName <div><strong>``string``</strong></div> |  |
| SaleBranchId <div><strong>``string``</strong></div> |  |
| SaleBranchName <div><strong>``string``</strong></div> |  |
| SaleCustomerAccountNumber <div><strong>``string``</strong></div> |  |
| SaleCustomerId <div><strong>``string``</strong></div> |  |
| SaleInvoiceDate <div><strong>``DateTime?``</strong></div> |  |
| SaleWarrantyActivationDate <div><strong>``DateTime?``</strong></div> |  |
| SaleInvoiceNumber <div><strong>``string``</strong></div> |  |
| SaleRegionId <div><strong>``string``</strong></div> |  |
| SaleBrokerId <div><strong>``long?``</strong></div> |  |
| SaleBrokerName <div><strong>``string``</strong></div> |  |
| SaleBrokerInvoiceNumber <div><strong>``long?``</strong></div> |  |
| SaleBrokerInvoiceDate <div><strong>``DateTime?``</strong></div> |  |
| SaleEndCustomerId <div><strong>``string``</strong></div> |  |
| SaleEndCustomerName <div><strong>``string``</strong></div> |  |
| SaleEndCustomerPhone <div><strong>``string``</strong></div> |  |
| SaleEndCustomerIdNumber <div><strong>``string``</strong></div> |  |
| WarrantyHasActiveWarranty <div><strong>``bool``</strong></div> |  |
| WarrantyStartDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyEndDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyActivationIsRequired <div><strong>``bool``</strong></div> |  |
| WarrantyHasExtendedWarranty <div><strong>``bool``</strong></div> |  |
| WarrantyExtendedStartDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyExtendedEndDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyDistributorExtendedStartDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyDistributorExtendedEndDate <div><strong>``DateTime?``</strong></div> |  |
| WarrantyFreeServiceStartDate <div><strong>``DateTime?``</strong></div> |  |
| VariantInfoModelCode <div><strong>``string``</strong></div> |  |
| VariantInfoSfx <div><strong>``string``</strong></div> |  |
| VariantInfoModelYear <div><strong>``int?``</strong></div> |  |
| VehicleSpecModelCode <div><strong>``string``</strong></div> |  |
| VehicleSpecModelYear <div><strong>``int?``</strong></div> |  |
| VehicleSpecProductionDate <div><strong>``DateTime?``</strong></div> |  |
| VehicleSpecModelDescription <div><strong>``string``</strong></div> |  |
| VehicleSpecVariantDescription <div><strong>``string``</strong></div> |  |
| VehicleSpecClass <div><strong>``string``</strong></div> |  |
| VehicleSpecBodyType <div><strong>``string``</strong></div> |  |
| VehicleSpecEngine <div><strong>``string``</strong></div> |  |
| VehicleSpecCylinders <div><strong>``string``</strong></div> |  |
| VehicleSpecLightHeavyType <div><strong>``string``</strong></div> |  |
| VehicleSpecDoors <div><strong>``string``</strong></div> |  |
| VehicleSpecFuel <div><strong>``string``</strong></div> |  |
| VehicleSpecTransmission <div><strong>``string``</strong></div> |  |
| VehicleSpecSide <div><strong>``string``</strong></div> |  |
| VehicleSpecEngineType <div><strong>``string``</strong></div> |  |
| VehicleSpecTankCap <div><strong>``string``</strong></div> |  |
| VehicleSpecStyle <div><strong>``string``</strong></div> |  |
| VehicleSpecFuelLiter <div><strong>``int?``</strong></div> |  |
| VehicleSpecExteriorColor <div><strong>``string``</strong></div> |  |
| VehicleSpecInteriorColor <div><strong>``string``</strong></div> |  |
| ClassificationCodes <div><strong>``string``</strong></div> | The classification codes the lookup resolved, in its order, separated by a semicolon and a space. These are the stable values to filter on. Empty when the host resolves none or configures no classifier. |
| ClassificationLabels <div><strong>``string``</strong></div> | The display labels of `ClassificationCodes`, in the same order and with the same separator. A classification without a label shows its code. Display text only; filter on the codes. |

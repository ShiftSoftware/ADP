import type { VehicleSaleInformation } from './vehicle-sale-information';
import type { VehicleServiceItemDTO } from './vehicle-service-item-dto';
import type { VehicleIdentifiersDTO } from './vehicle-identifiers-dto';
import type { VehicleVariantInfoDTO } from './vehicle-variant-info-dto';
import type { VehicleSpecificationDTO } from './vehicle-specification-dto';
export type ItemClaimDTO = {
    vin?: string;
    invoice?: string;
    jobNumber?: string;
    qrCode?: string;
    saleInformation?: VehicleSaleInformation;
    serviceItem?: VehicleServiceItemDTO;
    identifiers?: VehicleIdentifiersDTO;
    vehicleVariantInfo?: VehicleVariantInfoDTO;
    vehicleSpecification?: VehicleSpecificationDTO;
};
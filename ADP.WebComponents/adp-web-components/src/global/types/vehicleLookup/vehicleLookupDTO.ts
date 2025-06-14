import type { vehicleIdentifiersDTO } from './vehicleIdentifiersDTO';
import type { vehicleSaleInformation } from './vehicleSaleInformation';
import type { paintThicknessDTO } from './paintThicknessDTO';
import type { vehicleWarrantyDTO } from './vehicleWarrantyDTO';
import type { vehicleServiceHistoryDTO } from './vehicleServiceHistoryDTO';
import type { sscDTO } from './sscDTO';
import type { vehicleVariantInfoDTO } from './vehicleVariantInfoDTO';
import type { vehicleSpecificationDTO } from './vehicleSpecificationDTO';
import type { vehicleServiceItemDTO } from './vehicleServiceItemDTO';
import type { accessoryDTO } from './accessoryDTO';
export type vehicleLookupDTO = {
    vin: string;
    identifiers: vehicleIdentifiersDTO;
    saleInformation: vehicleSaleInformation;
    paintThickness: paintThicknessDTO;
    isAuthorized: boolean;
    warranty: vehicleWarrantyDTO;
    nextServiceDate?: string;
    serviceHistory: vehicleServiceHistoryDTO[];
    sscLogId?: string;
    ssc: sscDTO[];
    vehicleVariantInfo: vehicleVariantInfoDTO;
    vehicleSpecification: vehicleSpecificationDTO;
    serviceItems: vehicleServiceItemDTO[];
    accessories: accessoryDTO[];
    basicModelCode: string;
};
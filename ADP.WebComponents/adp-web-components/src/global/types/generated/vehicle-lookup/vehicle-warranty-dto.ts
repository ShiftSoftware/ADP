import type { VehicleExtendedWarrantyDTO } from './vehicle-extended-warranty-dto';
export type VehicleWarrantyDTO = {
    hasActiveWarranty: boolean;
    warrantyStartDate?: string;
    warrantyEndDate?: string;
    activationIsRequired: boolean;
    activationStatus: 'NotRequired' | 'Required' | 'BlockedNotAllocated';
    hasExtendedWarranty: boolean;
    extendedWarrantyStartDate?: string;
    extendedWarrantyEndDate?: string;
    extendedWarranties: VehicleExtendedWarrantyDTO[];
    freeServiceStartDate?: string;
    deFactoServiceStartDate?: string;
};
export type VehicleWarrantyDTO = {
    hasActiveWarranty: boolean;
    warrantyStartDate?: string;
    warrantyEndDate?: string;
    activationIsRequired: boolean;
    activationStatus: 'NotRequired' | 'Required' | 'BlockedNotAllocated';
    hasExtendedWarranty: boolean;
    extendedWarrantyStartDate?: string;
    extendedWarrantyEndDate?: string;
    freeServiceStartDate?: string;
    deFactoServiceStartDate?: string;
};
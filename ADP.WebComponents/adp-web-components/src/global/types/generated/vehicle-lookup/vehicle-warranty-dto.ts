export type VehicleWarrantyDTO = {
    hasActiveWarranty: boolean;
    warrantyStartDate?: string;
    warrantyEndDate?: string;
    activationIsRequired: boolean;
    hasExtendedWarranty: boolean;
    extendedWarrantyStartDate?: string;
    extendedWarrantyEndDate?: string;
    freeServiceStartDate?: string;
};
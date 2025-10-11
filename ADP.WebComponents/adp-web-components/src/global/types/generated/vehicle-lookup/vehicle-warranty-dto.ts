export type VehicleWarrantyDTO = {
    hasActiveWarranty: boolean;
    warrantyStartDate?: string;
    warrantyEndDate?: string;
    hasExtendedWarranty: boolean;
    extendedWarrantyStartDate?: string;
    extendedWarrantyEndDate?: string;
    freeServiceStartDate?: string;
};
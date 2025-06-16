export type VehicleIdentifiersDTO = {
    vin: string;
    variant: string;
    katashiki: string;
    color: string;
    trim: string;
    brand?: 'Toyota' | 'Lexus' | 'Hino' | 'Other' | 'NA';
    brandID: string;
};
export type vehicleIdentifiersDTO = {
    vin: string;
    variant: string;
    katashiki: string;
    color: string;
    trim: string;
    brand?: 'toyota' | 'lexus' | 'hino' | 'other' | 'na';
    brandID: string;
};
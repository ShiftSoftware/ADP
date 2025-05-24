import type { priceDTO } from './priceDTO';
export type partPriceDTO = {
    countryID: string;
    countryName: string;
    regionID: string;
    regionName: string;
    retailPrice: priceDTO;
    purchasePrice: priceDTO;
    warrantyPrice: priceDTO;
};
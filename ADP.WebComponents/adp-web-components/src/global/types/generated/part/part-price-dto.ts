import type { PriceDTO } from './price-dto';
export type PartPriceDTO = {
    countryID: string;
    countryName: string;
    regionID: string;
    regionName: string;
    retailPrice: PriceDTO;
    purchasePrice: PriceDTO;
    warrantyPrice: PriceDTO;
};
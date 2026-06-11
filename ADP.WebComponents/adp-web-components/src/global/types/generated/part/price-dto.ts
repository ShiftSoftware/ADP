import type { PartUnitPriceDTO } from './part-unit-price-dto';
export type PriceDTO = {
    value?: number;
    currencySymbol: string;
    cultureName: string;
    unitPrices: PartUnitPriceDTO[];
    formattedValue: string;
};
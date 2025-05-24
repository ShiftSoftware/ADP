import type { cultureInfo } from './cultureInfo';
export type priceDTO = {
    value?: number;
    currecntySymbol: string;
    cultureName: string;
    culture: cultureInfo;
    formattedValue: string;
};
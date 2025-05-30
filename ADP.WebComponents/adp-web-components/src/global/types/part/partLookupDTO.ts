import type { stockPartDTO } from './stockPartDTO';
import type { partPriceDTO } from './partPriceDTO';
import type { hsCodeDTO } from './hsCodeDTO';
import type { deadStockDTO } from './deadStockDTO';
export type partLookupDTO = {
    partNumber: string;
    partDescription: string;
    localDescription: string;
    productGroup: string;
    pnc: string;
    binType: string;
    distributorPurchasePrice?: number;
    dimension1?: number;
    dimension2?: number;
    dimension3?: number;
    netWeight?: number;
    grossWeight?: number;
    cubicMeasure?: number;
    hsCode: string;
    origin: string;
    supersededTo: string[];
    stockParts: stockPartDTO[];
    prices: partPriceDTO[];
    hsCodes: hsCodeDTO[];
    deadStock: deadStockDTO[];
    logId?: string;
};
import type { ManufacturerDataDTO } from './manufacturer-data-dto';
export type ManufacturerPartLookupDTO = {
    isSuccess: boolean;
    message?: string;
    manufacturerResult?: ManufacturerDataDTO[];
};
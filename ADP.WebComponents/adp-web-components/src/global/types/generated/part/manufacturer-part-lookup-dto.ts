import type { ManufacturerResultDTO } from './manufacturer-result-dto';
export type ManufacturerPartLookupDTO = {
    isSuccess: boolean;
    message?: string;
    manufacturerResult?: ManufacturerResultDTO[];
};

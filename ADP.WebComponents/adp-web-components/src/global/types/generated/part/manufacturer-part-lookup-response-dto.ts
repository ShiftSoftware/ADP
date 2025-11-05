import type { KeyValuePairDTO } from './key-value-pair-dto';
export type ManufacturerPartLookupResponseDTO = {
    id: string;
    partNumber: string;
    orderType: 'Sea' | 'Airplane';
    status: 'Pendding' | 'Resolved' | 'UnResolved';
    manufacturerResult?: KeyValuePairDTO[];
    message?: string;
};
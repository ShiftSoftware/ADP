export type ManufacturerPartLookupRequestDTO = {
    partNumber: string;
    quantity: number;
    orderType: 'Sea' | 'Airplane';
    logId?: string;
};
import type { VehicleServiceMenuLineDTO } from './vehicle-service-menu-line-dto';
export type VehicleServiceMenuDTO = {
    status: 'NoBasicModelCode' | 'NotFound' | 'Found' | 'Unavailable' | 'NotRegistered';
    basicModelCode: string;
    countryID: number;
    language: string;
    transferRate: number;
    services: VehicleServiceMenuLineDTO[];
};
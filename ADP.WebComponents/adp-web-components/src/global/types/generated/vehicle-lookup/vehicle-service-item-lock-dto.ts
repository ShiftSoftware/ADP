import type { VehicleServiceItemPrerequisiteDTO } from './vehicle-service-item-prerequisite-dto';
export type VehicleServiceItemLockDTO = {
    state: 'Locked' | 'Missed';
    prerequisites: VehicleServiceItemPrerequisiteDTO[];
};
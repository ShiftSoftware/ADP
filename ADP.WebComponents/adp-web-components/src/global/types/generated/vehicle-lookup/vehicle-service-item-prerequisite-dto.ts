import type { VehicleServiceItemPrerequisiteEvidenceDTO } from './vehicle-service-item-prerequisite-evidence-dto';
export type VehicleServiceItemPrerequisiteDTO = {
    mileage: number;
    label: string;
    satisfied: boolean;
    satisfiedOn?: string;
    evidence?: VehicleServiceItemPrerequisiteEvidenceDTO;
};
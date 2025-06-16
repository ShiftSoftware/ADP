import type { VehicleLaborDTO } from './vehicle-labor-dto';
import type { VehiclePartDTO } from './vehicle-part-dto';
export type VehicleServiceHistoryDTO = {
    serviceType: string;
    serviceDate?: string;
    mileage?: number;
    companyName: string;
    branchName: string;
    companyID: string;
    branchID: string;
    accountNumber: string;
    invoiceNumber: string;
    jobNumber: string;
    laborLines: VehicleLaborDTO[];
    partLines: VehiclePartDTO[];
};
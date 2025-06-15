import type { vehicleLaborDTO } from './vehicleLaborDTO';
import type { vehiclePartDTO } from './vehiclePartDTO';
export type vehicleServiceHistoryDTO = {
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
    laborLines: vehicleLaborDTO[];
    partLines: vehiclePartDTO[];
};
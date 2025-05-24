import type { branchDeadStockDTO } from './branchDeadStockDTO';
export type deadStockDTO = {
    companyIntegrationID: string;
    companyName: string;
    branchDeadStock: branchDeadStockDTO[];
};
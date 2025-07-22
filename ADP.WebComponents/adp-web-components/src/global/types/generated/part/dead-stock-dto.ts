import type { BranchDeadStockDTO } from './branch-dead-stock-dto';
export type DeadStockDTO = {
    companyIntegrationID: string;
    companyName: string;
    branchDeadStock: BranchDeadStockDTO[];
};
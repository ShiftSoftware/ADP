import type { BranchDeadStockDTO } from './branch-dead-stock-dto';
export type DeadStockDTO = {
    companyHashID: string;
    companyName: string;
    branchDeadStock: BranchDeadStockDTO[];
};
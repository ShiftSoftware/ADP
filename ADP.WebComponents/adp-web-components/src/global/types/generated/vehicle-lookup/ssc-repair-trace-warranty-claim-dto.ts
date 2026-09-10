import type { SscRepairTraceLaborCodeDTO } from './ssc-repair-trace-labor-code-dto';
export type SscRepairTraceWarrantyClaimDTO = {
    claimNumber: string;
    dealerClaimNumber: string;
    claimStatus: 'Draft' | 'PendingProcess' | 'Accepted' | 'RejectedWithError' | 'RejectedPermanently' | 'Certified' | 'Invoiced';
    repairCompletionDate?: string;
    statusQualifies: boolean;
    campaignCodeInComment: boolean;
    distributorComment: string;
    matchedLaborCodes: SscRepairTraceLaborCodeDTO[];
    matches: boolean;
    selected: boolean;
};
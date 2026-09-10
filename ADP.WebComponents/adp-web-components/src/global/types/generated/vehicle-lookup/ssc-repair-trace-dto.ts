import type { SscRepairTraceLaborCodeDTO } from './ssc-repair-trace-labor-code-dto';
import type { SscRepairTraceWarrantyClaimDTO } from './ssc-repair-trace-warranty-claim-dto';
import type { SscRepairTraceLaborLineDTO } from './ssc-repair-trace-labor-line-dto';
export type SscRepairTraceDTO = {
    campaignLaborCodes: string[];
    interchangeableLaborCodes: SscRepairTraceLaborCodeDTO[];
    recordRepairDate?: string;
    warrantyClaims: SscRepairTraceWarrantyClaimDTO[];
    serviceHistoryLaborLinesExamined: number;
    serviceHistoryLaborLines: SscRepairTraceLaborLineDTO[];
    repairSource: 'None' | 'SSCRecord' | 'WarrantyClaim' | 'ServiceHistory';
};
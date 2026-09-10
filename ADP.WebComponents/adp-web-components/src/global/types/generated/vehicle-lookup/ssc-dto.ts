import type { SSCLaborDTO } from './ssc-labor-dto';
import type { SSCPartDTO } from './ssc-part-dto';
import type { SscRepairTraceDTO } from './ssc-repair-trace-dto';
export type SscDTO = {
    sscCode: string;
    description: string;
    labors: SSCLaborDTO[];
    repaired: boolean;
    repairDate?: string;
    repairSource: 'None' | 'SSCRecord' | 'WarrantyClaim' | 'ServiceHistory';
    parts: SSCPartDTO[];
    trace?: SscRepairTraceDTO;
};
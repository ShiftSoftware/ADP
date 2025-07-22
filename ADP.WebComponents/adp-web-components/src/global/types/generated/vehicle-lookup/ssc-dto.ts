import type { SSCLaborDTO } from './ssc-labor-dto';
import type { SSCPartDTO } from './ssc-part-dto';
export type SscDTO = {
    sscCode: string;
    description: string;
    labors: SSCLaborDTO[];
    repaired: boolean;
    repairDate?: string;
    parts: SSCPartDTO[];
};
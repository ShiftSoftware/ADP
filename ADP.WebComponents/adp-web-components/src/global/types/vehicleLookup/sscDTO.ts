import type { sscLaborDTO } from './sscLaborDTO';
import type { sscPartDTO } from './sscPartDTO';
export type sscDTO = {
    sscCode: string;
    description: string;
    labors: sscLaborDTO[];
    repaired: boolean;
    repairDate?: string;
    parts: sscPartDTO[];
};
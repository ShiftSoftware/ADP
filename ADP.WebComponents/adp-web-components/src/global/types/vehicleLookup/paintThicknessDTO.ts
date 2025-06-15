import type { paintThicknessPartDTO } from './paintThicknessPartDTO';
import type { paintThicknessImageDTO } from './paintThicknessImageDTO';
export type paintThicknessDTO = {
    parts: paintThicknessPartDTO[];
    imageGroups: paintThicknessImageDTO[];
};
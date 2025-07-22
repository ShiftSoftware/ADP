import type { PaintThicknessPartDTO } from './paint-thickness-part-dto';
import type { PaintThicknessImageDTO } from './paint-thickness-image-dto';
export type PaintThicknessDTO = {
    parts: PaintThicknessPartDTO[];
    imageGroups: PaintThicknessImageDTO[];
};
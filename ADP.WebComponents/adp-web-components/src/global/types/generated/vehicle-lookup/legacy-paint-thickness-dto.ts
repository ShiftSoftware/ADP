import type { LegacyPaintThicknessPartDTO } from './legacy-paint-thickness-part-dto';
import type { LegacyPaintThicknessImageGroupDTO } from './legacy-paint-thickness-image-group-dto';
export type LegacyPaintThicknessDTO = {
    parts: LegacyPaintThicknessPartDTO[];
    imageGroups: LegacyPaintThicknessImageGroupDTO[];
};
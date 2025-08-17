import type { PaintThicknessInspectionPanelDTO } from './paint-thickness-inspection-panel-dto';
export type PaintThicknessInspectionDTO = {
    source: string;
    inspectionDate?: string;
    panels: PaintThicknessInspectionPanelDTO[];
};
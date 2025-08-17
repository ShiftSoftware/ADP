export type PaintThicknessInspectionPanelDTO = {
    panelType: 'Door' | 'Fender' | 'Roof' | 'Hood' | 'TailGate';
    measuredThickness: number;
    panelSide?: 'Left' | 'Center' | 'Right';
    panelPosition?: 'Front' | 'Middle' | 'Rear';
    images: string[];
};
export type PaintThicknessCertificateReadingModel = {
    panelType: 'Door' | 'Fender' | 'Roof' | 'Hood' | 'TailGate';
    panelSide?: 'Left' | 'Center' | 'Right';
    panelPosition?: 'Front' | 'Middle' | 'Rear';
    measuredThickness: number;
    panelLabel: string;
    images: string[];
};
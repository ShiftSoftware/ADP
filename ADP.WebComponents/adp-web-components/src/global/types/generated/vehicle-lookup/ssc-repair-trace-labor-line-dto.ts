export type SscRepairTraceLaborLineDTO = {
    invoiceNumber: string;
    invoiceDate?: string;
    invoiceStatus: string;
    orderStatus: string;
    laborCode: string;
    campaignLaborCode: string;
    statusQualifies: boolean;
    selected: boolean;
};
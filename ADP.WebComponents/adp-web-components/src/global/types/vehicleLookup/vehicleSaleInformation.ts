import type { vehicleBrokerSaleInformation } from './vehicleBrokerSaleInformation';
export type vehicleSaleInformation = {
    countryID: string;
    countryName: string;
    companyID: string;
    companyName: string;
    branchID: string;
    branchName: string;
    customerAccountNumber: string;
    customerID: string;
    invoiceDate?: string;
    warrantyActivationDate?: string;
    invoiceNumber: string;
    broker: vehicleBrokerSaleInformation;
    regionID: string;
};
import type { VehicleBrokerSaleInformation } from './vehicle-broker-sale-information';
import type { VehicleSaleEndCustomerInformationDTO } from './vehicle-sale-end-customer-information-dto';
export type VehicleSaleInformation = {
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
    broker: VehicleBrokerSaleInformation;
    regionID: string;
    endCustomer: VehicleSaleEndCustomerInformationDTO;
};
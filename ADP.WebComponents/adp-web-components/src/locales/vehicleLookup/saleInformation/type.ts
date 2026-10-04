import { object } from 'yup';
import yupTypeMapper from '~lib/yup-type-mapper';

const saleInformationSchema = object({}).concat(
  yupTypeMapper([
    'vehicleSaleInformation',
    'companyName',
    'branchName',
    'sale',
    'location',
    'invoiceNumber',
    'invoiceDate',
    'warrantyActivationDate',
    'customerAccountNumber',
    'customerID',
    'endCustomer',
    'endCustomerName',
    'endCustomerPhone',
    'endCustomerIdNumber',
    'endCustomerId',
    'supplyChain',
    'distributorName',
    'intermediaryName',
    'brokerName',
    'nonOfficialBrokerName',
    'noEndCustomer',
    'inBrokerStock',
    'unauthorizedNotice',
  ]),
);

export default saleInformationSchema;

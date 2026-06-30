import yupTypeMapper from '~lib/yup-type-mapper';

const saleInformationSchema = yupTypeMapper([
  'vehicleSaleInformation',
  'noData',
  'countryName',
  'companyName',
  'branchName',
  'customerAccountNumber',
  'invoiceNumber',
  'invoiceDate',
  'warrantyActivationDate',
  'brokerName',
  'brokerInvoiceNumber',
  'brokerInvoiceDate',
  'distributorName',
  'distributorInvoiceNumber',
  'distributorInvoiceDate',
  'intermediaryName',
  'intermediaryInvoiceNumber',
  'intermediaryInvoiceDate',
  'customerInformation',
  'endCustomerName',
  'endCustomerPhone',
  'endCustomerIdNumber',
  'customerID',
  'Vehicle has no end customer.',
]);

export default saleInformationSchema;

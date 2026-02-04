import yupTypeMapper from '~lib/yup-type-mapper';

const ServiceHistorySchema = yupTypeMapper([
  'serviceHistory',
  'noData',
  'branch',
  'dealer',
  'invoiceNumber',
  'date',
  'serviceType',
  'odometer',
  'laborLines',
  'partLines',
  'laborCode',
  'packageCode',
  'serviceCode',
  'serviceDescription',
  'partNumber',
  'qty',
  'partDescription',
]);

export default ServiceHistorySchema;

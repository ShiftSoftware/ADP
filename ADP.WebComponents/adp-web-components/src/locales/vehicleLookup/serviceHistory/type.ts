import yupTypeMapper from '~lib/yup-type-mapper';

const ServiceHistorySchema = yupTypeMapper(['serviceHistory', 'noData', 'branch', 'dealer', 'invoiceNumber', 'date', 'serviceType', 'odometer']);

export default ServiceHistorySchema;

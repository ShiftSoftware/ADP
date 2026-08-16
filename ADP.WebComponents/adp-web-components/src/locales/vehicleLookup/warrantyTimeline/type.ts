import yupTypeMapper from '~lib/yup-type-mapper';

const warrantyTimelineSchema = yupTypeMapper([
  'dealer',
  'activatedBy',
  'broker',
  'authorized',
  'unauthorized',
  'activeWarranty',
  'notActiveWarranty',
  'warrantyCoverage',
  'totalPlannedProtection',
  'standardWarranty',
  'standardWarrantyMark',
  'extendedWarranty',
  'standard',
  'extended',
  'today',
  'year',
  'years',
  'month',
  'months',
  'warrantyNotStarted',
  'awaitingBrokerInvoice',
  'awaitingEndCustomerSale',
  'awaitingActivation',
]);

export default warrantyTimelineSchema;

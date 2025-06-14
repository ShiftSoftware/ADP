import yupTypeMapper from '~lib/yup-type-mapper';

const warrantySchema = yupTypeMapper([
  'dealer',
  'authorized',
  'unauthorized',
  'activeWarranty',
  'notActiveWarranty',
  'notInvoiced',
  'from',
  'to',
  'pendingSSC',
  'noPendingSSC',
  'checkingTMC',
  'sscCampings',
  'sscTableCode',
  'sscTableDescription',
  'sscTableRepairStatus',
  'sscTableOPCode',
  'sscTablePartNumber',
]);

export default warrantySchema;

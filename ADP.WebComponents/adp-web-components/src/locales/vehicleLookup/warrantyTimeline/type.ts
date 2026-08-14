import yupTypeMapper from '~lib/yup-type-mapper';

const warrantyTimelineSchema = yupTypeMapper([
  'dealer',
  'authorized',
  'unauthorized',
  'activeWarranty',
  'notActiveWarranty',
  'warrantyCoverage',
  'totalPlannedProtection',
  'standardWarranty',
  'extendedWarranty',
  'standard',
  'extended',
  'today',
  'year',
  'years',
  'month',
  'months',
]);

export default warrantyTimelineSchema;

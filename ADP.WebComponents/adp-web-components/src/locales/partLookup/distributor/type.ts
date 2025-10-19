import yupTypeMapper from '~lib/yup-type-mapper';

const distributerSchema = yupTypeMapper([
  'info',
  'distributorStock',
  'TMCLookup',
  'availability',
  'notAvailable',
  'partiallyAvailable',
  'available',
  'location',
  'description',
  'productGroup',
  'localDescription',
  'dealerPurchasePrice',
  'recommendedRetailPrice',
  'supersededFrom',
  'supersessions',
  'This field is required.',
  'orderType',
  'Part Number',
  'quantity',
  'submit',
  'RequestFailed',
]);

export default distributerSchema;

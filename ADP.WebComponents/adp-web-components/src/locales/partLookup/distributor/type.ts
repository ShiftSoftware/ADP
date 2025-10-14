import yupTypeMapper from '~lib/yup-type-mapper';

const distributerSchema = yupTypeMapper([
  'info',
  'distributorStock',
  'TMCStock',
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
]);

export default distributerSchema;

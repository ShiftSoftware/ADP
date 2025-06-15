import yupTypeMapper from '~lib/yup-type-mapper';

const distributerSchema = yupTypeMapper([
  'info',
  'distributorStock',
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
]);

export default distributerSchema;

import yupTypeMapper from '~lib/yup-type-mapper';

const accessoriesSchema = yupTypeMapper([
  'vehicleAccessories',
  'partNumber',
  'description',
  'image',
  'jobNumber',
  'invoiceNumber',
  'companyName',
  'columnsLabel',
  'noImage',
  'imageFailed',
  'openImage',
  'unauthorizedNotice',
  'noAccessories',
  'noFieldsShown',
  'accessoryCountZero',
  'accessoryCountOne',
  'accessoryCountTwo',
  'accessoryCountFew',
  'accessoryCountMany',
  'accessoryCountOther',
  'showAll',
  'showFewer',
]);

export default accessoriesSchema;

import yupTypeMapper from '~lib/yup-type-mapper';

const manufacturerSchema = yupTypeMapper([
  'origin',
  'warrantyPrice',
  'specialPrice',
  'wholesalesPrice',
  'pnc',
  'pncName',
  'binCode',
  'length',
  'width',
  'height',
  'netWeight',
  'grossWeight',
  'cubicMeasure',
  'hsCode',
  'uzHsCode',
]);

export default manufacturerSchema;

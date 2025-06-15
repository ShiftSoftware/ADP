import yupTypeMapper from '~lib/yup-type-mapper';

const manufacturerSchema = yupTypeMapper([
  'origin',
  'warrantyPrice',
  'specialPrice',
  'wholesalesPrice',
  'pnc',
  'pncName',
  'binCode',
  'dimension1',
  'dimension2',
  'dimension3',
  'netWeight',
  'grossWeight',
  'cubicMeasure',
  'hsCode',
  'uzHsCode',
]);

export default manufacturerSchema;

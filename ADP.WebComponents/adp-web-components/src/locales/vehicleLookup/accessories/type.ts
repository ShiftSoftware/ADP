import yupTypeMapper from '~lib/yup-type-mapper';

const accessoriesSchema = yupTypeMapper(['vehicleAccessories', 'noData', 'partNumber', 'description', 'image', 'expand']);

export default accessoriesSchema;

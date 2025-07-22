import yupTypeMapper from '~lib/yup-type-mapper';

const specificationSchema = yupTypeMapper(['vehicleSpecification', 'noData', 'model', 'variant', 'katashiki', 'modelYear', 'sfx', 'productionDate']);

export default specificationSchema;

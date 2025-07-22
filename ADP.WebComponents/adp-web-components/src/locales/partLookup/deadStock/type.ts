import yupTypeMapper from '~lib/yup-type-mapper';

const deadStockSchema = yupTypeMapper(['deadStock', 'branch', 'availableQuantity']);

export default deadStockSchema;

import yupTypeMapper from '~lib/yup-type-mapper';

const globalSchema = yupTypeMapper(['lang', 'direction', 'language', 'noData', 'onRecord', 'noRecords', 'notInRecords']);

export default globalSchema;

import yupTypeMapper from '~lib/yup-type-mapper';

const globalSchema = yupTypeMapper(['lang', 'direction', 'language', 'noData']);

export default globalSchema;

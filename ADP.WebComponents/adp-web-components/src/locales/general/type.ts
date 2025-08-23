import yupTypeMapper from '~lib/yup-type-mapper';

const generalSchema = yupTypeMapper(['close', 'submit', 'formSubmittedSuccessfully']);

export default generalSchema;

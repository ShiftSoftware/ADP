import yupTypeMapper from '~lib/yup-type-mapper';

const generalSchema = yupTypeMapper(['close', 'submit', 'noSelectOptions', 'formSubmittedSuccessfully']);

export default generalSchema;

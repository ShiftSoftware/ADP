import yupTypeMapper from '~lib/yup-type-mapper';

const formsSchema = yupTypeMapper(['reCaptchaIsRequired', 'inputValueIsIncorrect']);

export default formsSchema;

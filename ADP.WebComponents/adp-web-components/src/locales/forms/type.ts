import { object } from 'yup';
import yupTypeMapper from '~lib/yup-type-mapper';

const errors = yupTypeMapper(['wildCard']);

const formsSchema = object({ errors }).concat(yupTypeMapper(['reCaptchaIsRequired', 'noSelectOptions', 'inputValueIsIncorrect', 'submit']));

export default formsSchema;

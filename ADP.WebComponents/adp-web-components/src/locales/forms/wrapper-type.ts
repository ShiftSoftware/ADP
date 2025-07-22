import { object } from 'yup';
import formsSchema from './type';
import contactUsSchema from './contactUs/type';

const formWrapperSchema = object({
  contactUs: contactUsSchema,
}).concat(formsSchema);

export default formWrapperSchema;

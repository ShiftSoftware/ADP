import { object, string } from 'yup';

const popoverSchema = object({
  dialog: string().required().default(''),
  search: string().required().default(''),
});

export default popoverSchema;

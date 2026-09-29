import { object, string } from 'yup';

const inputSchema = object({
  chooseDate: string().required().default(''),
  changeDate: string().required().default(''),
  clear: string().required().default(''),
  invalid: string().required().default(''),
  tooEarly: string().required().default(''),
  tooLate: string().required().default(''),
  unavailable: string().required().default(''),
  day: string().required().default(''),
  month: string().required().default(''),
  year: string().required().default(''),
  open: string().required().default(''),
  required: string().required().default(''),
});

export default inputSchema;

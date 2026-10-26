import { object, string } from 'yup';

const imageViewerSchema = object({
  dialog: string().required().default(''),
  close: string().required().default(''),
  open: string().required().default(''),
});

export default imageViewerSchema;

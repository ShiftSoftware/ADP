import { object, string } from 'yup';

const vinExtractorSchema = object({
  dialog: string().required().default(''),
  switchCamera: string().required().default(''),
  close: string().required().default(''),
  capture: string().required().default(''),
  capturing: string().required().default(''),
  preview: string().required().default(''),
  upload: string().required().default(''),
});

export default vinExtractorSchema;

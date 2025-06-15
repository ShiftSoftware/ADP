import { AnyObjectSchema, InferType } from 'yup';
import { SharedLocales } from './get-local-language';

type ComponentLocale<T extends AnyObjectSchema> = {
  sharedLocales?: SharedLocales;
} & InferType<T>;

export default ComponentLocale;

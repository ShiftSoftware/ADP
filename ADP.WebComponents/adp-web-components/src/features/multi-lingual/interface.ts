import { AnyObjectSchema } from 'yup';

import { ComponentLocale, LanguageCode } from './types';

export interface MultiLingual {
  language: LanguageCode;
  locale: ComponentLocale<AnyObjectSchema>;

  componentWillLoad: () => Promise<void>;
  changeLanguage: (newLanguage: LanguageCode) => Promise<void>;
}

import { AnyObjectSchema } from 'yup';

import ComponentLocale from '~lib/component-locale';

import { LanguageKeys } from '~types/locale';

export default interface MultiLingual {
  language: LanguageKeys;
  locale: ComponentLocale<AnyObjectSchema>;

  componentWillLoad: () => Promise<void>;
  changeLanguage: (newLanguage: LanguageKeys) => Promise<void>;

  /** static content ( can be extended )
   
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ComponentLocale<typeof dynamicClaimSchema> = { sharedLocales: sharedLocalesSchema.getDefault(), ...dynamicClaimSchema.getDefault() };

  async componentWillLoad() {
      await this.changeLanguage(this.language);
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
      const [sharedLocales, locale] = await Promise.all([getSharedLocal(newLanguage), getLocaleLanguage(newLanguage, 'partLookup.manufacturer', dynamicClaimSchema)]);
      this.locale = { sharedLocales, ...locale };
  }

  */
}

/* TODO;
- create interfaces for each feature
- refactor claimable items mainly
- refactor other components partially
- add mock up request
- add a mock factory ( probably not ) 
- migrate part lookup to new UI
- dev ops code publishing
- do we need docs here?
*/

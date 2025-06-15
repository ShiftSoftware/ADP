import { AnyObjectSchema } from 'yup';

import ComponentLocale from '~lib/component-locale';

import { LanguageKeys } from '~types/locale';

export default interface MultiLingual {
  language: LanguageKeys;
  componentWillLoad: () => Promise<void>;
  locale: ComponentLocale<AnyObjectSchema>;
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
1- create interfaces for each feature
2- refactor claimable items mainly
3- refactor other components partially
4- add mock up request
5- add a mock factory ( probably not ) 
6- migrate part lookup to new UI
7- further improve the project setup ( husky i think doesn't work ) ( some cleanup by deleting old repo stuff )
8- dev ops code publishing
9- do we need docs here?
*/

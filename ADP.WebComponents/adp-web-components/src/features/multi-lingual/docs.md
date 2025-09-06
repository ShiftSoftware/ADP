## static content ( can be extended )

```typescript
  // #region Localization

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

  // #endregion  Localization
```

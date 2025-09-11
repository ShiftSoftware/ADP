import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import { Grecaptcha } from '~lib/recaptcha';

import demoSchema from '~locales/forms/demo/type';

import { DemoStructures } from './demo/structure';
import { demoElementNames, demoElements } from './demo/element-mapper';
import { Demo, DemoFormLocale, demoInputsValidation } from './demo/validations';

import { FormHook } from '~features/form-hook/form-hook';
import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: false,
  tag: 'demo-form',
  styleUrl: 'demo/themes.css',
})
export class DemoForm implements FormHookInterface<Demo>, MultiLingual {
  // #region Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: DemoFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...demoSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.demo', demoSchema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.form.rerender({ rerenderAll: true });
  }
  // #endregion

  // #region Form Hook logic
  @State() isLoading: boolean;
  @State() errorMessage: string;

  @Prop() theme: string;
  @Prop() gistId?: string;
  @Prop() brandId: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() structure: FormElementStructure<demoElementNames> | undefined;

  @Element() el: HTMLElement;

  setIsLoading(isLoading: boolean) {
    this.isLoading = isLoading;
    if (this.loadingChanges) this.loadingChanges(true);
  }

  setErrorCallback(error: any) {
    if (error?.message) this.errorMessage = error.message;
    if (this.errorCallback) this.errorCallback(error);
  }

  setSuccessCallback(data: any) {
    if (this.successCallback) this.successCallback(data);
  }

  async componentWillLoad() {
    await this.changeLanguage(this.language);

    if (this.structure) return;

    if (this.gistId) this.structure = await gistLoader(this.gistId);
    else if (this.theme === 'tiq') this.structure = DemoStructures.tiq;
  }

  async formSubmit(formValues: Demo) {
    try {
      console.log(formValues);

      this.setIsLoading(true);

      if (formValues) throw new Error('a');

      const token = await grecaptcha.execute(this.recaptchaKey, { action: 'submit' });

      const response = await fetch(`${this.baseUrl}?${this.queryString}`, {
        method: 'post',
        body: JSON.stringify(formValues),
        headers: {
          'Brand': this.brandId,
          'Recaptcha-Token': token,
          'Accept-Language': this.language,
          'Content-Type': 'application/json',
        },
      });

      const data = await response.json();

      this.setSuccessCallback(data);

      this.form.openDialog();
      setTimeout(() => {
        this.form.reset();
      }, 1000);
    } catch (error) {
      console.error(error);

      this.setErrorCallback(error);
    } finally {
      this.setIsLoading(false);
    }
  }
  // #endregion

  @Prop() baseUrl: string;
  @Prop() queryString: string = '';

  @Prop() recaptchaKey: string = '6Lehq6IpAAAAAETTDS2Zh60nHIT1a8oVkRtJ2WsA';

  recaptchaWidget: number | null = null;

  form = new FormHook(this, demoInputsValidation);

  async componentDidLoad() {
    try {
      if (this.recaptchaKey) {
        const script = document.createElement('script');
        script.src = `https://www.google.com/recaptcha/api.js?render=${this.recaptchaKey}&hl=${this.language}`;
        script.async = true;
        script.defer = true;

        document.head.appendChild(script);
      }
    } catch (error) {
      console.log(error);
    }
  }

  render() {
    return (
      <Host class={`demo-${this.theme}`}>
        <form-structure
          form={this.form}
          language={this.language}
          formLocale={this.locale}
          structure={this.structure}
          isLoading={this.isLoading}
          errorMessage={this.errorMessage}
          formElementMapper={demoElements}
        >
          <slot></slot>
        </form-structure>
      </Host>
    );
  }
}

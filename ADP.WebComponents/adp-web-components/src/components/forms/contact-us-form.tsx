import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import { Grecaptcha } from '~types/general';

import cn from '~lib/cn';
import { FormHook } from '~features/form-hook/form-hook';

import contactUsSchema from '~locales/forms/contactUs/type';
import { ContactUsStructures } from './contact-us/structure';
import { contactUsElementNames, contactUsElements } from './contact-us/element-mapper';
import { ContactUs, ContactUsFormLocale, contactUsInputsValidation } from './contact-us/validations';

import { FormHookInterface, FormElementStructure, gistLoader } from '~features/form-hook';
import { getLocaleLanguage, getSharedFormLocal, LanguageKeys, MultiLingual, sharedFormLocalesSchema } from '~features/multi-lingual';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: false,
  tag: 'contact-us-form',
  styleUrl: 'contact-us/form.css',
})
export class ContactUsForm implements FormHookInterface<ContactUs>, MultiLingual {
  // ====== Start Localization
  @Prop() language: LanguageKeys = 'en';

  @State() locale: ContactUsFormLocale = { sharedFormLocales: sharedFormLocalesSchema.getDefault(), ...contactUsSchema.getDefault() };

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const [sharedLocales, locale] = await Promise.all([getSharedFormLocal(newLanguage), getLocaleLanguage(newLanguage, 'forms.contactUs', contactUsSchema)]);

    this.locale = { ...sharedLocales, ...locale };

    this.form.rerender({ rerenderAll: true });
  }
  // ====== End Localization

  // ====== Start Form Hook logic
  @State() isLoading: boolean;
  @State() errorMessage: string;

  @Prop() theme: string;
  @Prop() gistId?: string;
  @Prop() brandId: string;
  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (data: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;
  @Prop() structure: FormElementStructure<contactUsElementNames> | undefined;

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
    else if (this.theme === 'tiq') this.structure = ContactUsStructures.tiq;
  }

  async formSubmit(formValues: ContactUs) {
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

      this.form.successAnimation();
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
  // ====== End Form Hook logic

  @Prop() baseUrl: string;
  @Prop() queryString: string = '';

  @Prop() recaptchaKey: string = '6Lehq6IpAAAAAETTDS2Zh60nHIT1a8oVkRtJ2WsA';

  recaptchaWidget: number | null = null;

  form = new FormHook(this, contactUsInputsValidation);

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
      <Host
        class={cn({
          [`contact-us-${this.theme}`]: this.theme,
        })}
      >
        <form-structure
          form={this.form}
          language={this.language}
          formLocale={this.locale}
          structure={this.structure}
          isLoading={this.isLoading}
          errorMessage={this.errorMessage}
          formElementMapper={contactUsElements}
        >
          <slot></slot>
        </form-structure>
      </Host>
    );
  }
}

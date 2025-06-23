import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import { Grecaptcha } from '~types/general';
import { FormHookInterface } from '~types/forms';

import cn from '~lib/cn';
import { FormHook } from '~lib/form-hook';

import { ContactUs, contactUsSchema } from './contact-us/validations';

import themes from './contact-us/themes.json';
import { contactUsElements } from './contact-us/element-mapper';

import { LanguageKeys } from '~features/multi-lingual';

declare const grecaptcha: Grecaptcha;

@Component({
  shadow: false,
  tag: 'contact-us-form',
  styleUrl: 'contact-us/form.css',
})
export class ContactUsForm implements FormHookInterface<ContactUs> {
  @Prop() theme: string;
  @Prop() baseUrl: string;
  @Prop() brandId: string;
  @Prop() queryString: string = '';
  @Prop() language: LanguageKeys = 'en';

  @Prop() structure: string = '["submit.Submit"]';
  @Prop() recaptchaKey: string = '6Lehq6IpAAAAAETTDS2Zh60nHIT1a8oVkRtJ2WsA';

  @Prop() errorCallback: (error: any) => void;
  @Prop() successCallback: (values: any) => void;
  @Prop() loadingChanges: (loading: boolean) => void;

  @State() isLoading: boolean;
  @State() renderControl = {};
  @State() errorMessage: string;

  recaptchaWidget: number | null = null;

  form = new FormHook(this, contactUsSchema);

  @Element() el: HTMLElement;

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

  async formSubmit(formValues: ContactUs) {
    try {
      if (this.loadingChanges) this.loadingChanges(true);

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

      if (this.successCallback) this.successCallback(data);

      this.form.successAnimation();
      setTimeout(() => {
        this.form.reset();
      }, 1000);
    } catch (error) {
      console.error(error);

      if (this.errorCallback) this.errorCallback(error);
      if (error?.message) this.errorMessage = error.message;
    } finally {
      if (this.loadingChanges) this.loadingChanges(false);
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
          themes={themes}
          form={this.form}
          theme={this.theme}
          language={this.language}
          isLoading={this.isLoading}
          errorMessage={this.errorMessage}
          renderControl={this.renderControl}
          formElementMapper={contactUsElements}
        >
          <slot></slot>
        </form-structure>
      </Host>
    );
  }
}

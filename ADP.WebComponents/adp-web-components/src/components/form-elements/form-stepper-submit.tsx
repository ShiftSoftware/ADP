import { Component, Prop, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormElement, FormHook, FormInputLocalization } from '~features/form-hook';

import Loader from '~assets/white-loader.svg';

const buttonSubscriberKey = 'stepper-submit-button';
@Component({
  shadow: false,
  tag: 'form-stepper-submit',
  styleUrl: 'form-inputs.css',
})
export class FormStepperSubmit implements FormElement {
  @Prop() step?: number;
  @Prop() language?: string;
  @Prop() wrapperId: string;
  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() submitTextKey?: string = 'submit';
  @Prop() localization?: FormInputLocalization = {};

  async componentWillLoad() {
    this.form.subscribe(buttonSubscriberKey, this);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(buttonSubscriberKey);
  }

  reset() {}

  render() {
    const [locale, language] = this.form.getFormLocale();

    let step = this.form.getStepLabels(language, this.step);

    const submitText =
      this.localization?.[language]?.label || step?.submitButton || getNestedValue(locale, this.submitTextKey) || getNestedValue(locale, 'sharedFormLocales.submit') || 'Submit';
    return (
      <button
        type="submit"
        formnovalidate
        disabled={this.isLoading}
        part={cn('submit-button', buttonSubscriberKey)}
        class={cn('form-submit', {
          loading: this.isLoading,
        })}
      >
        <div part="form-submit-text" class="opacity-0 form-submit-text-style">
          {submitText}
        </div>
        <div part="form-submit-text" class="form-submit-text-style form-submit-text-position">
          {submitText}
        </div>

        <div part="form-submit-loading-container" class="form-submit-loading-container">
          <img part="form-submit-loading-icon" class="form-submit-loading-icon" src={Loader} />
        </div>
      </button>
    );
  }
}

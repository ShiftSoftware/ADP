import { Component, Prop, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormElement, FormHook } from '~features/form-hook';

import Loader from '~assets/white-loader.svg';

const buttonSubscriberKey = 'submit-button';
@Component({
  shadow: false,
  tag: 'form-submit',
  styleUrl: 'form-inputs.css',
})
export class FormSubmit implements FormElement {
  @Prop() language?: string;
  @Prop() wrapperId: string;
  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() submitTextKey?: string;

  async componentWillLoad() {
    this.form.subscribe(buttonSubscriberKey, this);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(buttonSubscriberKey);
  }

  reset() {}

  render() {
    const [locale] = this.form.getFormLocale();

    const submitText = getNestedValue(locale, this.submitTextKey) || getNestedValue(locale, 'submit') || 'Submit';
    return (
      <button
        type="submit"
        formnovalidate
        part={buttonSubscriberKey}
        disabled={this.isLoading}
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

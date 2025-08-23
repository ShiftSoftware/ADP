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
        part={buttonSubscriberKey}
        disabled={this.isLoading}
        class={cn(
          'h-[38px] relative overflow-hidden px-4 enabled:hover:bg-slate-600 transition-colors duration-300 bg-slate-700 enabled:active:bg-slate-800 rounded text-white flex items-center',
          {
            'bg-slate-600': this.isLoading,
          },
        )}
      >
        <div class="opacity-0">{submitText}</div>
        <div class={cn('absolute size-full top-0 left-0 flex items-center justify-center transition !duration-1000', { 'translate-y-full': this.isLoading })}>{submitText}</div>

        <div class={cn('absolute flex justify-center items-center top-0 left-0 size-full transition !duration-1000 -translate-y-full', { 'translate-y-0': this.isLoading })}>
          <img class="spin-slow size-[22px]" src={Loader} />
        </div>
      </button>
    );
  }
}

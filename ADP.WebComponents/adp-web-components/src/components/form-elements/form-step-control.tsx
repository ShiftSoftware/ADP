import { Component, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormElement, FormHook, FormInputLocalization } from '~features/form-hook';

const buttonSubscriberKey = 'stepper-control-button';
@Component({
  shadow: false,
  tag: 'form-stepper-control',
  styleUrl: 'form-inputs.css',
})
export class FormStepperControl implements FormElement {
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
    const [_, language] = this.form.getFormLocale();

    let step = this.form.getStepLabels(language, this.step);

    const text = step?.back || '';
    return (
      <button
        type="button"
        formnovalidate
        disabled={this.isLoading}
        part={cn('stepper-control', buttonSubscriberKey)}
        onClick={() => (this.form.formStructure.currentStep -= 1)}
        class={cn('stepper-control', {
          loading: this.isLoading,
        })}
      >
        ss
        {text}
      </button>
    );
  }
}

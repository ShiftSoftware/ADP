import { Component, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormElement, FormHook, FormInputLocalization } from '~features/form-hook';
import { ChevronLeftIcon } from '~assets/chevron-left-icon';
import { ChevronRightIcon } from '~assets/chevron-right-icon';

const buttonSubscriberKey = 'stepper-control-button';
@Component({
  shadow: false,
  tag: 'form-stepper-control',
  styleUrl: 'form-inputs.css',
})
export class FormStepperControl implements FormElement {
  @Prop() language?: string;
  @Prop() wrapperId: string;
  @Prop() isLoading: boolean;
  @Prop() form: FormHook<any>;
  @Prop() wrapperClass: string;
  @Prop() submitTextKey?: string = 'submit';
  @Prop() localization?: FormInputLocalization = {};

  async componentWillLoad() {
    this.form.subscribe(buttonSubscriberKey, this);
    this.form.backButtonContexts.push(this);
  }

  async disconnectedCallback() {
    this.form.backButtonContexts = this.form.backButtonContexts.filter(back => back !== this);
    this.form.unsubscribe(buttonSubscriberKey);
  }

  reset: (newValue?: unknown) => void;

  render() {
    const [locale, language] = this.form.getFormLocale();

    const currentStep = this.form?.formStructure?.currentStep;
    const direction = locale?.sharedFormLocales?.direction;

    let step = this.form.getStepLabels(language, currentStep);

    const text = step?.back || '';

    return (
      <button
        type="button"
        formnovalidate
        disabled={this.isLoading}
        onClick={() => this.form.updateStep(-1)}
        part={cn('stepper-control', `stepper-control-${direction}`, buttonSubscriberKey, `stepper-control-step-${currentStep}`)}
        class={cn('transition-all duration-700 text-[16px] flex gap-1 items-center stepper-control', {
          'loading': this.isLoading,
          'pointer-events-none': currentStep === 10,
        })}
      >
        <div part={cn('stepper-control-icon', `stepper-control-icon-${direction}`)}>
          {direction === 'ltr' && <ChevronLeftIcon class="size-5" />}
          {direction === 'rtl' && <ChevronRightIcon class="size-5" />}
        </div>
        {text}
      </button>
    );
  }
}

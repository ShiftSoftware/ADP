import { Component, Element, Host, Prop, h } from '@stencil/core';

import cn from '~lib/cn';

import { ShiftCheckbox } from '../components/shift-checkbox';

import { FormElement, FormHook, FormInputMeta } from '~features/form-hook';
import { getNestedValue } from '~lib/get-nested-value';
import { FormErrorMessage } from './components/form-error-message';

@Component({
  shadow: false,
  tag: 'form-checkbox',
  styleUrl: 'form-inputs.css',
})
export class FormCheckbox implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() type: string = 'text';
  @Prop() defaultChecked: boolean;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
  }

  async componentDidLoad() {
    const element = this.el.getElementsByClassName('form-checkbox')[0] as unknown as ShiftCheckbox;
    this.inputRef = await element.getInputRef();
  }

  reset(newChecked?: boolean) {
    const checked = newChecked || this.defaultChecked || false;

    this.inputRef.checked = checked;
  }

  render() {
    const { disabled, meta, errorMessage, isError } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const label = getNestedValue(locale, meta?.label) || meta?.label;

    return (
      <Host>
        <div part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, disabled)}>
          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <shift-checkbox name={this.name} label={label} class="form-checkbox" />
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </div>
      </Host>
    );
  }
}

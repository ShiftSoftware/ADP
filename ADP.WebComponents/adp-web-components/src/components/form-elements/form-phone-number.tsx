import { AsYouType } from 'libphonenumber-js';
import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { FormInputPrefix } from './components/form-input-prefix';

const partKeyPrefix = 'form-input-';

@Component({
  shadow: false,
  tag: 'form-phone-number',
  styleUrl: 'form-inputs.css',
})
export class FormPhoneNumber implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() defaultValue: string;
  @Prop() type: string = 'text';
  @Prop() validator: AsYouType;

  @State() prefixWidth: number = 0;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
  }

  async componentDidLoad() {
    const prefix = this.el.getElementsByClassName('prefix')[0] as HTMLElement;
    this.prefixWidth = prefix ? prefix.getBoundingClientRect().width : 0;

    this.inputRef = this.el.getElementsByClassName(partKeyPrefix + this.name)[0] as HTMLInputElement;
  }

  async disconnectedCallback() {
    this.form.unsubscribe(this.name);
  }

  reset(newValue?: string) {
    const value = newValue || this.defaultValue || '';

    this.inputRef.value = value;
    this.validator.reset();
  }

  onInputChange = (event: InputEvent) => {
    const target = event.target as HTMLInputElement;

    this.validator.reset();

    target.value = this.validator.input(target.value as string);
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    return (
      <Host>
        <label part={part} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, disabled)}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <div dir="ltr" part="form-input-container" class="form-input-container">
            <FormInputPrefix direction={locale.direction} prefix={this.inputPrefix} />

            <input
              type={this.type}
              name={this.name}
              part="form-input"
              disabled={disabled}
              onInput={this.onInputChange}
              defaultValue={this.defaultValue}
              placeholder={placeholder || meta?.placeholder}
              style={{ ...(this.prefixWidth ? { [locale.direction === 'rtl' ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}) }}
              class={cn('form-input-style', part, {
                'form-input-error-style': isError,
              })}
            />
          </div>
          <FormErrorMessage isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}

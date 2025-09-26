import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormInputPrefix } from './components/form-input-prefix';
import { FormErrorMessage } from './components/form-error-message';

const partKeyPrefix = 'form-input-';
@Component({
  shadow: false,
  tag: 'form-input',
  styleUrl: 'form-inputs.css',
})
export class FormInput implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() defaultValue: string;
  @Prop() wrapperClass: string;
  @Prop() type: string = 'text';

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
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: this.isLoading })}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <FormInputPrefix name={this.name} direction={locale.sharedFormLocales.direction} prefix={this.inputPrefix} />
            <input
              type={this.type}
              name={this.name}
              defaultValue={this.defaultValue}
              disabled={disabled || this.isLoading}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              style={{ ...(this.prefixWidth ? { [locale.sharedFormLocales.direction === 'rtl' ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}) }}
              class={cn('form-input-style', part, {
                'form-input-error-style': isError,
              })}
            />
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
      </Host>
    );
  }
}

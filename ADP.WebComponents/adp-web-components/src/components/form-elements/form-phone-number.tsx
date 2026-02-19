import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormInputMeta, PhoneValidator, FormInputLocalization, getInputLocalization } from '~features/form-hook';
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
  @Prop() isLoading?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() defaultValue: string;
  @Prop() type: string = 'text';
  @Prop() isDisabled?: boolean;
  @Prop() staticValue?: string;
  @Prop() validator: PhoneValidator;
  @Prop() localization?: FormInputLocalization = {};

  @State() prefixWidth: number = 0;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.onDefaultValueChange();
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
  }

  @Watch('defaultValue')
  async onDefaultValueChange() {
    if (this.defaultValue) {
      this.validator.reset();
      this.defaultValue = this.validator.input(this.defaultValue || '');
    }
  }

  @Watch('staticValue')
  async onStaticValueChange(newStaticValue?: string, notInitialLoad = true) {
    if (!!newStaticValue) {
      this.defaultValue = newStaticValue;
      this.inputRef.value = newStaticValue;
    } else if (notInitialLoad) {
      this.defaultValue = '';
      this.inputRef.value = '';
    }
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

    if (value) this.validator.input(value);
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

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel name={this.name} isRequired={isRequired} label={label} />

          <div dir="ltr" part={`${this.name}-container form-input-container`} class="form-input-container">
            <FormInputPrefix name={this.name} direction={locale.sharedFormLocales.direction} prefix={this.inputPrefix} />

            <input
              type={this.type}
              name={this.name}
              disabled={isDisabled}
              onInput={this.onInputChange}
              defaultValue={this.defaultValue}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              style={{ ...(this.prefixWidth ? { [locale.sharedFormLocales.direction === 'rtl' ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}) }}
              class={cn('form-input-style', part, {
                'form-input-error-style': isError,
              })}
            />
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={errorTextMessage} />
        </label>
      </Host>
    );
  }
}

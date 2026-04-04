import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormInputMeta, PhoneValidator, FormInputLocalization, getInputLocalization } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { FormInputPrefix } from './components/form-input-prefix';
import { AnyObjectSchema, string } from 'yup';
import { AsYouType, CountryCode } from 'libphonenumber-js';
import { y } from '../forms/defaults/validation';

const partKeyPrefix = 'form-input-';

@Component({
  shadow: false,
  tag: 'form-phone-number',
  styleUrl: 'form-inputs.css',
})
export class FormPhoneNumber implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() required?: boolean;
  @Prop() isLoading?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() defaultValue: string;
  @Prop() type: string = 'text';
  @Prop() isDisabled?: boolean;
  @Prop() staticValue?: string;
  @State() prefixWidth: number = 0;
  @Prop() localization?: FormInputLocalization = {};
  @Prop() countryCode?: string | { code: string; icon?: string }[] = 'IQ';

  @Element() el: HTMLElement;

  @State() validator: PhoneValidator;
  @State() selectedCountryCode: CountryCode;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.onDefaultValueChange();
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
    if (typeof this.countryCode === 'string') {
      this.selectedCountryCode = this.countryCode as CountryCode;
    }
  }

  @Watch('selectedCountryCode')
  async onCountryChange() {
    this.validator = new AsYouType(this.selectedCountryCode) as PhoneValidator;

    this.inputPrefix = this.validator ? '+' + this.validator?.metadata?.numberingPlan?.metadata[0] : undefined;
  }

  @Watch('defaultValue')
  async onDefaultValueChange() {
    if (this.defaultValue && this.validator) {
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

  @Watch('inputPrefix')
  async onValidatorChange() {
    await this.componentDidLoad();
  }

  async componentDidLoad() {
    const prefix = this.el.getElementsByClassName(this.name + '-prefix')[0] as HTMLElement;
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
    target.value = this.validator.input(this.getValue()).replace(this.inputPrefix, '').trim();

    this.form.onInput(event);
  };

  validate = () => {
    // @ts-ignore
    let v: AnyObjectSchema = string().meta(y.meta(this.name));

    v = v.test(y.require(this.name), y.require(this.name), value => {
      if (!this.required) return true;
      return !!value.replace(this.inputPrefix, '').trim();
    });

    v = v.test(y.format(this.name), y.format(this.name), value => {
      if (!value.replace(this.inputPrefix, '').trim()) return true; // let required handle empty cases

      const checker = new AsYouType(this.selectedCountryCode);
      checker.input(this.getValue());
      return checker.isValid();
    });

    return v;
  };

  getValue = () => {
    const raw = this.inputRef.value.trim().replace(/^\+/, '');
    const prefix = this.inputPrefix?.trim().replace(/^\+/, '');

    const stripped = raw.startsWith(prefix) ? raw.slice(prefix.length).trim() : raw;

    const hasPrefix = raw.startsWith(prefix);

    return hasPrefix ? '+' + prefix + ' ' + stripped : this.inputPrefix + ' ' + raw;
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled || !this.selectedCountryCode;

    this.required = isRequired;

    return (
      <Host translate="no">
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

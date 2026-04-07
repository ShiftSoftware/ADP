import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';

import { FormInputMeta, PhoneValidator, FormInputLocalization, getInputLocalization, FormSelectItem } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormErrorMessage } from './components/form-error-message';
import { FormInputPrefix } from './components/form-input-prefix';
import { AnyObjectSchema, string } from 'yup';
import { AsYouType, CountryCode } from 'libphonenumber-js';
import { getCountryForTimezone } from 'countries-and-timezones';
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
  @Prop() countryCode?: string | string[] | { code: string; icon?: string }[] = 'IQ';

  @Element() el: HTMLElement;

  @State() selectedValue: string;
  @State() isOpen: boolean = false;
  @State() validator: PhoneValidator;
  @State() externalRequired: boolean = false;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.onDefaultValueChange();
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);

    if (typeof this.countryCode === 'string') {
      this.selectedValue = this.countryCode as CountryCode;
    } else if (Array.isArray(this.countryCode)) {
      const codes = this.countryCode.map(c => (typeof c === 'string' ? c : c.code).toUpperCase());
      const detectedCountry = this.detectCountryFromBrowser(codes);
      const match = detectedCountry;

      this.selectedValue = (match || codes[0]) as CountryCode;
    }
  }

  private detectCountryFromBrowser(availableCodes: string[]): string | null {
    // 1. Try timezone → country mapping
    try {
      const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
      const country = getCountryForTimezone(tz);
      if (country && availableCodes.includes(country.id)) return country.id;
    } catch {}

    // 2. Fallback to browser locale
    const languages = navigator.languages?.length ? navigator.languages : [navigator.language];
    for (const lang of languages) {
      const parts = lang?.split('-');
      if (parts?.length >= 2) {
        const country = parts[parts.length - 1].toUpperCase();
        if (country.length === 2 && availableCodes.includes(country)) return country;
      }
    }

    return null;
  }

  @Watch('selectedValue')
  async onCountryChange() {
    this.validator = new AsYouType(this.selectedValue as CountryCode) as PhoneValidator;

    this.inputPrefix = this.validator ? '+' + this.validator?.metadata?.numberingPlan?.metadata[0] : undefined;
    this.form.validateInput(this.name);
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
      if (!this.externalRequired && !this.required) return true;
      return !!value.replace(this.inputPrefix, '').trim();
    });

    v = v.test(y.format(this.name), y.format(this.name), value => {
      if (!value.replace(this.inputPrefix, '').trim()) return true; // let required handle empty cases

      const checker = new AsYouType(this.selectedValue as CountryCode) as PhoneValidator;
      checker.input(this.getValue());
      return checker.isValid();
    });

    return v;
  };

  getValue = () => {
    const raw = this?.inputRef?.value.trim().replace(/^\+/, '') || '';
    const prefix = this.inputPrefix?.trim().replace(/^\+/, '') || '';

    const stripped = raw.startsWith(prefix) ? raw.slice(prefix.length).trim() : raw;

    const hasPrefix = raw.startsWith(prefix);

    return hasPrefix ? '+' + prefix + ' ' + stripped : this.inputPrefix + ' ' + raw;
  };

  updateShiftSelectContext = (newValues: Record<string, any>) => {
    Object.entries(newValues).forEach(([key, value]) => {
      // @ts-ignore
      this[key] = value;
    });
  };
  handleSelection = (option: FormSelectItem) => {
    this.selectedValue = option.value;
    this.isOpen = false;
  };

  renderCountryOption = (renderOption: FormSelectItem) => {
    const v = new AsYouType(renderOption.meta?.code as CountryCode) as PhoneValidator;

    const countryNumber = v.metadata?.numberingPlan?.metadata[0] || '';

    const divIdentifier = cn('custom-shift-select-option custom-shift-select-country-option', {
      'custom-shift-select-option-selected shift-select-option-selected': this.selectedValue === renderOption.value,
    });

    const labelIdentifier = 'shift-select-country-code-label';

    const numberIdentifier = 'shift-select-country-number';
    return (
      <div part={divIdentifier} class={divIdentifier}>
        <span part={labelIdentifier} class={labelIdentifier}>
          {renderOption?.label}
        </span>
        {!!countryNumber && (
          <span part={numberIdentifier} class={numberIdentifier}>
            {'+' + countryNumber}
          </span>
        )}
      </div>
    );
  };

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);

    const part = partKeyPrefix + this.name;

    const { label, placeholder, errorTextMessage } = getInputLocalization(this, meta, errorMessage);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled || !this.selectedValue;

    this.externalRequired = isRequired;

    const hasMultipleCountries = Array.isArray(this.countryCode) && this.countryCode.length > 1;

    const options: FormSelectItem[] =
      Array.isArray(this.countryCode) &&
      this.countryCode.map(country => {
        if (typeof country === 'string') {
          return { value: country, label: country, meta: { code: country } };
        } else {
          return { value: country.code, label: country?.code, meta: { code: country.code } };
        }
      });

    return (
      <Host translate="no">
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel name={this.name} isRequired={isRequired} label={label} />

          <div
            dir="ltr"
            part={cn(`${this.name}-container form-input-container form-phone-container`, { 'form-input-container-country': hasMultipleCountries })}
            class={cn('form-input-container form-phone-container', { 'form-input-container-country': hasMultipleCountries })}
          >
            {hasMultipleCountries && (
              <div part={`${this.name}-input-country-selection form-input form-input-country-selection`} class={cn('form-input-country-selection shrink-0')}>
                <shift-select
                  hasTick={false}
                  options={options}
                  isOpen={this.isOpen}
                  disableInput={isDisabled}
                  selectedValue={this.selectedValue}
                  handleSelection={this.handleSelection}
                  renderOption={this.renderCountryOption}
                  name={this.name + '-country-selection'}
                  updateContext={this.updateShiftSelectContext}
                />
              </div>
            )}
            <div part="form-input-container-wrapper" class={cn('form-input-container-wrapper', { 'flex-1 relative': hasMultipleCountries })}>
              <FormInputPrefix name={this.name} direction="ltr" prefix={this.inputPrefix} />

              <input
                type={this.type}
                name={this.name}
                disabled={isDisabled}
                onInput={this.onInputChange}
                defaultValue={this.defaultValue}
                part={cn(`${this.name}-input form-input`, { [`${this.name}-input-with-country form-input-with-country`]: hasMultipleCountries })}
                placeholder={placeholder || meta?.placeholder}
                style={{ ...(this.prefixWidth ? { ['paddingLeft']: `${this.prefixWidth}px` } : {}) }}
                class={cn('form-input-style', part, {
                  'form-input-error-style': isError,
                })}
              />
            </div>
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={errorTextMessage} />
        </label>
      </Host>
    );
  }
}

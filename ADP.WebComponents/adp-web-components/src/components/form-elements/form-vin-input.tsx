import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

import { FormInputLabel } from './components/form-input-label';
import { FormInputPrefix } from './components/form-input-prefix';
import { FormErrorMessage } from './components/form-error-message';
import { QrCodeScanIcon } from '~assets/qr-code-scan';

const partKeyPrefix = 'form-vin-input-';
@Component({
  shadow: false,
  tag: 'form-vin-input',
  styleUrl: 'form-inputs.css',
})
export class FormVinInput implements FormElement {
  @Prop() name: string;
  @Prop() wrapperId: string;
  @Prop() isLoading?: boolean;
  @Prop() isDisabled?: boolean;
  @Prop() form: FormHook<any>;
  @Prop() inputPrefix: string;
  @Prop() wrapperClass: string;
  @Prop() staticValue?: string;
  @Prop() inputProps?: any = {};
  @Prop({ mutable: true }) defaultValue: string;

  @Prop() useOcr?: boolean;
  @Prop() readSticker?: boolean;
  @Prop() scannerIcon?: '' | 'qr-code' = '';

  @State() prefixWidth: number = 0;

  @Element() el: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.name, this);
    this.onStaticValueChange(this.staticValue, false);
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
  }

  render() {
    const { disabled, isRequired, meta, isError, errorMessage } = this.form.getInputState<FormInputMeta>(this.name);
    const [locale] = this.form.getFormLocale();

    const part = partKeyPrefix + this.name;

    const label = getNestedValue(locale, meta?.label) || meta?.label;
    const placeholder = getNestedValue(locale, meta?.placeholder);

    const isDisabled = disabled || this.isLoading || !!this.staticValue || this.isDisabled;

    const extractorTitle = this.form?.context?.structure?.data?.localization?.[this.form.context?.language]?.['vin-scan'] || 'Scan Your VIN';

    return (
      <Host>
        <label part={this.name} id={this.wrapperId} class={cn('form-input-label-container', this.wrapperClass, { disabled: isDisabled })}>
          <FormInputLabel isRequired={isRequired} label={label} />

          <div part={`${this.name}-container form-input-container`} class="form-input-container">
            <FormInputPrefix name={this.name} direction={locale?.sharedFormLocales?.direction} prefix={this.inputPrefix} />
            <input
              {...this.inputProps}
              name={this.name}
              disabled={isDisabled}
              defaultValue={this.defaultValue}
              part={`${this.name}-input form-input`}
              placeholder={placeholder || meta?.placeholder}
              style={{ ...(this.prefixWidth ? { [locale?.sharedFormLocales?.direction === 'rtl' ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}) }}
              class={cn('form-input-style !uppercase', part, {
                'form-input-error-style': isError,
              })}
            />
            {!!this.scannerIcon && (this.useOcr || this.readSticker) && (
              <button
                type="button"
                disabled={isDisabled}
                part={cn('vin-validator', part + '-vin')}
                onClick={() => this?.el.getElementsByTagName('vin-extractor')[0]?.open()}
                class="absolute transition-all duration-500 disabled:!pointer-events-none disabled:!opacity-0 flex justify-center items-center right-3.5 !text-[#576675] enabled:hover:!text-black/85 aspect-square h-[70%] top-1/2 -translate-y-1/2"
              >
                {this.scannerIcon === 'qr-code' && <QrCodeScanIcon class="size-[90%] text-inherit" />}
              </button>
            )}
          </div>
          <FormErrorMessage name={this.name} isError={isError} errorMessage={locale[errorMessage] || errorMessage} />
        </label>
        {(this.useOcr || this.readSticker) && (
          <div class="absolute">
            <vin-extractor
              verbose
              manualCapture
              useOcr={this.useOcr}
              title={extractorTitle}
              skipValidation={false}
              readSticker={this.readSticker}
              onExtract={newVin => {
                this.inputRef.value = newVin;
                this.form?.validateInput(this?.name);
                this?.el.getElementsByTagName('vin-extractor')[0]?.close();
              }}
              ocrEndpoint={this.form?.context?.structure?.data?.ocrEndpoint}
            />
          </div>
        )}
      </Host>
    );
  }
}

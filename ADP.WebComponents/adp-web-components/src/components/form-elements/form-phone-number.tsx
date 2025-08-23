import { AsYouType } from 'libphonenumber-js';
import { Component, Element, Host, Prop, State, h } from '@stencil/core';

import cn from '~lib/cn';
import { getNestedValue } from '~lib/get-nested-value';

import { FormInputMeta } from '~features/form-hook';
import { FormHook } from '~features/form-hook/form-hook';
import { FormElement } from '~features/form-hook/interface';

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
  @Prop() inputPreFix: string;
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

    const label = getNestedValue(locale, meta?.label);
    const placeholder = getNestedValue(locale, meta?.placeholder);

    return (
      <Host>
        <label part={part} id={this.wrapperId} class={cn('relative w-full inline-flex flex-col', this.wrapperClass)}>
          {label && (
            <div class="mb-[4px]">
              {label}
              {isRequired && <span class="ms-0.5 text-red-600">*</span>}
            </div>
          )}

          <div dir="ltr" class={cn('relative', { 'opacity-75': disabled })}>
            {this.inputPreFix && (
              <div
                class={cn('prefix absolute h-[38px] px-2 left-0 top-0 pointer-events-none items-center justify-center flex', { 'left-auto right-0': locale.direction === 'rtl' })}
              >
                {this.inputPreFix}
              </div>
            )}
            <input
              type={this.type}
              name={this.name}
              disabled={disabled}
              onInput={this.onInputChange}
              defaultValue={this.defaultValue}
              placeholder={placeholder || meta?.placeholder}
              style={{ ...(this.prefixWidth ? { [locale.direction === 'rtl' ? 'paddingRight' : 'paddingLeft']: `${this.prefixWidth}px` } : {}) }}
              class={cn(
                'border appearance-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none form-input disabled:bg-white flex-1 py-[6px] px-[12px] transition duration-300 rounded-md outline-none focus:border-slate-600 focus:shadow-[0_0_0_0.2rem_rgba(71,85,105,0.25)] w-full',
                part,
                {
                  '!border-red-500 focus:shadow-[0_0_0_0.2rem_rgba(239,68,68,0.25)]': isError,
                  'rtl-form-input': locale.direction === 'rtl',
                },
              )}
            />
          </div>
          <form-error-message isError={isError} errorMessage={locale[errorMessage] || locale?.inputValueIsIncorrect || errorMessage} />
        </label>
      </Host>
    );
  }
}

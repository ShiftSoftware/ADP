import { InferType, ObjectSchema } from 'yup';
import { Component, Element, Host, Prop, State, Watch, h } from '@stencil/core';

import cn from '~lib/cn';
import { FormHook } from '~lib/form-hook';
import { getLocaleLanguage, LocaleKeyEntries } from '~lib/get-local-language';

import { InputParams } from '~types/general';
import { LanguageKeys } from '~types/locale';
import { FormElement, LocaleFormKeys } from '~types/forms';

import formsSchema from '~locales/forms/type';
import formWrapperSchema from '~locales/forms/wrapper-type';

@Component({
  shadow: false,
  tag: 'form-text-area',
  styleUrl: 'form-text-area.css',
})
export class FormTextArea implements FormElement {
  @Prop() label: string;
  @Prop() isError: boolean;
  @Prop() wrapperId: string;
  @Prop() form: FormHook<any>;
  @Prop() isRequired: boolean;
  @Prop() wrapperClass: string;
  @Prop() errorMessage: string;
  @Prop() inputParams: InputParams;
  @Prop() language: LanguageKeys = 'en';
  @Prop() formLocaleName: LocaleFormKeys;

  @State() locale: InferType<typeof formWrapperSchema> = formWrapperSchema.getDefault();

  @Element() el!: HTMLElement;

  private inputRef: HTMLInputElement;

  async componentWillLoad() {
    this.form.subscribe(this.inputParams.name, this);
    await this.changeLanguage(this.language);
  }

  async componentDidLoad() {
    this.inputRef = this.el.getElementsByClassName('form-textarea-' + this.inputParams.name)[0] as HTMLInputElement;
  }

  @Watch('language')
  async changeLanguage(newLanguage: LanguageKeys) {
    const currentFormSchema = formWrapperSchema.fields[this.formLocaleName] as ObjectSchema<InferType<any>>;

    const localeResponses = await Promise.all([
      getLocaleLanguage(newLanguage, 'forms*', formsSchema),
      getLocaleLanguage(newLanguage, ('forms.' + this.formLocaleName) as LocaleKeyEntries, currentFormSchema),
    ]);
    Object.assign(this.locale, localeResponses[0]);
    Object.assign(this.locale[this.formLocaleName], localeResponses[1]);
  }

  reset(newValue?: string) {
    const value = newValue || this.inputParams.defaultValue || '';

    this.inputRef.value = value;
  }

  render() {
    const { label, isError, errorMessage, isRequired } = this;

    const prefix = this.el.getElementsByClassName('prefix')[0];

    const prefixWidth = prefix ? prefix.getBoundingClientRect().width : 0;

    const texts = this.locale[this.formLocaleName];

    return (
      <Host>
        <label id={this.wrapperId} class={cn('relative w-full inline-flex flex-col', this.wrapperClass)}>
          {label && (
            <div class="mb-[4px]">
              {texts[label] || label}
              {isRequired && <span class="ms-0.5 text-red-600">*</span>}
            </div>
          )}
          <div class={cn('relative', { 'opacity-75': this.inputParams.disabled })}>
            <textarea
              {...this.inputParams}
              style={{ ...(prefixWidth ? { paddingLeft: `${prefixWidth}px` } : {}) }}
              placeholder={texts[this.inputParams.placeholder] || this.inputParams.placeholder}
              class={cn(
                'border h-[200px] form-input resize-none disabled:bg-white flex-1 py-[6px] px-[12px] transition duration-300 rounded-md outline-none focus:border-slate-600 focus:shadow-[0_0_0_0.2rem_rgba(71,85,105,0.25)] w-full',
                'form-textarea-' + this.inputParams.name,
                { '!border-red-500 focus:shadow-[0_0_0_0.2rem_rgba(239,68,68,0.25)]': isError },
              )}
            />
          </div>

          <div
            class={cn('absolute text-[12px] -mt-[4px] -z-10 text-red-500 opacity-0 -translate-y-[10px] bottom-0 transition duration-300', {
              'translate-y-[calc(100%-5px)] error-message opacity-100': isError,
            })}
          >
            {texts[errorMessage] || this.locale.inputValueIsIncorrect || errorMessage}
          </div>
        </label>
      </Host>
    );
  }
}
